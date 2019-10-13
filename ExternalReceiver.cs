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

[RequireComponent(typeof(uOSC.uOscServer))]
public class ExternalReceiver : MonoBehaviour
{
    public GameObject Model;

    public bool BlendSharpSynchronize = true;
    public bool RootPositionSynchronize = true;
    public bool BonePositionSynchronize = false;
    public bool ShowInformation = false;

    private int Available = 0;
    private float time = 0;

    private GameObject OldModel = null;

    Animator animator = null;
    VRMBlendShapeProxy blendShapeProxy = null;

    uOSC.uOscServer server;

    void Start()
    {
        server = GetComponent<uOSC.uOscServer>();
        server.onDataReceived.AddListener(OnDataReceived);
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
        if (blendShapeProxy == null)
        {
            blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
            foreach (var b in blendShapeProxy.GetValues()) {
                Debug.Log(b.Key + " / " + b.Value);
            }
        }
    }

    void OnDataReceived(uOSC.Message message)
    {
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
            if(RootPositionSynchronize)
            {
                Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);

                Model.transform.localPosition = pos;
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
            if (Enum.TryParse<HumanBodyBones>((string)message.values[0], out bone))
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
                            t.localPosition = pos;
                        }
                        t.localRotation = rot;
                    }
                }
            }
        }

        else if (message.address == "/VMC/Ext/Blend/Val")
        {
            if (BlendSharpSynchronize)
            {
                blendShapeProxy.AccumulateValue((string)message.values[0], (float)message.values[1]);
            }
        }
        else if (message.address == "/VMC/Ext/Blend/Apply")
        {
            if (BlendSharpSynchronize)
            {
                blendShapeProxy.Apply();
            }
        }
    }
}
