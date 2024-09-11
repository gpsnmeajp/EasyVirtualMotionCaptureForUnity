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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EVMC4U
{
    public class InputTesting : MonoBehaviour
    {
        public InputReceiver receiver;
        private void Start()
        {
            receiver.ControllerInputAction.AddListener(ControllerInputEvent);
            receiver.KeyInputAction.AddListener(KeyInputEvent);

            receiver.MidiNoteInputAction.AddListener(MidiNoteEvent);
            receiver.MidiCCValueInputAction.AddListener(MidiCCValEvent);
            receiver.MidiCCButtonInputAction.AddListener(MidiCCButtonEvent);
        }

        public void KeyInputEvent(EVMC4U.KeyInput key)
        {
            switch (key.active)
            {
                case 1:
                    Debug.Log("" + key.name + "(" + key.keycode + ") pressed.");
                    break;
                case 0:
                    Debug.Log("" + key.name + "(" + key.keycode + ") released.");
                    break;
                default:
                    Debug.Log("" + key.name + "(" + key.keycode + ") unknown.");
                    break;
            }
        }

        public void ControllerInputEvent(EVMC4U.ControllerInput con)
        {
            switch (con.active)
            {
                case 2:
                    Debug.Log("" + con.name + "(" + ((con.IsAxis == 1) ? "Axis" : "Non Axis") + "/" + ((con.IsLeft == 1) ? "Left" : "Right") + "/" + ((con.IsTouch == 1) ? "Touch" : "Non Touch") + " [" + con.Axis.x + "," + con.Axis.y + "," + con.Axis.z + "]" + ") changed.");
                    break;
                case 1:
                    Debug.Log("" + con.name + "(" + ((con.IsAxis == 1) ? "Axis" : "Non Axis") + "/" + ((con.IsLeft == 1) ? "Left" : "Right") + "/" + ((con.IsTouch == 1) ? "Touch" : "Non Touch") + " [" + con.Axis.x + "," + con.Axis.y + "," + con.Axis.z + "]" + ") pressed.");
                    break;
                case 0:
                    Debug.Log("" + con.name + "(" + ((con.IsAxis == 1) ? "Axis" : "Non Axis") + "/" + ((con.IsLeft == 1) ? "Left" : "Right") + "/" + ((con.IsTouch == 1) ? "Touch" : "Non Touch") + " [" + con.Axis.x + "," + con.Axis.y + "," + con.Axis.z + "]" + ") released.");
                    break;
                default:
                    Debug.Log("" + con.name + "(" + ((con.IsAxis == 1) ? "Axis" : "Non Axis") + "/" + ((con.IsLeft == 1) ? "Left" : "Right") + "/" + ((con.IsTouch == 1) ? "Touch" : "Non Touch") + " [" + con.Axis.x + "," + con.Axis.y + "," + con.Axis.z + "]" + ") unknown.");
                    break;
            }
        }

        public void MidiNoteEvent(EVMC4U.MidiNote note)
        {
            if (note.active == 1)
            {
                Debug.Log("MIDI Note ON =" + note.note + " channel=" + note.channel + " velocity=" + note.velocity);
            }
            else
            {
                Debug.Log("MIDI note OFF =" + note.note + " channel=" + note.channel + " velocity=" + note.velocity);
            }
        }

        public void MidiCCValEvent(EVMC4U.MidiCCValue val)
        {
            Debug.Log("MIDI CC Value knob=" + val.knob + " value=" + val.value);
        }

        public void MidiCCButtonEvent(EVMC4U.MidiCCButton bit)
        {
            Debug.Log("MIDI CC Button knob=" + bit.knob + " active=" + bit.active);
        }
    }
}