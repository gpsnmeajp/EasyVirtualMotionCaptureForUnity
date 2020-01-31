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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EVMC4U;

public class CalibrationByController : MonoBehaviour
{
    public InputReceiver inputReceiver;
    public CommunicationValidator communicationValidator;
    public ExternalController externalController;

    [Header("Button Configuration")]
    public string Key = "ScrollLock";
    public string Button = "ClickAbutton";

    [Header("Time Configuration")]
    public float Time = 3f;

    [Header("Button Monitor")]
    public bool LeftButton = false;
    public bool RightButton = false;

    readonly Rect rect1 = new Rect(0, 0, 300, 40);

    void Start()
    {
        inputReceiver.KeyInputAction.AddListener(OnKey);
        inputReceiver.ControllerInputAction.AddListener(OnCon);
    }

    void OnGUI()
    {
        if (communicationValidator.calibrationState == CalibrationState.Uncalibrated)
        {
            GUI.TextField(rect1, "★キャリブレーションを待っています\n準備ができたらボタンを押してください");
        }
        if (communicationValidator.calibrationState == CalibrationState.WaitingForCalibrating)
        {
            GUI.TextField(rect1, "★姿勢を整えてください");
        }
        if (communicationValidator.calibrationState == CalibrationState.Calibrating)
        {
            GUI.TextField(rect1, "！動かないでください！");
        }
    }

    void OnKey(KeyInput key)
    {
        if (key.name == Key && key.active == 1)
        {
            CalibrationReady();
        }
    }
    void OnCon(ControllerInput con)
    {
        if (con.name == Button)
        {
            if (con.IsLeft == 1)
            {
                LeftButton = (con.active == 1);
            }
            else {
                RightButton = (con.active == 1);
            }
            if (LeftButton && RightButton) {
                //キャリブレーションできていないときのみ実行
                if (communicationValidator.calibrationState == CalibrationState.Uncalibrated)
                {
                    CalibrationReady();
                }

                LeftButton = false;
                RightButton = false;
            }
        }
    }

    void CalibrationReady()
    {
        Debug.Log("[CalibrationByController] CalibrationReady");
        //多重キャリブレーション時の不良動作対処に、2回キャリブレーション要求する
        externalController.CalibrationReady = true;
        Invoke("CalibrationReady2", 0.5f);
    }
    void CalibrationReady2() {
        Debug.Log("[CalibrationByController] CalibrationReady");

        externalController.CalibrationReady = true;
        Invoke("CalibrationExecute", Time);
    }
    void CalibrationExecute() {
        Debug.Log("[CalibrationByController] CalibrationExecute");

        //キャリブレーション実施
        externalController.CalibrationExecute = true;
    }
}
