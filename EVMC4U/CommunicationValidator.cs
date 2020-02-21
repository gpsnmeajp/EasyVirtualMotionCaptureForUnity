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
    public class CommunicationValidator : MonoBehaviour, IExternalReceiver
    {
        [Header("CommunicationValidator v1.2")]
        [SerializeField]
        private string StatusMessage = "";  //Inspector表示用

        [Header("UI Option")]
        public bool ShowInformation = false;

        [Header("Status Monitor")]
        [SerializeField]
        private int CallCountMonitor = 0; //Inspector表示用

        public int Available = 0;
        public float time = 0;

        public CalibrationState calibrationState = 0;
        public CalibrationMode calibrationMode = 0;

        [Header("Receive Status(Read only)")]
        public bool ReceiveEnable = false;
        public int ReceivePort = 0;
        public string LoadedConfigPath = "";
        public Color backgroundColor = Color.clear;
        [Header("Window Attribute Info(Read only)")]
        public bool IsTopMost = false;
        public bool IsTransparent = false;
        public bool WindowClickThrough = false;
        public bool HideBorder = false;


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

                //V2.5 キャリブレーション状態(長さ3以上)
                if (message.values.Length >= 3)
                {
                    if ((message.values[1] is int) && (message.values[2] is int))
                    {
                        calibrationState = (CalibrationState)message.values[1];
                        calibrationMode = (CalibrationMode)message.values[2];
                    }
                }
            }
            //データ送信時刻
            else if (message.address == "/VMC/Ext/T"
                && (message.values[0] is float))
            {
                time = (float)message.values[0];
            }
            //V2.4 受信情報
            else if (message.address == "/VMC/Ext/Rcv"
                && (message.values[0] is int)
                && (message.values[1] is int))
            {
                ReceiveEnable = (int)message.values[0] != 0;
                ReceivePort = (int)message.values[1];
            }
            //V2.4 背景色情報
            else if (message.address == "/VMC/Ext/Setting/Color"
                && (message.values[0] is float)
                && (message.values[1] is float)
                && (message.values[2] is float)
                && (message.values[3] is float))
            {
                backgroundColor = new Color((float)message.values[0], (float)message.values[1], (float)message.values[2], (float)message.values[3]);
            }
            //V2.4 ウィンドウ情報
            else if (message.address == "/VMC/Ext/Setting/Win"
                && (message.values[0] is int)
                && (message.values[1] is int)
                && (message.values[2] is int)
                && (message.values[3] is int))
            {
                IsTopMost = (int)message.values[0] != 0;
                IsTransparent = (int)message.values[1] != 0;
                WindowClickThrough = (int)message.values[2] != 0;
                HideBorder = (int)message.values[3] != 0;
            }
            //V2.5 読み込み済み設定ファイルパス情報
            else if (message.address == "/VMC/Ext/Config"
                && (message.values[0] is string))
            {
                LoadedConfigPath = (string)message.values[0];
            }
        }
    }
}