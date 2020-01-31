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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using VRM;

namespace EVMC4U
{
    [RequireComponent(typeof(uOSC.uOscClient))]
    public class ExternalController : MonoBehaviour
    {
        [Header("ExternalController v1.0")]
        public bool enable = true;
        [Header("Frame Period")]
        public int PeriodOfStatus = 1;
        public int PeriodOfRoot = 1;
        public int PeriodOfBone = 1;
        public int PeriodOfBlendShape = 1;
        public int PeriodOfCamera = 1;
        public int PeriodOfDevices = 1;
        public bool PeriodEnable = false;

        [Header("Virtual Device")]
        public VirtualDevice DeviceMode = VirtualDevice.Tracker;
        public Transform DeviceTransform = null;
        public String DeviceSerial = "VIRTUAL_DEVICE";
        public bool DeviceEnable = false;

        [Header("Virtual MIDI CC")]
        public int MidiKnob = 0;
        public float MidiValue = 0f;
        public bool MidiEnable = false;

        [Header("Camera Control")]
        public Transform CameraTransform = null;
        public float CameraFOV = 30f;
        public bool CameraEnable = false;

        [Header("BlendShapeProxy")]
        public string BlendShapeName = "";
        public float BlendShapeValue = 0f;
        public bool BlendShapeEnable = false;

        [Header("Eye Tracking Target Position")]
        public Transform EyeTrackingTargetTransform = null;
        public bool EyeTrackingTargetUse = false;
        public bool EyeTrackingTargetEnable = false;

        [Header("Response String")]
        public string ResponseString = "";
        public bool ResponseStringEnable = false;

        [Header("Calibration")]
        public bool CalibrationReady = false;
        public CalibrationMode calibrationMode = 0;
        public bool CalibrationExecute = false;

        [Header("Config")]
        public string ConfigPath = "";
        public bool ConfigLoad = false;

        [Header("Request Information")]
        public bool RequestInformation = false;

        uOSC.uOscClient client = null;

        void Start()
        {
            client = GetComponent<uOSC.uOscClient>();
        }
        void Update()
        {
            if (client == null) {
                return;
            }
            if (!enable) {
                return;
            }

            if (PeriodEnable) {
                client.Send("/VMC/Ext/Set/Period", PeriodOfStatus, PeriodOfRoot, PeriodOfBone, PeriodOfBlendShape, PeriodOfCamera, PeriodOfDevices);
            }
            if (DeviceEnable) {
                string name = null;
                switch (DeviceMode) {
                    case VirtualDevice.HMD:
                        name = "/VMC/Ext/Hmd/Pos";
                        break;
                    case VirtualDevice.Controller:
                        name = "/VMC/Ext/Con/Pos";
                        break;
                    case VirtualDevice.Tracker:
                        name = "/VMC/Ext/Tra/Pos";
                        break;
                    default:
                        name = null;
                        break;
                }
                if (name != null && DeviceTransform != null && DeviceSerial != null) {
                    client.Send(name,
                        (string)DeviceSerial,
                        (float)DeviceTransform.position.x,
                        (float)DeviceTransform.position.y,
                        (float)DeviceTransform.position.z,
                        (float)DeviceTransform.rotation.x,
                        (float)DeviceTransform.rotation.y,
                        (float)DeviceTransform.rotation.z,
                        (float)DeviceTransform.rotation.w);
                }
            }
            if (MidiEnable)
            {
                client.Send("/VMC/Ext/Midi/CC/Val", MidiKnob, MidiValue);
            }
            if (CameraEnable)
            {
                client.Send("/VMC/Ext/Cam",
                    "Camera",
                    (float)CameraTransform.position.x,
                    (float)CameraTransform.position.y,
                    (float)CameraTransform.position.z,
                    (float)CameraTransform.rotation.x,
                    (float)CameraTransform.rotation.y,
                    (float)CameraTransform.rotation.z,
                    (float)CameraTransform.rotation.w,
                    (float)CameraFOV);
            }
            if (BlendShapeEnable)
            {
                client.Send("/VMC/Ext/Blend/Val", BlendShapeName, BlendShapeValue);
                client.Send("/VMC/Ext/Blend/Apply");
            }
            if (EyeTrackingTargetEnable)
            {
                client.Send("/VMC/Ext/Set/Eye",
                    EyeTrackingTargetUse?(int)1: (int)0,
                    (float)EyeTrackingTargetTransform.position.x,
                    (float)EyeTrackingTargetTransform.position.y,
                    (float)EyeTrackingTargetTransform.position.z);
            }
            if (ResponseStringEnable)
            {
                client.Send("/VMC/Ext/Set/Res", ResponseString);
            }
            if (CalibrationReady)
            {
                CalibrationReady = false;
                client.Send("/VMC/Ext/Set/Calib/Ready");
            }
            if (CalibrationExecute)
            {
                CalibrationExecute = false;
                client.Send("/VMC/Ext/Set/Calib/Exec", (int)calibrationMode);
            }
            if (ConfigLoad)
            {
                ConfigLoad = false;
                client.Send("/VMC/Ext/Set/Config", ConfigPath);
            }
            if (RequestInformation)
            {
                //毎フレーム送信要求をする
                client.Send("/VMC/Ext/Set/Req");
            }
        }
    }

}