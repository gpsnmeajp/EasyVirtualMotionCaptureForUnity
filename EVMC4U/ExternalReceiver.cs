/*
 * ExternalReceiver
 * https://sabowl.sakura.ne.jp/gpsnmeajp/
 *
 * MIT License
 * 
 * Copyright (c) 2019 gpsnmeajp
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
#pragma warning disable 0414,0219

using System;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using VRM;

namespace EVMC4U
{
    //[RequireComponent(typeof(uOSC.uOscServer))]
    public class ExternalReceiver : MonoBehaviour, IExternalReceiver
    {
        [Header("ExternalReceiver v3.7")]
        public GameObject Model = null;
        public bool Freeze = false; //すべての同期を止める(撮影向け)
        public bool PacktLimiter = true; //パケットフレーム数が一定値を超えるとき、パケットを捨てる

        [Header("Root Synchronize Option")]
        public Transform RootPositionTransform = null; //VR向けroot位置同期オブジェクト指定
        public Transform RootRotationTransform = null; //VR向けroot回転同期オブジェクト指定
        public bool RootPositionSynchronize = true; //ルート座標同期(ルームスケール移動)
        public bool RootRotationSynchronize = true; //ルート回転同期
        public bool RootScaleOffsetSynchronize = false; //MRスケール適用

        [Header("Other Synchronize Option")]
        public bool BlendShapeSynchronize = true; //表情等同期
        public bool BonePositionSynchronize = true; //ボーン位置適用(回転は強制)

        [Header("Synchronize Cutoff Option")]
        public bool HandPoseSynchronizeCutoff = false; //指状態反映オフ
        public bool EyeBoneSynchronizeCutoff = false; //目ボーン反映オフ

        [Header("Lowpass Filter Option")]
        public bool BonePositionFilterEnable = false; //ボーン位置フィルタ
        public bool BoneRotationFilterEnable = false; //ボーン回転フィルタ
        public float BoneFilter = 0.7f; //ボーンフィルタ係数
        public bool BlendShapeFilterEnable = false; //BlendShapeフィルタ
        public float BlendShapeFilter = 0.7f; //BlendShapeフィルタ係数

        [Header("VRM Loader Option")]
        public bool enableAutoLoadVRM = true;        //VRMの自動読み込みの有効可否

        [Header("Other Option")]
        public bool HideInUncalibrated = false; //キャリブレーション出来ていないときは隠す
        public bool SyncCalibrationModeWithScaleOffsetSynchronize = true; //キャリブレーションモードとスケール設定を連動させる

        [Header("Status (Read only)")]
        [SerializeField]
        private string StatusMessage = ""; //状態メッセージ(Inspector表示用)
        public string OptionString = ""; //VMCから送信されるオプション文字列

        public string loadedVRMPath = "";        //読み込み済みVRMパス
        public string loadedVRMName = "";        //読み込み済みVRM名前
        public GameObject LoadedModelParent = null; //読み込んだモデルの親

        public int LastPacketframeCounterInFrame = 0; //1フレーム中に受信したパケットフレーム数
        public int DropPackets = 0; //廃棄されたパケット(not パケットフレーム)

        public Vector3 HeadPosition = Vector3.zero;

        [Header("Daisy Chain")]
        public GameObject[] NextReceivers = new GameObject[6]; //デイジーチェーン

        
        //---Const---

        //rootパケット長定数(拡張判別)
        const int RootPacketLengthOfScaleAndOffset = 8;

        //---Private---

        private ExternalReceiverManager externalReceiverManager = null;

        //フィルタ用データ保持変数
        private Vector3[] bonePosFilter = new Vector3[Enum.GetNames(typeof(HumanBodyBones)).Length];
        private Quaternion[] boneRotFilter = new Quaternion[Enum.GetNames(typeof(HumanBodyBones)).Length];
        private Dictionary<string, float> blendShapeFilterDictionaly = new Dictionary<string, float>();

        //通信状態保持変数
        private int Available = 0; //データ送信可能な状態か
        private float time = 0; //送信時の時刻

        //モデル切替検出用reference保持変数
        private GameObject OldModel = null;

        //ボーン情報取得
        Animator animator = null;
        //VRMのブレンドシェーププロキシ
        VRMBlendShapeProxy blendShapeProxy = null;

        //ボーンENUM情報テーブル
        Dictionary<string, HumanBodyBones> HumanBodyBonesTable = new Dictionary<string, HumanBodyBones>();

        //ボーン情報テーブル
        Dictionary<HumanBodyBones, Vector3> HumanBodyBonesPositionTable = new Dictionary<HumanBodyBones, Vector3>();
        Dictionary<HumanBodyBones, Quaternion> HumanBodyBonesRotationTable = new Dictionary<HumanBodyBones, Quaternion>();

        //ブレンドシェープ変換テーブル
        Dictionary<string, BlendShapeKey> StringToBlendShapeKeyDictionary = new Dictionary<string, BlendShapeKey>();
        Dictionary<BlendShapeKey, float> BlendShapeToValueDictionary = new Dictionary<BlendShapeKey, float>();


        //uOSCサーバー
        uOSC.uOscServer server = null;

        //エラー・無限ループ検出フラグ(trueで一切の受信を停止する)
        bool shutdown = false;

        //フレーム間パケットフレーム数測定
        int PacketCounterInFrame = 0;

        //1フレームに30パケットフレーム来たら、同一フレーム内でそれ以上は受け取らない。
        const int PACKET_LIMIT_MAX = 30;

        //読込中は読み込まない
        bool isLoading = false;

        //メッセージ処理一時変数struct(負荷対策)
        Vector3 pos;
        Quaternion rot;
        Vector3 scale;
        Vector3 offset;

        public void Start()
        {
            //nullチェック
            if (NextReceivers == null)
            {
                NextReceivers = new GameObject[0];
            }
            //NextReciverのインターフェースを取得する
            externalReceiverManager = new ExternalReceiverManager(NextReceivers);

            //サーバーを取得
            server = GetComponent<uOSC.uOscServer>();
            if (server)
            {
                //サーバーを初期化
                StatusMessage = "Waiting for VMC...";
                server.onDataReceived.AddListener(OnDataReceived);
            }
            else
            {
                //デイジーチェーンスレーブモード
                StatusMessage = "Waiting for Master...";
            }

            //初期状態で読み込み済みのモデルが有る場合はVRMの自動読み込みは禁止する
            if (Model != null) {
                enableAutoLoadVRM = false;
            }
        }

        //デイジーチェーンを更新
        public void UpdateDaisyChain()
        {
            //nullチェック
            if (NextReceivers == null)
            {
                NextReceivers = new GameObject[0];
            }
            externalReceiverManager.GetIExternalReceiver(NextReceivers);
        }

        //外部から通信状態を取得するための公開関数
        public int GetAvailable()
        {
            return Available;
        }

        //外部から通信時刻を取得するための公開関数
        public float GetRemoteTime()
        {
            return time;
        }

        public void Update()
        {
            //エラー・無限ループ時は処理をしない
            if (shutdown) { return; }

            //Freeze有効時は動きを一切止める
            if (Freeze) { return; }

            LastPacketframeCounterInFrame = PacketCounterInFrame;
            PacketCounterInFrame = 0;

            //5.6.3p1などRunInBackgroundが既定で無効な場合Unityが極めて重くなるため対処
            Application.runInBackground = true;

            //VRMモデルからBlendShapeProxyを取得(タイミングの問題)
            if (blendShapeProxy == null && Model != null)
            {
                blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
            }

            //ルート位置がない場合
            if (RootPositionTransform == null && Model != null)
            {
                //モデル姿勢をルート姿勢にする
                RootPositionTransform = Model.transform;
            }

            //ルート回転がない場合
            if (RootRotationTransform == null && Model != null)
            {
                //モデル姿勢をルート姿勢にする
                RootRotationTransform = Model.transform;
            }

            //モデルがない場合はエラー表示をしておく(親切心)
            if (Model == null)
            {
                StatusMessage = "Model not found.";
                return;
            }

            //モデルが更新されたときに関連情報を更新する
            if (OldModel != Model && Model != null)
            {
                animator = Model.GetComponent<Animator>();
                blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
                OldModel = Model;

                Debug.Log("[ExternalReceiver] New model detected");

                //v0.56 BlendShape仕様変更対応
                //Debug.Log("-- Make BlendShapeProxy BSKey Table --");

                //BSキー値辞書の初期化(SetValueで無駄なキーが適用されるのを防止する)
                BlendShapeToValueDictionary.Clear();

                //文字-BSキー辞書の初期化(キー情報の初期化)
                StringToBlendShapeKeyDictionary.Clear();

                //全Clipsを取り出す
                foreach (var c in blendShapeProxy.BlendShapeAvatar.Clips) {
                    string key = "";
                    bool unknown = false;
                    //プリセットかどうかを調べる
                    if (c.Preset == BlendShapePreset.Unknown) {
                        //非プリセット(Unknown)であれば、Unknown用の名前変数を参照する
                        key = c.BlendShapeName;
                        unknown = true;
                    }
                    else {
                        //プリセットであればENUM値をToStringした値を利用する
                        key = c.Preset.ToString();
                        unknown = false;
                    }

                    //非ケース化するために小文字変換する
                    string lowerKey = key.ToLower();
                    //Debug.Log("Add: [key]->" + key + " [lowerKey]->" + lowerKey + " [clip]->" + c.ToString() + " [bskey]->"+c.Key.ToString() + " [unknown]->"+ unknown);

                    //小文字名-BSKeyで登録する                    
                    StringToBlendShapeKeyDictionary.Add(lowerKey, c.Key);
                }

                //メモ: プリセット同名の独自キー、独自キーのケース違いの重複は、共に区別しないと割り切る
                /*
                Debug.Log("-- Registered List --");
                foreach (var k in StringToBlendShapeKeyDictionary) {
                    Debug.Log("[k.Key]" + k.Key + " -> [k.Value.Name]" + k.Value.Name);
                }
                */

                //Debug.Log("-- End BlendShapeProxy BSKey Table --");
            }

            BoneSynchronizeByTable();

        }

        //データ受信イベント
        private void OnDataReceived(uOSC.Message message)
        {
            //チェーン数0としてデイジーチェーンを発生させる
            MessageDaisyChain(ref message, 0);
        }

        //デイジーチェーン処理
        public void MessageDaisyChain(ref uOSC.Message message, int callCount)
        {
            //Startされていない場合無視
            if (externalReceiverManager == null || enabled == false || gameObject.activeInHierarchy == false)
            {
                return;
            }

            //エラー・無限ループ時は処理をしない
            if (shutdown) {
                return;
            }

            //パケットリミッターが有効な場合、一定以上のパケットフレーム/フレーム数を観測した場合、次のフレームまでパケットを捨てる
            if (PacktLimiter && (LastPacketframeCounterInFrame > PACKET_LIMIT_MAX)) {
                DropPackets++;
                return;
            }

            //メッセージを処理
            if (!Freeze) {
                //異常を検出して動作停止
                try
                {
                    ProcessMessage(ref message);
                }
                catch (Exception e) {
                    StatusMessage = "Error: Exception";
                    Debug.LogError(" --- Communication Error ---");
                    Debug.LogError(e.ToString());
                    shutdown = true;
                    return;
                }
            }

            //次のデイジーチェーンへ伝える
            if (!externalReceiverManager.SendNextReceivers(message, callCount))
            {
                //無限ループ対策
                StatusMessage = "Infinite loop detected!";

                //以降の処理を全部停止
                shutdown = true;
            }
        }

        //メッセージ処理本体
        private void ProcessMessage(ref uOSC.Message message)
        {
            //メッセージアドレスがない、あるいはメッセージがない不正な形式の場合は処理しない
            if (message.address == null || message.values == null)
            {
                StatusMessage = "Bad message.";
                return;
            }

            //ルート位置がない場合
            if (RootPositionTransform == null && Model != null)
            {
                //モデル姿勢をルート姿勢にする
                RootPositionTransform = Model.transform;
            }

            //ルート回転がない場合
            if (RootRotationTransform == null && Model != null)
            {
                //モデル姿勢をルート姿勢にする
                RootRotationTransform = Model.transform;
            }

            //モーションデータ送信可否
            if (message.address == "/VMC/Ext/OK"
                && (message.values[0] is int))
            {
                Available = (int)message.values[0];
                if (Available == 0)
                {
                    StatusMessage = "Waiting for [Load VRM]";
                }

                //V2.5 キャリブレーション状態(長さ3以上)
                if (message.values.Length >= 3) {
                    if ((message.values[1] is int) && (message.values[2] is int))
                    {
                        int calibrationState = (int)message.values[1];
                        int calibrationMode = (int)message.values[2];

                        //キャリブレーション出来ていないときは隠す
                        if (HideInUncalibrated && Model != null)
                        {
                            Model.SetActive(calibrationState == 3);
                        }
                        //スケール同期をキャリブレーションと連動させる
                        if (SyncCalibrationModeWithScaleOffsetSynchronize)
                        {
                            RootScaleOffsetSynchronize = !(calibrationMode == 0); //通常モードならオフ、MR系ならオン
                        }

                    }
                }
                return;
            }
            //データ送信時刻
            else if (message.address == "/VMC/Ext/T"
                && (message.values[0] is float))
            {
                time = (float)message.values[0];
                PacketCounterInFrame++; //フレーム中のパケットフレーム数を測定
                return;
            }
            //VRM自動読み込み
            else if (message.address == "/VMC/Ext/VRM"
                && (message.values[0] is string)
                && (message.values[1] is string)
                )
            {
                string path = (string)message.values[0];
                string title = (string)message.values[1];

                //前回読み込んだパスと違う場合かつ、読み込みが許可されている場合
                if (path != loadedVRMPath && enableAutoLoadVRM == true)
                {
                    loadedVRMPath = path;
                    loadedVRMName = title;
                    LoadVRM(path);
                }
                return;
            }
            //オプション文字列
            else if (message.address == "/VMC/Ext/Opt"
                && (message.values[0] is string))
            {
                OptionString = (string)message.values[0];
                return;
            }


            //モデルがないか、モデル姿勢、ルート姿勢が取得できないなら以降何もしない
            if (Model == null || Model.transform == null || RootPositionTransform == null || RootRotationTransform == null)
            {
                return;
            }

            //Root姿勢
            if (message.address == "/VMC/Ext/Root/Pos"
                && (message.values[0] is string)
                && (message.values[1] is float)
                && (message.values[2] is float)
                && (message.values[3] is float)
                && (message.values[4] is float)
                && (message.values[5] is float)
                && (message.values[6] is float)
                && (message.values[7] is float)
                )
            {
                StatusMessage = "OK";

                pos.x = (float)message.values[1];
                pos.y = (float)message.values[2];
                pos.z = (float)message.values[3];
                rot.x = (float)message.values[4];
                rot.y = (float)message.values[5];
                rot.z = (float)message.values[6];
                rot.w = (float)message.values[7];

                //位置同期
                if (RootPositionSynchronize)
                {
                    RootPositionTransform.localPosition = pos;
                }
                //回転同期
                if (RootRotationSynchronize)
                {
                    RootRotationTransform.localRotation = rot;
                }
                //スケール同期とオフセット補正(v2.1拡張プロトコルの場合のみ)
                if (RootScaleOffsetSynchronize && message.values.Length > RootPacketLengthOfScaleAndOffset
                    && (message.values[8] is float)
                    && (message.values[9] is float)
                    && (message.values[10] is float)
                    && (message.values[11] is float)
                    && (message.values[12] is float)
                    && (message.values[13] is float)
                    )
                {
                    scale.x = 1.0f / (float)message.values[8];
                    scale.y = 1.0f / (float)message.values[9];
                    scale.z = 1.0f / (float)message.values[10];
                    offset.x = (float)message.values[11];
                    offset.y = (float)message.values[12];
                    offset.z = (float)message.values[13];

                    Model.transform.localScale = scale;
                    RootPositionTransform.localPosition = Vector3.Scale(RootPositionTransform.localPosition, scale);

                    //位置同期が有効な場合のみオフセットを反映する
                    if (RootPositionSynchronize)
                    {
                        offset = Vector3.Scale(offset, scale);
                        RootPositionTransform.localPosition -= offset;
                    }
                }
                else {
                    Model.transform.localScale = Vector3.one;
                }
            }
            //ボーン姿勢
            else if (message.address == "/VMC/Ext/Bone/Pos"
                && (message.values[0] is string)
                && (message.values[1] is float)
                && (message.values[2] is float)
                && (message.values[3] is float)
                && (message.values[4] is float)
                && (message.values[5] is float)
                && (message.values[6] is float)
                && (message.values[7] is float)
                )
            {
                string boneName = (string)message.values[0];
                pos.x = (float)message.values[1];
                pos.y = (float)message.values[2];
                pos.z = (float)message.values[3];
                rot.x = (float)message.values[4];
                rot.y = (float)message.values[5];
                rot.z = (float)message.values[6];
                rot.w = (float)message.values[7];

                //Humanoidボーンに該当するボーンがあるか調べる
                HumanBodyBones bone;
                if (HumanBodyBonesTryParse(ref boneName, out bone))
                {
                    //あれば位置と回転をキャッシュする
                    if (HumanBodyBonesPositionTable.ContainsKey(bone))
                    {
                        HumanBodyBonesPositionTable[bone] = pos;
                    }
                    else
                    {
                        HumanBodyBonesPositionTable.Add(bone, pos);
                    }

                    if (HumanBodyBonesRotationTable.ContainsKey(bone))
                    {
                        HumanBodyBonesRotationTable[bone] = rot;
                    }
                    else
                    {
                        HumanBodyBonesRotationTable.Add(bone, rot);
                    }
                }
                //受信と更新のタイミングは切り離した
            }
            //ブレンドシェープ同期
            else if (message.address == "/VMC/Ext/Blend/Val"
                && (message.values[0] is string)
                && (message.values[1] is float)
                )
            {
                //一旦変数に格納する
                string key = (string)message.values[0];
                float value = (float)message.values[1];

                //BlendShapeフィルタが有効なら
                if (BlendShapeFilterEnable)
                {
                    //フィルタテーブルに存在するか確認する
                    if (blendShapeFilterDictionaly.ContainsKey(key))
                    {
                        //存在する場合はフィルタ更新して値として反映する
                        blendShapeFilterDictionaly[key] = (blendShapeFilterDictionaly[key] * BlendShapeFilter) + value * (1.0f - BlendShapeFilter);
                        value = blendShapeFilterDictionaly[key];
                    }
                    else {
                        //存在しない場合はフィルタに登録する。値はそのまま
                        blendShapeFilterDictionaly.Add(key, value);
                    }
                }

                if (BlendShapeSynchronize && blendShapeProxy != null)
                {
                    //v0.56 BlendShape仕様変更対応
                    //辞書からKeyに変換し、Key値辞書に値を入れる

                    //通信で受信したキーを小文字に変換して非ケース化
                    string lowerKey = key.ToLower();

                    //キーに該当するBSKeyが存在するかチェックする
                    BlendShapeKey bskey;
                    if (StringToBlendShapeKeyDictionary.TryGetValue(lowerKey, out bskey)){
                        //キーに対して値を登録する
                        BlendShapeToValueDictionary[bskey] = value;

                        //Debug.Log("[lowerKey]->"+ lowerKey+" [bskey]->"+bskey.ToString()+" [value]->"+value);
                    }
                    else {
                        //そんなキーは無い
                        //Debug.LogError("[lowerKey]->" + lowerKey + " is not found");
                    }
                }
            }
            //ブレンドシェープ適用
            else if (message.address == "/VMC/Ext/Blend/Apply")
            {
                if (BlendShapeSynchronize && blendShapeProxy != null)
                {
                    blendShapeProxy.SetValues(BlendShapeToValueDictionary);
                }
            }
        }

        //モデル破棄
        public void DestroyModel()
        {
            //存在すれば即破壊(異常顔防止)
            if (Model != null)
            {
                Destroy(Model);
                Model = null;
            }
            if (LoadedModelParent != null)
            {
                Destroy(LoadedModelParent);
                LoadedModelParent = null;
            }
        }

        //ファイルからモデルを読み込む
        public void LoadVRM(string path)
        {
            DestroyModel();

            //バイナリの読み込み
            if (File.Exists(path))
            {
                byte[] VRMdata = File.ReadAllBytes(path);
                LoadVRMFromData(VRMdata);
            }
            else {
                Debug.LogError("VRM load failed.");
            }
        }

        //ファイルからモデルを読み込む
        public void LoadVRMFromData(byte[] VRMdata)
        {
            if (isLoading) {
                Debug.LogError("Now Loading! load request is rejected.");
                return;
            }
            DestroyModel();

            //読み込み
            VRMImporterContext vrmImporter = new VRMImporterContext();
            vrmImporter.ParseGlb(VRMdata);

            isLoading = true;
            vrmImporter.LoadAsync(() =>
            {
                isLoading = false;
                Model = vrmImporter.Root;

                //ExternalReceiverの下にぶら下げる
                LoadedModelParent = new GameObject();
                LoadedModelParent.transform.SetParent(transform, false);
                LoadedModelParent.name = "LoadedModelParent";
                //その下にモデルをぶら下げる
                Model.transform.SetParent(LoadedModelParent.transform, false);

                vrmImporter.EnableUpdateWhenOffscreen();
                vrmImporter.ShowMeshes();

                //カメラなどの移動補助のため、頭の位置を格納する
                animator = Model.GetComponent<Animator>();
                HeadPosition = animator.GetBoneTransform(HumanBodyBones.Head).position;
            });
        }

        //ボーン位置をキャッシュテーブルに基づいて更新
        private void BoneSynchronizeByTable()
        {
            //キャッシュテーブルを参照
            foreach (var bone in HumanBodyBonesTable)
            {
                //キャッシュされた位置・回転を適用
                if (HumanBodyBonesPositionTable.ContainsKey(bone.Value) && HumanBodyBonesRotationTable.ContainsKey(bone.Value))
                {
                    BoneSynchronize(bone.Value, HumanBodyBonesPositionTable[bone.Value], HumanBodyBonesRotationTable[bone.Value]);
                }
            }
        }

        //ボーン位置同期
        private void BoneSynchronize(HumanBodyBones bone, Vector3 pos, Quaternion rot)
        {
            //操作可能な状態かチェック
            if (animator != null && bone != HumanBodyBones.LastBone)
            {
                //ボーンによって操作を分ける
                var t = animator.GetBoneTransform(bone);
                if (t != null)
                {
                    //指ボーン
                    if (bone == HumanBodyBones.LeftIndexDistal ||
                        bone == HumanBodyBones.LeftIndexIntermediate ||
                        bone == HumanBodyBones.LeftIndexProximal ||
                        bone == HumanBodyBones.LeftLittleDistal ||
                        bone == HumanBodyBones.LeftLittleIntermediate ||
                        bone == HumanBodyBones.LeftLittleProximal ||
                        bone == HumanBodyBones.LeftMiddleDistal ||
                        bone == HumanBodyBones.LeftMiddleIntermediate ||
                        bone == HumanBodyBones.LeftMiddleProximal ||
                        bone == HumanBodyBones.LeftRingDistal ||
                        bone == HumanBodyBones.LeftRingIntermediate ||
                        bone == HumanBodyBones.LeftRingProximal ||
                        bone == HumanBodyBones.LeftThumbDistal ||
                        bone == HumanBodyBones.LeftThumbIntermediate ||
                        bone == HumanBodyBones.LeftThumbProximal ||

                        bone == HumanBodyBones.RightIndexDistal ||
                        bone == HumanBodyBones.RightIndexIntermediate ||
                        bone == HumanBodyBones.RightIndexProximal ||
                        bone == HumanBodyBones.RightLittleDistal ||
                        bone == HumanBodyBones.RightLittleIntermediate ||
                        bone == HumanBodyBones.RightLittleProximal ||
                        bone == HumanBodyBones.RightMiddleDistal ||
                        bone == HumanBodyBones.RightMiddleIntermediate ||
                        bone == HumanBodyBones.RightMiddleProximal ||
                        bone == HumanBodyBones.RightRingDistal ||
                        bone == HumanBodyBones.RightRingIntermediate ||
                        bone == HumanBodyBones.RightRingProximal ||
                        bone == HumanBodyBones.RightThumbDistal ||
                        bone == HumanBodyBones.RightThumbIntermediate ||
                        bone == HumanBodyBones.RightThumbProximal)
                    {
                        //指ボーンカットオフが有効でなければ
                        if (!HandPoseSynchronizeCutoff)
                        {
                            //ボーン同期する。ただしフィルタはかけない
                            BoneSynchronizeSingle(t, ref bone, ref pos, ref rot, false, false);
                        }
                    }
                    //目ボーン
                    else if (bone == HumanBodyBones.LeftEye ||
                        bone == HumanBodyBones.RightEye)
                    {
                        //目ボーンカットオフが有効でなければ
                        if (!EyeBoneSynchronizeCutoff)
                        {
                            //ボーン同期する。ただしフィルタはかけない
                            BoneSynchronizeSingle(t, ref bone, ref pos, ref rot, false, false);
                        }
                    }
                    else
                    {
                        //ボーン同期する。フィルタは設定依存
                        BoneSynchronizeSingle(t, ref bone, ref pos, ref rot, BonePositionFilterEnable, BoneRotationFilterEnable);
                    }
                }
            }
        }

        //1本のボーンの同期
        private void BoneSynchronizeSingle(Transform t, ref HumanBodyBones bone, ref Vector3 pos, ref Quaternion rot, bool posFilter, bool rotFilter)
        {
            BoneFilter = Mathf.Clamp(BoneFilter, 0f, 1f);

            //ボーン位置同期が有効か
            if (BonePositionSynchronize)
            {
                //ボーン位置フィルタが有効か
                if (posFilter)
                {
                    bonePosFilter[(int)bone] = (bonePosFilter[(int)bone] * BoneFilter) + pos * (1.0f - BoneFilter);
                    t.localPosition = bonePosFilter[(int)bone];
                }
                else
                {
                    t.localPosition = pos;
                }
            }

            //ボーン回転フィルタが有効か
            if (rotFilter)
            {
                boneRotFilter[(int)bone] = Quaternion.Slerp(boneRotFilter[(int)bone], rot, 1.0f - BoneFilter);
                t.localRotation = boneRotFilter[(int)bone];
            }
            else
            {
                t.localRotation = rot;
            }
        }

        //ボーンENUM情報をキャッシュして高速化
        private bool HumanBodyBonesTryParse(ref string boneName, out HumanBodyBones bone)
        {
            //ボーンキャッシュテーブルに存在するなら
            if (HumanBodyBonesTable.ContainsKey(boneName))
            {
                //キャッシュテーブルから返す
                bone = HumanBodyBonesTable[boneName];
                //ただしLastBoneは発見しなかったことにする(無効値として扱う)
                if (bone == HumanBodyBones.LastBone)
                {
                    return false;
                }
                return true;
            }
            else
            {
                //キャッシュテーブルにない場合、検索する
                var res = EnumTryParse<HumanBodyBones>(boneName, out bone);
                if (!res)
                {
                    //見つからなかった場合はLastBoneとして登録する(無効値として扱う)ことにより次回から検索しない
                    bone = HumanBodyBones.LastBone;
                }
                //キャシュテーブルに登録する
                HumanBodyBonesTable.Add(boneName, bone);
                return res;
            }
        }

        //互換性を持ったTryParse
        private static bool EnumTryParse<T>(string value, out T result) where T : struct
        {
#if NET_4_6
            return Enum.TryParse(value, out result);
#else
            try
            {
                result = (T)Enum.Parse(typeof(T), value, true);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
#endif
        }

        //アプリケーションを終了させる
        public void ApplicationQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
