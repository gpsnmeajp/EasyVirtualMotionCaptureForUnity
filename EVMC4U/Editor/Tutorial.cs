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
using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using VRM;
namespace EVMC4U
{
    public class Tutorial : EditorWindow
    {
        static Texture2D texture;
        static GUIStyle style = new GUIStyle();
        static int page = 1;
        static AnimFloat anim = new AnimFloat(0.001f);
        const bool check = VRMVersion.MAJOR != 0 || VRMVersion.MINOR != 80;

        [InitializeOnLoadMethod]
        static void InitializeOnLoad()
        {
            if (EditorUserSettings.GetConfigValue("Opened") != "1" || (check && EditorUserSettings.GetConfigValue("VRMCheckCaution") != "1")) {
                Open();
            }
        }

        [MenuItem("EVMC4U/チュートリアル")]
        public static void Open()
        {
            EditorUserSettings.SetConfigValue("Opened", "1");

            var window = GetWindow<Tutorial>();
            window.maxSize = new Vector2(400, 400);
            window.minSize = window.maxSize;

            anim.value = 0.001f;
            anim.speed = 10f;
            anim.target = 0.001f;
            anim.valueChanged = null;
            page = 1;
            texture = null;

            //バージョンチェック
            if (check)
            {
                EditorUserSettings.SetConfigValue("VRMCheckCaution", "1");
                page = 0;
            }
            else {
                EditorUserSettings.SetConfigValue("VRMCheckCaution", "0");
            }
        }

        void OnGUI()
        {
            if (anim.valueChanged == null) {
                var repaintEvent = new UnityEvent();
                repaintEvent.AddListener(() => Repaint());
                anim.valueChanged = repaintEvent;
            }
            if (anim.value > anim.target-0.1f) {
                anim.target = 0.001f;
            }

            //背景描画
            if (texture == null)
            {
                if (page == 0)
                {
                    loadSlide(page);
                }
                else {
                    loadSlide(1);
                }
            }
            if (texture == null)
            {
                GUI.Label(new Rect(10, 10, 300, 300), "チュートリアルの読み込みに失敗しました。\nアセットが移動されている可能性があります。\nUnityPackageの導入からやり直してみてください");
                return;
            }
            else {
                EditorGUI.DrawPreviewTexture(new Rect(0,0, 400, 400), texture);
                EditorGUI.DrawPreviewTexture(new Rect(anim.value, anim.value, 400 - anim.value*2, 400 - anim.value * 2), texture);
            }

            //GUI制御
            switch (page) {
                case 0:
                    agreeButtons();
                    break;
                case 1:
                    toppageButtons();
                    break;
                case 3:
                    urlButton("https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/wiki/%E8%AA%AC%E6%98%8E%E5%8B%95%E7%94%BB");
                    slideButtons();
                    break;
                case 4:
                    urlButton("https://sh-akira.github.io/VirtualMotionCapture/");
                    slideButtons();
                    break;
                case 7:
                    urlButton("https://sh-akira.github.io/VirtualMotionCapture/");
                    slideButtons();
                    break;
                default:
                    slideButtons();
                    break;
            }
        }

        void loadSlide(int p) {
            page = p;
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/EVMC4U/manual/スライド" + p+".png");
            anim.target = 2f;
        }
        void urlButton(string url) {
            if (GUI.Button(new Rect(5, 8, 388, 320), new GUIContent(), style))
            {
                System.Diagnostics.Process.Start(url);
                anim.target = 5f;
            }
        }
        void agreeButtons()
        {
            if (GUI.Button(new Rect(7, 336, 185+ 194, 55), new GUIContent(), style))
            {
                loadSlide(page + 1);
            }
        }
        void slideButtons()
        {
            if (GUI.Button(new Rect(7, 336, 185, 55), new GUIContent(),style))
            {
                loadSlide(page - 1);
            }
            if (GUI.Button(new Rect(200, 336, 194, 55), new GUIContent(),style))
            {
                loadSlide(page + 1);
            }
        }
        void toppageButtons() {
            if (GUI.Button(new Rect(25, 180, 365, 55), new GUIContent(), style))
            {
                loadSlide(page + 1);
            }
            if (GUI.Button(new Rect(25, 180 + 65, 365, 55), new GUIContent(), style))
            {
                System.Diagnostics.Process.Start("https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/wiki");
                anim.target = 5f;
            }
            if (GUI.Button(new Rect(25, 180 + 65 * 2, 365, 55), new GUIContent(), style))
            {
                System.Diagnostics.Process.Start("https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/wiki/Discord");
                anim.target = 5f;
            }
        }

    }
}