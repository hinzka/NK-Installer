# NK Installer

VRChat用アバターが持っているARKitシェイプキーを検知し、VRCFT向けのフェイストラッキング設定を簡単に導入するための Unity Editor ツールです。

VCC / VPM からインストールして使用できます。

※VRChatでフェイストラッキングを利用するには別途VRCFTが必要です。VRChat公式のSelfie Expression（Webカメラトラッキング）では動作しません。

同期するExpression Parametersの消費数は151です。（一部のシェイプキーが欠落していたり、シェイプキー名の登録だけで中身が空だった場合、検知してその分のパラメータを節約します。）

## Installation

VCC（VRChat Creator Companion）にVPMリポジトリを追加し、対象のUnityプロジェクトへ **NK Installer** をインストールしてください。

[📦 Add NK Installer to VCC](https://hinzka.github.io/NK-Installer/)

## Requirements

* VRChat SDK - Avatars
* Unity / VRChat Creator Companion

## Third-Party Software

### OSCmooth

This package includes assets created using or derived from **OSCmooth**.

OSCmooth is licensed under the MIT License.

Original project:
https://github.com/regzo2/OSCmooth

The copyright notice and license text for OSCmooth are included with this package.

See:

`ThirdPartyLicenses/OSCmooth-LICENSE.txt`

## License

The license for NK Installer and the licenses for third-party components are handled separately.

Third-party components remain subject to their respective licenses.
