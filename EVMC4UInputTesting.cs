/*
 * ExternalReceiver
 * https://sabowl.sakura.ne.jp/gpsnmeajp/
 *
 * These codes are licensed under CC0.
 * http://creativecommons.org/publicdomain/zero/1.0/deed.ja
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EVMC4UInputTesting : MonoBehaviour {
    public void KeyInputEvent(EVMC4U.KeyInput key)
    {
        switch (key.active) {
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
        switch (con.active) {
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
}
