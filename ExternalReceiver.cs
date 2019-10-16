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
using VRM;

//[RequireComponent(typeof(uOSC.uOscServer))]
public class ExternalReceiver : MonoBehaviour
{
    [Header("ExternalReceiver v2.7")]
    public GameObject Model;

    [Header("Synchronize Option")]
    public bool BlendShapeSynchronize = true;
    public bool RootPositionSynchronize = true;
    public bool RootRotationSynchronize = true;
    public bool BonePositionSynchronize = true;
    [Header("UI Option")]
    public bool ShowInformation = false;
    [Header("Filter Option")]
    public bool BonePositionFilterEnable = false;
    public bool BoneRotationFilterEnable = false;
    public float filter = 0.7f;

    [Header("Status")]
    public string StatusMessage = "";
    [Header("Daisy Chain")]
    public ExternalReceiver NextReceiver = null;


    private Vector3[] bonePosFilter = new Vector3[Enum.GetNames(typeof(HumanBodyBones)).Length];
    private Quaternion[] boneRotFilter = new Quaternion[Enum.GetNames(typeof(HumanBodyBones)).Length];

    private int Available = 0;
    private float time = 0;

    private GameObject OldModel = null;

    Animator animator = null;
    VRMBlendShapeProxy blendShapeProxy = null;

    uOSC.uOscServer server;

    void Start()
    {
        server = GetComponent<uOSC.uOscServer>();
        if (server)
        {
            StatusMessage = "Waiting for VMC...";
            server.onDataReceived.AddListener(OnDataReceived);
        }
        else {
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
        if (ShowInformation) {
            GUIStyle color = new GUIStyle();
            GUI.TextField(new Rect(0, 0, 120, 70), "ExternalReceiver");
            GUI.Label(new Rect(10, 20, 100, 30), "Available: " + GetAvailable());
            GUI.Label(new Rect(10, 40, 100, 300), "Time: " + GetRemoteTime());
        }
    }

    void Update()
    {
        Application.runInBackground = true;

        if (blendShapeProxy == null)
        {
            blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
            foreach (var b in blendShapeProxy.GetValues()) {
                Debug.Log(b.Key + " / " + b.Value);
            }
        }
    }

    public void OnDataReceived(uOSC.Message message)
    {
        StatusMessage = "OK";

        if (message.address == "/VMC/Ext/OK")
        {
            Available = (int)message.values[0];
        }
        else if (message.address == "/VMC/Ext/T")
        {
            time = (float)message.values[0];
        }

        else if (message.address == "/VMC/Ext/Root/Pos")
        {
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
        }

        else if (message.address == "/VMC/Ext/Bone/Pos")
        {
            //モデルが更新されたときのみ読み込み
            if (Model != null && OldModel != Model)
            {
                animator = Model.GetComponent<Animator>();
                blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
                OldModel = Model;
                Debug.Log("new model detected");
            }

            HumanBodyBones bone;
            if (EnumTryParse<HumanBodyBones>((string)message.values[0], out bone))
            {
                if (animator != null && bone != HumanBodyBones.LastBone)
                {
                    Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                    Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);

                    var t = animator.GetBoneTransform(bone);
                    if (t != null)
                    {
                        if (BonePositionSynchronize)
                        {
                            if (BonePositionFilterEnable)
                            {
                                bonePosFilter[(int)bone] = (bonePosFilter[(int)bone] * filter) + pos*(1.0f- filter);
                                t.localPosition = bonePosFilter[(int)bone];
                            }
                            else {
                                t.localPosition = pos;
                            }
                        }

                        if (BoneRotationFilterEnable)
                        {
                            boneRotFilter[(int)bone] = Quaternion.Slerp(boneRotFilter[(int)bone], rot, 1.0f - filter);
                            t.localRotation = boneRotFilter[(int)bone];
                        }
                        else
                        {
                            t.localRotation = rot;
                        }
                    }
                }
            }
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

        //Next
        if (NextReceiver != null)
        {
            NextReceiver.OnDataReceived(message);
        }
    }

    private static bool EnumTryParse<T>(string value, out T result) where T : struct
    {
#if NET_4_6
        return Enum.TryParse(value, out result);
#else
        try
        {
            result = (T)Enum.Parse(typeof (T), value, true);
            return true;
        }
        catch
        {
            result = default(T);
            return false;
        }
#endif
    }
}
