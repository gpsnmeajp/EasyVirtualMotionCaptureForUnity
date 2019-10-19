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
using uOSC;

public class EVMC4UDaisyChainTesting : MonoBehaviour,EVMC4U.IExternalReceiver {
    //デイジーチェーンテスト
    public void MessageDaisyChain(Message message, int callCount)
    {
        if (message.address == "/VMC/Ext/T") {
            Debug.Log(message.address + "[" + (float)message.values[0] + "]");
        }

        //メッセージ全部Logに出そうとか考えないこと。Unityが死ぬほど送られてきます。
    }
}
