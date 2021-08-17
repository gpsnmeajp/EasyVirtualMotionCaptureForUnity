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
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using VRM;
using DVRSDK.Auth;
using DVRSDK.Avatar;
using DVRSDK.Serializer;
using DVRSDK.Utilities;

namespace EVMC4U
{
    public class DVRReceiver : MonoBehaviour, IExternalReceiver
    {
        [Header("DVRReceiver v1.2")]
        public ExternalReceiver externalReceiver;
        public string API_KEY;

        [HideInInspector]
        public bool Login = false;
        [HideInInspector]
        public bool Logout = false;

        [SerializeField]
        private string StatusMessage = "";  //Inspector表示用
        [SerializeField]
        private string VerificationCode = "";  //Inspector表示用

        [Header("Daisy Chain")]
        public GameObject[] NextReceivers = new GameObject[1];

        private ExternalReceiverManager externalReceiverManager = null;
        bool shutdown = false;

        string oldjson = "";

        //同期コンテキスト
        SynchronizationContext synchronizationContext;

        [Serializable]
        public class dmmvrconnect {
            public string user_id;
            public string avatar_id;
        }

        void LoadVRM(dmmvrconnect info) {
            externalReceiver.DestroyModel();

            synchronizationContext.Post(async (arg) => {
                var vrmLoader = new VRMLoader();
                var currentUser = await Authentication.Instance.Okami.GetCurrentUserAsync();
                var myUser = await Authentication.Instance.Okami.GetUserAsync(currentUser.id);
                var currentAvatar = myUser.current_avatar;

                if (info == null) {
                    info = new dmmvrconnect { user_id = currentUser.id, avatar_id = currentAvatar.id };
                }

                var avatar = await Authentication.Instance.Okami.GetAvatarAsync(info.user_id, info.avatar_id);

                if (externalReceiver.Model != null)
                {
                    Destroy(externalReceiver.Model);
                }
                externalReceiver.Model = await Authentication.Instance.Okami.LoadAvatarVRMAsync(avatar, vrmLoader.LoadVRMModelFromConnect) as GameObject;

                //ExternalReceiverの下にぶら下げる
                if (externalReceiver.LoadedModelParent != null)
                {
                    Destroy(externalReceiver.LoadedModelParent);
                }
                externalReceiver.LoadedModelParent = new GameObject();
                externalReceiver.LoadedModelParent.transform.SetParent(externalReceiver.transform, false);
                externalReceiver.LoadedModelParent.name = "LoadedModelParent";
                //その下にモデルをぶら下げる
                externalReceiver.Model.transform.SetParent(externalReceiver.LoadedModelParent.transform, false);

                vrmLoader.ShowMeshes();

                //カメラなどの移動補助のため、頭の位置を格納する
                var animator = externalReceiver.Model.GetComponent<Animator>();
                externalReceiver.HeadPosition = animator.GetBoneTransform(HumanBodyBones.Head).position;
            }, null);
        }

        void Start()
        {
            synchronizationContext = SynchronizationContext.Current;
            externalReceiverManager = new ExternalReceiverManager(NextReceivers);
            StatusMessage = "Waiting for Master...";
            VerificationCode = "*** Please login ***";
            Login = false;
            Logout = false;

            string api_key = API_KEY.Replace("*", "");

            if (string.IsNullOrEmpty(api_key)) {
                StatusMessage = "Error";
                VerificationCode = "*** No API KEY ***";
                shutdown = true;
                return;
            }
            
            var config = new DVRAuthConfiguration(api_key, new UnitySettingStore(), new UniWebRequest(), new NewtonsoftJsonSerializer());
            Authentication.Instance.Init(config);
            Authentication.Instance.TryAutoLogin((bool ok)=> {
                if (ok)
                {
                    VerificationCode = "*** Login OK ***";
                    LoadVRM(null);
                }
            });
        }

        //デイジーチェーンを更新
        public void UpdateDaisyChain()
        {
            externalReceiverManager.GetIExternalReceiver(NextReceivers);
        }

        void Update()
        {
            if (Login && !shutdown)
            {
                Login = false;

                Authentication.Instance.Authorize(
                openBrowser: (OpenBrowserResponse openBrowserResponse) =>
                {
                    Application.OpenURL(openBrowserResponse.VerificationUri);
                    VerificationCode = openBrowserResponse.UserCode;
                },
                onAuthSuccess: isSuccess =>
                {
                    if (isSuccess)
                    {
                        VerificationCode = "*** Login OK ***";
                        LoadVRM(null);
                    }
                    else
                    {
                        VerificationCode = "*** Login Failed ***";
                        externalReceiver.DestroyModel();
                    }
                },
                onAuthError: exception =>
                {
                    VerificationCode = exception.ToString();
                    Debug.LogError(exception);
                });
            }
            if (Logout && !shutdown)
            {
                Logout = false;
                externalReceiver.DestroyModel();
                Authentication.Instance.DoLogout();
                VerificationCode = "*** Logout ***";
            }
        }

        public void MessageDaisyChain(ref uOSC.Message message, int callCount)
        {
            //Startされていない場合無視
            if (externalReceiverManager == null || enabled == false || gameObject.activeInHierarchy == false)
            {
                return;
            }

            if (shutdown)
            {
                return;
            }

            StatusMessage = "OK";

            //異常を検出して動作停止
            try
            {
                ProcessMessage(ref message);
            }
            catch (Exception e)
            {
                StatusMessage = "Error: Exception";
                Debug.LogError(" --- Communication Error ---");
                Debug.LogError(e.ToString());
                shutdown = true;
                return;
            }

            if (!externalReceiverManager.SendNextReceivers(message, callCount))
            {
                StatusMessage = "Infinite loop detected!";
                shutdown = true;
            }
        }

        private void ProcessMessage(ref uOSC.Message message)
        {
            //メッセージアドレスがない、あるいはメッセージがない不正な形式の場合は処理しない
            if (message.address == null || message.values == null)
            {
                StatusMessage = "Bad message.";
                return;
            }

            //リモートVRM基本情報 v3.0
            if (message.address == "/VMC/Ext/Remote"
                && (message.values[0] is string) //service
                && (message.values[1] is string) //json
                )
            {
                string service = (string)message.values[0];
                string json = (string)message.values[1];

                //変化がないなら無視
                if (oldjson == json)
                {
                    return;
                }
                oldjson = json;

                //DMM VR Connect
                if (service == "dmmvrconnect" && json != null) {
                    Debug.Log(json);
                    dmmvrconnect decoded = JsonUtility.FromJson<dmmvrconnect>(json);
                    LoadVRM(decoded);
                }
            }
        }

        private void OnValidate()
        {
            if (0 < API_KEY.Length && API_KEY.Length < 300) {
                string fill = new string('*', 300);

                API_KEY = API_KEY.Replace("*", "");
                API_KEY = fill + API_KEY;
            }
        }
    }
}