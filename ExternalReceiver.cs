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
    public interface IExternalReceiver
    {
        void MessageDaisyChain(uOSC.Message message, int callCount);
    }

    public struct KeyInput
    {
        public int active;
        public string name;
        public int keycode;
    }

    public struct ControllerInput
    {
        public int active;
        public string name;
        public int IsLeft;
        public int IsTouch;
        public int IsAxis;
        public Vector3 Axis;
    }

    [Serializable]
    public class KeyInputEvent : UnityEvent<KeyInput> { };
    [Serializable]
    public class ControllerInputEvent : UnityEvent<ControllerInput> { };


    //[RequireComponent(typeof(uOSC.uOscServer))]
    public class ExternalReceiver : MonoBehaviour, IExternalReceiver
    {
        [Header("ExternalReceiver v2.9d")]
        public GameObject Model;

        [Header("Synchronize Option")]
        public bool BlendShapeSynchronize = true;
        public bool RootPositionSynchronize = true;
        public bool RootRotationSynchronize = true;
        public bool RootScaleOffsetSynchronize = false;
        public bool BonePositionSynchronize = true;

        [Header("Synchronize Cutoff Option")]
        public bool HandPoseSynchronizeCutoff = false;
        public bool EyeBoneSynchronizeCutoff = false;

        [Header("UI Option")]
        public bool ShowInformation = false;
        public bool StrictMode = false;

        [Header("Lowpass Filter Option")]
        public bool BonePositionFilterEnable = false;
        public bool BoneRotationFilterEnable = false;
        public float BoneFilter = 0.7f;

        public bool CameraPositionFilterEnable = false;
        public bool CameraRotationFilterEnable = false;
        public float CameraFilter = 0.95f;

        [Header("Status")]
        [SerializeField]
        private string StatusMessage = "";
        [Header("Camera Control")]
        public Camera VMCControlledCamera;

        [Header("Daisy Chain")]
        public GameObject NextReceiver = null;

        [Header("Event Callback")]
        public KeyInputEvent KeyInputAction;
        public ControllerInputEvent ControllerInputAction;

        IExternalReceiver NextReceiverInterface = null;

        private Vector3[] bonePosFilter = new Vector3[Enum.GetNames(typeof(HumanBodyBones)).Length];
        private Quaternion[] boneRotFilter = new Quaternion[Enum.GetNames(typeof(HumanBodyBones)).Length];
        private Vector3 cameraPosFilter = Vector3.zero;
        private Quaternion cameraRotFilter = Quaternion.identity;

        private int Available = 0;
        private float time = 0;

        private GameObject OldModel = null;

        Animator animator = null;
        VRMBlendShapeProxy blendShapeProxy = null;

        const int RootPacketLengthOfScaleAndOffset = 8;

        uOSC.uOscServer server;
        bool shutdown = false;

        void Start()
        {
            if (NextReceiver != null) {
                NextReceiverInterface = NextReceiver.GetComponent(typeof(IExternalReceiver)) as IExternalReceiver;
            }

            server = GetComponent<uOSC.uOscServer>();
            if (server)
            {
                StatusMessage = "Waiting for VMC...";
                server.onDataReceived.AddListener(OnDataReceived);
            }
            else
            {
                StatusMessage = "Waiting for Master...";
            }
        }

        int GetAvailable()
        {
            return Available;
        }
        float GetRemoteTime()
        {
            return time;
        }

        void OnGUI()
        {
            if (ShowInformation)
            {
                GUIStyle color = new GUIStyle();
                GUI.TextField(new Rect(0, 0, 120, 70), "ExternalReceiver");
                GUI.Label(new Rect(10, 20, 100, 30), "Available: " + GetAvailable());
                GUI.Label(new Rect(10, 40, 100, 300), "Time: " + GetRemoteTime());
            }
        }

        void Update()
        {
            if (shutdown) { return; }

            Application.runInBackground = true;

            if (blendShapeProxy == null && Model != null)
            {
                blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
                /*
                foreach (var b in blendShapeProxy.GetValues()) {
                    Debug.Log("[ExternalReceiver]" + b.Key + " / " + b.Value);
                }
                */
            }

            if (Model == null)
            {
                StatusMessage = "Model not found.";
                return;
            }

        }
        private void OnDataReceived(uOSC.Message message)
        {
            MessageDaisyChain(message, 0);
        }

        public void MessageDaisyChain(uOSC.Message message, int callCount)
        {
            if (shutdown) { return; }

            ProcessMessage(message);

            //Next
            if (NextReceiver != null)
            {
                if (callCount > 100)
                {
                    //無限ループ対策
                    Debug.LogError("[ExternalReceiver] Too many call(maybe infinite loop).");
                    StatusMessage = "Infinite loop detected!";
                    shutdown = true;
                }
                else
                {
                    if (NextReceiverInterface != null)
                    {
                        NextReceiverInterface.MessageDaisyChain(message, callCount + 1);
                    }
                    else {
                        NextReceiver = null;
                        Debug.LogError("[ExternalReceiver] NextReceiver not implemented IExternalReceiver. set null");
                    }
                }
            }
        }

        private void ProcessMessage(uOSC.Message message)
        {
            if (Model == null)
            {
                return;
            }

            if (message.address == "/VMC/Ext/OK")
            {
                Available = (int)message.values[0];
                if (Available == 0)
                {
                    StatusMessage = "Waiting for [Load VRM]";
                }

            }
            else if (message.address == "/VMC/Ext/T")
            {
                time = (float)message.values[0];
            }

            else if (message.address == "/VMC/Ext/Root/Pos")
            {
                StatusMessage = "OK";

                Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);

                if (RootPositionSynchronize)
                {
                    Model.transform.localPosition = pos;
                }
                if (RootRotationSynchronize)
                {
                    Model.transform.localRotation = rot;
                }
                if (RootScaleOffsetSynchronize && message.values.Length > RootPacketLengthOfScaleAndOffset)
                {
                    Vector3 scale = new Vector3(1.0f / (float)message.values[8], 1.0f / (float)message.values[9], 1.0f / (float)message.values[10]);
                    Vector3 offset = new Vector3((float)message.values[11], (float)message.values[12], (float)message.values[13]);

                    Model.transform.localScale = scale;
                    Model.transform.position -= offset;
                }
            }

            else if (message.address == "/VMC/Ext/Bone/Pos")
            {
                Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);

                BoneSynchronize((string)message.values[0], pos, rot);
            }

            else if (message.address == "/VMC/Ext/Blend/Val")
            {
                if (BlendShapeSynchronize)
                {
                    blendShapeProxy.AccumulateValue((string)message.values[0], (float)message.values[1]);
                }
            }
            else if (message.address == "/VMC/Ext/Blend/Apply")
            {
                if (BlendShapeSynchronize)
                {
                    blendShapeProxy.Apply();
                }
            }
            else if (message.address == "/VMC/Ext/Cam")
            {
                if (VMCControlledCamera != null)
                {
                    Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                    Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);
                    float fov = (float)message.values[8];

                    if (CameraPositionFilterEnable)
                    {
                        cameraPosFilter = (cameraPosFilter * CameraFilter) + pos * (1.0f - CameraFilter);
                        VMCControlledCamera.transform.localPosition = cameraPosFilter;
                    }
                    else {
                        VMCControlledCamera.transform.localPosition = pos;
                    }
                    if (CameraRotationFilterEnable)
                    {
                        cameraRotFilter = Quaternion.Slerp(cameraRotFilter, rot, 1.0f - CameraFilter);
                        VMCControlledCamera.transform.localRotation = cameraRotFilter;
                    }
                    else {
                        VMCControlledCamera.transform.localRotation = rot;
                    }
                    VMCControlledCamera.fieldOfView = fov;
                }
            }
            else if (message.address == "/VMC/Ext/Con")
            {
                ControllerInput con;
                con.active = (int)message.values[0];
                con.name = (string)message.values[1];
                con.IsLeft = (int)message.values[2];
                con.IsTouch = (int)message.values[3];
                con.IsAxis = (int)message.values[4];
                con.Axis = new Vector3((float)message.values[5], (float)message.values[6], (float)message.values[7]);

                ControllerInputAction.Invoke(con);
                /*
                if (con.active < 2)
                {
                    Debug.Log("[ExternalReceiver]Con:" + active + "/" + name + "/" + IsLeft + "/" + IsTouch + "/" + IsAxis + "/" + Axis);
                }
                */
            }
            else if (message.address == "/VMC/Ext/Key")
            {
                KeyInput key;
                key.active = (int)message.values[0];
                key.name = (string)message.values[1];
                key.keycode = (int)message.values[2];

                KeyInputAction.Invoke(key);
                //Debug.Log("[ExternalReceiver]Key:" + active + "/" + name + "/" + keycode);
            }
            else {
                if (StrictMode) {
                    Debug.LogError("[ExternalReceiver] " + message.address + " is not valid");
                    StatusMessage = "Communication error.";
                    shutdown = true;
                }
            }
        }

        private void BoneSynchronize(string boneName, Vector3 pos, Quaternion rot)
        {
            //モデルが更新されたときのみ読み込み
            if (Model != null && OldModel != Model)
            {
                animator = Model.GetComponent<Animator>();
                blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
                OldModel = Model;
                Debug.Log("[ExternalReceiver] New model detected");
            }

            HumanBodyBones bone;
            if (EnumTryParse<HumanBodyBones>(boneName, out bone))
            {
                if (animator != null && bone != HumanBodyBones.LastBone)
                {
                    var t = animator.GetBoneTransform(bone);
                    if (t != null)
                    {
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
                            //指ボーン
                            if (!HandPoseSynchronizeCutoff)
                            {
                                //フィルタはオフ
                                BoneSynchronizeSingle(t, bone, pos, rot, false, false);
                            }
                        }
                        else if (bone == HumanBodyBones.LeftEye ||
                            bone == HumanBodyBones.RightEye)
                        {
                            //目ボーン
                            if (!EyeBoneSynchronizeCutoff)
                            {
                                //フィルタはオフ
                                BoneSynchronizeSingle(t, bone, pos, rot, false, false);
                            }
                        }
                        else
                        {
                            BoneSynchronizeSingle(t, bone, pos, rot, BonePositionFilterEnable, BoneRotationFilterEnable);
                        }
                    }
                }
            }
        }

        //1本のボーンの同期
        private void BoneSynchronizeSingle(Transform t, HumanBodyBones bone, Vector3 pos, Quaternion rot, bool posFilter, bool rotFilter)
        {
            if (BonePositionSynchronize)
            {
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
