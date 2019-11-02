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
using System;
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
        [Header("ExternalReceiver v3.0")]
        public GameObject Model = null;
        public bool Freeze = false; //すべての同期を止める(撮影向け)

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

        [Header("Status")]
        [SerializeField]
        private string StatusMessage = ""; //状態メッセージ(Inspector表示用)

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

        //uOSCサーバー
        uOSC.uOscServer server = null;

        //エラー・無限ループ検出フラグ(trueで一切の受信を停止する)
        bool shutdown = false;

        //メッセージ処理一時変数struct(負荷対策)
        Vector3 pos;
        Quaternion rot;
        Vector3 scale;
        Vector3 offset;

        void Start()
        {
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
        }

        //外部から通信状態を取得するための公開関数
        int GetAvailable()
        {
            return Available;
        }

        //外部から通信時刻を取得するための公開関数
        float GetRemoteTime()
        {
            return time;
        }

        void Update()
        {
            //エラー・無限ループ時は処理をしない
            if (shutdown) { return; }

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

            //メッセージを処理
            if (!Freeze) {
                ProcessMessage(ref message);
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

            //モデルがないか、モデル姿勢、ルート姿勢が取得できないなら何もしない
            if (Model == null || Model.transform == null || RootPositionTransform == null || RootRotationTransform == null)
            {
                return;
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
            }
            //データ送信時刻
            else if (message.address == "/VMC/Ext/T"
                && (message.values[0] is float))
            {
                time = (float)message.values[0];
            }

            //Root姿勢
            else if (message.address == "/VMC/Ext/Root/Pos"
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

                    //位置同期が有効な場合のみオフセットを反映する
                    if (RootPositionSynchronize) {
                        RootPositionTransform.position -= offset;
                    }
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
                pos.x = (float)message.values[1];
                pos.y = (float)message.values[2];
                pos.z = (float)message.values[3];
                rot.x = (float)message.values[4];
                rot.y = (float)message.values[5];
                rot.z = (float)message.values[6];
                rot.w = (float)message.values[7];

                BoneSynchronize((string)message.values[0], ref pos, ref rot);
            }

            //ブレンドシェープ同期
            else if (message.address == "/VMC/Ext/Blend/Val"
                && (message.values[0] is string)
                && (message.values[1] is float)
                )
            {
                if (BlendShapeSynchronize && blendShapeProxy != null)
                {
                    blendShapeProxy.AccumulateValue((string)message.values[0], (float)message.values[1]);
                }
            }
            //ブレンドシェープ適用
            else if (message.address == "/VMC/Ext/Blend/Apply")
            {
                if (BlendShapeSynchronize && blendShapeProxy != null)
                {
                    blendShapeProxy.Apply();
                }
            }
        }

        //ボーン位置同期
        private void BoneSynchronize(string boneName, ref Vector3 pos, ref Quaternion rot)
        {
            //モデルが更新されたときに関連情報を更新する
            if (OldModel != Model && Model != null)
            {
                animator = Model.GetComponent<Animator>();
                blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
                OldModel = Model;
                Debug.Log("[ExternalReceiver] New model detected");
            }

            //Humanoidボーンに該当するボーンがあるか調べる
            HumanBodyBones bone;
            if (HumanBodyBonesTryParse(ref boneName, out bone))
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
        }

        //1本のボーンの同期
        private void BoneSynchronizeSingle(Transform t, ref HumanBodyBones bone, ref Vector3 pos, ref Quaternion rot, bool posFilter, bool rotFilter)
        {
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
                if (bone == HumanBodyBones.LastBone) {
                    return false;
                }
                return true;
            }
            else {
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
