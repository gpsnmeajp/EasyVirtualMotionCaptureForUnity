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

//掴まれたとき: 手の位置に持つ
//離されたとき: 鞘の位置なら、収まるべき場所に戻る
//              そうでないなら、親なしにしてとどまる

public class HandCatch_WeaponHelper : MonoBehaviour
{
    public Transform LeftHoldPosition; //左手保持位置
    public Transform RightHoldPosition; //右手保持位置

    //初期位置(鞘に収まっている状態)
    Vector3 CasePosition;
    Quaternion CaseRotation;

    void Start()
    {
        CasePosition = transform.localPosition;
        CaseRotation = transform.localRotation;
    }

    void OnCatchedLeftHand()
    {
        Debug.Log("C:L");
        transform.localPosition = LeftHoldPosition.localPosition;
        transform.localRotation = LeftHoldPosition.localRotation;
    }
    void OnCatchedRightHand()
    {
        Debug.Log("C:R");
        transform.localPosition = RightHoldPosition.localPosition;
        transform.localRotation = RightHoldPosition.localRotation;
    }

    void OnReleasedLeftHand()
    {
        Debug.Log("R:L");
        transform.localPosition = CasePosition;
        transform.localRotation = CaseRotation;
    }

    void OnReleasedRightHand()
    {
        Debug.Log("R:R");
        transform.localPosition = CasePosition;
        transform.localRotation = CaseRotation;
    }

}
