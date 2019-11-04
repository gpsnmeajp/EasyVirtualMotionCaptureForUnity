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
    public class TeleportManager : MonoBehaviour
    {
        public InputReceiver inputReceiver;
        public GameObject ParentObject;
        public Animator model;

        public string[] TriggerKey = new string[5];
        public Transform[] TeleportTarget = new Transform[5];

        private Transform footpos;

        void Start()
        {
            inputReceiver.KeyInputAction.AddListener(OnKey);

            footpos = model.GetBoneTransform(HumanBodyBones.LeftFoot);
        }

        void OnKey(KeyInput key)
        {
            //押されたときのみ
            if (key.active != 1) {
                return;
            }
            //該当するキーを探す
            for (int i = 0; i < TriggerKey.Length; i++) {
                if (TriggerKey[i] == key.name) {
                    Debug.Log("Key:" + key.name);
                    //発見したらターゲット有効性をチェックする
                    if (TeleportTarget.Length > i) {
                        if (TeleportTarget[i] != null) {
                            //モデルに反映
                            ParentObject.transform.position -= (footpos.position - TeleportTarget[i].position);
                            ParentObject.transform.rotation = TeleportTarget[i].rotation;
                        }
                    }
                }
            }
        }
    }
}