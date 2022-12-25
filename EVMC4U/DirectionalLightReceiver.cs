/*
 * ExternalReceiver
 * https://sabowl.sakura.ne.jp/gpsnmeajp/
 *
 * MIT License
 * 
 * Copyright (c) 2020 gpsnmeajp
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
    public class DirectionalLightReceiver : MonoBehaviour, IExternalReceiver
    {
        [Header("DirectionalLightReceiver v1.2")]
        [SerializeField, Label("VMCディレクショナルライト制御同期Light")]
        public Light VMCControlledLight = null; //VMCディレクショナルライト制御同期
        [SerializeField, Label("動作状況")]
        private string StatusMessage = "";  //Inspector表示用

#if EVMC4U_JA
        [Header("デイジーチェーン")]
#else
        [Header("Daisy Chain")]
#endif
        public GameObject[] NextReceivers = new GameObject[1];

        private ExternalReceiverManager externalReceiverManager = null;
        bool shutdown = false;

        Vector3 pos;
        Quaternion rot;
        Color col;

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

            //ライト同期 v2.4
            if (message.address == "/VMC/Ext/Light"
                && (message.values[0] is string) //name
                && (message.values[1] is float) //pos.x
                && (message.values[2] is float) //poy.y
                && (message.values[3] is float) //pos.z
                && (message.values[4] is float) //q.x
                && (message.values[5] is float) //q.y
                && (message.values[6] is float) //q.z
                && (message.values[7] is float) //q.w
                && (message.values[8] is float) //r
                && (message.values[9] is float) //g
                && (message.values[10] is float) //b
                && (message.values[11] is float) //a
                )
            {
                if(VMCControlledLight != null && VMCControlledLight.transform != null)
                {
                    pos.x = (float)message.values[1];
                    pos.y = (float)message.values[2];
                    pos.z = (float)message.values[3];
                    rot.x = (float)message.values[4];
                    rot.y = (float)message.values[5];
                    rot.z = (float)message.values[6];
                    rot.w = (float)message.values[7];
                    col.r = (float)message.values[8];
                    col.g = (float)message.values[9];
                    col.b = (float)message.values[10];
                    col.a = (float)message.values[11];

                    VMCControlledLight.transform.localPosition = pos;
                    VMCControlledLight.transform.localRotation = rot;
                    VMCControlledLight.color = col;
                }
            }
        }
    }
}