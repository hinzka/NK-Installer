// -*- coding: utf-8 -*-
using System.Collections.Generic;
using UnityEngine;

namespace hinzka.FaceTracking
{
    /// <summary>
    /// ARKit FaceTracking インストーラーの設定を保存するProfile。
    /// ScriptableObjectとして .asset ファイルに保存できる。
    /// </summary>
    [CreateAssetMenu(
        fileName = "ARKitFTProfile",
        menuName = "hinzka/FaceTracking/ARKit FT Profile")]
    public class ARKitFTProfile : ScriptableObject
    {
        [Header("アバター自動選択")]
        [Tooltip("Avatarを選択したとき、この文字列のいずれかがAvatar名に含まれていれば最優先で" +
                 "このProfileを自動選択する。カンマ区切りで複数指定できる。\n" +
                 "空なら未使用(その場合はファイル名からの推測にフォールバックする)。\n" +
                 "バージョン名や接頭辞・接尾辞が付いたAvatar名でも確実に対応付けたい場合は、" +
                 "本質的な識別文字列だけを指定しておくとよい。")]
        public string avatarMatchTag = "";

        [Header("顔メッシュ")]
        [Tooltip("FXアニメーションが参照するSMRのヒエラルキーパス (例: Body)")]
        public string faceSMRPath = "Body";

        [Tooltip("ARKitシェイプキーに独自の接頭辞が付いている場合の接頭辞文字列(例: 'custom_')。空なら接頭辞なし。")]
        public string arkitShapePrefix = "";

        [Tooltip("ARKit標準名のシェイプキーが見つからない場合、VRCFaceTrackingの" +
                 "「Unified Expressions」側の代替名でも検索するか。")]
        public bool ueFallbackEnabled = false;

        [Tooltip("中身が空(頂点差分が実質ゼロ)のARKitシェイプキーに対応するExpression Parametersの" +
                 "同期を自動的にオフにするか。")]
        public bool disableSyncForEmptyShapes = false;

        [Header("目ボーンコンストレイント (上級者向け)")]
        [Tooltip("目ボーンがコンストレイント経由で動いている場合の、実際の適用先Transformの" +
                 "アバタールートからの相対パス。空なら未使用。")]
        public string leftEyeConstraintPath = "";
        public string rightEyeConstraintPath = "";

        [Tooltip("表情メッシュと目メッシュが別々のSkinnedMeshRendererかどうか。" +
                 "有効な場合、EyeLookシェイプキーはeyeSMRPath側へ生成し、" +
                 "Install完了時にAAO Merge Skinned Meshヘルパーを自動作成する。")]
        public bool eyeSmrSeparate = false;

        [Tooltip("目メッシュのSMRヒエラルキーパス (eyeSmrSeparateが有効な場合のみ使用)")]
        public string eyeSMRPath = "";

        [Header("にっこり目")]
        [Tooltip("にっこり目のシェイプキー名 (複数可)")]
        public List<string> squintShapeNames = new List<string>();

        [Header("ジェスチャー")]
        [Tooltip("FT有効中にweight=0にするFXレイヤーのインデックス (複数可・旧Profile互換のため残置)")]
        public List<int> gestureLayerIndices = new List<int>();

        [Tooltip("FT有効中にweight=0にするFXレイヤーの名前 (複数可)。" +
                 "FXのレイヤー順が変わっても名前から再解決できるよう、こちらを優先的に使用する。")]
        public List<string> gestureLayerNames = new List<string>();

        [Tooltip("false(既定): MouthTrackingが有効なときだけジェスチャー表情を抑制する。\n" +
                 "true: EyeTracking・MouthTrackingのどちらか一方でも有効なら抑制する。")]
        public bool gestureSuppressOnEyesOrMouth = false;

        [Header("Viseme補償")]
        public bool generateVisemeCompensation = true;

        [Tooltip("AvatarDescriptorに登録されたVisemeシェイプキーの変化量をこの倍率に縮小する。" +
                 "1.0で変更なし。アバター作者のVisemeが大きすぎてFTと組み合わせて破綻する場合に下げる。")]
        [Range(0.3f, 1f)]
        public float visemeScale = 1f;

        [Header("EyeLook自動生成")]
        [Tooltip("AvatarDescriptorのEye Look角度(目ボーンの回転)から、eyeLook系8シェイプキー" +
                 "(eyeLookUp/Down/In/OutLeft/Right)のうち不足しているものを自動生成する。")]
        public bool generateEyeLookShapes = true;

        [Tooltip("視線シェイプキーの強度。1.0が等倍。実際のアイトラッキングが" +
                 "AvatarDescriptorの角度まで届かないことが多い場合に上げる。")]
        public float eyeLookIntensity = 1f;

        [Tooltip("AvatarDescriptorのEye Look機能自体を無効化するか。" +
                 "ジェスチャー変化等をきっかけにVRChat標準の目線制御へ一時的にフォールバックしてしまう" +
                 "問題を、フォールバック先自体を無くすことで防ぐ。トレードオフとして、FTオフ時に" +
                 "目が全く動かなくなる。")]
        public bool disableNativeEyeLook = false;

        [Header("眉アシスト")]
        [Tooltip("眉トラッキング非搭載デバイス向けに、標準ARKit眉シェイプキー(browInnerUp等)を複製した" +
                 "sub_brow*シェイプキーを生成し、まばたき(v2/EyeLidLeft・v2/EyeLidRight)に連動させる。")]
        public bool generateBrowAssistShapes = false;

        [Range(0f, 1f)]
        [Tooltip("眉アシスト全体の強度。1.0で標準ARKit眉シェイプと同じ最大量まで動く。")]
        public float browAssistIntensity = 0.5f;

        [Header("まばたきエフェクト (おまけ)")]
        [Tooltip("まばたきするたびに1回だけ再生される「おまけ」のアニメーションを追加するか。\n" +
                 "テンプレートFXにはデフォルトの演出(瞳のうるうる)が同梱されている。")]
        public bool addBlinkEffect = false;

        [Tooltip("まばたきエフェクトとして再生するAnimationClip。\n" +
                 "未指定(null)の場合はテンプレートFXに同梱されているデフォルトの演出をそのまま使う。\n" +
                 "指定した場合、そのクリップのコピーがFXへ埋め込まれ、まばたき検出時に1回だけ再生される。")]
        public AnimationClip blinkEffectClip;

        [Header("出力先")]
        public string outputFolder = "Assets/NK_Installer_Generated";
    }
}
