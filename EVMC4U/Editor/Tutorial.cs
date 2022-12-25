/*
 * ExternalReceiver
 * https://sabowl.sakura.ne.jp/gpsnmeajp/
 *
 * MIT License
 * 
 * Copyright (c) 2020-2022 gpsnmeajp
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
#pragma warning disable CS0162
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using VRM;
using System.Linq;

namespace EVMC4U
{
    public class Tutorial : EditorWindow
    {
        const bool check = VRMVersion.MAJOR != 0 || VRMVersion.MINOR != 99;
        const int window_w = 400;
        const int window_h = 400;

        //ページ名
        static string page = "";

        //ボタン押下アニメーション
        static AnimFloat anim = new AnimFloat(0.001f);
        static string animTargetName = ""; //1つのボタンだけアニメさせる識別名

        static string jsonError = "";

        static TutorialJson tutorialJson = null;
        static Dictionary<string, TutorialPage> tutorialPages = new Dictionary<string, TutorialPage>();

        //JSON設定ファイル定義
        [Serializable]
        private class TutorialJson
        {
            public bool debug;
            public TutorialPage[] pages;
            public override string ToString()
            {
                return "TutorialJson debug:" + debug + " pages:" + pages.Length;
            }
        }

        //JSONページ定義
        [Serializable]
        private class TutorialPage
        {
            public string name = "";
            public string text = "";
            public string image = "";
            public TutorialButton[] buttons = new TutorialButton[0];

            public override string ToString()
            {
                return "TutorialPage name:" + name + " text:" + text + " iamge:" + image + " buttons:" + buttons.Length;
            }
        }

        //JSONボタン定義
        [Serializable]
        private class TutorialButton
        {
            public int x = 0;
            public int y = 0;
            public int w = 0;
            public int h = 0;

            public string text = "";

            public string image = "";
            public string uri = ""; //"page://" = page, "http://" or "https://" = url
            public string fire = ""; //event
            public override string ToString()
            {
                return "TutorialButton (" + x + "," + y + "," + w + "," + h + ") text:" + text + " image:" + image + " uri:" + uri;
            }
        }

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            //一度も開いたことない場合は、ここで開く
            if (EditorUserSettings.GetConfigValue("Opened") != "1" || (check && EditorUserSettings.GetConfigValue("VRMCheckCaution") != "1"))
            {
                Open();
            }
        }

        [MenuItem("EVMC4U/Oepn Tutorial")]
        public static void Open()
        {
            //ウィンドウサイズを固定
            var window = GetWindow<Tutorial>();
            window.maxSize = new Vector2(window_w, window_h - 6);
            window.minSize = window.maxSize;

            //アニメーション定義
            anim.value = 0.001f;
            anim.speed = 10f;
            anim.target = 0.001f;
            anim.valueChanged = null;

            if (Resources.Load<TextAsset>("tutorial/define") == null)
            {
                //読み込み準備ができていない
                return;
            }

            //ページを初期位置に設定
            page = "start";
            if (EditorUserSettings.GetConfigValue("Language") == "ja")
            {
                page = "start_ja";
            }
            if (EditorUserSettings.GetConfigValue("Language") == "en")
            {
                page = "start_en";
            }


            //データを読み込む
            tutorialPages = new Dictionary<string, TutorialPage>();

            try
            {
                jsonError = "";
                var r = Resources.Load<TextAsset>("tutorial/define");
                tutorialJson = JsonUtility.FromJson<TutorialJson>(r.text);
                if (tutorialJson.debug)
                {
                    Debug.Log(tutorialJson);
                }

                //各ページのデータを読み込む
                foreach (var p in tutorialJson.pages)
                {
                    tutorialPages.Add(p.name, p);
                    if (tutorialJson.debug)
                    {
                        Debug.Log(p);
                    }
                }

                //一度開いたのを覚えておく
                EditorUserSettings.SetConfigValue("Opened", "1");
            }
            catch (ArgumentException e)
            {
                //Debug.LogError(e);
                jsonError = e.ToString();
                tutorialJson = null;
            }

            //バージョンチェック(失敗したら失敗ページに飛ばす)
            if (check)
            {
                EditorUserSettings.SetConfigValue("VRMCheckCaution", "1");
                page = "versionCheckFailed";
            }
            else
            {
                EditorUserSettings.SetConfigValue("VRMCheckCaution", "0");
            }
        }

        [MenuItem("EVMC4U/Reset Language")]
        public static void ResetLanguage()
        {
            EditorUserSettings.SetConfigValue("Language", "");
            Open();
        }

        void OnGUI()
        {
            //ページを開いたまま初期化されたら、初期ロード処理に飛ばす
            if (page == "")
            {
                GUI.Label(new Rect(10, 10, window_w, window_h), "INVALID STATE\n\nチュートリアルの読み込みに失敗しました。Unityを再起動してください。\nそれでもダメな場合は、UnityPackageの導入からやり直してみてください\n\nTutorial load failed.\nPlease restart Unity.\nor Please re-import UnityPackage.");
                Open();
                return;
            }

            //アニメーションを立ち上げる
            if (anim.valueChanged == null)
            {
                var repaintEvent = new UnityEvent();
                repaintEvent.AddListener(() => Repaint());
                anim.valueChanged = repaintEvent;
            }

            //アニメーション折り返し
            if (anim.value > anim.target - 0.1f)
            {
                anim.target = 0.001f;
            }

            //ページの表示処理を開始
            TutorialPage tutorialPage;
            if (!tutorialPages.TryGetValue(page, out tutorialPage))
            {
                //JSONが多分バグってるときに表示
                GUI.Label(new Rect(10, 10, window_w - 20, window_h), "JSON LOAD FAILED\n" + jsonError + "\n\nチュートリアルの読み込みに失敗しました。Unityを再起動してください。\nそれでもダメな場合は、UnityPackageの導入からやり直してみてください\n\nTutorial load failed.\nPlease restart Unity.\nor Please re-import UnityPackage.");
                if (GUI.Button(new Rect(0, window_h - 30, window_w, 30), "Reload"))
                {
                    Open();
                }
                return;
            }

            //デバッグログ
            if (tutorialJson.debug)
            {
                Debug.Log("OnGUI: " + anim.value);
                Debug.Log(tutorialPage);
            }

            //背景画像があれば表示
            if (tutorialPage.image != "")
            {
                var bgtexture = Resources.Load<Texture>("tutorial/" + tutorialPage.image);
                EditorGUI.DrawPreviewTexture(new Rect(0, 0, window_w, window_h), bgtexture);
            }

            //ページのテキストを表示(代替テキスト)
            GUI.Label(new Rect(0, 0, window_w, window_h), tutorialPage.text);

            //ボタンを1つずつ表示
            foreach (var b in tutorialPage.buttons)
            {
                if (tutorialJson.debug)
                {
                    Debug.Log(b);
                }

                //ボタンに画像があればそれを表示
                if (b.image != "")
                {
                    //画像を読み込む
                    var texture = Resources.Load<Texture>("tutorial/" + b.image);

                    //位置情報がない場合、下端として扱う
                    if (b.x == 0 && b.y == 0 && b.w == 0 && b.h == 0)
                    {
                        b.y = window_h - window_w * texture.height / texture.width;
                        b.w = window_w;
                    }

                    string buttonName = "btn#" + page + "#" + b.x + "-" + b.y + "-" + b.w + "-" + b.h;
                    float height = b.w * texture.height / texture.width;

                    Rect r = new Rect(b.x, b.y, b.w, height);

                    //アニメ対象の場合だけ動く
                    if (buttonName == animTargetName)
                    {
                        r = new Rect(b.x + anim.value, b.y + anim.value, b.w, height);
                    }

                    //ボタンを表示
                    if (GUI.Button(r, texture, new GUIStyle()))
                    {
                        //アニメーション処理と、遷移を実行
                        buttonFireProcess(b.fire);
                        buttonUriProcess(b.uri);
                        animTargetName = buttonName;
                        anim.target = 2f;
                    }
                }
                else
                {
                    //テキストボタンを表示
                    if (GUI.Button(new Rect(b.x, b.y, b.w, b.h), b.text))
                    {
                        buttonFireProcess(b.fire);
                        buttonUriProcess(b.uri);
                    }
                }
            }

            //デバッグ再読み込みボタン
            if (tutorialJson.debug)
            {
                if (GUI.Button(new Rect(0, window_h - 30, 30, 30), "#"))
                {
                    Open();
                }
            }
        }

        void buttonUriProcess(string uri)
        {
            if (tutorialJson.debug)
            {
                Debug.LogWarning("buttonProcess: " + uri);
            }

            if (uri == null)
            {
                return;
            }
            if (uri.StartsWith("page://"))
            {
                page = uri.Replace("page://", "");
            }
            if (uri.StartsWith("http://") || uri.StartsWith("https://"))
            {
                System.Diagnostics.Process.Start(uri);
            }
        }

        void buttonFireProcess(string fire)
        {
            switch (fire)
            {
                case "SaveLanguageJa":
                    {
                        EditorUserSettings.SetConfigValue("Language", "ja");
                        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(';').ToList();
                        if (symbols.Contains("EVMC4U_EN"))
                        {
                            symbols.Remove("EVMC4U_EN");
                        }
                        if (!symbols.Contains("EVMC4U_JA"))
                        {
                            symbols.Add("EVMC4U_JA");
                        }
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, String.Join(";", symbols.ToArray()));

                        break;
                    }
                case "SaveLanguageEn":
                    {
                        EditorUserSettings.SetConfigValue("Language", "en");
                        var symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Split(';').ToList();
                        if (symbols.Contains("EVMC4U_JA"))
                        {
                            symbols.Remove("EVMC4U_JA");
                        }
                        if (!symbols.Contains("EVMC4U_EN"))
                        {
                            symbols.Add("EVMC4U_EN");
                        }
                        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, String.Join(";", symbols.ToArray()));

                        break;
                    }
                default: break;
            }
        }
    }
}