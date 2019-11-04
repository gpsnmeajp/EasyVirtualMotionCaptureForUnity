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
    [RequireComponent(typeof(ExternalReceiver))]
    public class HandCatch : MonoBehaviour
    {
        //表示オンオフ
        public bool ShowCollider = true;
        bool ShowColliderOld = true;

        public float NonHoldFilter = 0f;
        public float InHoldFilter = 0.90f;

        float offset = 0.06f;
        float size = 0.15f;

        public string CollisionTag = "";

        public float SpeedMultiplier = 1.0f;

        public string LeftKey = "Z";
        public string RightKey = "X";
        public string ControllerButton = "ClickTrigger";

        public bool signaling = true;

        public bool StickyMode = false;

        bool stickyLeft = false;
        bool stickyRight = false;

        ExternalReceiver exrcv;
        InputReceiver inputrcv;

        Transform leftHand;
        Transform rightHand;

        GameObject leftSphere;
        GameObject rightSphere;

        Rigidbody leftRigidBody;
        Rigidbody rightRigidBody;

        Vector3 leftLastPos;
        Vector3 rightLastPos;
        Vector3 leftLastSpeed;
        Vector3 rightLastSpeed;

        HandCatch_Helper leftHelper;
        HandCatch_Helper rightHelper;

        GameObject leftCatchedObject;
        GameObject rightCatchedObject;

        bool leftCatchedObjectIsKinematic;
        bool rightCatchedObjectIsKinematic;

        Transform leftCatchedObjectParent;
        Transform rightCatchedObjectParent;

        void Start()
        {
            //ExternalReceiverにキー操作を登録
            exrcv = GetComponent<EVMC4U.ExternalReceiver>();
            inputrcv = GetComponentInChildren<EVMC4U.InputReceiver>();

            inputrcv.ControllerInputAction.AddListener(ControllerInputEvent);
            inputrcv.KeyInputAction.AddListener(KeyInputEvent);

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
            leftSphere.transform.localPosition = new Vector3(-offset, 0f, 0f);
            leftSphere.transform.localScale = new Vector3(size, size, size);

            //左手当たり判定スフィアコライダー設定
            var leftCollider = leftSphere.GetComponent<Collider>();
            //コライダーは反応のみで衝突しない
            leftCollider.isTrigger = true;

            //左手当たり判定物理演算追加
            leftRigidBody = leftSphere.AddComponent<Rigidbody>();
            //物理は反応のみで演算しない
            leftRigidBody.isKinematic = true;

            //左手当たり判定ヘルパー追加
            leftHelper = leftSphere.AddComponent<HandCatch_Helper>();

            //右手当たり判定スフィア生成
            rightSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightSphere.transform.parent = rightHand;
            rightSphere.transform.localPosition = new Vector3(offset, 0f, 0f);
            rightSphere.transform.localScale = new Vector3(size, size, size);

            //右手当たり判定スフィアコライダー設定
            var rightCollider = rightSphere.GetComponent<Collider>();
            //コライダーは反応のみで衝突しない
            rightCollider.isTrigger = true;

            //右手当たり判定物理演算追加
            rightRigidBody = rightSphere.AddComponent<Rigidbody>();
            //物理は反応のみで演算しない
            rightRigidBody.isKinematic = true;

            //右手当たり判定ヘルパー追加
            rightHelper = rightSphere.AddComponent<HandCatch_Helper>();
        }

        //物理演算のためFixedUpdate
        void FixedUpdate()
        {
            //剥がれ防止で親を設定
            leftSphere.transform.parent = leftHand;
            leftSphere.transform.localPosition = new Vector3(-offset, 0f, 0f);
            leftSphere.transform.localScale = new Vector3(size, size, size);
            rightSphere.transform.parent = rightHand;
            rightSphere.transform.localPosition = new Vector3(offset, 0f, 0f);
            rightSphere.transform.localScale = new Vector3(size, size, size);

            //表示非表示を反映
            if (ShowColliderOld != ShowCollider) {
                leftSphere.GetComponent<MeshRenderer>().enabled = ShowCollider;
                rightSphere.GetComponent<MeshRenderer>().enabled = ShowCollider;

                ShowColliderOld = ShowCollider;
            }

            //投げるとき用にフレーム間速度を求める
            leftLastSpeed = SpeedMultiplier * (leftHand.transform.position - leftLastPos)/Time.fixedDeltaTime;
            leftLastPos = leftHand.transform.position;
            rightLastSpeed = SpeedMultiplier * (rightHand.transform.position - rightLastPos)/Time.fixedDeltaTime;
            rightLastPos = rightHand.transform.position;
        }

        //左手掴む処理
        void CatchLeft(bool s)
        {
            if (s)
            {
                //つかみ処理
                if (leftHelper.Trigger && leftHelper.other != null)
                {
                    //コリジョンタグになにか文字が入っていて、対象と一致しない場合は処理しない
                    if (CollisionTag != "" && CollisionTag != leftHelper.other.tag) {
                        return;
                    }
                    //左手ですでに掴んでいるものは掴まない
                    if (leftHelper.other.gameObject.transform.parent == leftSphere.transform)
                    {
                        return;
                    }
                    //右手ですでに掴んでいるものは掴まない
                    if (leftHelper.other.gameObject.transform.parent == rightSphere.transform)
                    {
                        return;
                    }

                    //解除用に保持
                    leftCatchedObject = leftHelper.other.gameObject;

                    //親を保存
                    leftCatchedObjectParent = leftCatchedObject.transform.parent;

                    //手を親に上書き
                    leftCatchedObject.transform.parent = leftSphere.transform;

                    //掴むために物理演算を切る
                    var rigid = leftCatchedObject.GetComponent<Rigidbody>();
                    if (rigid != null) {
                        //IsKinematicを保存
                        leftCatchedObjectIsKinematic = rigid.isKinematic;
                        //設定に関わらずtrueにする
                        rigid.isKinematic = true;
                    }

                    //フィルタ強く
                    exrcv.BoneFilter = InHoldFilter;

                    //オブジェクトにメッセージを送る
                    if (signaling)
                    {
                        leftCatchedObject.SendMessage("OnCatchedLeftHand");
                    }
                }
            }
            else
            {
                if (leftCatchedObject != null)
                {
                    //解除して親に戻す
                    leftCatchedObject.transform.parent = leftCatchedObjectParent;

                    //掴むために物理演算を切る
                    var rigid = leftCatchedObject.GetComponent<Rigidbody>();
                    if (rigid != null)
                    {
                        //IsKinematicを保存していた設定にする
                        rigid.isKinematic = leftCatchedObjectIsKinematic;

                        //投げるために速度を転送する
                        rigid.velocity = leftLastSpeed;
                    }

                    //フィルタ解除
                    exrcv.BoneFilter = NonHoldFilter;

                    //オブジェクトにメッセージを送る
                    if (signaling)
                    {
                        leftCatchedObject.SendMessage("OnReleasedLeftHand");
                    }
                }
            }
        }

        void CatchRight(bool s)
        {
            if (s)
            {
                if (rightHelper.Trigger && rightHelper.other != null)
                {
                    //コリジョンタグになにか文字が入っていて、対象と一致しない場合は処理しない
                    if (CollisionTag != "" && CollisionTag != rightHelper.other.tag)
                    {
                        return;
                    }
                    //左手ですでに掴んでいるものは掴まない
                    if (rightHelper.other.gameObject.transform.parent == leftSphere.transform)
                    {
                        return;
                    }
                    //右手ですでに掴んでいるものは掴まない
                    if (rightHelper.other.gameObject.transform.parent == rightSphere.transform)
                    {
                        return;
                    }

                    //解除用に保持
                    rightCatchedObject = rightHelper.other.gameObject;

                    //親を保存
                    rightCatchedObjectParent = rightCatchedObject.transform.parent;

                    //手を親に上書き
                    rightCatchedObject.transform.parent = rightSphere.transform;

                    //掴むために物理演算を切る
                    var rigid = rightCatchedObject.GetComponent<Rigidbody>();
                    if (rigid != null)
                    {
                        //IsKinematicを保存
                        rightCatchedObjectIsKinematic = rigid.isKinematic;
                        //設定に関わらずtrueにする
                        rigid.isKinematic = true;
                    }

                    //フィルタ強く
                    exrcv.BoneFilter = InHoldFilter;

                    //オブジェクトにメッセージを送る
                    if (signaling)
                    {
                        rightCatchedObject.SendMessage("OnCatchedRightHand");
                    }
                }
            }
            else
            {
                if (rightCatchedObject != null)
                {
                    //解除して親に戻す
                    rightCatchedObject.transform.parent = rightCatchedObjectParent;

                    //掴むために物理演算を切る
                    var rigid = rightCatchedObject.GetComponent<Rigidbody>();
                    if (rigid != null)
                    {
                        //IsKinematicを保存していた設定にする
                        rigid.isKinematic = rightCatchedObjectIsKinematic;

                        Debug.Log(rightRigidBody.velocity);
                        //投げるために速度を転送する
                        rigid.velocity = rightLastSpeed;
                    }

                    //フィルタ解除
                    exrcv.BoneFilter = NonHoldFilter;

                    //オブジェクトにメッセージを送る
                    if (signaling)
                    {
                        rightCatchedObject.SendMessage("OnReleasedRightHand");
                    }
                }
            }
        }

        public void KeyInputEvent(EVMC4U.KeyInput key)
        {
            if (!StickyMode)
            {
                //Zキーが押されたか
                if (key.name == LeftKey)
                {
                    //つかみ・離し
                    CatchLeft(key.active == 1);
                }
                //Xキー押されたか
                if (key.name == RightKey)
                {
                    //つかみ・離し
                    CatchRight(key.active == 1);
                }
            }
            else {
                if (key.active == 1)
                {
                    //Zキーが押されたか
                    if (key.name == LeftKey)
                    {
                        //つかみ・離し
                        stickyLeft = !stickyLeft;
                        CatchLeft(stickyLeft);
                    }
                    //Xキー押されたか
                    if (key.name == RightKey)
                    {
                        //つかみ・離し
                        stickyRight = !stickyRight;
                        CatchRight(stickyRight);
                    }
                }
            }
        }

        public void ControllerInputEvent(EVMC4U.ControllerInput con)
        {
            //トリガー引かれたか
            if (con.name == ControllerButton)
            {
                if (!StickyMode)
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
                else {
                    if (con.active == 1) {
                        if (con.IsLeft == 1)
                        {
                            //つかみ・離し
                            stickyLeft = !stickyLeft;
                            CatchLeft(stickyLeft);
                        }
                        else
                        {
                            //つかみ・離し
                            stickyRight = !stickyRight;
                            CatchRight(stickyRight);
                        }
                    }
                }
            }
        }
    }
}