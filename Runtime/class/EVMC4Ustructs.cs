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

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EVMC4U {
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

    public enum CalibrationState
    {
        Uncalibrated = 0,
        WaitingForCalibrating = 1,
        Calibrating = 2,
        Calibrated = 3,
    }
    public enum CalibrationMode
    {
        Normal = 0,
        MR_Hand = 1,
        MR_Floor = 2,
        T_Pose = 3,
    }
    public enum VirtualDevice
    {
        HMD = 0,
        Controller = 1,
        Tracker = 2,
    }
    public class LabelAttribute : PropertyAttribute
    {
        public readonly string name;
        public LabelAttribute(string name)
        {
            this.name = name;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(LabelAttribute))]
    public class LabelAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
#if EVMC4U_JA
            var attr = attribute as LabelAttribute;
            label.text = attr.name;
#endif
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }
    }
#endif
}
