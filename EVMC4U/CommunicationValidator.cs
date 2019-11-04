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
    public class CommunicationValidator : MonoBehaviour, IExternalReceiver
    {
        [Header("CommunicationValidator v1.0")]
        [SerializeField]
        private string StatusMessage = "";  //Inspector表示用

        [Header("UI Option")]
        public bool ShowInformation = false;
        public bool StrictMode = false;

        [Header("Status Monitor")]
        [SerializeField]
        private int CallCountMonitor = 0; //Inspector表示用

        public int Available = 0;
        public float time = 0;

        [Header("Daisy Chain")]
        public GameObject[] NextReceivers = new GameObject[1];

        private ExternalReceiverManager externalReceiverManager = null;
        bool shutdown = false;
        
        readonly Rect rect1 = new Rect(0, 0, 120, 70);
        readonly Rect rect2 = new Rect(10, 20, 100, 30);
        readonly Rect rect3 = new Rect(10, 40, 100, 300);

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
                GUI.TextField(rect1, "ExternalReceiver");
                GUI.Label(rect2, "Available: " + GetAvailable());
                GUI.Label(rect3, "Time: " + GetRemoteTime());
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

            CallCountMonitor = callCount;
            StatusMessage = "OK";

            ProcessMessage(ref message);

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

            if (message.address == "/VMC/Ext/OK"
                && (message.values[0] is int))
            {
                Available = (int)message.values[0];
                if (Available == 0)
                {
                    StatusMessage = "Waiting for [Load VRM]";
                }
                else {
                    StatusMessage = "OK";
                }
            }
            //データ送信時刻
            else if (message.address == "/VMC/Ext/T"
                && (message.values[0] is float))
            {
                time = (float)message.values[0];
            }

            //厳格モード
            if (StrictMode)
            {
                CommunicationValidate(ref message);
            }
        }

        //プロトコルチェック(未知の情報がないかチェックする)
        void CommunicationValidate(ref uOSC.Message message)
        {
            if (message.address == "/VMC/Ext/OK"
                && (message.values[0] is int))
            {
                //OK
            }
            else if (message.address == "/VMC/Ext/T"
                && (message.values[0] is float))
            {
                //OK
            }
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
                //OK
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
                //OK
            }

            //ブレンドシェープ同期
            else if (message.address == "/VMC/Ext/Blend/Val"
                && (message.values[0] is string)
                && (message.values[1] is float)
                )
            {
                //OK
            }
            //ブレンドシェープ適用
            else if (message.address == "/VMC/Ext/Blend/Apply")
            {
                //OK
            }
            //カメラ姿勢FOV同期 v2.1
            else if (message.address == "/VMC/Ext/Cam"
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
                //OK
            }
            //コントローラ操作情報 v2.1
            else if (message.address == "/VMC/Ext/Con"
                && (message.values[0] is int)
                && (message.values[1] is string)
                && (message.values[2] is int)
                && (message.values[3] is int)
                && (message.values[4] is int)
                && (message.values[5] is float)
                && (message.values[6] is float)
                && (message.values[7] is float)
                )
            {
                //OK
            }
            //キーボード操作情報 v2.1
            else if (message.address == "/VMC/Ext/Key"
                && (message.values[0] is int)
                && (message.values[1] is string)
                && (message.values[2] is int)
                )
            {
                //OK
            }
            // v2.2
            else if (message.address == "/VMC/Ext/Midi/Note"
                && (message.values[0] is int)
                && (message.values[1] is int)
                && (message.values[2] is int)
                && (message.values[3] is float)
                )
            {
                //OK
            }
            // v2.2
            else if (message.address == "/VMC/Ext/Midi/CC/Val"
                && (message.values[0] is int)
                && (message.values[1] is float)
                )
            {
                //OK
            }
            // v2.2
            else if (message.address == "/VMC/Ext/Midi/CC/Bit"
                && (message.values[0] is int)
                && (message.values[1] is int)
                )
            {
                //OK
            }
            // v2.2
            else if (message.address == "/VMC/Ext/Hmd/Pos"
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
                //OK
            }
            // v2.2
            else if (message.address == "/VMC/Ext/Con/Pos"
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
                //OK
            }
            // v2.2
            else if (message.address == "/VMC/Ext/Tra/Pos"
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
                //OK
            }
            else
            {
                //プロトコルにないアドレスを検出したら以後の処理を一切しない
                Debug.LogError("[CommunicationValidator] " + message.address + " is not valid");
                StatusMessage = "Communication error.";
                shutdown = true;
            }
        }
    }
}