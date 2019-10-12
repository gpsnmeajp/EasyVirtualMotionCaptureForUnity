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

public class ExternalReceiver : MonoBehaviour
{
    private int Available = 0;
    private float time = 0;

    public GameObject Model;

    uOSC.uOscServer server;

    // Start is called before the first frame update
    void Start()
    {
        server = GetComponent<uOSC.uOscServer>();
        server.onDataReceived.AddListener(OnDataReceived);
    }

    void OnDataReceived(uOSC.Message message)
    {
        if (message.address == "/VMC/ExternalSender/Available")
        {
            Available = (int)message.values[0];
        }
        if (message.address == "/VMC/ExternalSender/Time")
        {
            time = (float)message.values[0];
        }
        if (message.address == "/VMC/ExternalSender/Bone/Transform")
        {
            Animator animator = null;
            if (Model != null)
            {
                animator = Model.GetComponent<Animator>();
            }

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
    }
}
