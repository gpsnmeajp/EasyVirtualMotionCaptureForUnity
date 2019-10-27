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

namespace EVMC4U {
    //デイジーチェーン受信の最低限のインターフェース
    public interface IExternalReceiver
    {
        void MessageDaisyChain(ref uOSC.Message message, int callCount);
    }

    public class ExternalReceiverManager
    {
        List<IExternalReceiver> receivers = new List<IExternalReceiver>();

        //コンストラクタ
        public ExternalReceiverManager(GameObject[] gameObjects) {
            GetIExternalReceiver(gameObjects);
        }

        //ゲームオブジェクトからIExternalReceiverを探す
        public void GetIExternalReceiver(GameObject[] gameObjects)
        {
            //リストをクリア
            receivers.Clear();

            //GameObjectを調べる
            foreach (var g in gameObjects)
            {
                //GameObjectが存在するなら
                if (g != null) {
                    //IExternalReceiverを探す
                    var f = g.GetComponent(typeof(IExternalReceiver)) as IExternalReceiver;
                    if (f != null) {
                        //リストに突っ込む
                        receivers.Add(f);
                    }
                }
            }
        }

        //IExternalReceiverのリストを使って配信する
        public bool SendNextReceivers(uOSC.Message message, int callCount)
        {
            if (callCount > 100)
            {
                //無限ループ対策
                Debug.LogError("[ExternalReceiver] Too many call(maybe infinite loop).");
                return false;
            }

            foreach (var r in receivers) {
                //インターフェースがあるか
                if (r != null)
                {
                    //Chain数を+1して次へ
                    r.MessageDaisyChain(ref message, callCount + 1);
                }
            }
            return true;
        }
    }

    //キーボード入力情報
    public struct KeyInput
    {
        public int active;
        public string name;
        public int keycode;
    }

    //コントローラ入力情報
    public struct ControllerInput
    {
        public int active;
        public string name;
        public int IsLeft;
        public int IsTouch;
        public int IsAxis;
        public Vector3 Axis;
    }

    //MIDI Note入力情報
    public struct MidiNote
    {
        public int active;
        public int channel;
        public int note;
        public float velocity;
    }

    //MIDI CC Value入力情報
    public struct MidiCCValue
    {
        public int knob;
        public float value;
    }

    //MIDI CC Button入力情報
    public struct MidiCCButton
    {
        public int knob;
        public float active;
    }


    //イベント定義
    [Serializable]
    public class KeyInputEvent : UnityEvent<KeyInput> { };
    [Serializable]
    public class ControllerInputEvent : UnityEvent<ControllerInput> { };
    [Serializable]
    public class MidiNoteInputEvent : UnityEvent<MidiNote> { };
    [Serializable]
    public class MidiCCValueInputEvent : UnityEvent<MidiCCValue> { };
    [Serializable]
    public class MidiCCButtonInputEvent : UnityEvent<MidiCCButton> { };
}
