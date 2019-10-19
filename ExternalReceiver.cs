/*
 * ExternalReceiver
 * https://sabowl.sakura.ne.jp/gpsnmeajp/
 *
 * These codes are licensed under CC0.
 * http://creativecommons.org/publicdomain/zero/1.0/deed.ja
 */
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VRM;

namespace EVMC4U
{
    //デイジーチェーン受信の最低限のインターフェース
    public interface IExternalReceiver
    {
        void MessageDaisyChain(uOSC.Message message, int callCount);
    }

    //キーボード入力情報
    public struct KeyInput
    {
        public int active;
        public string name;
        public int keycode;
    }

    //コントローラ入力情報
    public struct ControllerInput
    {
        public int active;
        public string name;
        public int IsLeft;
        public int IsTouch;
        public int IsAxis;
        public Vector3 Axis;
    }

    //イベント定義
    [Serializable]
    public class KeyInputEvent : UnityEvent<KeyInput> { };
    [Serializable]
    public class ControllerInputEvent : UnityEvent<ControllerInput> { };


    //[RequireComponent(typeof(uOSC.uOscServer))]
    public class ExternalReceiver : MonoBehaviour, IExternalReceiver
    {
        [Header("ExternalReceiver v2.9d(indev)")]
        public GameObject Model;

        [Header("Synchronize Option")]
        public bool BlendShapeSynchronize = true; //表情等同期
        public bool RootPositionSynchronize = true; //ルート座標同期(ルームスケール移動)
        public bool RootRotationSynchronize = true; //ルート回転同期
        public bool RootScaleOffsetSynchronize = false; //MRスケール適用
        public bool BonePositionSynchronize = true; //ボーン位置適用(回転は強制)

        [Header("Synchronize Cutoff Option")]
        public bool HandPoseSynchronizeCutoff = false; //指状態反映オフ
        public bool EyeBoneSynchronizeCutoff = false; //目ボーン反映オフ

        [Header("UI Option")]
        public bool ShowInformation = false; //通信状態表示UI
        public bool StrictMode = false; //プロトコルチェックモード

        [Header("Lowpass Filter Option")]
        public bool BonePositionFilterEnable = false; //ボーン位置フィルタ
        public bool BoneRotationFilterEnable = false; //ボーン回転フィルタ
        public float BoneFilter = 0.7f; //ボーンフィルタ係数

        public bool CameraPositionFilterEnable = false; //カメラ位置フィルタ(手ブレ補正)
        public bool CameraRotationFilterEnable = false; //カメラ回転フィルタ(手ブレ補正)
        public float CameraFilter = 0.95f; //カメラフィルタ係数

        [Header("Status")]
        [SerializeField]
        private string StatusMessage = ""; //状態メッセージ(Inspector表示用)
        [Header("Camera Control")]
        public Camera VMCControlledCamera; //VMCカメラ制御同期

        [Header("Daisy Chain")]
        public GameObject NextReceiver = null; //デイジーチェーン

        [Header("Event Callback")]
        public KeyInputEvent KeyInputAction; //キーボード入力イベント
        public ControllerInputEvent ControllerInputAction; //コントローラボタンイベント

        //---Const---

        //rootパケット長定数(拡張判別)
        const int RootPacketLengthOfScaleAndOffset = 8;

        //---Private---
        IExternalReceiver NextReceiverInterface = null; //デイジーチェーンのインターフェース保持用(Start時に取得)

        //フィルタ用データ保持変数
        private Vector3[] bonePosFilter = new Vector3[Enum.GetNames(typeof(HumanBodyBones)).Length];
        private Quaternion[] boneRotFilter = new Quaternion[Enum.GetNames(typeof(HumanBodyBones)).Length];
        private Vector3 cameraPosFilter = Vector3.zero;
        private Quaternion cameraRotFilter = Quaternion.identity;

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
        uOSC.uOscServer server;

        //エラー・無限ループ検出フラグ(trueで一切の受信を停止する)
        bool shutdown = false;

