# EasyVirtualMotionCaptureForUnity
VirtualMotionCaptureのUDP姿勢情報を受信してUnityシーンに反映します。

sh_akiraさんのVirtualMotionCapture(OSC対応機能搭載版)が必要です。  
https://sh-akira.booth.pm/items/999760  
https://sh-akira.github.io/VirtualMotionCapture/  
https://github.com/sh-akira/VirtualMotionCapture  

動作確認済み環境
+ Windows 10
+ UniVRM-0.53.0_6b07
+ uOSC v0.0.2
+ Unity 2018.1.6f1

**注意！**  
**VMCとUnityで同じVRMを読み込むようにしてください！**  

# ExternalReceiverPackを使う場合(かんたん)
0. Unityを準備する
1. ExternalReceiverPackをダウンロードして新しい3Dプロジェクトに入れる
https://sabowl.sakura.ne.jp/gpsnmeajp/vmc/ExternalReceiverPack_v2_1.unitypackage
2. 読み込みたいVRMファイル入れて、ExternalReceiverSceneを開いて配置する(あるいはExternalReceiverプレハブを配置する)
3. Scene ViewでExternalReceiverに、読み込んだVRMのGameObjectを「Model」に割り当てる
4. 再生して実行開始(VirtualMotionCaptureを起動してOSC送信開始状態にしてください)

以下を同梱しています。
+ [UniVRM-0.53.0_6b07(MIT Licence)](https://github.com/vrm-c/UniVRM/blob/master/LICENSE.txt)
+ [uOSC v0.0.2(MIT Licence)](https://github.com/hecomi/uOSC/blob/master/README.md)

![配置例](https://github.com/gpsnmeajp/VMC_ExternalReceiver/blob/README-image/img2.png?raw=true)

# 一から準備する場合
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

# 送信側の例
https://github.com/gpsnmeajp/VirtualMotionCapture/blob/v0.22basefix/Assets/ExternalSender/ExternalSender.cs
