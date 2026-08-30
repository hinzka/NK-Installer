// -*- coding: utf-8 -*-
using System.Collections.Generic;
using UnityEngine;

namespace hinzka.FaceTracking
{
    /// <summary>
    /// まばたき制御方式。FXテンプレートにUEFxGeneratorで生成した両方式
    /// (Blink2D / Blink Simple 1D)が同梱されている場合に、どちらを有効にするか選択する。
    /// </summary>
    public enum BlinkControlMode
    {
        /// <summary>Blink2D: v2/EyeLidLeft・Rightを2D Freeformでブレンドし、左右がある程度連動する方式。</summary>
        TwoD = 0,
        /// <summary>Blink Simple 1D: v2/EyeLidLeft・Rightをそれぞれ独立したSimple1Dで駆動する、左右非連動の方式。</summary>
        OneD = 1,
    }

    /// <summary>
    /// 舌アシストで検出頂点を持ち上げる方向。メッシュのローカル座標系によっては
    /// 「上」が必ずしもY+軸とは限らない(モデルによってはZ軸が実質的な上下方向になっている場合がある)ため、
    /// 選択式にしている。
    /// </summary>
    public enum TongueLiftAxis
    {
        PlusY = 0,
        MinusY = 1,
        PlusZ = 2,
        MinusZ = 3,
        PlusX = 4,
        MinusX = 5,
    }

    /// <summary>
    /// 舌アシストの「持ち上げ」形状(TongueUp_Generated)をどう作るか。
    /// アバターが既に舌を持ち上げるシェイプキーを持っている場合は、それを直接使う方が
    /// 頂点検出による自動生成よりも自然な形状になるため、そちらを優先できるようにしている。
    /// </summary>
    public enum TongueLiftSource
    {
        /// <summary>アバターが既に持っている舌持ち上げ用シェイプキーを、指定した強度で流用する。</summary>
        ExistingShapeKey = 0,
        /// <summary>tongueOut等の動きから舌頂点を自動検出し、指定軸方向へ一律に持ち上げる(従来方式)。</summary>
        AutoDetect = 1,
    }


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

        [Tooltip("ショップ名・作者名(任意)。自動選択のマッチングには一切使用されない、\n" +
                 "表示専用のメタデータ。複数のProfileを見比べる際に、どの作者/ショップが\n" +
                 "配布したものかを目視で確認しやすくするためのもの。")]
        public string shopName = "";

        [Tooltip("バージョン名(任意)。例: \"v1.2\"。自動選択のマッチングには一切使用されない、\n" +
                 "表示専用のメタデータ。アバター本体の更新に合わせてProfileを複製・更新した際に、\n" +
                 "どのバージョン向けかを目視で確認しやすくするためのもの。")]
        public string versionName = "";

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

        [Tooltip("目線シェイプキー生成時に追加シェイプキーを有効化する機能自体のON/OFF(既定false)。\n" +
                 "falseの場合、eyeLookBakeShapeNamesを指定していても一切ベイクに反映しない。")]
        public bool useEyeLookBakeShapes = false;

        [Tooltip("目線シェイプキー生成(ボーン回転のベイク)時に、あらかじめ重み100で有効にしておく\n" +
                 "追加シェイプキー名(複数可)。目のハイライト・瞳孔等のサブメッシュを手前に移動させる\n" +
                 "シェイプキーを持つアバターで、そのシェイプキーを有効にした状態を基準にベイクしないと、\n" +
                 "サブメッシュの回転移動量が不足して眼球メッシュを貫通してしまうことがあるための対策。\n" +
                 "生成後は元の重みに戻す。useEyeLookBakeShapesがtrueのときのみ使用される。")]
        public List<string> eyeLookBakeShapeNames = new List<string>();

        [Header("ジェスチャー")]
        [Tooltip("FT有効中にweight=0にするFXレイヤーのインデックス (複数可・旧Profile互換のため残置)")]
        public List<int> gestureLayerIndices = new List<int>();