        void Start()
        {
            //NextReciverのインターフェースを取得する
            //インターフェースではInspectorに登録できないためGameObjectにしているが、毎度GetComponentすると重いため
            if (NextReceiver != null) {
                NextReceiverInterface = NextReceiver.GetComponent(typeof(IExternalReceiver)) as IExternalReceiver;
            }

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

        //通信状態表示用UI
        void OnGUI()
        {
            if (ShowInformation)
            {
                GUI.TextField(new Rect(0, 0, 120, 70), "ExternalReceiver");
                GUI.Label(new Rect(10, 20, 100, 30), "Available: " + GetAvailable());
                GUI.Label(new Rect(10, 40, 100, 300), "Time: " + GetRemoteTime());
            }
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
            MessageDaisyChain(message, 0);
        }

        //デイジーチェーン処理
        public void MessageDaisyChain(uOSC.Message message, int callCount)
        {
            //エラー・無限ループ時は処理をしない
            if (shutdown) { return; }

            //メッセージを処理
            ProcessMessage(message);

            //次のデイジーチェーンへ伝える
            if (NextReceiver != null)
            {
                //100回以上もChainするとは考えづらい
                if (callCount > 100)
                {
                    //無限ループ対策
                    Debug.LogError("[ExternalReceiver] Too many call(maybe infinite loop).");
                    StatusMessage = "Infinite loop detected!";

                    //以降の処理を全部停止
                    shutdown = true;
                }
                else
                {
                    //インターフェースがあるか
                    if (NextReceiverInterface != null)
                    {
                        //Chain数を+1して次へ
                        NextReceiverInterface.MessageDaisyChain(message, callCount + 1);
                    }
                    else {
                        //GameObjectはあるがIExternalReceiverじゃないのでnullにする
                        NextReceiver = null;
                        Debug.LogError("[ExternalReceiver] NextReceiver not implemented IExternalReceiver. set null");
                    }
                }
            }
        }

        //メッセージ処理本体
        private void ProcessMessage(uOSC.Message message)
        {
            //モデルがないなら何もしない
            if (Model == null)
            {
                return;
            }

            //モーションデータ送信可否
            if (message.address == "/VMC/Ext/OK")
            {
                Available = (int)message.values[0];
                if (Available == 0)
                {
                    StatusMessage = "Waiting for [Load VRM]";
                }

            }
            //データ送信時刻
            else if (message.address == "/VMC/Ext/T")
            {
                time = (float)message.values[0];
            }

            //Root姿勢
            else if (message.address == "/VMC/Ext/Root/Pos")
            {
                StatusMessage = "OK";

                Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);

                //位置同期
                if (RootPositionSynchronize)
                {
                    Model.transform.localPosition = pos;
                }
                //回転同期
                if (RootRotationSynchronize)
                {
                    Model.transform.localRotation = rot;
                }
                //スケール同期とオフセット補正(拡張プロトコルの場合のみ)
                if (RootScaleOffsetSynchronize && message.values.Length > RootPacketLengthOfScaleAndOffset)
                {
                    Vector3 scale = new Vector3(1.0f / (float)message.values[8], 1.0f / (float)message.values[9], 1.0f / (float)message.values[10]);
                    Vector3 offset = new Vector3((float)message.values[11], (float)message.values[12], (float)message.values[13]);

                    Model.transform.localScale = scale;
                    Model.transform.position -= offset;
                }
            }

            //ボーン姿勢
            else if (message.address == "/VMC/Ext/Bone/Pos")
            {
                Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);

                BoneSynchronize((string)message.values[0], pos, rot);
            }

