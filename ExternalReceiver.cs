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
    private int Available = 0;
    private float time = 0;

    public bool BlendsharpEnable = true;

    public GameObject Model;
    private GameObject OldModel = null;

    Animator animator = null;
    VRMBlendShapeProxy blendShapeProxy = null;

    uOSC.uOscServer server;

    // Start is called before the first frame update
    void Start()
    {
        server = GetComponent<uOSC.uOscServer>();
        server.onDataReceived.AddListener(OnDataReceived);
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
            Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
            Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);

            Model.transform.localPosition = pos;
            Model.transform.localRotation = rot;
        }

        else if (message.address == "/VMC/Ext/Bone/Pos")
        {
            //モデルが更新されたときのみ読み込み
            if (Model != null && OldModel != Model)
            {
                animator = Model.GetComponent<Animator>();
                blendShapeProxy = Model.GetComponent<VRMBlendShapeProxy>();
                Debug.Log("new model detected");
            }
            OldModel = Model;

            HumanBodyBones bone;
            if (Enum.TryParse<HumanBodyBones>((string)message.values[0], out bone))
            {
                if (animator != null)
                {
                    Vector3 pos = new Vector3((float)message.values[1], (float)message.values[2], (float)message.values[3]);
                    Quaternion rot = new Quaternion((float)message.values[4], (float)message.values[5], (float)message.values[6], (float)message.values[7]);

                    if (bone != HumanBodyBones.LastBone)
                    {
                        var t = animator.GetBoneTransform(bone);
                        if (t != null)
                        {
                            if (!(
                                bone == HumanBodyBones.LeftIndexDistal ||
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
                                bone == HumanBodyBones.RightThumbProximal ||

                                bone == HumanBodyBones.LeftEye ||
                                bone == HumanBodyBones.RightEye
                                ))
                            {
                                t.localPosition = pos;
                            }
                            t.localRotation = rot;
                        }
                    }
                }
            }
        }

        else if (message.address == "/VMC/Ext/Blend/Val")
        {
            string BlendName = (string)message.values[0];
            float BlendValue = (float)message.values[1];

            if (BlendsharpEnable)
            {
                blendShapeProxy.AccumulateValue(BlendName, BlendValue);
            }
        }
        else if (message.address == "/VMC/Ext/Blend/Apply")
        {
            if (BlendsharpEnable)
            {
                blendShapeProxy.Apply();
            }
        }
    }
}
