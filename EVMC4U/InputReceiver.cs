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
    public class InputReceiver : MonoBehaviour, IExternalReceiver
    {
        [Header("InputReceiver v1.0")]
        [SerializeField]
        private string StatusMessage = "";  //Inspector表示用
        [SerializeField]
        private string LastInput = "";

        [Header("Event Callback")]
        public KeyInputEvent KeyInputAction = new KeyInputEvent(); //キーボード入力イベント
        public ControllerInputEvent ControllerInputAction = new ControllerInputEvent(); //コントローラボタンイベント
        public MidiNoteInputEvent MidiNoteInputAction = new MidiNoteInputEvent();
        public MidiCCValueInputEvent MidiCCValueInputAction = new MidiCCValueInputEvent();
        public MidiCCButtonInputEvent MidiCCButtonInputAction = new MidiCCButtonInputEvent();

        [Header("Daisy Chain")]
        public GameObject[] NextReceivers = new GameObject[1];

        private ExternalReceiverManager externalReceiverManager = null;
        bool shutdown = false;

        //メッセージ処理一時変数struct(負荷対策)
        ControllerInput con;
        KeyInput key;
        MidiNote note;
        MidiCCValue ccvalue;
        MidiCCButton ccbutton;

        void Start()
        {
            externalReceiverManager = new ExternalReceiverManager(NextReceivers);
            StatusMessage = "Waiting for Master...";
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

            //コントローラ操作情報 v2.1
            if (message.address == "/VMC/Ext/Con"
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
                con.active = (int)message.values[0];
                con.name = (string)message.values[1];
                con.IsLeft = (int)message.values[2];
                con.IsTouch = (int)message.values[3];
                con.IsAxis = (int)message.values[4];
                con.Axis.x = (float)message.values[5];
                con.Axis.y = (float)message.values[6];
                con.Axis.z = (float)message.values[7];

                //イベントを呼び出す
                if (ControllerInputAction != null)
                {
                    ControllerInputAction.Invoke(con);
                }

                if (con.IsLeft==1) {
                    LastInput = "Left: " + con.name + "=" + con.active;
                }
                else {
                    LastInput = "Right: " + con.name + "=" + con.active;
                }
            }
            //キーボード操作情報 v2.1
            else if (message.address == "/VMC/Ext/Key"
                && (message.values[0] is int)
                && (message.values[1] is string)
                && (message.values[2] is int)
                )
            {
                key.active = (int)message.values[0];
                key.name = (string)message.values[1];
                key.keycode = (int)message.values[2];

                //イベントを呼び出す
                if (KeyInputAction != null)
                {
                    KeyInputAction.Invoke(key);
                }

                LastInput = "Key " + key.name + "("+key.keycode+")="+key.active;
            }
            // v2.2
            else if (message.address == "/VMC/Ext/Midi/Note"
                && (message.values[0] is int)
                && (message.values[1] is int)
                && (message.values[2] is int)
                && (message.values[3] is float)
                )
            {
                note.active = (int)message.values[0];
                note.channel = (int)message.values[1];
                note.note = (int)message.values[2];
                note.velocity = (float)message.values[3];

                //イベントを呼び出す
                if (MidiNoteInputAction != null)
                {
                    MidiNoteInputAction.Invoke(note);
                }

                LastInput = "Note " + note.active + "/" + note.channel + "/" + note.note + "/" + note.velocity;
            }
            // v2.2
            else if (message.address == "/VMC/Ext/Midi/CC/Val"
                && (message.values[0] is int)
                && (message.values[1] is float)
                )
            {
                ccvalue.knob = (int)message.values[0];
                ccvalue.value = (float)message.values[1];

                //イベントを呼び出す
                if (MidiCCValueInputAction != null)
                {
                    MidiCCValueInputAction.Invoke(ccvalue);
                }

                LastInput = "CC Val " + ccvalue.knob + "/" + ccvalue.value;
            }
            // v2.2
            else if (message.address == "/VMC/Ext/Midi/CC/Bit"
                && (message.values[0] is int)
                && (message.values[1] is int)
                )
            {
                ccbutton.knob = (int)message.values[0];
                ccbutton.active = (int)message.values[1];

                //イベントを呼び出す
                if (MidiCCButtonInputAction != null)
                {
                    MidiCCButtonInputAction.Invoke(ccbutton);
                }
                LastInput = "CC Bit " + ccbutton.knob + "/" + ccbutton.active;
            }
        }
    }
}