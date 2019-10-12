# VMC_ExternalReceiver
動作確認済み環境
+ Windows 10
+ UniVRM-0.53.0_6b07
+ uOSC v0.0.2
+ Unity 2018.1.6f1

0. Unityを準備する
1. ExternalReceiver.csをダウンロードして、動かしたいプロジェクトに入れる
2. UniVRMをダウンロードして、動かしたいプロジェクトに入れる  
https://github.com/vrm-c/UniVRM/releases
3. uOSCをダウンロードして、動かしたいプロジェクトに入れる  
https://github.com/hecomi/uOSC/releases
4. 読み込みたいVRMファイル入れて、Sceneに配置する
5. Scene ViewでCreate Empty
6. Inspectorで、ExternalReceiver.csと、uOSC Serverを割り当てる
7. ExternalReceiverに、読み込んだVRMのGameObjectを「Model」に割り当てる
8. uOSC ServerのPortをVirtualMotionCaptureに合わせる(デフォルト: 39539)
9. 再生して実行開始(VirtualMotionCaptureを起動して送信開始状態にしてください)

![配置例](https://github.com/gpsnmeajp/VMC_ExternalReceiver/blob/README-image/img1.png?raw=true)
