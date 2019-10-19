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