            //ブレンドシェープ同期
            else if (message.address == "/VMC/Ext/Blend/Val")
            {
                if (BlendShapeSynchronize)
                {
                    blendShapeProxy.AccumulateValue((string)message.values[0], (float)message.values[1]);
                }
            }
            //ブレンドシェープ適用
            else if (message.address == "/VMC/Ext/Blend/Apply")
            {
                if (BlendShapeSynchronize)
                {
                    blendShapeProxy.Apply();
                }
            }
            //カメラ姿勢FOV同期
            else if (message.address == "/VMC/Ext/Cam")
            {
                //カメラがセットされているならば
                if (VMCControlledCamera != null)
                {
                    Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                    Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);
                    float fov = (float)message.values[8];

                    //カメラ移動フィルタ
                    if (CameraPositionFilterEnable)
                    {
                        cameraPosFilter = (cameraPosFilter * CameraFilter) + pos * (1.0f - CameraFilter);
                        VMCControlledCamera.transform.localPosition = cameraPosFilter;
                    }
                    else {
                        VMCControlledCamera.transform.localPosition = pos;
                    }
                    //カメラ回転フィルタ
                    if (CameraRotationFilterEnable)
                    {
                        cameraRotFilter = Quaternion.Slerp(cameraRotFilter, rot, 1.0f - CameraFilter);
                        VMCControlledCamera.transform.localRotation = cameraRotFilter;
                    }
                    else {
                        VMCControlledCamera.transform.localRotation = rot;
                    }
                    //FOV同期
                    VMCControlledCamera.fieldOfView = fov;
                }
            }
            //コントローラ操作情報
            else if (message.address == "/VMC/Ext/Con")
            {
                ControllerInput con;
                con.active = (int)message.values[0];
                con.name = (string)message.values[1];
                con.IsLeft = (int)message.values[2];
                con.IsTouch = (int)message.values[3];
                con.IsAxis = (int)message.values[4];
                con.Axis = new Vector3((float)message.values[5], (float)message.values[6], (float)message.values[7]);

                //イベントを呼び出す
                ControllerInputAction.Invoke(con);
            }
            //キーボード操作情報
            else if (message.address == "/VMC/Ext/Key")
            {
                KeyInput key;
                key.active = (int)message.values[0];
                key.name = (string)message.values[1];
                key.keycode = (int)message.values[2];

                //イベントを呼び出す
                KeyInputAction.Invoke(key);
            }
            else {
                //厳格モード
                if (StrictMode) {
                    //プロトコルにないアドレスを検出したら以後の処理を一切しない
                    //ほぼデバッグ用
                    Debug.LogError("[ExternalReceiver] " + message.address + " is not valid");
                    StatusMessage = "Communication error.";
                    shutdown = true;
                }
            }
        }

        //ボーン位置同期
        private void BoneSynchronize(string boneName, Vector3 pos, Quaternion rot)
        {
            //モデルが更新されたときに関連情報を更新する
            if (Model != null && OldModel != Model)
            {
                animator = Model.GetComponent<Animator>();
                blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
                OldModel = Model;
                Debug.Log("[ExternalReceiver] New model detected");
            }

            //Humanoidボーンに該当するボーンがあるか調べる
            HumanBodyBones bone;
            if (HumanBodyBonesTryParse(boneName, out bone))
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
                                BoneSynchronizeSingle(t, bone, pos, rot, false, false);
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
                                BoneSynchronizeSingle(t, bone, pos, rot, false, false);
                            }
                        }
                        else
                        {
                            //ボーン同期する。フィルタは設定依存
                            BoneSynchronizeSingle(t, bone, pos, rot, BonePositionFilterEnable, BoneRotationFilterEnable);
                        }
                    }
                }
            }
        }

        //1本のボーンの同期
        private void BoneSynchronizeSingle(Transform t, HumanBodyBones bone, Vector3 pos, Quaternion rot, bool posFilter, bool rotFilter)
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
        private bool HumanBodyBonesTryParse(string boneName, out HumanBodyBones bone)
        {
            if (HumanBodyBonesTable.ContainsKey(boneName))
            {
                bone = HumanBodyBonesTable[boneName];
                if (bone == HumanBodyBones.LastBone) {
                    return false;
                }
                return true;
            }
            else {
                var res = EnumTryParse<HumanBodyBones>(boneName, out bone);
                if (res)
                {
                    HumanBodyBonesTable.Add(boneName, bone);
                    return true;
                }
                else {
                    //無効なボーン
                    bone = HumanBodyBones.LastBone;
                    HumanBodyBonesTable.Add(boneName, bone);
                    return false;
                }
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
