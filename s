[1mdiff --git a/com.hinzka.nkinstaller/CHANGELOG.md b/com.hinzka.nkinstaller/CHANGELOG.md[m
[1mindex 7012bfb..825dcf7 100644[m
[1m--- a/com.hinzka.nkinstaller/CHANGELOG.md[m
[1m+++ b/com.hinzka.nkinstaller/CHANGELOG.md[m
[36m@@ -24,4 +24,22 @@[m
 -  Visemee制御タイミング修正[m
 - 対象FXレイヤーが0番目の場合、ジェスチャー抑制が正しく機能しないことがある不具合を修正[m
 - VRCFTが強制終了した際、Mouth側のトラッキングも正しく自動停止するように[m
[31m-- 出力フォルダに不要な中間生成メッシュが残り続ける問題を修正[m
\ No newline at end of file[m
[32m+[m[32m- 出力フォルダに不要な中間生成メッシュが残り続ける問題を修正[m
[32m+[m
[32m+[m[32m## [1.2.0][m
[32m+[m
[32m+[m[32m### 新機能・改善[m
[32m+[m
[32m+[m[32m- 複数Templateに対応[m
[32m+[m[32m- 同期パラメータを削減（現151bit⇒141bit）[m
[32m+[m[32m- さらに同期パラメータを削減したTemplateを追加（126bit）[m
[32m+[m[32m- Cross-Eyed(寄り眼)対応版のTemplateを追加[m
[32m+[m[32m- 目線BlendShapeの生成時に中間シェイプキーを追加[m
[32m+[m[32m- 左右のまばたきを完全同期するオプションを追加[m
[32m+[m[32m- Profile追加（計48個）[m
[32m+[m
[32m+[m[32m### 不具合修正[m
[32m+[m
[32m+[m[32m- カクつき修正（ローカル動作をFloat参照に変更）[m
[32m+[m[32m- トラッキングON／OFF値の保存処理修正[m
[32m+[m[32m- 既存パラメータ数のカウント処理を修正（空白のエントリは無視する）[m
\ No newline at end of file[m
[1mdiff --git a/com.hinzka.nkinstaller/Editor/ARKitFaceTrackingInstallerWindow.cs b/com.hinzka.nkinstaller/Editor/ARKitFaceTrackingInstallerWindow.cs[m
[1mindex 48e07c2..1bd3b40 100644[m
[1m--- a/com.hinzka.nkinstaller/Editor/ARKitFaceTrackingInstallerWindow.cs[m
[1m+++ b/com.hinzka.nkinstaller/Editor/ARKitFaceTrackingInstallerWindow.cs[m
[36m@@ -126,6 +126,35 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             ["表情メッシュと目メッシュが別々"] = new[] { "Face mesh and eye mesh are separate", "表情网格与眼部网格是分离的", "표정 메시와 눈 메시가 분리됨" },[m
             ["にっこり目"] = new[] { "Smile Eyes", "笑眼", "스마일 아이" },[m
             ["任意のシェイプキーを「にっこり目」として指定できます。\n未指定の場合はARKitのeyeSquintLeft・eyeSquintRightが設定されます。"] = new[] { "You can designate any shape key as \"Smile Eyes\".\nIf left unspecified, ARKit's eyeSquintLeft/eyeSquintRight will be used.", "可以将任意形态键指定为“笑眼”。\n如果未指定，将使用ARKit的eyeSquintLeft・eyeSquintRight。", "임의의 쉐이프 키를 \"스마일 아이\"로 지정할 수 있습니다.\n지정하지 않으면 ARKit의 eyeSquintLeft・eyeSquintRight가 사용됩니다。" },[m
[32m+[m[32m            ["視線ベイク分割数"] = new[] { "Eye Bake Stages", "视线烘焙分段数", "시선 베이크 분할 수" },[m
[32m+[m[32m            ["テンプレートセット"] = new[] { "Template Set", "模板集", "템플릿 세트" },[m
[32m+[m[32m            ["左右のまばたきを完全に同期させる"] = new[] {[m
[32m+[m[32m                "Fully sync left/right blinking", "完全同步左右眨眼", "좌우 눈 깜빡임을 완전히 동기화"[m
[32m+[m[32m            },[m
[32m+[m[32m            ["ONにすると、片方の目のトラッキング値だけを使って両目を同じタイミング・同じ量で開閉させます。左右の開閉タイミングがズレるのが気になる場合に使ってください。\nもう片方の目の独立した動き(ウインク等)は再現されなくなります。"] = new[] {[m
[32m+[m[32m                "When on, both eyes open and close with the same timing and amount, using only one eye's tracking value. Use this if you're bothered by the left/right timing being out of sync.\nThe other eye's independent movement (such as winking) will no longer be reproduced.",[m
[32m+[m[32m                "开启后，仅使用一只眼睛的追踪数值，让双眼以相同的时机和幅度开闭。如果在意左右开闭时机不一致，可以使用此选项。\n另一只眼睛的独立动作(如眨单眼)将不再被还原。",[m
[32m+[m[32m                "켜면 한쪽 눈의 트래킹 값만 사용하여 양쪽 눈이 같은 타이밍・같은 양으로 개폐됩니다. 좌우 개폐 타이밍이 어긋나는 것이 신경 쓰일 때 사용하세요.\n반대쪽 눈의 독립적인 움직임(윙크 등)은 더 이상 재현되지 않습니다."[m
[32m+[m[32m            },[m
[32m+[m[32m            ["採用する目"] = new[] { "Source Eye", "采用的眼睛", "채택할 눈" },[m
[32m+[m[32m            ["左目"] = new[] { "Left Eye", "左眼", "왼쪽 눈" },[m
[32m+[m[32m            ["右目"] = new[] { "Right Eye", "右眼", "오른쪽 눈" },[m
[32m+[m[32m            ["同期パラメータ：{0}bit"] = new[] { "Synced params: {0}bit", "同步参数：{0}bit", "동기화 파라미터：{0}bit" },[m
[32m+[m[32m            ["同期パラメータ数を取得できませんでした"] = new[] {[m
[32m+[m[32m                "Could not retrieve synced parameter count",[m
[32m+[m[32m                "无法获取同步参数数量",[m
[32m+[m[32m                "동기화 파라미터 수를 가져올 수 없습니다"[m
[32m+[m[32m            },[m
[32m+[m[32m            ["目線シェイプキーをベイクする際、rest姿勢(正面)から到達姿勢(見た方向)までの回転を、何段階に分けて近似するかを指定します。\n1(既定)は従来通り、rest→到達の2点間を直線で結ぶだけの近似です。\n値を大きくすると、その間に中間姿勢を追加でベイクし、回転の弧(円弧軌道)をより正確に再現します。特に目の可動域が大きいデフォルメアバターで、視線を動かした際の見た目の破綻が軽減されます。\n実行時の負荷はほぼ変わりません(SkinnedMeshRendererは常に隣接する2フレーム間だけを補間するため)。メッシュのデータサイズは段階数にほぼ比例して増えます。\nまず3程度から試すことをお勧めします。"] = new[] {[m
[32m+[m[32m                "Specifies how many stages to split the rotation into when baking eye-look shape keys, from the rest pose (facing forward) to the target pose (looking direction).\n1 (default) is the same as before: a simple straight-line approximation between the rest and target points.\nHigher values bake additional intermediate poses in between, more accurately reproducing the arc of rotation. This especially reduces visual glitches when moving the gaze on deformed avatars with a large eye range of motion.\nRuntime cost barely changes (SkinnedMeshRenderer always interpolates between only the two adjacent frames). Mesh data size increases roughly in proportion to the stage count.\nWe recommend starting around 3.",[m
[32m+[m[32m                "烘焙视线形态键时，指定将从rest姿势(正面)到目标姿势(视线方向)之间的旋转分成几个阶段来近似。\n1(默认)与以往相同,只是rest与目标两点之间的直线近似。\n数值越大，会在中间额外烘焙中间姿势,更准确地还原旋转的弧线(圆弧轨迹)。尤其是在眼睛可动范围较大的Q版化身上，移动视线时的形状崩坏会得到缓解。\n运行时负荷几乎不变(SkinnedMeshRenderer始终只在相邻的两帧之间插值)。网格数据大小会大致与阶段数成比例增加。\n建议先从3左右开始尝试。",[m
[32m+[m[32m                "시선 쉐이프 키를 베이크할 때, rest 자세(정면)에서 도달 자세(시선 방향)까지의 회전을 몇 단계로 나누어 근사할지 지정합니다.\n1(기본값)은 기존과 동일하게 rest→도달 두 점 사이를 직선으로 잇는 근사입니다.\n값을 크게 하면 그 사이에 중간 자세를 추가로 베이크하여, 회전의 호(원호 궤도)를 더 정확하게 재현합니다. 특히 눈의 가동 범위가 큰 디포르메 아바타에서, 시선을 움직였을 때의 형태 붕괴가 줄어듭니다.\n실행 시 부하는 거의 변하지 않습니다(SkinnedMeshRenderer는 항상 인접한 두 프레임 사이만 보간하기 때문입니다). 메시 데이터 크기는 단계 수에 거의 비례하여 증가합니다.\n먼저 3 정도부터 시도해 보는 것을 추천합니다."[m
[32m+[m[32m            },[m
[32m+[m[32m            ["目線の回転が大きいと、生成したシェイプキーの中間形状が歪むことがあります。中間シェイプキーを増やすことで、本来の回転に近づけることができます。"] = new[] {[m
[32m+[m[32m                "If the eye rotation is large, the in-between shape of the generated shape key can look distorted. Adding more intermediate shape keys brings it closer to the true rotation.",[m
[32m+[m[32m                "当眼睛旋转角度较大时，生成的形态键中间形状可能会变形。通过增加中间形态键，可以使其更接近原本的旋转。",[m
[32m+[m[32m                "눈의 회전이 크면 생성된 쉐이프 키의 중간 형태가 일그러질 수 있습니다. 중간 쉐이프 키를 늘리면 본래의 회전에 더 가깝게 만들 수 있습니다."[m
[32m+[m[32m            },[m
             ["アバターを選択するとShape Keyを指定できます。"] = new[] { "Select an avatar to specify Shape Keys.", "选择角色后即可指定Shape Key。", "아바타를 선택하면 Shape Key를 지정할 수 있습니다." },[m
             ["検索"] = new[] { "Search", "搜索", "검색" },[m
             ["＋ Shape Keyを追加"] = new[] { "+ Add Shape Key", "＋ 添加Shape Key", "+ Shape Key 추가" },[m
[36m@@ -417,6 +446,9 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         private Transform _rightEyeConstraintTarget;[m
         private bool _disableNativeEyeLook = false; // false=標準Eye Lookを維持、true=無効化(ラジオボタンで選択)[m
         private float _eyeLookIntensity = 1f;[m
[32m+[m[32m        // 目線シェイプキーのベイク時、rest→target間を何段階に分けてSlerpするか(1=多段化なし・[m
[32m+[m[32m        // 従来通りの直線補間。値が大きいほど、回転角の大きいシェイプで弧を正確に近似できる)。[m
[32m+[m[32m        private int _eyeLookStageCount = 3;[m
         // 目線シェイプキー生成(ボーン回転のベイク)時、あらかじめ重み100で有効にしておく[m
         // 追加シェイプキー(_shapeNamesのインデックス、複数選択可)。目のハイライト・瞳孔等の[m
         // サブメッシュを手前に移動させるシェイプキーを持つアバターで、そのシェイプキーを[m
[36m@@ -455,9 +487,15 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         private int _estimatedTotalParamBits = 0;[m
         private int _estimatedExistingParamBits = 0;[m
         private int _estimatedFtParamBits = 0;[m
[32m+[m[32m        private int _estimatedFtParamCount = 0;[m
         private bool _estimatedParamBitsOverBudget = false;[m
         private bool _disableSyncForEmptyShapes = false;[m
         private ArkitShapeParameterMap _shapeParameterMap;[m
[32m+[m[32m        // 選択中のテンプレートセット(FX/Parameters/Menu/ShapeParamMapの組)が置かれているフォルダ。[m
[32m+[m[32m        // 空文字列の場合は「セットが1つも見つからない、または未選択」を意味し、[m
[32m+[m[32m        // 後方互換としてプロジェクト全体からの名前検索(FindTemplate)にフォールバックする。[m
[32m+[m[32m        private string _selectedTemplateSetFolder = "";[m
[32m+[m[32m        private List<TemplateSetOption> _availableTemplateSets = new List<TemplateSetOption>();[m
         private string[] _fxLayerNames = Array.Empty<string>();[m
         private List<string> _eyeTrackingControlLayerNames = new List<string>();[m
         private string _gestureSearchQuery = "";[m
[36m@@ -472,6 +510,10 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         // まばたき制御方式(Blink2D / Blink Simple 1D)。テンプレートFXに両方式が同梱されている[m
         // 場合、Install時に選択されなかった方を無効化する。[m
         private BlinkControlMode _blinkControlMode = BlinkControlMode.TwoD;[m
[32m+[m[32m        // 左右のまばたきを、片方の目のトラッキング値だけで同期させるかどうか。[m
[32m+[m[32m        private bool _syncBlinkLeftRight = false;[m
[32m+[m[32m        // trueなら右目、falseなら左目のトラッキング値を両目に採用する。[m
[32m+[m[32m        private bool _syncBlinkUseRightAsSource = false;[m
 [m
         // 眉アシスト[m
         private bool _generateBrowAssistShapes = false;[m
[36m@@ -538,6 +580,10 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         private VisualElement _uiAvatarMatchTagRow;[m
         private TextField _uiProfileShopNameField;[m
         private TextField _uiProfileVersionNameField;[m
[32m+[m[32m        private VisualElement _uiTemplateSetRow;[m
[32m+[m[32m        private DropdownField _uiTemplateSetField;[m
[32m+[m[32m        private VisualElement _uiTemplateSetHint;[m
[32m+[m[32m        private Label _uiTemplateSetParamCountLabel;[m
         private VisualElement _uiProfileMetaRow;[m
         private VisualElement _uiProfileReadyBanner;[m
         private Label _uiProfileReadyBannerLabel;[m
[36m@@ -584,10 +630,14 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         private VisualElement _uiBlinkModeCard;[m
         private Toggle _uiBlinkSimple1DToggle;[m
         private VisualElement _uiBlinkModeHint;[m
[32m+[m[32m        private Toggle _uiSyncBlinkToggle;[m
[32m+[m[32m        private DropdownField _uiSyncBlinkSourceField;[m
 [m
         private Toggle _uiEyeLookToggle;[m
         private Slider _uiEyeLookSlider;[m
         private FloatField _uiEyeLookValue;[m
[32m+[m[32m        private SliderInt _uiEyeLookStageCountSlider;[m
[32m+[m[32m        private IntegerField _uiEyeLookStageCountValue;[m
         private VisualElement _uiEyeLookDetail;[m
         private Toggle _uiEyeConstraintToggle;[m
         private VisualElement _uiEyeConstraintFields;[m
[36m@@ -802,9 +852,9 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         private static readonly string[] TONGUE_LIP_EXCLUDE_SHAPES =[m
             { "mouthRollUpper", "mouthRollLower", "cheekPuff" };[m
         // TongueOutSteps_BT配下の専用クリップ名の接頭辞。標準の汎用Binding[m
[31m-        // (hinzkaUE_Bind_v2_TongueOut等)と区別するために使う。[m
[31m-        private const string TONGUE_STEP_CLIP_PREFIX = "hinzkaUE_TongueStep_";[m
[31m-        private const string TONGUE_GAIN_TREE_NAME = "hinzkaUE_Gain_v2_TongueOut";[m
[32m+[m[32m        // (hinzkaNK_Bind_v2_TongueOut等)と区別するために使う。[m
[32m+[m[32m        private const string TONGUE_STEP_CLIP_PREFIX = "hinzkaNK_TongueStep_";[m
[32m+[m[32m        private const string TONGUE_GAIN_TREE_NAME = "hinzkaNK_Gain_v2_TongueOut";[m
         // 標準の舌駆動BlendTree(UEFxGeneratorが全ARKitシェイプ共通で生成する、汎用の[m
         // "Gain_v2_<パラメータ名>"命名規則のtongueOut版)の固定名。持ち上げエンベロープを[m
         // 組み込む際、このBlendTreeをfx内から名前で探して直接組み替える。[m
[36m@@ -812,8 +862,8 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         // ── まばたき制御方式(Blink2D / Blink Simple 1D) ──────────────────[m
         // UEFxGeneratorのUEFxGenConfigデフォルト命名規則に合わせた、専用レイヤー名。[m
         // LegacySeparateLayer配置の場合、この名前のレイヤーが見つかれば無効化対象にする。[m
[31m-        private const string BLINK_2D_LAYER_NAME = "UE_Blink2D";[m
[31m-        private const string BLINK_SIMPLE1D_LAYER_NAME = "UE_BlinkSimple1D";[m
[32m+[m[32m        private const string BLINK_2D_LAYER_NAME = "NK_Blink2D";[m
[32m+[m[32m        private const string BLINK_SIMPLE1D_LAYER_NAME = "NK_BlinkSimple1D";[m
         // InMainDriverDirect配置(Direct BlendTreeへの直接注入)の場合、Direct BlendTreeの[m
         // 子Motionの名前にこの文字列が含まれていれば無効化対象にする[m
         // (BrowModeSwitch/Modulationでラップされていない、素の状態でのみ確実に検出できる)。[m
[36m@@ -829,10 +879,23 @@[m [mnamespace hinzka.FaceTracking.Editor[m
 [m
         private void OnEnable()[m
         {[m
[32m+[m[32m            // テンプレートセット(複数用意されている場合、フォルダ単位で切り替えられる)を発見する。[m
[32m+[m[32m            // 1つも見つからない場合は、従来通りプロジェクト全体からの名前検索にフォールバックする[m
[32m+[m[32m            // (単一セットのみを配置している既存ユーザーとの後方互換のため)。[m
[32m+[m[32m            _availableTemplateSets = DiscoverTemplateSets();[m
[32m+[m[32m            _selectedTemplateSetFolder = _availableTemplateSets.Count > 0 ? _availableTemplateSets[0].folderPath : "";[m
[32m+[m
             // ARKitシェイプ⇔OSCmoothパラメータの対応表(開発者側でFXジェネレータから生成・配置済み)を[m
             // 自動で読み込む。エンドユーザーが指定する必要はない。見つからなければnullのままで、[m
             // その場合は部分一致による判定にフォールバックする。[m
[31m-            _shapeParameterMap = FindTemplate<ArkitShapeParameterMap>("ARKit_FT_ShapeParamMap.asset");[m
[32m+[m[32m            _shapeParameterMap = !string.IsNullOrEmpty(_selectedTemplateSetFolder)[m
[32m+[m[32m                ? FindTemplateInFolder<ArkitShapeParameterMap>(_selectedTemplateSetFolder, "NK_FT_ShapeParamMap.asset")[m
[32m+[m[32m                : FindTemplate<ArkitShapeParameterMap>("NK_FT_ShapeParamMap.asset");[m
[32m+[m
[32m+[m[32m            // ウィンドウを開いた時点(まだアバター未選択の可能性もある)でも、テンプレート自体の[m
[32m+[m[32m            // パラメータ数/bit数をプルダウンの下に表示できるよう、ここでも計算しておく。[m
[32m+[m[32m            RefreshParameterBudgetEstimate();[m
[32m+[m
             SceneView.duringSceneGui += OnTongueSceneGUI;[m
         }[m
 [m
[36m@@ -1191,6 +1254,57 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             _uiProfileMetaRow = metaRow;[m
             hero.Add(metaRow);[m
 [m
[32m+[m[32m            // テンプレートセット(FX/Parameters/Menu/ShapeParamMapの組)が複数用意されている[m
[32m+[m[32m            // 場合のみ、選択用のドロップダウンを表示する。1つしか無い場合は選ぶ意味が無いため[m
[32m+[m[32m            // 非表示にする(単一セットのみを配置している既存ユーザーには何も変わって見えない)。[m
[32m+[m[32m            var templateSetRow = new VisualElement();[m
[32m+[m[32m            templateSetRow.AddToClassList("toolbar-row");[m
[32m+[m[32m            templateSetRow.style.flexDirection = FlexDirection.Row;[m
[32m+[m
[32m+[m[32m            // ドロップダウン本体は1行分の高さしかない一方、右側の概要テキストが2行になると[m
[32m+[m[32m            // ドロップダウンの下に1行分の余白ができる。この余白へ、同期パラメータのbit数を[m
[32m+[m[32m            // 小さく添える(別行に大きな箱を作らない)。[m
[32m+[m[32m            var templateSetLeftColumn = new VisualElement();[m
[32m+[m[32m            templateSetLeftColumn.style.flexGrow = 1;[m
[32m+[m[32m            templateSetLeftColumn.style.flexShrink = 1;[m
[32m+[m[32m            templateSetLeftColumn.style.flexBasis = 0;[m
[32m+[m
[32m+[m[32m            _uiTemplateSetField = new DropdownField(ArkitFTLoc.T("テンプレートセット"));[m
[32m+[m[32m            _uiTemplateSetField.RegisterValueChangedCallback(evt =>[m
[32m+[m[32m            {[m
[32m+[m[32m                var chosen = _availableTemplateSets.FirstOrDefault(s => s.displayName == evt.newValue);[m
[32m+[m[32m                _selectedTemplateSetFolder = chosen?.folderPath ?? "";[m
[32m+[m[32m                // テンプレートセットを切り替えると、参照するNK_FT_Parameters.assetも変わるため、[m
[32m+[m[32m                // bit予算の見積りを再計算する(以前はここが抜けており、切り替え後も直前の[m
[32m+[m[32m                // セットの見積りが残ったままになっていた)。[m
[32m+[m[32m                RefreshParameterBudgetEstimate();[m
[32m+[m[32m                RefreshToolkitUI();[m
[32m+[m[32m            });[m
[32m+[m[32m            templateSetLeftColumn.Add(_uiTemplateSetField);[m
[32m+[m
[32m+[m[32m            _uiTemplateSetParamCountLabel = new Label("");[m
[32m+[m[32m            _uiTemplateSetParamCountLabel.style.marginTop = 2;[m
[32m+[m[32m            _uiTemplateSetParamCountLabel.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.6f));[m
[32m+[m[32m            // ドロップダウン内蔵ラベル("テンプレートセット")の実際の描画幅と揃うよう、[m
[32m+[m[32m            // 左マージンを動的に同期する(固定値だとテーマ・言語切替で値の開始位置とズレるため)。[m
[32m+[m[32m            _uiTemplateSetField.labelElement.RegisterCallback<GeometryChangedEvent>(evt =>[m
[32m+[m[32m            {[m
[32m+[m[32m                // ラベル文字自体の幅に加えて、ラベルと値の間の内部余白(Unity標準フィールドの[m
[32m+[m[32m                // label-input間ギャップ)の分だけ足りないため、少し余分に足す。[m
[32m+[m[32m                _uiTemplateSetParamCountLabel.style.marginLeft = evt.newRect.width + 6f;[m
[32m+[m[32m            });[m
[32m+[m[32m            templateSetLeftColumn.Add(_uiTemplateSetParamCountLabel);[m
[32m+[m
[32m+[m[32m            templateSetRow.Add(templateSetLeftColumn);[m
[32m+[m
[32m+[m[32m            _uiTemplateSetHint = MakeHint("", "soft");[m
[32m+[m[32m            _uiTemplateSetHint.style.flexGrow = 1;[m
[32m+[m[32m            _uiTemplateSetHint.style.flexShrink = 1;[m
[32m+[m[32m            _uiTemplateSetHint.style.flexBasis = 0;[m
[32m+[m[32m            templateSetRow.Add(_uiTemplateSetHint);[m
[32m+[m[32m            _uiTemplateSetRow = templateSetRow;[m
[32m+[m[32m            hero.Add(templateSetRow);[m
[32m+[m
             _uiHeaderHost.Add(hero);[m
 [m
             // Profileが読み込まれている場合、「このままInstallできます」という目立つ帯を[m
[36m@@ -1547,6 +1661,26 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             eyeSliderRow.name = "eye-strength-row";[m
             _uiEyeLookDetail.Add(eyeSliderRow);[m
 [m
[32m+[m[32m            var eyeStageCountRow = MakeIntSlider([m
[32m+[m[32m                ArkitFTLoc.T("視線ベイク分割数"), 1, 8,[m
[32m+[m[32m                out _uiEyeLookStageCountSlider, out _uiEyeLookStageCountValue,[m
[32m+[m[32m                value => _eyeLookStageCount = value);[m
[32m+[m[32m            eyeStageCountRow.name = "eye-stage-count-row";[m
[32m+[m[32m            string eyeLookStageCountTooltip = ArkitFTLoc.T([m
[32m+[m[32m                "目線シェイプキーをベイクする際、rest姿勢(正面)から到達姿勢(見た方向)までの回転を、何段階に分けて近似するかを指定します。\n" +[m
[32m+[m[32m                "1(既定)は従来通り、rest→到達の2点間を直線で結ぶだけの近似です。\n" +[m
[32m+[m[32m                "値を大きくすると、その間に中間姿勢を追加でベイクし、回転の弧(円弧軌道)をより正確に再現します。特に目の可動域が大きいデフォルメアバターで、視線を動かした際の見た目の破綻が軽減されます。\n" +[m
[32m+[m[32m                "実行時の負荷はほぼ変わりません(SkinnedMeshRendererは常に隣接する2フレーム間だけを補間するため)。メッシュのデータサイズは段階数にほぼ比例して増えます。\n" +[m
[32m+[m[32m                "まず3程度から試すことをお勧めします。");[m
[32m+[m[32m            eyeStageCountRow.tooltip = eyeLookStageCountTooltip;[m
[32m+[m[32m            _uiEyeLookStageCountSlider.tooltip = eyeLookStageCountTooltip;[m
[32m+[m[32m            _uiEyeLookStageCountValue.tooltip = eyeLookStageCountTooltip;[m
[32m+[m[32m            _uiEyeLookDetail.Add(eyeStageCountRow);[m
[32m+[m[32m            _uiEyeLookDetail.Add(MakeHint([m
[32m+[m[32m                ArkitFTLoc.T("目線の回転が大きいと、生成したシェイプキーの中間形状が歪むことがあります。" +[m
[32m+[m[32m                "中間シェイプキーを増やすことで、本来の回転に近づけることができます。"),[m
[32m+[m[32m                "soft"));[m
[32m+[m
             _uiEyeLookDetail.Add(MakeHint([m
                 ArkitFTLoc.T("フェイストラッキングで動く目線のシェイプキーを、アバターのEyeLook設定から自動生成します。\n" +[m
                 "EyeLook Strengthを大きくするとわずかな動きにも敏感に反応します。\n" +[m
[36m@@ -1734,6 +1868,29 @@[m [mnamespace hinzka.FaceTracking.Editor[m
                 "soft");[m
             card.Add(_uiBlinkModeHint);[m
 [m
[32m+[m[32m            // 左右まばたき完全同期(片方の目のトラッキング値だけで両目を揃える)。[m
[32m+[m[32m            // 上のBlink2D/Simple1D選択とは独立した、追加のオプション。[m
[32m+[m[32m            _uiSyncBlinkToggle = new Toggle(ArkitFTLoc.T("左右のまばたきを完全に同期させる"));[m
[32m+[m[32m            _uiSyncBlinkToggle.tooltip =[m
[32m+[m[32m                ArkitFTLoc.T("ONにすると、片方の目のトラッキング値だけを使って両目を同じタイミング・同じ量で" +[m
[32m+[m[32m                "開閉させます。左右の開閉タイミングがズレるのが気になる場合に使ってください。\n" +[m
[32m+[m[32m                "もう片方の目の独立した動き(ウインク等)は再現されなくなります。");[m
[32m+[m[32m            _uiSyncBlinkToggle.RegisterValueChangedCallback(evt =>[m
[32m+[m[32m            {[m
[32m+[m[32m                _syncBlinkLeftRight = evt.newValue;[m
[32m+[m[32m                RefreshToolkitUI();[m
[32m+[m[32m            });[m
[32m+[m[32m            card.Add(_uiSyncBlinkToggle);[m
[32m+[m
[32m+[m[32m            _uiSyncBlinkSourceField = new DropdownField([m
[32m+[m[32m                ArkitFTLoc.T("採用する目"),[m
[32m+[m[32m                new List<string> { ArkitFTLoc.T("左目"), ArkitFTLoc.T("右目") }, 0);[m
[32m+[m[32m            _uiSyncBlinkSourceField.RegisterValueChangedCallback(evt =>[m
[32m+[m[32m            {[m
[32m+[m[32m                _syncBlinkUseRightAsSource = (evt.newValue == ArkitFTLoc.T("右目"));[m
[32m+[m[32m            });[m
[32m+[m[32m            card.Add(_uiSyncBlinkSourceField);[m
[32m+[m
             _uiTrackingPage.Add(card);[m
         }[m
 [m
[36m@@ -3039,6 +3196,33 @@[m [mnamespace hinzka.FaceTracking.Editor[m
                 _uiProfileMetaRow.style.display = _profile != null ? DisplayStyle.Flex : DisplayStyle.None;[m
             _uiAvatarField?.SetValueWithoutNotify(_avatarPrefab);[m
 [m
[32m+[m[32m            // テンプレートセットが2つ以上ある場合のみ選択UIを表示する。[m
[32m+[m[32m            if (_uiTemplateSetRow != null)[m
[32m+[m[32m            {[m
[32m+[m[32m                bool showSetSelector = _availableTemplateSets.Count > 1;[m
[32m+[m[32m                _uiTemplateSetRow.style.display = showSetSelector ? DisplayStyle.Flex : DisplayStyle.None;[m
[32m+[m[32m                if (showSetSelector && _uiTemplateSetField != null)[m
[32m+[m[32m                {[m
[32m+[m[32m                    var names = _availableTemplateSets.Select(s => s.displayName).ToList();[m
[32m+[m[32m                    _uiTemplateSetField.choices = names;[m
[32m+[m[32m                    var current = _availableTemplateSets.FirstOrDefault(s => s.folderPath == _selectedTemplateSetFolder);[m
[32m+[m[32m                    _uiTemplateSetField.SetValueWithoutNotify(current?.displayName ?? (names.Count > 0 ? names[0] : ""));[m
[32m+[m[32m                    if (_uiTemplateSetHint != null && _uiTemplateSetHint.childCount > 0 &&[m
[32m+[m[32m                        _uiTemplateSetHint[0] is Label setHintLabel)[m
[32m+[m[32m                        setHintLabel.text = current?.description ?? "";[m
[32m+[m[32m                }[m
[32m+[m[32m            }[m
[32m+[m
[32m+[m[32m            // 選択中テンプレートの同期パラメータ数(個数・bit数)を表示する。[m
[32m+[m[32m            // テンプレートセットのプルダウンが1つしかなく非表示の場合でも、この行だけは表示する[m
[32m+[m[32m            // (単一セット環境でも、テンプレートが実際に何bit使うのかは有用な情報のため)。[m
[32m+[m[32m            if (_uiTemplateSetParamCountLabel != null)[m
[32m+[m[32m            {[m
[32m+[m[32m                _uiTemplateSetParamCountLabel.text = _estimatedFtParamCount > 0[m
[32m+[m[32m                    ? string.Format(ArkitFTLoc.T("同期パラメータ：{0}bit"), _estimatedFtParamBits)[m
[32m+[m[32m                    : ArkitFTLoc.T("同期パラメータ数を取得できませんでした");[m
[32m+[m[32m            }[m
[32m+[m
             // Profileが読み込まれていれば、上部の帯で状態を案内する。ダミーファイル等、[m
             // 顔まわりのデータが実質何も無いアバターの場合は「このままInstallできます」と[m
             // 案内してしまうと誤解を招くため、警告表示に切り替える。[m
[36m@@ -3148,6 +3332,8 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             _uiEyeLookToggle?.SetValueWithoutNotify(_generateEyeLookShapes);[m
             _uiEyeLookSlider?.SetValueWithoutNotify(_eyeLookIntensity);[m
             _uiEyeLookValue?.SetValueWithoutNotify(_eyeLookIntensity);[m
[32m+[m[32m            _uiEyeLookStageCountSlider?.SetValueWithoutNotify(_eyeLookStageCount);[m
[32m+[m[32m            _uiEyeLookStageCountValue?.SetValueWithoutNotify(_eyeLookStageCount);[m
             if (_uiEyeLookDetail != null)[m
                 _uiEyeLookDetail.style.display = _generateEyeLookShapes ? DisplayStyle.Flex : DisplayStyle.None;[m
             if (_uiEyeLookConfigWarningHint != null)[m
[36m@@ -3163,6 +3349,11 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             if (_uiBlinkModeHint != null)[m
                 _uiBlinkModeHint.style.display = (_blinkControlMode == BlinkControlMode.TwoD) ? DisplayStyle.Flex : DisplayStyle.None;[m
 [m
[32m+[m[32m            _uiSyncBlinkToggle?.SetValueWithoutNotify(_syncBlinkLeftRight);[m
[32m+[m[32m            _uiSyncBlinkSourceField?.SetValueWithoutNotify(_syncBlinkUseRightAsSource ? ArkitFTLoc.T("右目") : ArkitFTLoc.T("左目"));[m
[32m+[m[32m            if (_uiSyncBlinkSourceField != null)[m
[32m+[m[32m                _uiSyncBlinkSourceField.style.display = _syncBlinkLeftRight ? DisplayStyle.Flex : DisplayStyle.None;[m
[32m+[m
             _uiEyeConstraintToggle?.SetValueWithoutNotify(_eyeUsesConstraint);[m
             _uiEyeConstraintToggle?.SetEnabled(_generateEyeLookShapes);[m
             if (_uiEyeConstraintFields != null)[m
[36m@@ -3659,11 +3850,25 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             _profileShopName = _profile.shopName ?? "";[m
             _profileVersionName = _profile.versionName ?? "";[m
 [m
[32m+[m[32m            // テンプレートセット: Profileに保存されたフォルダパスが、現在発見できているセットの[m
[32m+[m[32m            // いずれかと一致すればそれを選択する。見つからない場合(フォルダが移動・削除された等)は[m
[32m+[m[32m            // 警告を出し、現在の選択(既定は先頭のセット)をそのまま維持する。[m
[32m+[m[32m            if (!string.IsNullOrEmpty(_profile.templateSetFolderPath))[m
[32m+[m[32m            {[m
[32m+[m[32m                if (_availableTemplateSets.Any(s => s.folderPath == _profile.templateSetFolderPath))[m
[32m+[m[32m                    _selectedTemplateSetFolder = _profile.templateSetFolderPath;[m
[32m+[m[32m                else[m
[32m+[m[32m                    Debug.LogWarning($"[hinzka ARKit FT] Profileが指定するテンプレートセット " +[m
[32m+[m[32m                        $"'{_profile.templateSetFolderPath}' が見つからないため、既定のセットを使用します。");[m
[32m+[m[32m            }[m
[32m+[m
             // アバターに依存しない項目は常に適用[m
             _generateVisemeCompensation = _profile.generateVisemeCompensation;[m
             _visemeScale = _profile.visemeScale;[m
             _generateEyeLookShapes = _profile.generateEyeLookShapes;[m
             _blinkControlMode = _profile.blinkControlMode;[m
[32m+[m[32m            _syncBlinkLeftRight = _profile.syncBlinkLeftRight;[m
[32m+[m[32m            _syncBlinkUseRightAsSource = _profile.syncBlinkUseRightAsSource;[m
             _generateBrowAssistShapes = _profile.generateBrowAssistShapes;[m
             _browAssistIntensity = _profile.browAssistIntensity;[m
             _generateTongueAssistShapes = _profile.generateTongueAssistShapes;[m
[36m@@ -3681,6 +3886,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             _addBlinkEffect = _profile.addBlinkEffect;[m
             _blinkEffectClip = _profile.blinkEffectClip;[m
             _eyeLookIntensity = _profile.eyeLookIntensity;[m
[32m+[m[32m            _eyeLookStageCount = _profile.eyeLookStageCount;[m
             _disableNativeEyeLook = _profile.disableNativeEyeLook;[m
             _outputFolder = _profile.outputFolder;[m
 [m
[36m@@ -3782,6 +3988,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             _profile.avatarMatchTag = _avatarMatchTag ?? "";[m
             _profile.shopName = _profileShopName ?? "";[m
             _profile.versionName = _profileVersionName ?? "";[m
[32m+[m[32m            _profile.templateSetFolderPath = _selectedTemplateSetFolder ?? "";[m
 [m
             _profile.faceSMRPath = _smrIndex < _smrPaths.Length ? _smrPaths[_smrIndex] : "";[m
             _profile.arkitShapePrefix = _arkitShapePrefix;[m
[36m@@ -3821,6 +4028,8 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             _profile.visemeScale = _visemeScale;[m
             _profile.generateEyeLookShapes = _generateEyeLookShapes;[m
             _profile.blinkControlMode = _blinkControlMode;[m
[32m+[m[32m            _profile.syncBlinkLeftRight = _syncBlinkLeftRight;[m
[32m+[m[32m            _profile.syncBlinkUseRightAsSource = _syncBlinkUseRightAsSource;[m
             _profile.generateBrowAssistShapes = _generateBrowAssistShapes;[m
             _profile.browAssistIntensity = _browAssistIntensity;[m
             _profile.generateTongueAssistShapes = _generateTongueAssistShapes;[m
[36m@@ -3838,6 +4047,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             _profile.addBlinkEffect = _addBlinkEffect;[m
             _profile.blinkEffectClip = _blinkEffectClip;[m
             _profile.eyeLookIntensity = _eyeLookIntensity;[m
[32m+[m[32m            _profile.eyeLookStageCount = _eyeLookStageCount;[m
             _profile.disableNativeEyeLook = _disableNativeEyeLook;[m
             _profile.outputFolder = _outputFolder;[m
 [m
[36m@@ -4436,19 +4646,26 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             _estimatedTotalParamBits = 0;[m
             _estimatedExistingParamBits = 0;[m
             _estimatedFtParamBits = 0;[m
[32m+[m[32m            _estimatedFtParamCount = 0;[m
             _estimatedParamBitsOverBudget = false;[m
 [m
[32m+[m[32m            // テンプレート自体のパラメータ数/bit数は、アバター未選択でも(プルダウンの下に[m
[32m+[m[32m            // 表示するために)常に計算しておく。[m
[32m+[m[32m            var templateParams = !string.IsNullOrEmpty(_selectedTemplateSetFolder)[m
[32m+[m[32m                ? FindTemplateInFolder<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>(_selectedTemplateSetFolder, "NK_FT_Parameters.asset")[m
[32m+[m[32m                : FindTemplate<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>("NK_FT_Parameters.asset");[m
[32m+[m[32m            if (templateParams != null)[m
[32m+[m[32m            {[m
[32m+[m[32m                _estimatedFtParamBits = ComputeVrcParameterBits(templateParams);[m
[32m+[m[32m                _estimatedFtParamCount = CountVrcSyncedParameters(templateParams);[m
[32m+[m[32m            }[m
[32m+[m
             if (_avatarPrefab == null) return;[m
 [m
             var desc = _avatarPrefab.GetComponentInChildren<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);[m
             if (desc != null)[m
                 _estimatedExistingParamBits = ComputeVrcParameterBits(desc.expressionParameters);[m
 [m
[31m-            var templateParams = FindTemplate<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>([m
[31m-                "ARKit_FT_Parameters.asset");[m
[31m-            if (templateParams != null)[m
[31m-                _estimatedFtParamBits = ComputeVrcParameterBits(templateParams);[m
[31m-[m
             _estimatedTotalParamBits = _estimatedExistingParamBits + _estimatedFtParamBits;[m
             _estimatedParamBitsOverBudget = _estimatedTotalParamBits > VRC_PARAM_BIT_BUDGET;[m
         }[m
[36m@@ -4518,7 +4735,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         /// 副作用だけを止めたい場合に使う。[m
         ///[m
         /// Stable Eye ModeではAvatarDescriptor Eye Look自体を無効化するため、[m
[31m-        /// UE_FT_AutoStop_EyesからEyes=Animation/Trackingを再主張する必要がない。[m
[32m+[m[32m        /// NK_FT_AutoStop_EyesからEyes=Animation/Trackingを再主張する必要がない。[m
         /// レイヤー丸ごとの削除ではUEFx/FT_EnableEyesのウォッチドッグまで失われるので、[m
         /// trackingEyesのみNoChangeにする。[m
         /// </summary>[m
[36m@@ -4594,7 +4811,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         }[m
 [m
         /// <summary>[m
[31m-        /// UE_FT_AutoStop_Eyes の Idle_A / Idle_B の無条件反復を単一Idleへ整理する。[m
[32m+[m[32m        /// NK_FT_AutoStop_Eyes の Idle_A / Idle_B の無条件反復を単一Idleへ整理する。[m
         /// 実機検証で、AvatarDescriptor Eye Lookの有効/無効どちらでもA/B反復を削除して[m
         /// AutoStop・Eye Look復帰とも問題がないことを確認したため、両モード共通で適用する。[m
         ///[m
[36m@@ -5039,7 +5256,9 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             // ウィンドウを開いたまま後からアセットを追加・配置した場合は反映されない。[m
             // 未検出のままの間はここでも再探索し、次に見つかったタイミングで自動的に拾う。[m
             if (_shapeParameterMap == null)[m
[31m-                _shapeParameterMap = FindTemplate<ArkitShapeParameterMap>("ARKit_FT_ShapeParamMap.asset");[m
[32m+[m[32m                _shapeParameterMap = !string.IsNullOrEmpty(_selectedTemplateSetFolder)[m
[32m+[m[32m                    ? FindTemplateInFolder<ArkitShapeParameterMap>(_selectedTemplateSetFolder, "NK_FT_ShapeParamMap.asset")[m
[32m+[m[32m                    : FindTemplate<ArkitShapeParameterMap>("NK_FT_ShapeParamMap.asset");[m
 [m
             _missingArkitShapes.Clear();[m
             _emptyArkitShapes.Clear();[m
[36m@@ -5238,9 +5457,16 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             }[m
 [m
             // テンプレートは複製前に確認する。ここで失敗してもシーンに半端な複製を残さない。[m
[31m-            var templateFx    = FindTemplate<AnimatorController>("ARKit_FT_Template.controller");[m
[31m-            var templateMenu  = FindTemplate<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>("ARKit_FT_Menu.asset");[m
[31m-            var templateParam = FindTemplate<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>("ARKit_FT_Parameters.asset");[m
[32m+[m[32m            bool useSetFolder = !string.IsNullOrEmpty(_selectedTemplateSetFolder);[m
[32m+[m[32m            var templateFx    = useSetFolder[m
[32m+[m[32m                ? FindTemplateInFolder<AnimatorController>(_selectedTemplateSetFolder, "NK_FT_Template.controller")[m
[32m+[m[32m                : FindTemplate<AnimatorController>("NK_FT_Template.controller");[m
[32m+[m[32m            var templateMenu  = useSetFolder[m
[32m+[m[32m                ? FindTemplateInFolder<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>(_selectedTemplateSetFolder, "NK_FT_Menu.asset")[m
[32m+[m[32m                : FindTemplate<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>("NK_FT_Menu.asset");[m
[32m+[m[32m            var templateParam = useSetFolder[m
[32m+[m[32m                ? FindTemplateInFolder<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>(_selectedTemplateSetFolder, "NK_FT_Parameters.asset")[m
[32m+[m[32m                : FindTemplate<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>("NK_FT_Parameters.asset");[m
             if (templateFx == null || templateMenu == null || templateParam == null)[m
             {[m
                 EditorUtility.DisplayDialog("Error",[m
[36m@@ -5321,7 +5547,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
                     throw new InvalidOperationException(ArkitFTLoc.T("コピーしたFXを読み込めませんでした。"));[m
 [m
                 // Stable Eye Mode (AvatarDescriptor Eye Look無効化)では、テンプレートの[m
[31m-                // UE_FT_AutoStop_Eyes が持つ VRCAnimatorTrackingControl(Eyes=Animation/Tracking)を[m
[32m+[m[32m                // NK_FT_AutoStop_Eyes が持つ VRCAnimatorTrackingControl(Eyes=Animation/Tracking)を[m
                 // 発火させない。AutoStopレイヤー自体を削除すると UEFx/FT_EnableEyes の計算や[m
                 // Eyeパラメータのリセットまで失われるため、TrackingControlのtrackingEyesだけを[m
                 // NoChangeへ置き換え、Parameter Driver等のウォッチドッグ機能はそのまま残す。[m
[36m@@ -5332,13 +5558,13 @@[m [mnamespace hinzka.FaceTracking.Editor[m
                 if (_disableNativeEyeLook)[m
                 {[m
                     NeutralizeTrackingControlFieldInLayer([m
[31m-                        fx, "UE_FT_AutoStop_Eyes", "trackingEyes");[m
[32m+[m[32m                        fx, "NK_FT_AutoStop_Eyes", "trackingEyes");[m
                 }[m
 [m
[31m-                // UE_FT_AutoStop_Eyes の Idle_A / Idle_B 反復は、実機検証で[m
[32m+[m[32m                // NK_FT_AutoStop_Eyes の Idle_A / Idle_B 反復は、実機検証で[m
                 // AvatarDescriptor Eye Lookの有効/無効どちらでも削除して問題ないことを確認済み。[m
                 // Idle_A側のParameter Driver / TrackingControl等は残したまま単一Idle化する。[m
[31m-                CollapseEyeAutoStopIdleLoop(fx, "UE_FT_AutoStop_Eyes");[m
[32m+[m[32m                CollapseEyeAutoStopIdleLoop(fx, "NK_FT_AutoStop_Eyes");[m
 [m
                 string realSmrPath = _smrPaths[_smrIndex];[m
                 string eyeSmrPath = _eyeSmrSeparate ? _smrPaths[_eyeSmrIndex] : null;[m
[36m@@ -5500,6 +5726,9 @@[m [mnamespace hinzka.FaceTracking.Editor[m
                 // 同梱されている場合、選択されなかった方をここで無効化する。[m
                 ApplyBlinkControlModeSelection(fx, _blinkControlMode);[m
 [m
[32m+[m[32m                // 左右まばたき同期(片方の目のトラッキング値で両目を揃える)。[m
[32m+[m[32m                ApplySyncBlinkLeftRightIfNeeded(fx, _syncBlinkLeftRight, _syncBlinkUseRightAsSource);[m
[32m+[m
                 // 眉アシスト[m
                 if (_generateBrowAssistShapes)[m
                 {[m
[36m@@ -5510,7 +5739,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
                 // 舌アシスト(検出頂点の持ち上げ + tongueOut本体とのミックス)。[m
                 // tongueOutの0%→100%遷移の50%地点(唇を越えるタイミング)で持ち上げが[m
                 // 最大になるピーク形状を生成し、標準の舌駆動BlendTree[m
[31m-                // (hinzkaUE_Gain_v2_TongueOut)へ組み込む。[m
[32m+[m[32m                // (hinzkaNK_Gain_v2_TongueOut)へ組み込む。[m
                 if (_generateTongueAssistShapes)[m
                 {[m
                     // mm(ワールド実寸)で指定された閾値を、このSMRのメッシュ空間の値へ変換する[m
[36m@@ -6583,7 +6812,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             {[m
                 added = EyeLookBoneToBlendShapeBaker.GenerateMissingShapesAdditive([m
                     desc, faceSmr, newMesh, frameWeight, EYELOOK_BONE_PREFIX,[m
[31m-                    leftConstraintTarget, rightConstraintTarget, out var emptyDeltaNames);[m
[32m+[m[32m                    leftConstraintTarget, rightConstraintTarget, out var emptyDeltaNames, _eyeLookStageCount);[m
                 _lastEyeLookEmptyDeltaShapes = emptyDeltaNames;[m
             }[m
             finally[m
[36m@@ -7220,7 +7449,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         /// 最大になるよう舌を持ち上げる「ピーク形状」を1つだけ生成する。[m
         /// ピーク形状の頂点データには、①持ち上げ量の100%と、②tongueOut本体の伸び50%ぶんが[m
         /// あらかじめ合成されている。実際の遷移カーブ(0%→50%→100%)はFX側の[m
[31m-        /// ApplyTongueLiftEnvelopeで、既存の標準舌駆動BlendTree(hinzkaUE_Gain_v2_TongueOut)を[m
[32m+[m[32m        /// ApplyTongueLiftEnvelopeで、既存の標準舌駆動BlendTree(hinzkaNK_Gain_v2_TongueOut)を[m
         /// 組み替えることで実現する(離散的な2ポーズ切り替えではなく、連続的な山型エンベロープ)。[m
         ///[m
         /// ①「持ち上げ」の作り方はliftSourceで選べる:[m
[36m@@ -7410,7 +7639,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
 [m
         /// <summary>[m
         /// v2/TongueOutの0→100%遷移の間、50%地点(唇を越えるタイミング)で舌の持ち上げ[m
[31m-        /// (peakShapeName)が最大になるよう、標準の舌駆動BlendTree(hinzkaUE_Gain_v2_TongueOut)を[m
[32m+[m[32m        /// (peakShapeName)が最大になるよう、標準の舌駆動BlendTree(hinzkaNK_Gain_v2_TongueOut)を[m
         /// 3点構成(0%=無反応, 50%=持ち上げピーク, 100%=tongueOut本体そのまま・持ち上げ無し)へ[m
         /// 組み替える。既存の0%・100%用クリップはそのまま再利用し、50%用のクリップだけ新規作成する。[m
         /// これにより、離散的な2ポーズの切り替えではなく、遷移の後半にかけて持ち上げが自然に[m
[36m@@ -7463,13 +7692,13 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             // persist上げ量を0へ強制的にリセットしてしまう(「一切動かなくなった」不具合の[m
             // 直接的な原因と考えられる)。この副作用を避けるため、共有クリップは一切変更せず、[m
             // 0%地点・100%地点にも専用の新規クリップを作成する(ピーク・中間点と同じ方式)。[m
[31m-            var startClip = new AnimationClip { name = "hinzkaUE_TongueEnvelope_Start" };[m
[32m+[m[32m            var startClip = new AnimationClip { name = "hinzkaNK_TongueEnvelope_Start" };[m
             AssetDatabase.AddObjectToAsset(startClip, fx);[m
             startClip.hideFlags = GENERATED_SUBASSET_HIDE_FLAGS;[m
             SetConstantCurve(startClip, smrPath, bsPrefix + tongueOutPropName, 0f);[m
             SetConstantCurve(startClip, smrPath, bsPrefix + peakShapeName, 0f);[m
 [m
[31m-            var endClip = new AnimationClip { name = "hinzkaUE_TongueEnvelope_End" };[m
[32m+[m[32m            var endClip = new AnimationClip { name = "hinzkaNK_TongueEnvelope_End" };[m
             AssetDatabase.AddObjectToAsset(endClip, fx);[m
             endClip.hideFlags = GENERATED_SUBASSET_HIDE_FLAGS;[m
             SetConstantCurve(endClip, smrPath, bsPrefix + tongueOutPropName, 100f);[m
[36m@@ -7485,7 +7714,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             // 山の頂点用の新規クリップ。tongueOut本体は0にする[m
             // (peakShapeName自体の頂点データにtongueOut本体の指定%ぶんが既に焼き込まれているため、[m
             // 生カーブも同時に駆動すると二重に伸びてしまう)。[m
[31m-            var peakClip = new AnimationClip { name = "hinzkaUE_TonguePeak" };[m
[32m+[m[32m            var peakClip = new AnimationClip { name = "hinzkaNK_TonguePeak" };[m
             AssetDatabase.AddObjectToAsset(peakClip, fx);[m
             peakClip.hideFlags = GENERATED_SUBASSET_HIDE_FLAGS;[m
             SetConstantCurve(peakClip, smrPath, bsPrefix + tongueOutPropName, 0f);[m
[36m@@ -7498,7 +7727,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
 [m
             AnimationClip MakeEnvelopePointClip(string suffix, float valuePercent)[m
             {[m
[31m-                var clip = new AnimationClip { name = $"hinzkaUE_TongueEnvelope_{suffix}" };[m
[32m+[m[32m                var clip = new AnimationClip { name = $"hinzkaNK_TongueEnvelope_{suffix}" };[m
                 AssetDatabase.AddObjectToAsset(clip, fx);[m
                 clip.hideFlags = GENERATED_SUBASSET_HIDE_FLAGS;[m
                 SetConstantCurve(clip, smrPath, bsPrefix + tongueOutPropName, 0f);[m
[36m@@ -7557,7 +7786,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         }[m
 [m
         /// <summary>[m
[31m-        /// TongueOutSteps_BT配下の専用クリップ(hinzkaUE_TongueStep_*)が存在する場合、[m
[32m+[m[32m        /// TongueOutSteps_BT配下の専用クリップ(hinzkaNK_TongueStep_*)が存在する場合、[m
         /// そのBlendShapeカーブをすべて0に固定して無効化する。ApplyTongueLiftEnvelopeによる[m
         /// 持ち上げエンベロープと、同じv2/TongueOutを駆動源とする段階シェイプ系が[m
         /// 二重に舌を動かしてしまうのを防ぐ。該当クリップが無ければ何もしない。[m
[36m@@ -7620,6 +7849,64 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             if (disable1D) RemoveDirectBlendTreeChildByNameSuffix(fx, BLINK_SIMPLE1D_MOTION_NAME_SUFFIX);[m
         }[m
 [m
[32m+[m[32m        /// <summary>[m
[32m+[m[32m        /// 左右のまばたきを、片方の目のトラッキング値だけで同期させる。テンプレートFX内の[m
[32m+[m[32m        /// まばたき駆動BlendTree(Blink Simple 1D・Blink2D、配置方式や通常/WithBrow/Modulation[m
[32m+[m[32m        /// バリエーションを問わず全部)を名前で検索し、blendParameter(Blink2Dの場合は[m
[32m+[m[32m        /// blendParameterYも)を、指定した側の値へ揃える。[m
[32m+[m[32m        /// 対応するBlendTreeが見つからない(テンプレートがそもそも左右独立方式を持たない)場合は[m
[32m+[m[32m        /// 何もしない(安全にスキップする)。[m
[32m+[m[32m        /// </summary>[m
[32m+[m[32m        private static void ApplySyncBlinkLeftRightIfNeeded(AnimatorController fx, bool syncEnabled, bool useRightAsSource)[m
[32m+[m[32m        {[m
[32m+[m[32m            if (fx == null || !syncEnabled) return;[m
[32m+[m[32m            var fxPath = AssetDatabase.GetAssetPath(fx);[m
[32m+[m[32m            if (string.IsNullOrEmpty(fxPath)) return;[m
[32m+[m
[32m+[m[32m            var allTrees = AssetDatabase.LoadAllAssetsAtPath(fxPath).OfType<BlendTree>().ToList();[m
[32m+[m
[32m+[m[32m            // ── Blink Simple 1D ──[m
[32m+[m[32m            // "BlinkSimple1D_Left"を含む名前のBlendTree(通常/WithBrow/Mod問わず全バリエーション)の[m
[32m+[m[32m            // うち、代表として通常版(サフィックス無し)のblendParameterを採用元の値として使う。[m
[32m+[m[32m            var leftTrees = allTrees.Where(t => t.name.Contains("BlinkSimple1D_Left")).ToList();[m
[32m+[m[32m            var rightTrees = allTrees.Where(t => t.name.Contains("BlinkSimple1D_Right")).ToList();[m
[32m+[m[32m            if (leftTrees.Count > 0 && rightTrees.Count > 0)[m
[32m+[m[32m            {[m
[32m+[m[32m                var leftPlain = leftTrees.FirstOrDefault(t => !t.name.Contains("_WithBrow") && !t.name.Contains("_Mod")) ?? leftTrees[0];[m
[32m+[m[32m                var rightPlain = rightTrees.FirstOrDefault(t => !t.name.Contains("_WithBrow") && !t.name.Contains("_Mod")) ?? rightTrees[0];[m
[32m+[m[32m                var sourceParam = useRightAsSource ? rightPlain.blendParameter : leftPlain.blendParameter;[m
[32m+[m
[32m+[m[32m                int patched = 0;[m
[32m+[m[32m                foreach (var t in leftTrees.Concat(rightTrees))[m
[32m+[m[32m                {[m
[32m+[m[32m                    if (t.blendParameter == sourceParam) continue;[m
[32m+[m[32m                    t.blendParameter = sourceParam;[m
[32m+[m[32m                    EditorUtility.SetDirty(t);[m
[32m+[m[32m                    patched++;[m
[32m+[m[32m                }[m
[32m+[m[32m                if (patched > 0)[m
[32m+[m[32m                    Debug.Log($"[hinzka ARKit FT] 左右まばたき同期: Blink Simple 1D系BlendTree{patched}個を" +[m
[32m+[m[32m                              $"'{sourceParam}'に揃えました。");[m
[32m+[m[32m            }[m
[32m+[m
[32m+[m[32m            // ── Blink2D ──[m
[32m+[m[32m            // FreeformCartesian2Dで、名前に"Blink2D"を含むBlendTreeのX/Y軸を、指定した側の値に揃える。[m
[32m+[m[32m            var blink2DTrees = allTrees.Where(t =>[m
[32m+[m[32m                t.blendType == BlendTreeType.FreeformCartesian2D && t.name.Contains("Blink2D")).ToList();[m
[32m+[m[32m            int patched2D = 0;[m
[32m+[m[32m            foreach (var t in blink2DTrees)[m
[32m+[m[32m            {[m
[32m+[m[32m                var sourceParam2D = useRightAsSource ? t.blendParameterY : t.blendParameter;[m
[32m+[m[32m                if (t.blendParameter == sourceParam2D && t.blendParameterY == sourceParam2D) continue;[m
[32m+[m[32m                t.blendParameter = sourceParam2D;[m
[32m+[m[32m                t.blendParameterY = sourceParam2D;[m
[32m+[m[32m                EditorUtility.SetDirty(t);[m
[32m+[m[32m                patched2D++;[m
[32m+[m[32m            }[m
[32m+[m[32m            if (patched2D > 0)[m
[32m+[m[32m                Debug.Log($"[hinzka ARKit FT] 左右まばたき同期: Blink2D系BlendTree{patched2D}個のX/Y軸を揃えました。");[m
[32m+[m[32m        }[m
[32m+[m
         /// <summary>[m
         /// 指定名のAnimatorControllerLayerが存在すれば、defaultWeightを0にして無効化する。[m
         /// レイヤー自体は削除しない(他レイヤーのインデックス参照を壊さないため、非破壊的に[m
[36m@@ -7880,7 +8167,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         private const string BLINK_EFFECT_STATE_NAME = "FTextra_EyeBlinkEffect";[m
         // FXジェネレータ側のComboRuleが自動生成するクリップの命名規則(UEFxGeneratorWindow準拠)。[m
         // このプレフィックスに一致するクリップだけを「安全に破棄してよい生成物」とみなす。[m
[31m-        private const string BLINK_EFFECT_CLIP_PREFIX = "hinzkaUE_Combo_FTextra_EyeBlinkEffect";[m
[32m+[m[32m        private const string BLINK_EFFECT_CLIP_PREFIX = "hinzkaNK_Combo_FTextra_EyeBlinkEffect";[m
 [m
         /// <summary>[m
         /// まばたき検出時に1回だけ再生される「おまけ」のState(FTextra_EyeBlinkEffect)が[m
[36m@@ -7903,7 +8190,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             // 外部ファイル参照のままにせず、コピーをFXアセット内に埋め込む[m
             // (他のcombo/生成クリップと同様、FXを配布・共有しても参照が切れないようにするため)。[m
             var copy = UnityEngine.Object.Instantiate(userClip);[m
[31m-            copy.name = "hinzkaUE_Combo_FTextra_EyeBlinkEffect_User";[m
[32m+[m[32m            copy.name = "hinzkaNK_Combo_FTextra_EyeBlinkEffect_User";[m
             AssetDatabase.AddObjectToAsset(copy, fx);[m
             HideGeneratedSubAsset(copy);[m
 [m
[36m@@ -8147,7 +8434,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
                 Undo.RegisterCreatedObjectUndo(copy, "ARKit FT Install (Instantiate Avatar)");[m
             }[m
 [m
[31m-            copy.name = source.name + "_ARKitFT";[m
[32m+[m[32m            copy.name = source.name + "_FT";[m
 [m
             // 元アバターが既にVRChatへアップロード済みの場合、PipelineManagerに[m
             // Blueprint IDが記録されている。複製先にこれをそのまま引き継ぐと、[m
[36m@@ -8738,6 +9025,10 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             foreach (var p in paramAsset.parameters)[m
             {[m
                 if (p == null || !p.networkSynced) continue;[m
[32m+[m[32m                // 名前が空(未設定)のエントリは、Expression Menuからもアニメーターからも[m
[32m+[m[32m                // 参照しようがない無効なスロットなので、bit消費として数えない。[m
[32m+[m[32m                // (VRM Converter for VRChat等が予約枠として残す空エントリで実際に発生する)[m
[32m+[m[32m                if (string.IsNullOrWhiteSpace(p.name)) continue;[m
                 switch (p.valueType)[m
                 {[m
                     case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool:[m
[36m@@ -8750,6 +9041,23 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             return bits;[m
         }[m
 [m
[32m+[m[32m        /// <summary>[m
[32m+[m[32m        /// ComputeVrcParameterBitsと同じフィルタ条件(networkSynced かつ 名前が空でない)で、[m
[32m+[m[32m        /// 該当パラメータの「個数」を数える。bit数とあわせてUIに表示するために使う。[m
[32m+[m[32m        /// </summary>[m
[32m+[m[32m        private static int CountVrcSyncedParameters(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters paramAsset)[m
[32m+[m[32m        {[m
[32m+[m[32m            if (paramAsset?.parameters == null) return 0;[m
[32m+[m[32m            int count = 0;[m
[32m+[m[32m            foreach (var p in paramAsset.parameters)[m
[32m+[m[32m            {[m
[32m+[m[32m                if (p == null || !p.networkSynced) continue;[m
[32m+[m[32m                if (string.IsNullOrWhiteSpace(p.name)) continue;[m
[32m+[m[32m                count++;[m
[32m+[m[32m            }[m
[32m+[m[32m            return count;[m
[32m+[m[32m        }[m
[32m+[m
         private static bool IsValidAssetsFolder(string path)[m
         {[m
             if (string.IsNullOrWhiteSpace(path)) return false;[m
[36m@@ -8782,7 +9090,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
         private static string CreateUniqueInstallOutputFolder(string baseFolder, string avatarName)[m
         {[m
             EnsureAssetFolder(baseFolder);[m
[31m-            string safeName = SanitizeAssetName(string.IsNullOrWhiteSpace(avatarName) ? "Avatar_ARKitFT" : avatarName);[m
[32m+[m[32m            string safeName = SanitizeAssetName(string.IsNullOrWhiteSpace(avatarName) ? "Avatar_FT" : avatarName);[m
             string desired = baseFolder.TrimEnd('/') + "/" + safeName;[m
             string unique = AssetDatabase.GenerateUniqueAssetPath(desired);[m
 [m
[36m@@ -8802,7 +9110,7 @@[m [mnamespace hinzka.FaceTracking.Editor[m
             var invalid = Path.GetInvalidFileNameChars();[m
             var chars = name.Select(c => invalid.Contains(c) || c == '/' || c == '\\' ? '_' : c).ToArray();[m
             var result = new string(chars).Trim();[m
[31m-            return string.IsNullOrEmpty(result) ? "Avatar_ARKitFT" : result;[m
[32m+[m[32m            return string.IsNullOrEmpty(result) ? "Avatar_FT" : result;[m
         }[m
 [m
         /// <summary>[m
[36m@@ -8899,6 +9207,109 @@[m [mnamespace hinzka.FaceTracking.Editor[m
 [m
             return null;[m
         }[m
[32m+[m
[32m+[m[32m        /// <summary>[m
[32m+[m[32m        /// テンプレートセット1件分の情報(発見結果)。UIでの選択・Profileへの保存に使う。[m
[32m+[m[32m        /// </summary>[m
[32m+[m[32m        private sealed class TemplateSetOption[m
[32m+[m[32m        {[m
[32m+[m[32m            public string folderPath;   // 例: "Assets/.../Templates/軽量版" (ArkitTemplateSetInfoアセットが置かれているフォルダ)[m
[32m+[m[32m            public string displayName;  // ArkitTemplateSetInfo.displayNameが空ならフォルダ名を使う[m
[32m+[m[32m            public string description;[m
[32m+[m[32m        }[m
[32m+[m
[32m+[m[32m        /// <summary>[m
[32m+[m[32m        /// プロジェクト内から、ArkitTemplateSetInfoアセットが置かれている全フォルダを[m
[32m+[m[32m        /// テンプレートセットとして発見する。1つも見つからない場合は空リストを返す[m
[32m+[m[32m        /// (この場合、後方互換としてプロジェクト全体からの名前検索にフォールバックする)。[m
[32m+[m[32m        /// </summary>[m
[32m+[m[32m        private static List<TemplateSetOption> DiscoverTemplateSets()[m
[32m+[m[32m        {[m
[32m+[m[32m            var result = new List<TemplateSetOption>();[m
[32m+[m[32m            var guids = AssetDatabase.FindAssets("t:ArkitTemplateSetInfo");[m
[32m+[m[32m            foreach (var guid in guids)[m
[32m+[m[32m            {[m
[32m+[m[32m                var path = AssetDatabase.GUIDToAssetPath(guid);[m
[32m+[m[32m                var info = AssetDatabase.LoadAssetAtPath<ArkitTemplateSetInfo>(path);[m
[32m+[m[32m                if (info == null) continue;[m
[32m+[m
[32m+[m[32m                string folderPath = Path.GetDirectoryName(path)?.Replace('\\', '/');[m
[32m+[m[32m                if (string.IsNullOrEmpty(folderPath)) continue;[m
[32m+[m
[32m+[m[32m                string displayName = !string.IsNullOrWhiteSpace(info.displayName)[m
[32m+[m[32m                    ? info.displayName[m
[32m+[m[32m                    : Path.GetFileName(folderPath);[m
[32m+[m
[32m+[m[32m                result.Add(new TemplateSetOption[m
[32m+[m[32m                {[m
[32m+[m[32m                    folderPath = folderPath,[m
[32m+[m[32m                    displayName = displayName,[m
[32m+[m[32m                    description = info.description ?? ""[m
[32m+[m[32m                });[m
[32m+[m[32m            }[m
[32m+[m[32m            return result.OrderBy(e => e.displayName, StringComparer.Ordinal).ToList();[m
[32m+[m[32m        }[m
[32m+[m
[32m+[m[32m        /// <summary>[m
[32m+[m[32m        /// 指定したフォルダ直下から、ファイル名を指定してテンプレートアセットを読み込む。[m
[32m+[m[32m        /// FindTemplate(プロジェクト全体からの名前検索)と異なり、フォルダを固定して探すため、[m
[32m+[m[32m        /// 同名ファイルがプロジェクト内に複数存在していても、意図しない方を拾う心配が無い。[m
[32m+[m[32m        ///[m
[32m+[m[32m        /// 決め打ち名(fileName)で完全一致するファイルがあれば最優先でそれを使う(後方互換)。[m
[32m+[m[32m        /// 見つからない場合、フォルダ直下でfileNameの拡張子抜きの文字列(例: "NK_FT_Template")を[m
[32m+[m[32m        /// **ファイル名に含む**、かつ型がTに一致するアセットを探す(ユーザーがファイル名を[m
[32m+[m[32m        /// 分かりやすく変えても認識できるようにするため。例: "NK_FT_Template_軽量版.controller"等)。[m
[32m+[m[32m        /// 該当が1件だけならそれを自動採用し、0件または2件以上の場合はnullを返す[m
[32m+[m[32m        /// (2件以上の場合は、どれを使うべきか一意に決められないため、安全側に倒して不採用とする)。[m
[32m+[m[32m        /// </summary>[m
[32m+[m[32m        private static T FindTemplateInFolder<T>(string folderPath, string fileName) where T : UnityEngine.Object[m
[32m+[m[32m        {[m
[32m+[m[32m            if (string.IsNullOrEmpty(folderPath)) return null;[m
[32m+[m
[32m+[m[32m            // ① 決め打ち名との完全一致(最優先・後方互換)[m
[32m+[m[32m            string exactPath = folderPath.TrimEnd('/') + "/" + fileName;[m
[32m+[m[32m            var exactAsset = AssetDatabase.LoadAssetAtPath<T>(exactPath);[m
[32m+[m[32m            if (exactAsset != null) return exactAsset;[m
[32m+[m
[32m+[m[32m            // ② 部分一致によるフォールバック: フォルダ直下(サブフォルダは含めない)で、[m
[32m+[m[32m            // ファイル名にコア文字列(拡張子抜きのfileName)を含み、型がTに一致するものを探す。[m
[32m+[m[32m            string coreToken = Path.GetFileNameWithoutExtension(fileName);[m
[32m+[m[32m            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folderPath.TrimEnd('/') });[m
[32m+[m[32m            var candidates = new List<(string path, T asset)>();[m
[32m+[m[32m            foreach (var guid in guids)[m
[32m+[m[32m            {[m
[32m+[m[32m                var candidatePath = AssetDatabase.GUIDToAssetPath(guid);[m
[32m+[m[32m                // FindAssetsはサブフォルダも再帰的に検索するため、直下のファイルだけに絞る。[m
[32m+[m[32m                if (Path.GetDirectoryName(candidatePath)?.Replace('\\', '/') != folderPath.TrimEnd('/')) continue;[m
[32m+[m
[32m+[m[32m                string candidateName = Path.GetFileNameWithoutExtension(candidatePath);[m
[32m+[m[32m                if (candidateName.IndexOf(coreToken, StringComparison.OrdinalIgnoreCase) < 0) continue;[m
[32m+[m
[32m+[m[32m                var candidateAsset = AssetDatabase.LoadAssetAtPath<T>(candidatePath);[m
[32m+[m[32m                if (candidateAsset != null) candidates.Add((candidatePath, candidateAsset));[m
[32m+[m[32m            }[m
[32m+[m
[32m+[m[32m            if (candidates.Count == 1)[m
[32m+[m[32m            {[m
[32m+[m[32m                Debug.Log($"[hinzka ARKit FT] '{exactPath}' が見つからなかったため、名前に" +[m
[32m+[m[32m                          $"'{coreToken}'を含む'{candidates[0].path}'を自動的に採用しました。");[m
[32m+[m[32m                return candidates[0].asset;[m
[32m+[m[32m            }[m
[32m+[m
[32m+[m[32m            if (candidates.Count > 1)[m
[32m+[m[32m            {[m
[32m+[m[32m                var names = string.Join(", ", candidates.Select(c => c.path));[m
[32m+[m[32m                Debug.LogWarning($"[hinzka ARKit FT] '{folderPath}'内で、名前に'{coreToken}'を含む" +[m
[32m+[m[32m                                  $"{typeof(T).Name}が{candidates.Count}件見つかり、どれを使うべきか" +[m
[32m+[m[32m                                  $"一意に決められませんでした({names})。いずれか1つのファイル名を" +[m
[32m+[m[32m                                  $"'{fileName}'に変更するか、それ以外を別の場所へ移動してください。");[m
[32m+[m[32m                return null;[m
[32m+[m[32m            }[m
[32m+[m
[32m+[m[32m            Debug.Log($"[hinzka ARKit FT] '{exactPath}' が見つからず、名前に'{coreToken}'を含む" +[m
[32m+[m[32m                      $"{typeof(T).Name}も見つかりませんでした。");[m
[32m+[m[32m            return null;[m
[32m+[m[32m        }[m
     }[m
 }[m
 #endif[m
[1mdiff --git a/com.hinzka.nkinstaller/Editor/EyeLookBoneToBlendShapeBaker.cs b/com.hinzka.nkinstaller/Editor/EyeLookBoneToBlendShapeBaker.cs[m
[1mindex f712487..aacd190 100644[m
[1m--- a/com.hinzka.nkinstaller/Editor/EyeLookBoneToBlendShapeBaker.cs[m
[1m+++ b/com.hinzka.nkinstaller/Editor/EyeLookBoneToBlendShapeBaker.cs[m
[36m@@ -160,6 +160,88 @@[m [mnamespace hinzka.FaceTracking.DevTools[m
             return count;[m
         }[m
 [m
[32m+[m[32m        /// <summary>[m
[32m+[m[32m        /// rest→target姿勢の間をstageCount段階に分けてSlerpし、各段階の頂点/法線差分を[m
[32m+[m[32m        /// weight昇順のBlendShapeFrameとして返す。[m
[32m+[m[32m        ///[m
[32m+[m[32m        /// 単純に0%(rest)と100%(target)の2点だけでBlendShapeFrameを作ると、Unity側はその間を[m
[32m+[m[32m        /// 頂点の直線補間(弦)で埋める。回転角が小さいうちは弧と弦の差はごくわずかだが、[m
[32m+[m[32m        /// 回転角が大きいシェイプ(デフォルメアバター等で可動域が大きいケース)ほど弦は弧から[m
[32m+[m[32m        /// 内側に大きく食い込み、ウェイト中間域でメッシュが実際の回転軌跡を通らず凹む/潰れて[m
[32m+[m[32m        /// 見える原因になる。Quaternion.Slerpで実際に中間姿勢をベイクし、複数フレーム化する[m
[32m+[m[32m        /// (区分線形近似)ことでこの乖離を軽減する。stageCount=1なら従来通り単一フレームになる。[m
[32m+[m[32m        /// </summary>[m
[32m+[m[32m        /// <param name="applyPose">[m
[32m+[m[32m        /// (leftLocalRot, rightLocalRot) を受け取り、実際にボーン(および必要ならコンストレイント[m
[32m+[m[32m        /// ターゲット)へ姿勢を適用するコールバック。呼び出し側の既存ApplyPose相当のロジックを渡す。[m
[32m+[m[32m        /// </param>[m
[32m+[m[32m        /// <param name="anyDeltaOverall">[m
[32m+[m[32m        /// いずれかの段階で頂点差分が検出された(ほぼゼロではなかった)場合にtrue。[m
[32m+[m[32m        /// </param>[m
[32m+[m[32m        private static List<ShapeFrameData> BakeStagedShapeFrames([m
[32m+[m[32m            SkinnedMeshRenderer targetSmr,[m
[32m+[m[32m            Vector3[] baseVerts, Vector3[] baseNormals,[m
[32m+[m[32m            Quaternion leftRest, Quaternion rightRest,[m
[32m+[m[32m            EyeSide side, Quaternion targetRot,[m
[32m+[m[32m            System.Action<Quaternion, Quaternion> applyPose,[m
[32m+[m[32m            string outName, float frameWeight, int stageCount,[m
[32m+[m[32m            List<Mesh> cleanupList,[m
[32m+[m[32m            out bool anyDeltaOverall)[m
[32m+[m[32m        {[m
[32m+[m[32m            var frames = new List<ShapeFrameData>();[m
[32m+[m[32m            anyDeltaOverall = false;[m
[32m+[m[32m            if (stageCount < 1) stageCount = 1;[m
[32m+[m
[32m+[m[32m            var restRot = side == EyeSide.Left ? leftRest : rightRest;[m
[32m+[m
[32m+[m[32m            for (int stage = 1; stage <= stageCount; stage++)[m
[32m+[m[32m            {[m
[32m+[m[32m                float t = (float)stage / stageCount;[m
[32m+[m[32m                var midRot = Quaternion.Slerp(restRot, targetRot, t);[m
[32m+[m
[32m+[m[32m                applyPose([m
[32m+[m[32m                    side == EyeSide.Left ? midRot : leftRest,[m
[32m+[m[32m                    side == EyeSide.Right ? midRot : rightRest);[m
[32m+[m
[32m+[m[32m                var posed = new Mesh();[m
[32m+[m[32m                Physics.SyncTransforms();[m
[32m+[m[32m                targetSmr.BakeMesh(posed, true);[m
[32m+[m[32m                cleanupList.Add(posed);[m
[32m+[m
[32m+[m[32m                var pv = posed.vertices;[m
[32m+[m[32m                var pn = posed.normals;[m
[32m+[m[32m                if (pv.Length != baseVerts.Length)[m
[32m+[m[32m                {[m
[32m+[m[32m                    Debug.LogError($"[EyeLookBaker] 頂点数不一致のため '{outName}' (stage {stage}/{stageCount}) を" +[m
[32m+[m[32m                                    "スキップしました。");[m
[32m+[m[32m                    continue;[m
[32m+[m[32m                }[m
[32m+[m
[32m+[m[32m                var dv = new Vector3[pv.Length];[m
[32m+[m[32m                var dn = new Vector3[pv.Length];[m
[32m+[m[32m                var dt = new Vector3[pv.Length];[m
[32m+[m[32m                bool anyDelta = false;[m
[32m+[m[32m                for (int i = 0; i < pv.Length; i++)[m
[32m+[m[32m                {[m
[32m+[m[32m                    dv[i] = pv[i] - baseVerts[i];[m
[32m+[m[32m                    dn[i] = pn[i] - baseNormals[i];[m
[32m+[m[32m                    if (!anyDelta && dv[i].sqrMagnitude > 1e-12f) anyDelta = true;[m
[32m+[m[32m                }[m
[32m+[m[32m                if (anyDelta) anyDeltaOverall = true;[m
[32m+[m
[32m+[m[32m                frames.Add(new ShapeFrameData[m
[32m+[m[32m                {[m
[32m+[m[32m                    name = outName,[m
[32m+[m[32m                    weight = frameWeight * t,[m
[32m+[m[32m                    dv = dv,[m
[32m+[m[32m                    dn = dn,[m
[32m+[m[32m                    dt = dt[m
[32m+[m[32m                });[m
[32m+[m[32m            }[m
[32m+[m
[32m+[m[32m            return frames;[m
[32m+[m[32m        }[m
[32m+[m
         /// <summary>[m
         /// eyeLook系8シェイプキーを生成する。[m
         /// </summary>[m
[36m@@ -179,7 +261,8 @@[m [mnamespace hinzka.FaceTracking.DevTools[m
             string outputFolderIfCreating = "Assets/_Generated/BakedMeshes",[m
             float frameWeight = 100f,[m
             bool overwriteExistingShapes = true,[m
[31m-            string namePrefix = DefaultBonePrefix)[m
[32m+[m[32m            string namePrefix = DefaultBonePrefix,[m
[32m+[m[32m            int stageCount = 1)[m
         {[m
             var err = Validate(avatarDescriptor, targetSmr);[m
             if (err != null)[m
[36m@@ -239,40 +322,23 @@[m [mnamespace hinzka.FaceTracking.DevTools[m
                         continue;[m
                     }[m
 [m
[31m-                    leftEye.localRotation = leftRest;[m
[31m-                    rightEye.localRotation = rightRest;[m
[31m-[m
                     var rot = GetTargetRotation(eyeSettings, spec);[m
[31m-                    if (spec.side == EyeSide.Left) leftEye.localRotation = rot;[m
[31m-                    else rightEye.localRotation = rot;[m
[31m-[m
[31m-                    var posedMesh = new Mesh();[m
[31m-                    Physics.SyncTransforms();[m
[31m-                    targetSmr.BakeMesh(posedMesh, true);[m
[31m-                    posedMeshesToCleanup.Add(posedMesh);[m
[31m-[m
[31m-                    var posedVerts = posedMesh.vertices;[m
[31m-                    var posedNormals = posedMesh.normals;[m
 [m
[31m-                    if (posedVerts.Length != baseVerts.Length)[m
[32m+[m[32m                    void ApplyPoseSimple(Quaternion l, Quaternion r)[m
                     {[m
[31m-                        Debug.LogError($"[EyeLookBaker] 頂点数不一致のため '{outName}' をスキップしました。" +[m
[31m-                                        $"(base={baseVerts.Length}, posed={posedVerts.Length})");[m
[31m-                        continue;[m
[32m+[m[32m                        leftEye.localRotation = l;[m
[32m+[m[32m                        rightEye.localRotation = r;[m
                     }[m
 [m
[31m-                    var dv = new Vector3[posedVerts.Length];[m
[31m-                    var dn = new Vector3[posedVerts.Length];[m
[31m-                    var dt = new Vector3[posedVerts.Length];[m
[32m+[m[32m                    var frames = BakeStagedShapeFrames([m
[32m+[m[32m                        targetSmr, baseVerts, baseNormals, leftRest, rightRest,[m
[32m+[m[32m                        spec.side, rot, ApplyPoseSimple, outName, frameWeight, stageCount,[m
[32m+[m[32m                        posedMeshesToCleanup, out _);[m
 [m
[31m-                    for (int i = 0; i < posedVerts.Length; i++)[m
[31m-                    {[m
[31m-                        dv[i] = posedVerts[i] - baseVerts[i];[m
[31m-                        dn[i] = posedNormals[i] - baseNormals[i];[m
[31m-                    }[m
[32m+[m[32m                    if (frames.Count == 0) continue; // 頂点数不一致等で全段階スキップされた場合[m
 [m
                     savedShapes.RemoveAll(s => s.name == outName);[m
[31m-                    newShapes.Add(new ShapeFrameData { name = outName, weight = frameWeight, dv = dv, dn = dn, dt = dt });[m
[32m+[m[32m                    newShapes.AddRange(frames);[m
                 }[m
 [m
                 editableMesh.ClearBlendShapes();[m
[36m@@ -320,11 +386,12 @@[m [mnamespace hinzka.FaceTracking.DevTools[m
             float frameWeight = 100f,[m
             string namePrefix = DefaultBonePrefix,[m
             Transform leftConstraintTarget = null,[m
[31m-            Transform rightConstraintTarget = null)[m
[32m+[m[32m            Transform rightConstraintTarget = null,[m
[32m+[m[32m            int stageCount = 1)[m
         {[m
             return GenerateMissingShapesAdditive([m
                 avatarDescriptor, targetSmr, workingMesh, frameWeight, namePrefix,[m
[31m-                leftConstraintTarget, rightConstraintTarget, out _);[m
[32m+[m[32m                leftConstraintTarget, rightConstraintTarget, out _, stageCount);[m
         }[m
 [m
         /// <summary>[m
[36m@@ -348,7 +415,8 @@[m [mnamespace hinzka.FaceTracking.DevTools[m
             string namePrefix,[m
             Transform leftConstraintTarget,[m
             Transform rightConstraintTarget,[m
[31m-            out List<string> emptyDeltaNames)[m
[32m+[m[32m            out List<string> emptyDeltaNames,[m
[32m+[m[32m            int stageCount = 1)[m
         {[m
             var added = new List<string>();[m
             emptyDeltaNames = new List<string>();[m
[36m@@ -412,33 +480,13 @@[m [mnamespace hinzka.FaceTracking.DevTools[m
                         continue; // 既存はスキップ(上書きしない)[m
 [m
                     var rot = GetTargetRotation(eyeSettings, spec);[m
[31m-                    ApplyPose([m
[31m-                        spec.side == EyeSide.Left ? rot : leftRest,[m
[31m-                        spec.side == EyeSide.Right ? rot : rightRest);[m
[31m-[m
[31m-                    var posed = new Mesh();[m
[31m-                    Physics.SyncTransforms();[m
[31m-                    targetSmr.BakeMesh(posed, true);[m
[31m-                    posedCleanup.Add(posed);[m
[31m-[m
[31m-                    var pv = posed.vertices;[m
[31m-                    var pn = posed.normals;[m
[31m-                    if (pv.Length != baseVerts.Length)[m
[31m-                    {[m
[31m-                        Debug.LogError($"[EyeLookBaker] 頂点数不一致のため '{outName}' をスキップしました。");[m
[31m-                        continue;[m
[31m-                    }[m
 [m
[31m-                    var dv = new Vector3[pv.Length];[m
[31m-                    var dn = new Vector3[pv.Length];[m
[31m-                    var dt = new Vector3[pv.Length];[m
[31m-                    bool anyDelta = false;[m
[31m-                    for (int i = 0; i < pv.Length; i++)[m
[31m-                    {[m
[31m-                        dv[i] = pv[i] - baseVerts[i];[m
[31m-                        dn[i] = pn[i] - baseNormals[i];[m
[31m-                        if (!anyDelta && dv[i].sqrMagnitude > 1e-12f) anyDelta = true;[m
[31m-                    }[m
[32m+[m[32m                    var frames = BakeStagedShapeFrames([m
[32m+[m[32m                        targetSmr, baseVerts, baseNormals, leftRest, rightRest,[m
[32m+[m[32m                        spec.side, rot, ApplyPose, outName, frameWeight, stageCount,[m
[32m+[m[32m                        posedCleanup, out bool anyDelta);[m
[32m+[m
[32m+[m[32m                    if (frames.Count == 0) continue; // 頂点数不一致等で全段階スキップされた場合[m
 [m
                     if (!anyDelta)[m
                     {[m
[36m@@ -449,7 +497,8 @@[m [mnamespace hinzka.FaceTracking.DevTools[m
                             "(Face SMRと眼球メッシュが別々のSkinnedMeshRendererになっていないか確認してください)。");[m
                     }[m
 [m
[31m-                    workingMesh.AddBlendShapeFrame(outName, frameWeight, dv, dn, dt);[m
[32m+[m[32m                    foreach (var f in frames)[m
[32m+[m[32m                        workingMesh.AddBlendShapeFrame(f.name, f.weight, f.dv, f.dn, f.dt);[m
                     added.Add(outName);[m
                 }[m
 [m
[1mdiff --git a/com.hinzka.nkinstaller/Runtime/ARKitFTProfile.cs b/com.hinzka.nkinstaller/Runtime/ARKitFTProfile.cs[m
[1mindex 4cd03d8..8fedd9b 100644[m
[1m--- a/com.hinzka.nkinstaller/Runtime/ARKitFTProfile.cs[m
[1m+++ b/com.hinzka.nkinstaller/Runtime/ARKitFTProfile.cs[m
[36m@@ -72,6 +72,12 @@[m [mnamespace hinzka.FaceTracking[m
                  "どのバージョン向けかを目視で確認しやすくするためのもの。")][m
         public string versionName = "";[m
 [m
[32m+[m[32m        [Tooltip("このProfileが使用するテンプレートセット(ARKit_FT_Template.controller等の組)が\n" +[m
[32m+[m[32m                 "置かれているフォルダのパス。複数のテンプレートセットが用意されている場合のみ\n" +[m
[32m+[m[32m                 "意味を持つ。空の場合は、プロジェクト全体からの名前検索にフォールバックする\n" +[m
[32m+[m[32m                 "(単一セットのみの環境との後方互換)。")][m
[32m+[m[32m        public string templateSetFolderPath = "";[m
[32m+[m
         [Header("顔メッシュ")][m
         [Tooltip("FXアニメーションが参照するSMRのヒエラルキーパス (例: Body)")][m
         public string faceSMRPath = "Body";[m
[36m@@ -141,6 +147,11 @@[m [mnamespace hinzka.FaceTracking[m
                  "AvatarDescriptorの角度まで届かないことが多い場合に上げる。")][m
         public float eyeLookIntensity = 1f;[m
 [m
[32m+[m[32m        [Tooltip("視線シェイプキーをベイクする際、rest姿勢から到達姿勢までの回転を何段階に分けて" +[m
[32m+[m[32m                 "Slerp近似するか。1(既定)は従来通りの直線補間、値を大きくすると回転角の大きい" +[m
[32m+[m[32m                 "シェイプ(デフォルメアバターの目等)で、回転の弧をより正確に再現できる。")][m
[32m+[m[32m        public int eyeLookStageCount = 3;[m
[32m+[m
         [Tooltip("AvatarDescriptorのEye Look機能自体を無効化するか。" +[m
                  "ジェスチャー表情の抑制だけでは競合を解消しきれない場合の最終手段。" +[m
                  "トレードオフとして、FTオフ時に目が全く動かなくなる。")][m
[36m@@ -153,6 +164,12 @@[m [mnamespace hinzka.FaceTracking[m
                  "場合は該当ノードを除去)。テンプレートが片方の方式しか持たない場合は無視される。")][m
         public BlinkControlMode blinkControlMode = BlinkControlMode.TwoD;[m
 [m
[32m+[m[32m        [Tooltip("ONにすると、左右のまばたきを片方の目のトラッキング値だけで完全に同期させる。\n" +[m
[32m+[m[32m                 "もう片方の目の独立した開閉(ウインク等)は再現されなくなる。")][m
[32m+[m[32m        public bool syncBlinkLeftRight = false;[m
[32m+[m[32m        [Tooltip("syncBlinkLeftRightが有効な場合、true=右目・false=左目のトラッキング値を採用する。")][m
[32m+[m[32m        public bool syncBlinkUseRightAsSource = false;[m
[32m+[m
         [Header("眉アシスト")][m
         [Tooltip("眉トラッキング非搭載デバイス向けに、標準ARKit眉シェイプキー(browInnerUp等)を複製した" +[m
                  "sub_brow*シェイプキーを生成し、まばたき(v2/EyeLidLeft・v2/EyeLidRight)に連動させる。")][m
[1mdiff --git a/com.hinzka.nkinstaller/Templates/ARKit_FT_Menu.asset b/com.hinzka.nkinstaller/Templates/ARKit_FT_Menu.asset[m
[1mdeleted file mode 100644[m
[1mindex 0aae086..0000000[m
Binary files a/com.hinzka.nkinstaller/Templates/ARKit_FT_Menu.asset and /dev/null differ
[1mdiff --git a/com.hinzka.nkinstaller/Templates/ARKit_FT_Menu.asset.meta b/com.hinzka.nkinstaller/Templates/ARKit_FT_Menu.asset.meta[m
[1mdeleted file mode 100644[m
[1mindex 3686883..0000000[m
Binary files a/com.hinzka.nkinstaller/Templates/ARKit_FT_Menu.asset.meta and /dev/null differ
[1mdiff --git a/com.hinzka.nkinstaller/Templates/ARKit_FT_Parameters.asset b/com.hinzka.nkinstaller/Templates/ARKit_FT_Parameters.asset[m
[1mdeleted file mode 100644[m
[1mindex bffe222..0000000[m
Binary files a/com.hinzka.nkinstaller/Templates/ARKit_FT_Parameters.asset and /dev/null differ
[1mdiff --git a/com.hinzka.nkinstaller/Templates/ARKit_FT_Parameters.asset.meta b/com.hinzka.nkinstaller/Templates/ARKit_FT_Parameters.asset.meta[m
[1mdeleted file mode 100644[m
[1mindex aa1f1c6..0000000[m
Binary files a/com.hinzka.nkinstaller/Templates/ARKit_FT_Parameters.asset.meta and /dev/null differ
[1mdiff --git a/com.hinzka.nkinstaller/Templates/ARKit_FT_ShapeParamMap.asset b/com.hinzka.nkinstaller/Templates/ARKit_FT_ShapeParamMap.asset[m
[1mdeleted file mode 100644[m
[1mindex df962e7..0000000[m
Binary files a/com.hinzka.nkinstaller/Templates/ARKit_FT_ShapeParamMap.asset and /dev/null differ
[1mdiff --git a/com.hinzka.nkinstaller/Templates/ARKit_FT_ShapeParamMap.asset.meta b/com.hinzka.nkinstaller/Templates/ARKit_FT_ShapeParamMap.asset.meta[m
[1mdeleted file mode 100644[m
[1mindex e31ed60..0000000[m
Binary files a/com.hinzka.nkinstaller/Templates/ARKit_FT_ShapeParamMap.asset.meta and /dev/null differ
[1mdiff --git a/com.hinzka.nkinstaller/Templates/ARKit_FT_Template.controller b/com.hinzka.nkinstaller/Templates/ARKit_FT_Template.controller[m
[1mdeleted file mode 100644[m
[1mindex 4805585..0000000[m
Binary files a/com.hinzka.nkinstaller/Templates/ARKit_FT_Template.controller and /dev/null differ
[1mdiff --git a/com.hinzka.nkinstaller/Templates/ARKit_FT_Template.controller.meta b/com.hinzka.nkinstaller/Templates/ARKit_FT_Template.controller.meta[m
[1mdeleted file mode 100644[m
[1mindex 5d23ecd..0000000[m
Binary files a/com.hinzka.nkinstaller/Templates/ARKit_FT_Template.controller.meta and /dev/null differ
[1mdiff --git a/com.hinzka.nkinstaller/package.json b/com.hinzka.nkinstaller/package.json[m
[1mindex d97d309..38aaec6 100644[m
[1m--- a/com.hinzka.nkinstaller/package.json[m
[1m+++ b/com.hinzka.nkinstaller/package.json[m
[36m@@ -1,7 +1,7 @@[m
 {[m
   "name": "com.hinzka.nkinstaller",[m
   "displayName": "NK Installer - Native Key FaceTracking Installer",[m
[31m-  "version": "1.1.0",[m
[32m+[m[32m  "version": "1.2.0",[m
   "unity": "2022.3",[m
   "description": "アバター作者がつくった表情で、そのままフェイストラッキング。ARKit標準シェイプキーを非破壊で活用するVRCFaceTracking導入ツールです。",[m
   "author": {[m
