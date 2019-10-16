![icon](https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/blob/README-image/ExternalReceiver.gif?raw=true)
# EasyVirtualMotionCaptureForUnity(ExternalReceiver)
VirtualMotionCaptureからOSCで姿勢情報を受信してUnityシーンに反映します。  
  
あなたのUnityシーンをUnityPackage1つで全身トラッキング対応にできます。  
トラッキング品質は安心と信頼のバーチャルモーションキャプチャー同等。  
プロトコルは公開されており、自由に拡張可能。  

ハッシュタグは  
#EasyVirtualMotionCaptureForUnity  
#EVMC4U  

# 使い方動画
https://youtu.be/L5dkdnk5c9A

# 前提環境
sh_akiraさんのVirtualMotionCapture(v0.36～)が必要です。  
https://sh-akira.github.io/VirtualMotionCapture/  

**※旧バージョン(～v0.35)では使えません！！！！**  
先行リリース版はFanboxで提供されています。  
https://www.pixiv.net/fanbox/creator/10267568

動作確認済み環境
+ Windows 10
+ UniVRM-0.53.0_6b07
+ uOSC v0.0.2
+ Unity 2018.1.6f1 (5.6.3p1以上)
+ Steam VR
+ HTC Vive

なお、[EasyMotionRecorder](https://github.com/duo-inc/EasyMotionRecorder)を使うことで、モーションの記録も可能になります。  
注意点があります。下の**よくある質問**をご確認ください。  

# お問合せ先
discordサーバー: https://discord.gg/QSrDhE8  
twitter: https://twitter.com/@seg_faul  

**注意！**  
**VMCとUnityで同じVRMを読み込むようにしてください！**  

# 使い方
## ExternalReceiverPackを使う場合(かんたん)
動画: https://youtu.be/L5dkdnk5c9A

0. Unityを準備する
1. ExternalReceiverPackをダウンロードして新しい3Dプロジェクトに入れる  
[最新版](https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/releases)  
[安定版](https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/releases/tag/2.6)  
2. 読み込みたいVRMファイル入れて、ExternalReceiverSceneを開いて配置する(あるいはExternalReceiverプレハブを配置する)
3. Scene ViewでExternalReceiverに、読み込んだVRMのGameObjectを「Model」に割り当てる
4. 再生して実行開始(VirtualMotionCaptureを起動してOSC送信開始状態にしてください)

以下を同梱しています。
+ [UniVRM-0.53.0_6b07(MIT Licence)](https://github.com/vrm-c/UniVRM/blob/master/LICENSE.txt)
+ [uOSC v0.0.2(MIT Licence)](https://github.com/hecomi/uOSC/blob/master/README.md)

![配置例](https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/blob/README-image/img6.png?raw=true)

## 一から準備する場合
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

## オプション
![オプション](https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/blob/README-image/img6.png?raw=true)
### Synchronize Option
**Blend Sharp Synchronize**  
VRMのBlendSharp(表情・リップシンクなど)を同期するか(既定でtrue:する)  

**RootPositionSynchronize**  
Rootの**位置**情報を同期するか(既定でtrue:する)  
複数体同時に動かしたり、位置を制御したい場合はfalseにすると便利です。  
※v2.8で姿勢→位置になっています  
  
**RootRotationSynchronize**  
Rootの**回転**情報を同期するか(既定でtrue:する)  
  
**BonePositionSynchronize**  
ボーンの位置情報を同期するか(既定でtrue:する)  
  
### UI Option
**ShowInformation**  
通信状況表示をするか(既定でfalse:しない)

### Filter Option
**BonePositionFilterEnable**  
ボーンの位置にローパスフィルタを掛けるか(既定でfalse:しない)  
  
**BoneRotationFilterEnable**  
ボーンの回転にローパスフィルタを掛けるか(既定でfalse:しない)  
  
**filter**  
ローパスフィルタ係数(0～1:既定で0.7)  
1に近いほど過去の影響が強くなる  

**Status Message**  
現在の状態が表示されます

**Daisy Chain**  
デイジーチェーンとしてデータを横流しする先を設定します。  
  
**uOSC Serverを持たない**ExternalReceiverはスレーブモードで動作します。  
これは、親となるExternalReceiverからデータを横流ししてもらうモードです。  
  
**uOSC Serverを持つ**ExternalReceiverから順にNextに登録することで、  
ExternalReceiverを連結することができます。  
これにより、複数体のアバターを同時に動かすことができるようになります。  
**なお、無限ループを形成しないようにご注意ください**
  
![Daisy Chain](https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/blob/README-image/Daisy.png?raw=true)
  
## よくある質問
### EasyMotionRecorderを使うと動きが変
VRMの方にApply Root Motionのチェックが入っているかを確認してください。  
これがオンになっていないと、その場でくるくる回転してしまいます。  
  
また、再生の際は、Motion Data Player(Script)ではなく、animファイル生成を行って確認してください。  
[EasyMotionRecorder](https://github.com/duo-inc/EasyMotionRecorder)

![Root](https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/blob/README-image/img5.png?raw=true)


### Blend Sharp Synchronizeって何？オフにしていい？  
基本的にオンにしておいてください。  
オンのとき、VRMのBlendSharp(表情・リップシンクなど)を同期します。  
VRMじゃないモデルを動かしたいときや、表情などを同期したくないときにオフにします。  
  
### RootPositionSynchronizeって何？オフにしていい？  
~~基本的にオンにしておいてください。  
3点トラッキングのときはオフにして大丈夫ですが、フルトラの場合はオンにしてください。  
オンのとき、ルームスケールの位置を同期します。  
オフにすると、腰を中心に浮いたような挙動になり、フルトラ時にとても不自然になります。~~  

オフにして構いません。
ただし、ルームスケールでの移動ができなくなり、オブジェクトの原点に固定されます。

### VRMの位置を動かしたいからRootPositionSynchronizeオフにするね！  
~~オンにしておいてください。  
代わりに、VRMを何らかのオブジェクトの子にし、そのオブジェクトを動かしてください。~~  
  
オフにして構いません。特に複数体のアバターを動かすときはオフにすると便利です。  

### RootRotationSynchronizeって何？オフにしていい？  
ルームスケールでの回転ができなくなります。  
カメラに常に向かせたいときなどはオフにしても構いません  

### BonePositionSynchronizeって何？オフにしていい？  
UnityとVMCで全く同じVRMを読み込んでいるときはオンにすると首の位置などの精度が上がります。  
一方で、UnityとVMCでちょっとでもボーン位置が違うモデルを読み込むと酷い壊れ方をします。(指が伸びる、首が伸びるなど)  
~~通常は互換性のためにオフにします。~~   
腰が不自然に浮く問題が発生するため、v2.6より既定でオンになりました。  

### ShowInformationって何？オンにしていい？  
オンにすると、通信状況がGameビューに表示されます。  
VMCと通信できているか気になるときはオンにしてください。  
VMCでVRMモデルを読み込んでいないとAvailableが0になります。
通信が切れているとTimeが動かなくなります。  

### Status Messageが「Waiting for VMC...」
VMCと通信できていないときに発生します。  
VMCの詳細設定から「OSCでモーション送信を有効にする」を有効にしてください。  

### Status Messageが「Waiting for Master...」
uOSC Serverがないためデイジーチェーン状態になっています。  
デイジーチェーンの場合、親となるExternalReceiverのNextに登録されているか  
親となるExternalReceiverが受信できているかチェックしてください。  

### Status Messageが「Waiting for [Load VRM]」
VMCでVRMを読み込んでください  

### BonePositionFilterEnable, BoneRotationFilterEnableって何？オンにしていい？    
オンにすると、簡易的にボーンの回転にローパスフィルタを掛けます。  
手ブレやガクつきをなくしたい場合にオンにしてください。  
  
フィルタのかけ具合は、Filterを0.0～1.0の間で調整することで変更できます。  
なお、指の動きにも効いてしまいます。  

### 別のウィンドウを選択するとUnityがフリーズしたようになる
動作が一時停止し、通信がたまり過ぎると発生します。  
VMCの「OSCでモーション送信を有効にする」をオフにするか、VMCを終了すると復帰します。  
UnityのPlayer settingから「Run in Background」を有効にすると発生しなくなります。
  
v2.8からは強制的にRun in Backgroundが有効になり、この現象は発生しません。  

# How to use
## ExternalReceiverPack (Easy)
0. Open Unity project.
1. Download ExternalReceiverPack and install.  
[Latest](https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/releases)  
[Stable](https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/releases/tag/2.6)  
2. Drag&Drop your VRM file, and Open "ExternalReceiverScene", Place VRM Model.  
 (or put "ExternalReceiver" prefab on your scene)
3. put VRM Model game object on ExternalReceiver's "Model" in Scene View.
4. Let's Play. (And turn on VirtualMotionCaputres OSC Function)

## Manualy Setup
0. Open Unity project.
1. Download "ExternalReceiver.cs" and put in.
2. Download "UniVRM" and put in.
https://github.com/vrm-c/UniVRM/releases
3. Download "uOSC" and put in.
https://github.com/hecomi/uOSC/releases
4. Drag&Drop your VRM file, and Place VRM Model.  
5. "Create Empty" in Scene View.
6. Attach "ExternalReceiver.cs" and "uOSC Server"
7. put VRM Model game object on ExternalReceiver's "Model" in Scene View.
8. Set uOSC Server's "Port" to VirtualMotionCapture's Port. (Default: 39539)
9. Let's Play. (And turn on VirtualMotionCaputres OSC Function)

# Protocol
https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/wiki/Protocol-V2

# 送信側の例
https://github.com/gpsnmeajp/VirtualMotionCapture/blob/v0.22basefix/Assets/ExternalSender/ExternalSender.cs

# Licence
CC0  
http://creativecommons.org/publicdomain/zero/1.0/deed.ja  

# 作者
gpsnmeajp  
https://sabowl.sakura.ne.jp/gpsnmeajp/  
