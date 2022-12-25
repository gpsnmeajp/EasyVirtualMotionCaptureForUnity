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
    public class DeviceReceiver : MonoBehaviour, IExternalReceiver
    {
        const int arrayMax = 16;

        [Header("DeviceReceiver v1.1")]
        [SerializeField, Label("動作状況")]
        private string StatusMessage = "";  //Inspector表示用
        [SerializeField, Label("現実のトラッキング位置を反映")]
        public bool RealPosition = false;

#if EVMC4U_JA
        [Header("トラッキング設定")]
#else
        [Header("Tracking Config")]
#endif
        public string[] Serials = new string[arrayMax];
        public Transform[] Transforms = new Transform[arrayMax];

#if EVMC4U_JA
        [Header("トラッキングデバイス情報モニタ(表示用)")]
#else
        [Header("Tracking Device Monitor")]
#endif
        public string[] Types = new string[arrayMax];
        public Vector3[] Vector3s = new Vector3[arrayMax];

#if EVMC4U_JA
        [Header("デイジーチェーン")]
#else
        [Header("Daisy Chain")]
#endif
        public GameObject[] NextReceivers = new GameObject[1];

        private ExternalReceiverManager externalReceiverManager = null;
        bool shutdown = false;

        Dictionary<string, int> SerialIndexes = new Dictionary<string, int>();
        int ListIndex = 0;

        //メッセージ処理一時変数struct(負荷対策)
        Vector3 pos;
        Quaternion rot;


        void Start()
        {
            externalReceiverManager = new ExternalReceiverManager(NextReceivers);
            StatusMessage = "Waiting for Master...";

            //モニタ強制
            Types = new string[Serials.Length];
            Vector3s = new Vector3[Serials.Length];

            //登録処理
            ListIndex = 0;
            for (int i = 0; i < Serials.Length; i++)
            {
                //nullでも空白でもない場合(対象がある場合)
                if (Serials[i] != null && Serials[i] != "")
                {
                    //辞書に登録
                    SerialIndexes.Add(Serials[i], ListIndex);
                    //インデックスを更新
                    ListIndex++;
                }
            }
        }

        //デイジーチェーンを更新
        public void UpdateDaisyChain()
        {
            externalReceiverManager.GetIExternalReceiver(NextReceivers);
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

            if (!RealPosition)
            {
                if (message.address == "/VMC/Ext/Hmd/Pos"
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
                    pos.x = (float)message.values[1];
                    pos.y = (float)message.values[2];
                    pos.z = (float)message.values[3];
                    rot.x = (float)message.values[4];
                    rot.y = (float)message.values[5];
                    rot.z = (float)message.values[6];
                    rot.w = (float)message.values[7];

                    devideUpdate("HMD", (string)message.values[0], pos, rot);
                    //Debug.Log("HMD pos " + (string)message.values[0] + " : " + pos + "/" + rot);
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
                    pos.x = (float)message.values[1];
                    pos.y = (float)message.values[2];
                    pos.z = (float)message.values[3];
                    rot.x = (float)message.values[4];
                    rot.y = (float)message.values[5];
                    rot.z = (float)message.values[6];
                    rot.w = (float)message.values[7];

                    devideUpdate("Controller", (string)message.values[0], pos, rot);
                    //Debug.Log("Con pos " + (string)message.values[0] + " : " + pos + "/" + rot);
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
                    pos.x = (float)message.values[1];
                    pos.y = (float)message.values[2];
                    pos.z = (float)message.values[3];
                    rot.x = (float)message.values[4];
                    rot.y = (float)message.values[5];
                    rot.z = (float)message.values[6];
                    rot.w = (float)message.values[7];

                    devideUpdate("Tracker", (string)message.values[0], pos, rot);
                    //Debug.Log("Tra pos " + (string)message.values[0] + " : " + pos + "/" + rot);
                }
            }
            else {
                if (message.address == "/VMC/Ext/Hmd/Pos/Local"
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
                    pos.x = (float)message.values[1];
                    pos.y = (float)message.values[2];
                    pos.z = (float)message.values[3];
                    rot.x = (float)message.values[4];
                    rot.y = (float)message.values[5];
                    rot.z = (float)message.values[6];
                    rot.w = (float)message.values[7];

                    devideUpdate("HMD", (string)message.values[0], pos, rot);
                    //Debug.Log("HMD pos " + (string)message.values[0] + " : " + pos + "/" + rot);
                }
                // v2.2
                else if (message.address == "/VMC/Ext/Con/Pos/Local"
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
                    pos.x = (float)message.values[1];
                    pos.y = (float)message.values[2];
                    pos.z = (float)message.values[3];
                    rot.x = (float)message.values[4];
                    rot.y = (float)message.values[5];
                    rot.z = (float)message.values[6];
                    rot.w = (float)message.values[7];

                    devideUpdate("Controller", (string)message.values[0], pos, rot);
                    //Debug.Log("Con pos " + (string)message.values[0] + " : " + pos + "/" + rot);
                }
                // v2.2
                else if (message.address == "/VMC/Ext/Tra/Pos/Local"
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
                    pos.x = (float)message.values[1];
                    pos.y = (float)message.values[2];
                    pos.z = (float)message.values[3];
                    rot.x = (float)message.values[4];
                    rot.y = (float)message.values[5];
                    rot.z = (float)message.values[6];
                    rot.w = (float)message.values[7];

                    devideUpdate("Tracker", (string)message.values[0], pos, rot);
                    //Debug.Log("Tra pos " + (string)message.values[0] + " : " + pos + "/" + rot);
                }

            }
        }

        void devideUpdate(string type, string serial, Vector3 pos, Quaternion rot)
        {
            //辞書に登録済み
            if (SerialIndexes.ContainsKey(serial))
            {
                int i = SerialIndexes[serial];
                //配列を更新
                Types[i] = type;
                Vector3s[i] = pos;

                if (i < Transforms.Length && Transforms[i] != null) 
                {
                    Transforms[i].localPosition = pos;
                    Transforms[i].localRotation = rot;
                }
            }
            else
            {
                //最大を超えたら登録しない
                if (ListIndex < Serials.Length)
                {
                    //辞書に未登録

                    //辞書に登録
                    Serials[ListIndex] = serial;
                    SerialIndexes.Add(serial, ListIndex);
                    //インデックスを更新
                    ListIndex++;
                }
            }
        }
    }
}