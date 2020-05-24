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
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HiResolutionPhotoCamera : MonoBehaviour {
    //撮影したい解像度
    public int width = 4096;
    public int height = 2160;

    //撮影したいカメラ
    public Camera cam;

    //撮影ボタン
    public bool shot = false;
	
	void Update () {
        //撮影ボタンが押されたら撮影する
        if (shot) {
            shot = false;
            TakePhoto();
        }
	}

    void TakePhoto() {
        width = 8192;
        height = (int)((float)8192 * (float)Screen.height/ (float)Screen.width);

        //撮影したい解像度のRenderテクスチャを作成
        var renderTexture = new RenderTexture(width, height, 24);
        //アクティブなレンダーテクスチャを保存
        var save = RenderTexture.active;

        //カメラに描画対象を設定
        cam.targetTexture = renderTexture;
        //ReadPixelsの取得元(アクティブなレンダーテクスチャ)を設定
        RenderTexture.active = renderTexture;

        //即座にレンダリングする
        cam.Render();

        //テクスチャを生成して読み取り
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        //テクスチャをpngファイルに保存
        byte[] data = texture.EncodeToPNG();
        File.WriteAllBytes("output" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss-ff")+ ".png", data);

        //破棄
        DestroyImmediate(texture);

        //カメラの描画対象を元に戻す
        cam.targetTexture = null;
        //アクティブなレンダーテクスチャを復元
        RenderTexture.active = save;
    }
}
