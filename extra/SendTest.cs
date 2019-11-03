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

[RequireComponent(typeof(uOSC.uOscClient))]
public class SendTest : MonoBehaviour {
    public Transform HMD;
    public Transform con1;
    public Transform con2;
    public Transform tra;
    public Transform cam;

    uOSC.uOscClient client;
    // Use this for initialization
    void Start () {
        client = GetComponent<uOSC.uOscClient>();
    }
	
	// Update is called once per frame
	void Update () {
        client.Send("/VMC/Ext/Set/Period",
            1, 2, 3, 4, 5, 6);

        client.Send("/VMC/Ext/Midi/CC/Val", 0, Mathf.Sin(Time.time));

        client.Send("/VMC/Ext/Cam", 0, Mathf.Sin(Time.time));


        client.Send("/VMC/Ext/Cam", "FreeCam",
            cam.position.x, cam.position.y, cam.position.z,
            cam.rotation.x, cam.rotation.y, cam.rotation.z, cam.rotation.w,
            (float)90f);

        client.Send("/VMC/Ext/Hmd/Pos", "HMD",
            HMD.position.x, HMD.position.y, HMD.position.z,
            HMD.rotation.x, HMD.rotation.y, HMD.rotation.z, HMD.rotation.w);
        client.Send("/VMC/Ext/Con/Pos", "Con1",
            con1.position.x, con1.position.y, con1.position.z,
            con1.rotation.x, con1.rotation.y, con1.rotation.z, con1.rotation.w);
        client.Send("/VMC/Ext/Con/Pos", "Con2",
            con2.position.x, con2.position.y, con2.position.z,
            con2.rotation.x, con2.rotation.y, con2.rotation.z, con2.rotation.w);
        client.Send("/VMC/Ext/Tra/Pos", "Tra",
            tra.position.x, tra.position.y, tra.position.z,
            tra.rotation.x, tra.rotation.y, tra.rotation.z, tra.rotation.w);
    }
}
