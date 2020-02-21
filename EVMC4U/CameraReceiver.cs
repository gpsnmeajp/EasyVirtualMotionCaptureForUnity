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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using VRM;

namespace EVMC4U
{
    public class CameraReceiver : MonoBehaviour, IExternalReceiver
    {
        [Header("CameraReceiver v1.1")]
        public Camera VMCControlledCamera = null; //VMCカメラ制御同期
        [SerializeField]
        private string StatusMessage = "";  //Inspector表示用

        [Header("Lowpass Filter Option")]
        public bool CameraPositionFilterEnable = false; //カメラ位置フィルタ(手ブレ補正)
        public bool CameraRotationFilterEnable = false; //カメラ回転フィルタ(手ブレ補正)
        public float CameraFilter = 0.95f; //カメラフィルタ係数

        [Header("Daisy Chain")]
        public GameObject[] NextReceivers = new GameObject[1];

        private ExternalReceiverManager externalReceiverManager = null;
        bool shutdown = false;

        private Vector3 cameraPosFilter = Vector3.zero;
        private Quaternion cameraRotFilter = Quaternion.identity;

        //メッセージ処理一時変数struct(負荷対策)
        //Vector3 pos;
        //Quaternion rot;

        //カメラ情報のキャッシュ
        Vector3 cameraPos = Vector3.zero;
        Quaternion cameraRot = Quaternion.identity;
        float fov = 0;

        void Start()
        {
            externalReceiverManager = new ExternalReceiverManager(NextReceivers);
            StatusMessage = "Waiting for Master...";
        }

        //デイジーチェーンを更新
        public void UpdateDaisyChain()
        {
            externalReceiverManager.GetIExternalReceiver(NextReceivers);
        }

        void Update()
        {
            //カメラがセットされているならば
            if (VMCControlledCamera != null && VMCControlledCamera.transform != null && fov != 0)
            {
                CameraFilter = Mathf.Clamp(CameraFilter, 0f, 1f);

                //カメラ移動フィルタ
                if (CameraPositionFilterEnable)
                {
                    cameraPosFilter = (cameraPosFilter * CameraFilter) + cameraPos * (1.0f - CameraFilter);
                    VMCControlledCamera.transform.localPosition = cameraPosFilter;
                }
                else
                {
                    VMCControlledCamera.transform.localPosition = cameraPos;
                }
                //カメラ回転フィルタ
                if (CameraRotationFilterEnable)
                {
                    cameraRotFilter = Quaternion.Slerp(cameraRotFilter, cameraRot, 1.0f - CameraFilter);
                    VMCControlledCamera.transform.localRotation = cameraRotFilter;
                }
                else
                {
                    VMCControlledCamera.transform.localRotation = cameraRot;
                }
                //FOV同期
                VMCControlledCamera.fieldOfView = fov;
            }
        }

        public void MessageDaisyChain(ref uOSC.Message message, int callCount)
        {
            //Startされていない場合無視
            if (externalReceiverManager == null || enabled == false || gameObject.activeInHierarchy == false)
            {
                return;
            }

            if (shutdown)
            {
                return;
            }

            StatusMessage = "OK";

            //異常を検出して動作停止
            try
            {
                ProcessMessage(ref message);
            }
            catch (Exception e)
            {
                StatusMessage = "Error: Exception";
                Debug.LogError(" --- Communication Error ---");
                Debug.LogError(e.ToString());
                shutdown = true;
                return;
            }

            if (!externalReceiverManager.SendNextReceivers(message, callCount))
            {
                StatusMessage = "Infinite loop detected!";
                shutdown = true;
            }
        }

        private void ProcessMessage(ref uOSC.Message message)
        {
            //メッセージアドレスがない、あるいはメッセージがない不正な形式の場合は処理しない
            if (message.address == null || message.values == null)
            {
                StatusMessage = "Bad message.";
                return;
            }

            //カメラ姿勢FOV同期 v2.1
            if (message.address == "/VMC/Ext/Cam"
                && (message.values[0] is string)
                && (message.values[1] is float)
                && (message.values[2] is float)
                && (message.values[3] is float)
                && (message.values[4] is float)
                && (message.values[5] is float)
                && (message.values[6] is float)
                && (message.values[7] is float)
                && (message.values[8] is float)
                )
            {
                cameraPos.x = (float)message.values[1];
                cameraPos.y = (float)message.values[2];
                cameraPos.z = (float)message.values[3];
                cameraRot.x = (float)message.values[4];
                cameraRot.y = (float)message.values[5];
                cameraRot.z = (float)message.values[6];
                cameraRot.w = (float)message.values[7];
                fov = (float)message.values[8];
                //受信と更新のタイミングは切り離した
            }
        }
    }
}