        [Tooltip("FT有効中にweight=0にするFXレイヤーの名前 (複数可)。" +
                 "FXのレイヤー順が変わっても名前から再解決できるよう、こちらを優先的に使用する。")]
        public List<string> gestureLayerNames = new List<string>();

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
                 "ジェスチャー表情の抑制だけでは競合を解消しきれない場合の最終手段。" +
                 "トレードオフとして、FTオフ時に目が全く動かなくなる。")]
        public bool disableNativeEyeLook = false;

        [Header("まばたき制御方式")]
        [Tooltip("FXテンプレートにUEFxGeneratorで生成した両方式(Blink2D / Blink Simple 1D)が\n" +
                 "同梱されている場合に、どちらを有効にするか選択する。Install時、選択されなかった\n" +
                 "方は自動的に無効化される(専用レイヤーがあればweight=0、Driverへ直接注入されている\n" +
                 "場合は該当ノードを除去)。テンプレートが片方の方式しか持たない場合は無視される。")]
        public BlinkControlMode blinkControlMode = BlinkControlMode.TwoD;

        [Header("眉アシスト")]
        [Tooltip("眉トラッキング非搭載デバイス向けに、標準ARKit眉シェイプキー(browInnerUp等)を複製した" +
                 "sub_brow*シェイプキーを生成し、まばたき(v2/EyeLidLeft・v2/EyeLidRight)に連動させる。")]
        public bool generateBrowAssistShapes = false;

        [Range(0f, 1f)]
        [Tooltip("眉アシスト全体の強度。1.0で標準ARKit眉シェイプと同じ最大量まで動く。")]
        public float browAssistIntensity = 0.5f;

        [Header("舌アシスト")]
        [Tooltip("tongueOut(及びtongueOutStep1・tongueOutStep2等の段階シェイプ)の動きから舌の頂点を" +
                 "自動検出し、持ち上げた形状とのミックスシェイプキーを生成する。Install時、FX内で" +
                 "各ポーズシェイプ(tongueOut / tongueOutStepN)を駆動しているカーブは、対応する" +
                 "ミックスシェイプキーへ自動的に差し替えられる。")]
        public bool generateTongueAssistShapes = false;

        [Tooltip("「持ち上げ」形状(TongueUp_Generated)の作り方。ExistingShapeKey(既定): アバターが\n" +
                 "既に持っている舌持ち上げ用シェイプキーを指定して流用する(頂点検出より自然な形状に\n" +
                 "なりやすい)。AutoDetect: tongueOut等の動きから頂点を自動検出し、指定軸方向へ\n" +
                 "一律に持ち上げる(従来方式)。")]
        public TongueLiftSource tongueLiftSource = TongueLiftSource.ExistingShapeKey;

        [Tooltip("tongueLiftSource=ExistingShapeKeyのとき使用する、アバターが既に持っている\n" +
                 "舌持ち上げ用シェイプキーの名前(接頭辞は含めない実際の名前)。")]
        public string tongueExistingLiftShapeName = "";

        [Range(0f, 100f)]
        [Tooltip("tongueLiftSource=ExistingShapeKeyのとき使用する、既存シェイプキーをどれだけの強さで\n" +
                 "流用するか(%)。100で既存シェイプキーの変形量をそのまま使う。\n" +
                 "この持ち上げ形状は、v2/TongueOutが0%→100%へ遷移する間、50%地点(唇を越える\n" +
                 "タイミング)で最大になるよう自動的に組み込まれる(0%・100%では持ち上げ無し)。")]
        public float tongueExistingLiftShapeWeight = 100f;

        [Tooltip("検出した舌頂点を持ち上げる移動量(ローカル空間、tongueLiftAxis方向)。" +
                 "tongueLiftSource=AutoDetectのときのみ使用する。")]
        public float tongueMoveAmount = 0.01f;

        [Tooltip("検出頂点を持ち上げる軸(ローカル空間)。メッシュによっては「上」がY+軸ではなく、\n" +
                 "Z軸(奥行方向)などになっている場合がある。TongueUpAmountを上げたときに舌が\n" +
                 "奥や横に動いてしまう場合は、実際に見た目で正しい方向に切り替えてください。\n" +
                 "tongueLiftSource=AutoDetectのときのみ使用する。")]
        public TongueLiftAxis tongueLiftAxis = TongueLiftAxis.PlusY;

        [Tooltip("舌検出の閾値。ワールド空間の実寸(mm)で指定する。既定値1.0mmでtongue系シェイプキーの\n" +
                 "頂点移動が拾えない(検出頂点数が0になる)場合は、小さくしてください。\n" +
                 "アーマチュアのスケールがアバターごとに異なっても、自動的にメッシュ空間へ換算される。\n" +
                 "tongueLiftSource=AutoDetectのときのみ使用する。")]
        public float tongueDetectThresholdMm = 1.0f;

        [Tooltip("舌候補から除外する唇・歯系シェイプキーの判定閾値。ワールド空間の実寸(mm)で指定する。\n" +
                 "tongueLiftSource=AutoDetectのときのみ使用する。")]
        public float tongueLipExcludeThresholdMm = 0.5f;

        [Tooltip("歯除外を主判定(tongueOut)にも適用するかどうか(既定false)。歯とキーワード一致する\n" +
                 "シェイプキーが舌の可動域と大きく重なっているアバターでは、trueだと本来検出されるべき\n" +
                 "舌の頂点まで巻き込んで消えてしまうことがあるため、必要な場合にのみONへ切り替える。\n" +
                 "tongueLiftSource=AutoDetectのときのみ使用する。")]
        public bool tongueExcludeTeethFromPrimary = false;

        [Tooltip("mm→メッシュ空間への変換係数の手動上書き。0以下なら自動推定(バウンディングボックス比)を使う。\n" +
                 "自動推定がアバターによって大きく外れる場合(検出頂点数が0のまま/意図しない部位を拾う等)に指定する。\n" +
                 "tongueLiftSource=AutoDetectのときのみ使用する。")]
        public float tongueUnitOverride = 0f;

        [Tooltip("舌アシスト(AutoDetectモード)で、検出した頂点の移動前後をScene Viewにプレビュー表示するか(既定true)。")]
        public bool showTonguePreview = true;

        [Tooltip("Scene Viewプレビューで表示する点のサイズ(既定0.08)。")]
        public float tonguePreviewPointScale = 0.08f;

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
