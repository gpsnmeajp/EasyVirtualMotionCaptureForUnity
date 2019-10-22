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
    [RequireComponent(typeof(EVMC4U.ExternalReceiver))]
    public class EVMC4U_HandCatch : MonoBehaviour
    {
        //表示オンオフ
        public bool ShowCollider = true;
        bool ShowColliderOld = true;

        public float NonHoldFilter = 0f;
        public float InHoldFilter = 0.90f;

        ExternalReceiver exrcv;

        Transform leftHand;
        Transform rightHand;

        GameObject leftSphere;
        GameObject rightSphere;

        EVMC4U_HandCatch_Helper leftHelper;
        EVMC4U_HandCatch_Helper rightHelper;

        GameObject leftCatchedObject;
        GameObject rightCatchedObject;

        void Start()
        {
            //ExternalReceiverにキー操作を登録
            exrcv = GetComponent<EVMC4U.ExternalReceiver>();
            exrcv.ControllerInputAction.AddListener(ControllerInputEvent);
            exrcv.KeyInputAction.AddListener(KeyInputEvent);

            //ブレ防止用にフィルタを設定
            exrcv.BonePositionFilterEnable = true;
            exrcv.BoneRotationFilterEnable = true;
            exrcv.BoneFilter = NonHoldFilter;

            //手のボーンを取得
            var anim = exrcv.Model.GetComponent<Animator>();
            leftHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);

            //左手当たり判定スフィア生成
            leftSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftSphere.transform.parent = leftHand;
            leftSphere.transform.localPosition = new Vector3(-0.12f, 0f, 0f);
            leftSphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            //左手当たり判定スフィアコライダー設定
            var leftCollider = leftSphere.GetComponent<Collider>();
            //コライダーは反応のみで衝突しない
            leftCollider.isTrigger = true;

            //左手当たり判定物理演算追加
            var leftRigidBody = leftSphere.AddComponent<Rigidbody>();
            //物理は反応のみで演算しない
            leftRigidBody.isKinematic = true;

            //左手当たり判定ヘルパー追加
            leftHelper = leftSphere.AddComponent<EVMC4U_HandCatch_Helper>();

            //右手当たり判定スフィア生成
            rightSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightSphere.transform.parent = rightHand;
            rightSphere.transform.localPosition = new Vector3(0.12f, 0f, 0f);
            rightSphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            //右手当たり判定スフィアコライダー設定
            var rightCollider = rightSphere.GetComponent<Collider>();
            //コライダーは反応のみで衝突しない
            rightCollider.isTrigger = true;

            //右手当たり判定物理演算追加
            var rightRigidBody = rightSphere.AddComponent<Rigidbody>();
            //物理は反応のみで演算しない
            rightRigidBody.isKinematic = true;

            //右手当たり判定ヘルパー追加
            rightHelper = rightSphere.AddComponent<EVMC4U_HandCatch_Helper>();
        }

        void Update()
        {
            //剥がれ防止で親を設定
            leftSphere.transform.parent = leftHand;
            leftSphere.transform.localPosition = new Vector3(-0.12f, 0f, 0f);
            rightSphere.transform.parent = rightHand;
            rightSphere.transform.localPosition = new Vector3(0.12f, 0f, 0f);

            //表示非表示を反映
            if (ShowColliderOld != ShowCollider) {
                leftSphere.GetComponent<MeshRenderer>().enabled = ShowCollider;
                rightSphere.GetComponent<MeshRenderer>().enabled = ShowCollider;

                ShowColliderOld = ShowCollider;
            }
        }

        //左手掴む処理
        void CatchLeft(bool s)
        {
            if (s)
            {
                //つかみ処理
                if (leftHelper.Trigger && leftHelper.other != null)
                {
                    //手を親に、解除用に保持
                    leftCatchedObject = leftHelper.other.gameObject;
                    leftCatchedObject.transform.parent = leftSphere.transform;

                    //フィルタ強く
                    exrcv.BoneFilter = InHoldFilter;
                }
            }
            else
            {
                if (leftCatchedObject != null)
                {
                    //解除
                    leftCatchedObject.transform.parent = null;

                    //フィルタ解除
                    exrcv.BoneFilter = NonHoldFilter;
                }
            }
        }

        void CatchRight(bool s)
        {
            if (s)
            {
                if (rightHelper.Trigger && rightHelper.other != null)
                {
                    //手を親に、解除用に保持
                    rightCatchedObject = rightHelper.other.gameObject;
                    rightCatchedObject.transform.parent = rightSphere.transform;

                    //フィルタ強く
                    exrcv.BoneFilter = InHoldFilter;
                }
            }
            else
            {
                if (rightCatchedObject != null)
                {
                    //解除
                    rightCatchedObject.transform.parent = null;

                    //フィルタ解除
                    exrcv.BoneFilter = NonHoldFilter;
                }
            }
        }

        public void KeyInputEvent(EVMC4U.KeyInput key)
        {
            //Zキーが押されたか
            if (key.name == "Z")
            {
                //つかみ・離し
                CatchLeft(key.active == 1);
            }
            //Xキー押されたか
            if (key.name == "X")
            {
                //つかみ・離し
                CatchRight(key.active == 1);
            }
        }

        public void ControllerInputEvent(EVMC4U.ControllerInput con)
        {
            //トリガー引かれたか
            if (con.name == "ClickTrigger")
            {
                if (con.IsLeft == 1)
                {
                    //つかみ・離し
                    CatchLeft(con.active == 1);
                }
                else
                {
                    //つかみ・離し
                    CatchRight(con.active == 1);
                }
            }
        }
    }
}