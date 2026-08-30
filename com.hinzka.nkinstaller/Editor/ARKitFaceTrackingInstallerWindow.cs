#if UNITY_EDITOR
// -*- coding: utf-8 -*-
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using hinzka.FaceTracking;
using hinzka.FaceTracking.DevTools;

// MA
using nadena.dev.modular_avatar.core;

namespace hinzka.FaceTracking.Editor
{
    /// <summary>
    /// ARKit FT Installer の簡易ローカライズシステム。
    /// 日本語の原文そのものを辞書キーとして使い、EN/ZH/KRへ翻訳する
    /// (キー名を別途考える必要がなく、翻訳漏れの場合も自然に日本語へフォールバックする)。
    /// 言語切り替えはUIをまるごと再構築(CreateGUI再実行)することで反映する。
    /// </summary>
    internal static class ArkitFTLoc
    {
        public enum Lang { Japanese = 0, English = 1, ChineseSimplified = 2, Korean = 3 }

        private const string PrefsKey = "hinzka_ARKitFT_Lang";
        private static Lang _current = Lang.Japanese;
        private static bool _loaded;

        public static Lang Current
        {
            get { EnsureLoaded(); return _current; }
            set
            {
                _current = value;
                _loaded = true;
                EditorPrefs.SetInt(PrefsKey, (int)value);
            }
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            if (EditorPrefs.HasKey(PrefsKey))
            {
                _current = (Lang)EditorPrefs.GetInt(PrefsKey, 0);
                return;
            }
            // 初回起動時はOSの言語設定から推測する(EditorPrefsに保存済みならそちらを優先)
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Japanese: _current = Lang.Japanese; break;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional: _current = Lang.ChineseSimplified; break;
                case SystemLanguage.Korean: _current = Lang.Korean; break;
                default: _current = Lang.English; break;
            }
        }

        /// <summary>
        /// 日本語原文をキーとして翻訳を返す。現在の言語が日本語、またはテーブルに
        /// 該当する翻訳が無い場合は原文(日本語)をそのまま返す。
        /// </summary>
        public static string T(string japaneseText)
        {
            if (string.IsNullOrEmpty(japaneseText) || Current == Lang.Japanese) return japaneseText;
            if (_table.TryGetValue(japaneseText, out var arr))
            {
                int idx = (int)Current - 1; // EN=0, ZH=1, KR=2
                if (idx >= 0 && idx < arr.Length && !string.IsNullOrEmpty(arr[idx]))
                    return arr[idx];
            }
            return japaneseText;
        }

        public static string DisplayName(Lang lang)
        {
            switch (lang)
            {
                case Lang.Japanese: return "日本語";
                case Lang.English: return "English";
                case Lang.ChineseSimplified: return "中文";
                case Lang.Korean: return "한국어";
                default: return lang.ToString();
            }
        }

        // key: 日本語原文, value: [EN, ZH, KR]
        private static readonly Dictionary<string, string[]> _table = new Dictionary<string, string[]>
        {
            ["基本設定"] = new[] { "Basic Settings", "基本设置", "기본 설정" },
            ["トラッキング"] = new[] { "Tracking", "追踪", "트래킹" },
            ["詳細 / 結果"] = new[] { "Advanced / Result", "详细 / 结果", "상세 / 결과" },
            ["アバター作者がつくった表情で、そのままフェイストラッキング"] = new[] { "Face tracking, powered by the expressions your avatar's creator made", "使用角色作者制作的表情，直接进行面部追踪", "아바타 제작자가 만든 표정으로, 그대로 페이스 트래킹" },
            ["NK = Native Key。アバター作者が作った「そのままの(Native)」シェイプキーを使い、\n作り直したり別物に差し替えたりしません。"] = new[] {
                "NK = Native Key. Uses the shape keys your avatar's creator made as-is (native), without recreating or swapping them for something else.",
                "NK = Native Key。使用角色作者制作的“原样(Native)”形态键，不会重新制作或替换成别的东西。",
                "NK = Native Key. 아바타 제작자가 만든 \"그대로의(Native)\" 쉐이프 키를 사용하며, 다시 만들거나 다른 것으로 교체하지 않습니다."
            },
            ["Profile（任意）"] = new[] { "Profile (Optional)", "Profile（可选）", "Profile (선택)" },
            ["初回は空欄で構いません。保存済みの設定を再利用するときだけProfileを指定します。"] = new[] { "It's fine to leave this blank the first time. Only specify a Profile when reusing saved settings.", "首次使用时留空即可。仅在需要复用已保存的设置时才指定Profile。", "처음 사용할 때는 비워 두어도 됩니다. 저장된 설정을 재사용할 때만 Profile을 지정하세요." },
            ["既存Profile"] = new[] { "Existing Profile", "已有Profile", "기존 Profile" },
            ["プロジェクト内のARKit FT Profileから選択して読み込みます。"] = new[] { "Choose and load from ARKit FT Profiles in the project.", "从项目内的ARKit FT Profile中选择并读取。", "프로젝트 내의 ARKit FT Profile에서 선택하여 불러옵니다." },
            ["新規Profile"] = new[] { "New Profile", "新建Profile", "새 Profile" },
            ["現在の設定を保存する新しいProfileを作成します。"] = new[] { "Create a new Profile that saves the current settings.", "创建一个新的Profile来保存当前设置。", "현재 설정을 저장할 새 Profile을 만듭니다." },
            ["保存"] = new[] { "Save", "保存", "저장" },
            ["対象アバター"] = new[] { "Target Avatar", "目标角色", "대상 아바타" },
            ["再読込"] = new[] { "Reload", "重新读取", "다시 불러오기" },
            ["同じAvatarを選択したままFX等をアバター側で変更した場合、\nObjectFieldの値自体は変化しないため自動では再検出されません。\nこのボタンでAvatar情報(SMR / ARKitチェック / FXレイヤー)を強制的に再読み込みします。"] = new[] { "If you keep the same Avatar selected but change its FX etc. from the avatar side, the ObjectField's value itself doesn't change, so it won't be auto-detected.\nUse this button to force-reload Avatar info (SMR / ARKit check / FX layers).", "如果保持选择同一个Avatar，但在角色一侧修改了FX等内容，由于ObjectField本身的值没有变化，不会自动重新检测。\n请用此按钮强制重新读取Avatar信息(SMR / ARKit检查 / FX图层)。", "동일한 Avatar를 선택한 채로 아바타 쪽에서 FX 등을 변경한 경우, ObjectField의 값 자체는 변하지 않으므로 자동으로 재감지되지 않습니다.\n이 버튼으로 Avatar 정보(SMR / ARKit 체크 / FX 레이어)를 강제로 다시 불러옵니다." },
            ["元のアバターは変更しません。インストール時に複製して処理します。"] = new[] { "The original avatar is never modified. It is duplicated at install time.", "不会修改原始角色。安装时会进行复制后再处理。", "원본 아바타는 변경하지 않습니다. 설치 시 복제하여 처리합니다." },
            ["顔メッシュ設定"] = new[] { "Face Mesh Settings", "面部网格设置", "얼굴 메시 설정" },
            ["アバターを選択すると顔メッシュの設定ができます。"] = new[] { "Select an avatar to configure the face mesh.", "选择角色后即可设置面部网格。", "아바타를 선택하면 얼굴 메시를 설정할 수 있습니다." },
            ["Blendshapeに接頭辞がある"] = new[] { "Blendshapes have a prefix", "Blendshape带有前缀", "Blendshape에 접두사가 있음" },
            ["アバターによって、Blendshape名に接頭辞が付いているためにARKitのシェイプキーを\n正しく検出できないことがあります。接頭辞を指定すると、該当の文字列を除去して検索します。\n1つのアバター内で複数の異なる接頭辞が混在している場合(例: ARKit系は'FT.'、独自の表情\nシェイプ系は'facial.'等)は、カンマ区切りで複数指定してください。シェイプキーごとに、\n実際にメッシュ上へ存在する接頭辞が自動的に選ばれます。"] = new[] { "On some avatars, a prefix on Blendshape names can prevent ARKit shape keys from being detected\ncorrectly. Specifying the prefix will strip that text before searching.\nIf multiple different prefixes coexist within a single avatar (e.g. 'FT.' for the ARKit set and\n'facial.' for a custom expression set), specify them separated by commas. For each shape key, the\nprefix that actually exists on the mesh is chosen automatically.", "根据角色不同，Blendshape名称可能带有前缀，导致无法正确检测到ARKit形态键。\n指定前缀后，将在搜索时去除该字符串。\n如果同一个角色内混用了多种不同的前缀(例如：ARKit系为'FT.'，自定义表情\n形态键系为'facial.'等)，请用逗号分隔指定多个前缀。对于每个形态键，会自动\n选择实际存在于网格上的前缀。", "아바타에 따라 Blendshape 이름에 접두사가 붙어 있어 ARKit 쉐이프 키를\n올바르게 감지하지 못할 수 있습니다. 접두사를 지정하면 해당 문자열을 제거하고 검색합니다.\n하나의 아바타 내에 여러 다른 접두사가 혼재하는 경우(예: ARKit 계열은 'FT.',\n독자적인 표정 쉐이프 계열은 'facial.' 등), 쉼표로 구분하여 여러 개를 지정해\n주세요. 각 쉐이프 키마다, 실제로 메쉬에 존재하는 접두사가 자동으로 선택됩니다." },
            ["空 / 未検出シェイプの同期をオフにする"] = new[] { "Disable sync for empty / undetected shapes", "关闭空/未检测到的形态键的同步", "비어 있음/미검출 쉐이프의 동기화 끄기" },
            ["見た目に影響しないARKitシェイプに対応するNetwork Syncedパラメータをオフにし、bit予算を節約します。"] = new[] { "Turns off Network Synced parameters for ARKit shapes that have no visual effect, saving bit budget.", "关闭对外观没有影响的ARKit形态键所对应的Network Synced参数，以节省bit预算。", "외형에 영향을 주지 않는 ARKit 쉐이프에 대응하는 Network Synced 파라미터를 꺼서 bit 예산을 절약합니다." },
            ["空 / 未検出のシェイプキーを検出した場合、対応する同期パラメータをオフにして節約します。"] = new[] { "When empty / undetected shape keys are found, the corresponding sync parameters are turned off to save budget.", "检测到空/未检测到的形态键时，将关闭对应的同步参数以节省预算。", "비어 있음/미검출 쉐이프 키가 감지되면 해당 동기화 파라미터를 꺼서 절약합니다." },
            ["表情メッシュと目メッシュが別々"] = new[] { "Face mesh and eye mesh are separate", "表情网格与眼部网格是分离的", "표정 메시와 눈 메시가 분리됨" },
            ["にっこり目"] = new[] { "Smile Eyes", "笑眼", "스마일 아이" },
            ["任意のシェイプキーを「にっこり目」として指定できます。\n未指定の場合はARKitのeyeSquintLeft・eyeSquintRightが設定されます。"] = new[] { "You can designate any shape key as \"Smile Eyes\".\nIf left unspecified, ARKit's eyeSquintLeft/eyeSquintRight will be used.", "可以将任意形态键指定为“笑眼”。\n如果未指定，将使用ARKit的eyeSquintLeft・eyeSquintRight。", "임의의 쉐이프 키를 \"스마일 아이\"로 지정할 수 있습니다.\n지정하지 않으면 ARKit의 eyeSquintLeft・eyeSquintRight가 사용됩니다。" },
            ["アバターを選択するとShape Keyを指定できます。"] = new[] { "Select an avatar to specify Shape Keys.", "选择角色后即可指定Shape Key。", "아바타를 선택하면 Shape Key를 지정할 수 있습니다." },
            ["検索"] = new[] { "Search", "搜索", "검색" },
            ["＋ Shape Keyを追加"] = new[] { "+ Add Shape Key", "＋ 添加Shape Key", "+ Shape Key 추가" },
            ["ジェスチャー/メニュー表情の抑制"] = new[] { "Suppress Gesture/Menu Expressions", "抑制手势/菜单表情", "제스처/메뉴 표정 억제" },
            ["フェイストラッキング(MouthTracking)実行中は、ジェスチャー表情が動かないように設定できます。\n混ざってほしくないFXレイヤーをすべて選択してください。"] = new[] { "You can prevent gesture expressions from moving while Face Tracking (MouthTracking) is active.\nSelect all FX layers you don't want mixed in.", "在执行FaceTracking(MouthTracking)期间，可以设置为不让手势表情产生动作。\n请选择所有不希望混入的FX图层。", "페이스 트래킹(MouthTracking) 실행 중에는 제스처 표정이 움직이지 않도록 설정할 수 있습니다.\n섞이지 않았으면 하는 FX 레이어를 모두 선택하세요." },
            ["アバターを選択するとFXレイヤーを指定できます。"] = new[] { "Select an avatar to specify FX layers.", "选择角色后即可指定FX图层。", "아바타를 선택하면 FX 레이어를 지정할 수 있습니다." },
            ["＋ Layerを追加"] = new[] { "+ Add Layer", "＋ 添加图层", "+ 레이어 추가" },
            ["EyeTrackingだけでも抑制する"] = new[] { "Suppress on EyeTracking alone too", "仅EyeTracking时也抑制", "EyeTracking만으로도 억제" },
            ["OFF(既定): MouthTracking時のみ抑制します。\nON: Eyes / Mouthどちらかが有効なら抑制します。\nジェスチャー中も目を動かし続けたい場合はOFFのままにしてください。"] = new[] { "OFF (default): Suppresses only during MouthTracking.\nON: Suppresses if either Eyes or Mouth is active.\nKeep this OFF if you want your eyes to keep moving during gestures.", "OFF(默认): 仅在MouthTracking时抑制。\nON: 只要Eyes或Mouth任一有效就会抑制。\n如果希望在手势中眼睛仍能持续movement，请保持OFF。", "OFF(기본값): MouthTracking 시에만 억제합니다.\nON: Eyes / Mouth 중 하나라도 활성화되면 억제합니다.\n제스처 중에도 눈을 계속 움직이고 싶다면 OFF 상태로 두세요." },
            ["ONにするとEyeTrackingだけでもジェスチャー表情を抑制します。\nジェスチャー中も目を動かしたい場合はOFFのままにしてください。"] = new[] { "Turning this ON suppresses gesture expressions with EyeTracking alone.\nKeep it OFF if you want your eyes to keep moving during gestures.", "开启后，仅EyeTracking也会抑制手势表情。\n如果希望在手势中眼睛仍能movement，请保持OFF。", "ON으로 하면 EyeTracking만으로도 제스처 표정을 억제합니다.\n제스처 중에도 눈을 움직이고 싶다면 OFF 상태로 두세요." },
            ["音声リップシンク形状の抑制"] = new[] { "Suppress Voice Lipsync Shapes", "抑制语音口型同步形状", "음성 립싱크 형태 억제" },
            ["Viseme打消しシェイプキーを生成"] = new[] { "Generate Viseme-cancelling shape keys", "生成抵消Viseme的形态键", "Viseme 상쇄 쉐이프 키 생성" },
            ["発話中のVisemeとFaceTrackingの口形状が重なるのを補正します。"] = new[] { "Compensates for Viseme and FaceTracking mouth shapes overlapping while speaking.", "补偿说话时Viseme与FaceTracking口型形状重叠的问题。", "발화 중 Viseme와 FaceTracking의 입 모양이 겹치는 것을 보정합니다." },
            ["目線とまばたきの制御"] = new[] { "Eye Look & Blink Control", "视线与眨眼控制", "시선 및 눈 깜빡임 제어" },
            ["アイトラッキング用シェイプキーを生成"] = new[] { "Generate eye-tracking shape keys", "生成眼动追踪用形态键", "아이 트래킹용 쉐이프 키 생성" },
            ["フェイストラッキングで動く目線のシェイプキーを、アバターのEyeLook設定から自動生成します。\nEyeLook Strengthを大きくするとわずかな動きにも敏感に反応します。\n目の可動範囲そのものを調整したい場合はEyeLook設定を調整してください。"] = new[] { "Automatically generates eye-look shape keys driven by Face Tracking, based on the avatar's EyeLook settings.\nIncreasing EyeLook Strength makes it react more sensitively to small movements.\nTo adjust the eyes' range of motion itself, adjust the EyeLook settings.", "根据角色的EyeLook设置，自动生成由FaceTracking驱动视线的形态键。\n增大EyeLook Strength会让它对细微的movement更敏感。\n如果想调整眼睛本身的活动范围，请调整EyeLook设置。", "페이스 트래킹으로 움직이는 시선용 쉐이프 키를 아바타의 EyeLook 설정을 기반으로 자동 생성합니다.\nEyeLook Strength를 크게 하면 작은 움직임에도 민감하게 반응합니다.\n눈의 가동 범위 자체를 조정하고 싶다면 EyeLook 설정을 조정하세요." },
            ["アバターの目線制御がConstraint方式"] = new[] { "Avatar's eye look uses Constraints", "角色的视线控制为Constraint方式", "아바타의 시선 제어가 Constraint 방식" },
            ["アバターの目線制御がConstraint方式の場合、実際に目メッシュにウエイトが乗っているボーンを指定してください。"] = new[] { "If the avatar's eye look is Constraint-based, specify the bone that the eye mesh is actually weighted to.", "如果角色的视线控制为Constraint方式，请指定实际承载眼部网格权重的骨骼。", "아바타의 시선 제어가 Constraint 방식인 경우, 실제로 눈 메시에 웨이트가 걸려 있는 본을 지정하세요." },
            ["Eye Look競合対策"] = new[] { "Eye Look Conflict Handling", "Eye Look 冲突对策", "Eye Look 충돌 대책" },
            ["標準Eye Lookを維持"] = new[] { "Keep standard Eye Look", "保持标准Eye Look", "표준 Eye Look 유지" },
            ["FT OFF時もVRChat標準の目線へ戻ります。\nアバターによってはFT中に競合する場合があります。"] = new[] { "Returns to VRChat's standard eye look when FT is OFF too.\nOn some avatars this may conflict while FT is active.", "FT OFF时也会恢复为VRChat标准视线。\n根据角色不同，在FT期间可能会产生冲突。", "FT OFF 시에도 VRChat 표준 시선으로 돌아갑니다.\n아바타에 따라 FT 중 충돌이 발생할 수 있습니다." },
            ["AvatarDescriptor Eye Lookを無効化"] = new[] { "Disable AvatarDescriptor Eye Look", "禁用AvatarDescriptor Eye Look", "AvatarDescriptor Eye Look 비활성화" },
            ["VRChat標準Eye Lookとの競合を根本的に回避します。\nFT OFF時はVRChat標準の自動目線および自動まばたきが動作しません。"] = new[] { "Fundamentally avoids conflicts with VRChat's standard Eye Look.\nWhen FT is OFF, VRChat's standard automatic eye look and auto-blink will not work.", "从根本上避免与VRChat标准Eye Look的冲突。\nFT OFF时，VRChat标准的自动视线及自动眨眼将不会工作。", "VRChat 표준 Eye Look과의 충돌을 근본적으로 회피합니다.\nFT OFF 시 VRChat 표준 자동 시선 및 자동 눈 깜빡임이 동작하지 않습니다." },
            ["眉アシスト"] = new[] { "Brow Assist", "眉毛辅助", "눈썹 보조" },
            ["まばたきに連動して眉を動かすシェイプキーを生成"] = new[] { "Generate shape keys that move brows in sync with blinking", "生成随眨眼联动眉毛的形态键", "눈 깜빡임에 연동해 눈썹을 움직이는 쉐이프 키 생성" },
            ["眉のトラッキングに非対応のデバイス向けに、まばたきに連動して眉を動かす補助シェイプキーを追加します。\n動きすぎる場合はBrow Strengthを小さくすると弱められます。"] = new[] { "Adds auxiliary shape keys that move the brows in sync with blinking, for devices without brow tracking.\nIf the movement is too strong, reduce Brow Strength to weaken it.", "为不支持眉毛追踪的设备添加随眨眼联动眉毛的辅助形态键。\n如果movement过大，可以调低Brow Strength来减弱。", "눈썹 트래킹을 지원하지 않는 기기를 위해 눈 깜빡임에 연동하여 눈썹을 움직이는 보조 쉐이프 키를 추가합니다.\n움직임이 과하면 Brow Strength를 낮춰 약하게 만들 수 있습니다." },
            ["出力設定"] = new[] { "Output Settings", "输出设置", "출력 설정" },
            ["UnityプロジェクトのAssetsフォルダ内を選択してください。"] = new[] { "Please select a location inside the Unity project's Assets folder.", "请在Unity项目的Assets文件夹内进行选择。", "Unity 프로젝트의 Assets 폴더 내에서 선택해 주세요." },
            ["診断・技術情報"] = new[] { "Diagnostics / Technical Info", "诊断・技术信息", "진단・기술 정보" },
            ["ARKitシェイプやAnimatorの詳細診断はConsoleにも出力されます。通常は確認しなくてもインストールできます。"] = new[] { "Detailed diagnostics for ARKit shapes and the Animator are also output to the Console. You can normally install without checking this.", "关于ARKit形态键和Animator的详细诊断信息也会输出到Console。通常无需查看即可完成安装。", "ARKit 쉐이프 및 Animator에 대한 상세 진단은 Console에도 출력됩니다. 보통은 확인하지 않아도 설치할 수 있습니다." },
            ["インストール結果"] = new[] { "Install Result", "安装结果", "설치 결과" },
            ["まだインストール結果はありません。\nINSTALLが完了すると、生成内容・Parameter使用量・出力先などをここにまとめて表示します。"] = new[] { "There is no install result yet.\nOnce INSTALL completes, generated content, Parameter usage, output location, and more will be summarized here.", "尚无安装结果。\nINSTALL完成后，生成内容・Parameter使用量・输出位置等将汇总显示在此处。", "아직 설치 결과가 없습니다.\nINSTALL이 완료되면 생성 내용・Parameter 사용량・출력 위치 등이 여기에 정리되어 표시됩니다." },
            ["✓ インストール完了"] = new[] { "✓ Install Complete", "✓ 安装完成", "✓ 설치 완료" },
            ["Viseme補償"] = new[] { "Viseme Compensation", "Viseme补偿", "Viseme 보정" },
            ["EyeLook"] = new[] { "EyeLook", "EyeLook", "EyeLook" },
            ["標準Eye Look"] = new[] { "Standard Eye Look", "标准Eye Look", "표준 Eye Look" },
            ["同期最適化"] = new[] { "Sync Optimization", "同步优化", "동기화 최적화" },
            ["出力先"] = new[] { "Output Location", "输出位置", "출력 위치" },
            ["生成アバターを選択"] = new[] { "Select Generated Avatar", "选择生成的角色", "생성된 아바타 선택" },
            ["出力フォルダを表示"] = new[] { "Show Output Folder", "显示输出文件夹", "출력 폴더 표시" },
            ["Face Meshを選択するとShape Key一覧が表示されます。"] = new[] { "Select a Face Mesh to see the list of Shape Keys.", "选择Face Mesh后将显示Shape Key列表。", "Face Mesh를 선택하면 Shape Key 목록이 표시됩니다." },
            ["「{0}」に一致するShape Keyがありません。"] = new[] { "No Shape Key matches \"{0}\".", "没有与“{0}”匹配的Shape Key。", "\"{0}\"와(과) 일치하는 Shape Key가 없습니다." },
            ["AvatarDescriptorのFXが見つかりません。"] = new[] { "AvatarDescriptor's FX could not be found.", "找不到AvatarDescriptor的FX。", "AvatarDescriptor의 FX를 찾을 수 없습니다." },
            ["「{0}」に一致するLayerがありません。"] = new[] { "No Layer matches \"{0}\".", "没有与“{0}”匹配的Layer。", "\"{0}\"와(과) 일치하는 Layer가 없습니다." },
            ["Avatarを選択してください"] = new[] { "Please select an Avatar", "请选择Avatar", "Avatar를 선택해 주세요" },
            ["Avatar未選択"] = new[] { "Avatar not selected", "Avatar未选择", "Avatar 미선택" },
            ["インストール対象のAvatarをまだ選択していません。"] = new[] { "You haven't selected an Avatar to install to yet.", "尚未选择要安装的Avatar。", "설치 대상 Avatar를 아직 선택하지 않았습니다." },
            ["⚠ Face Mesh未検出"] = new[] { "⚠ Face Mesh not detected", "⚠ 未检测到Face Mesh", "⚠ Face Mesh 미검출" },
            ["アバターにSkinnedMeshRendererが見つかりません。FACEカードでFace Meshを確認してください。"] = new[] { "No SkinnedMeshRenderer found on the avatar. Please check Face Mesh in the FACE card.", "在角色上找不到SkinnedMeshRenderer。请在FACE卡片中确认Face Mesh。", "아바타에서 SkinnedMeshRenderer를 찾을 수 없습니다. FACE 카드에서 Face Mesh를 확인하세요." },
            ["ARKit標準52シェイプキーがすべてメッシュ上に存在します。"] = new[] { "All 52 standard ARKit shape keys are present on the mesh.", "网格上存在全部52个标准ARKit形态键。", "표준 ARKit 52개 쉐이프 키가 모두 메시에 존재합니다." },
            ["メッシュに存在しないARKitシェイプキー:\n"] = new[] { "ARKit shape keys not present on the mesh:\n", "网格上不存在的ARKit形态键：\n", "메시에 존재하지 않는 ARKit 쉐이프 키:\n" },
            ["中身が空 / 未検出のARKitシェイプキー:\n"] = new[] { "ARKit shape keys that are empty / undetected:\n", "内容为空/未检测到的ARKit形态键：\n", "내용이 비어 있음/미검출 ARKit 쉐이프 키:\n" },
            ["「にっこり目」シェイプキーが指定されています。"] = new[] { "A \"Smile Eyes\" shape key has been specified.", "已指定“笑眼”形态键。", "\"스마일 아이\" 쉐이프 키가 지정되어 있습니다." },
            ["にっこり目未設定 (EyeSquintで代替)"] = new[] { "Smile Eyes not set (falls back to EyeSquint)", "笑眼未设置(以EyeSquint代替)", "스마일 아이 미설정 (EyeSquint로 대체)" },
            ["未指定のため、ARKitのeyeSquintLeft・eyeSquintRightがそのまま使われます。"] = new[] { "Since it's unspecified, ARKit's eyeSquintLeft/eyeSquintRight are used as-is.", "由于未指定，将直接使用ARKit的eyeSquintLeft・eyeSquintRight。", "지정하지 않았으므로 ARKit의 eyeSquintLeft・eyeSquintRight가 그대로 사용됩니다." },
            ["ジェスチャー表情抑制"] = new[] { "Gesture Expression Suppression", "手势表情抑制", "제스처 표정 억제" },
            ["指定したFXレイヤーをフェイストラッキング中は抑制します。"] = new[] { "Suppresses the specified FX layers while Face Tracking is active.", "在FaceTracking期间抑制指定的FX图层。", "지정한 FX 레이어를 페이스 트래킹 중에 억제합니다." },
            ["⚠ 抑制レイヤー未選択"] = new[] { "⚠ No suppression layer selected", "⚠ 未选择抑制图层", "⚠ 억제 레이어 미선택" },
            ["抑制レイヤーが未選択です。フェイストラッキング中もジェスチャー/メニュー表情が混ざります。"] = new[] { "No suppression layer is selected. Gesture/Menu expressions will mix in even during Face Tracking.", "未选择抑制图层。即使在FaceTracking期间，手势/菜单表情也会混入。", "억제 레이어가 선택되지 않았습니다. 페이스 트래킹 중에도 제스처/메뉴 표정이 섞입니다." },
            ["EyeLookシェイプキーを自動生成します。"] = new[] { "Automatically generates EyeLook shape keys.", "自动生成EyeLook形态键。", "EyeLook 쉐이프 키를 자동 생성합니다." },
            ["AvatarDescriptorのEye Lookを無効化し、VRChat標準との競合を回避します。"] = new[] { "Disables AvatarDescriptor's Eye Look, avoiding conflicts with VRChat's standard.", "禁用AvatarDescriptor的Eye Look，避免与VRChat标准产生冲突。", "AvatarDescriptor의 Eye Look을 비활성화하여 VRChat 표준과의 충돌을 회피합니다。" },
            ["VRChat標準のEye Lookを維持します。アバターによってはFT中に競合する場合があります。"] = new[] { "Keeps VRChat's standard Eye Look. On some avatars this may conflict while FT is active.", "保持VRChat标准的Eye Look。根据角色不同，在FT期间可能会产生冲突。", "VRChat 표준 Eye Look을 유지합니다. 아바타에 따라 FT 중 충돌이 발생할 수 있습니다." },
            ["Viseme"] = new[] { "Viseme", "Viseme", "Viseme" },
            ["逆Viseme補償シェイプキーを生成します。"] = new[] { "Generates inverse-Viseme compensation shape keys.", "生成逆Viseme补偿形态键。", "역 Viseme 보정 쉐이프 키를 생성합니다." },
            ["まばたき連動の眉アシストシェイプキーを生成します。"] = new[] { "Generates blink-linked brow assist shape keys.", "生成与眨眼联动的眉毛辅助形态键。", "눈 깜빡임 연동 눈썹 보조 쉐이프 키를 생성합니다." },
            ["✓ Avatar"] = new[] { "✓ Avatar", "✓ Avatar", "✓ Avatar" },
            ["Avatarが選択されています。"] = new[] { "An Avatar is selected.", "已选择Avatar。", "Avatar가 선택되어 있습니다." },
            ["⚠ Eye競合 {0}"] = new[] { "⚠ Eye conflict {0}", "⚠ Eye冲突 {0}", "⚠ Eye 충돌 {0}" },
            ["EyesをTrackingへ切り替えるFXレイヤー"] = new[] { "FX layers that switch Eyes to Tracking", "将Eyes切换为Tracking的FX图层", "Eyes를 Tracking으로 전환하는 FX 레이어" },
            ["(ジェスチャー切替のたびに再発火し、VRChat標準の目線制御へ一時的に"] = new[] { "(Re-fires every time a gesture changes, temporarily reverting to VRChat's standard eye control", "(每次切换手势都会再次触发，暂时恢复为VRChat标准视线控制", "(제스처를 전환할 때마다 다시 발동되어, 일시적으로 VRChat 표준 시선 제어로" },
            ["戻ってしまう可能性があります):\n"] = new[] { "and may revert):\n", "，存在恢复的可能性)：\n", "돌아갈 가능성이 있습니다):\n" },
            ["Face Mesh未検出"] = new[] { "Face Mesh not detected", "未检测到Face Mesh", "Face Mesh 미검출" },
            ["アバターにSkinnedMeshRendererが見つかりません。"] = new[] { "No SkinnedMeshRenderer found on the avatar.", "在角色上找不到SkinnedMeshRenderer。", "아바타에서 SkinnedMeshRenderer를 찾을 수 없습니다." },
            ["Empty / 未検出 {0}"] = new[] { "Empty / undetected {0}", "空/未检测到 {0}", "비어있음/미검출 {0}" },
            ["対象メッシュのRead/Writeが無効なため、「中身が空」のシェイプキー検出ができません。\nImport SettingsでRead/Writeを有効にしてください。"] = new[] { "Read/Write is disabled on the target mesh, so \"empty content\" shape key detection can't run.\nEnable Read/Write in the Import Settings.", "由于目标网格的Read/Write已禁用，无法检测“内容为空”的形态键。\n请在Import Settings中启用Read/Write。", "대상 메시의 Read/Write가 비활성화되어 있어 \"내용이 비어 있음\" 쉐이프 키 감지를 할 수 없습니다.\nImport Settings에서 Read/Write를 활성화하세요." },
            ["対応表(ARKit_FT_ShapeParamMap.asset)を参照し、1対1対応が確認できたシェイプのみ同期をオフにします。"] = new[] { "References the mapping table (ARKit_FT_ShapeParamMap.asset) and disables sync only for shapes with a confirmed 1:1 mapping.", "参照对应表(ARKit_FT_ShapeParamMap.asset)，仅关闭已确认1对1对应的形态键的同步。", "매핑 테이블(ARKit_FT_ShapeParamMap.asset)을 참조하여 1:1 대응이 확인된 쉐이프에 대해서만 동기화를 끕니다." },
            ["対応表が見つからないため、パラメータ名の部分一致で判定します(やや不正確です)。"] = new[] { "No mapping table was found, so matching falls back to partial parameter-name matching (somewhat inaccurate).", "由于找不到对应表，将以参数名的部分匹配进行判定(略欠准确)。", "매핑 테이블을 찾을 수 없어 파라미터 이름의 부분 일치로 판정합니다(다소 부정확합니다)." },
            ["✓ TrackingControl競合候補 {0}件を検出していますが、\n"] = new[] { "✓ Detected {0} TrackingControl conflict candidate(s), but\n", "✓ 检测到 {0} 个TrackingControl冲突候选，但\n", "✓ TrackingControl 충돌 후보 {0}건을 감지했지만\n" },
            ["Stableモードが選択されているため影響を受けません。"] = new[] { "Stable mode is selected, so it is not affected.", "由于已选择Stable模式，不受其影响。", "Stable 모드가 선택되어 있어 영향을 받지 않습니다." },
            ["⚠ TrackingControl競合候補 {0}件\n"] = new[] { "⚠ {0} TrackingControl conflict candidate(s)\n", "⚠ {0} 个TrackingControl冲突候选\n", "⚠ TrackingControl 충돌 후보 {0}건\n" },
            ["\nStableモードを推奨します。"] = new[] { "\nStable mode is recommended.", "\n推荐使用Stable模式。", "\nStable 모드를 권장합니다." },
            ["✓ Eye TrackingControlの競合候補は見つかりませんでした。"] = new[] { "✓ No Eye TrackingControl conflict candidates were found.", "✓ 未发现Eye TrackingControl冲突候选。", "✓ Eye TrackingControl 충돌 후보를 찾지 못했습니다." },
            ["プロジェクト内にProfileが見つかりません"] = new[] { "No Profiles found in the project", "项目内未找到Profile", "프로젝트 내에서 Profile을 찾을 수 없습니다" },
            ["ファイルから選択..."] = new[] { "Choose from file...", "从文件中选择...", "파일에서 선택..." },
            ["ARKit FT Profileを選択"] = new[] { "Select ARKit FT Profile", "选择ARKit FT Profile", "ARKit FT Profile 선택" },
            ["Profileを読み込めません"] = new[] { "Cannot load Profile", "无法读取Profile", "Profile을 불러올 수 없습니다" },
            ["UnityプロジェクトのAssetsフォルダ内にある .asset を選択してください。"] = new[] { "Please select a .asset file inside the Unity project's Assets folder.", "请选择位于Unity项目Assets文件夹内的.asset文件。", "Unity 프로젝트의 Assets 폴더 안에 있는 .asset 파일을 선택하세요." },
            ["読み込み不可 / Missing Scriptの可能性あり"] = new[] { "Cannot load / possibly Missing Script", "无法读取 / 可能为Missing Script", "불러올 수 없음 / Missing Script 가능성 있음" },
            ["選択した .asset をARKit FT Profileとして認識できませんでした。\n\n"] = new[] { "The selected .asset could not be recognized as an ARKit FT Profile.\n\n", "无法将所选的.asset识别为ARKit FT Profile。\n\n", "선택한 .asset을 ARKit FT Profile로 인식할 수 없었습니다.\n\n" },
            ["既存Profileでこの表示になる場合は、ARKitFTProfile.cs の .meta が作り直されていないか確認してください。"] = new[] { "If an existing Profile shows this, check whether ARKitFTProfile.cs's .meta was recreated.", "如果已有的Profile出现此提示，请确认ARKitFTProfile.cs的.meta是否被重新生成。", "기존 Profile에서 이 화면이 나타난다면 ARKitFTProfile.cs의 .meta가 다시 생성되지 않았는지 확인하세요。" },
            ["ScriptableObjectは .meta のGUIDでスクリプトと結び付いているため、.metaを変更すると既存ProfileがMissing Scriptになります。"] = new[] { "Since ScriptableObjects are linked to their script via the .meta's GUID, changing the .meta will turn existing Profiles into Missing Script.", "由于ScriptableObject是通过.meta的GUID与脚本关联的，更改.meta会导致已有Profile变成Missing Script。", "ScriptableObject는 .meta의 GUID로 스크립트와 연결되어 있으므로, .meta를 변경하면 기존 Profile이 Missing Script가 됩니다." },
            ["OK"] = new[] { "OK", "确定", "확인" },
            ["プロファイルの保存先を選択してください"] = new[] { "Please choose where to save the Profile", "请选择Profile的保存位置", "Profile을 저장할 위치를 선택하세요" },
            ["Output FolderはUnityプロジェクトのAssetsフォルダ内を指定してください。"] = new[] { "Please specify an Output Folder inside the Unity project's Assets folder.", "请将Output Folder指定在Unity项目的Assets文件夹内。", "Output Folder는 Unity 프로젝트의 Assets 폴더 내로 지정해 주세요." },
            ["... 他{0}個"] = new[] { "... and {0} more", "...另有{0}个", "... 외 {0}개" },
            ["警告: ARKitシェイプキーが {0} 個不足しています"] = new[] { "Warning: {0} ARKit shape key(s) are missing", "警告：缺少 {0} 个ARKit形态键", "경고: ARKit 쉐이프 키가 {0}개 부족합니다" },
            ["以下のシェイプキーが見つかりません:\n{0}\n\n"] = new[] { "The following shape keys were not found:\n{0}\n\n", "以下形态键未找到：\n{0}\n\n", "다음 쉐이프 키를 찾을 수 없습니다:\n{0}\n\n" },
            ["テンプレートアセットが見つかりません。\n"] = new[] { "Template assets could not be found.\n", "未找到模板资源。\n", "템플릿 에셋을 찾을 수 없습니다.\n" },
            ["パッケージの Templates/ フォルダに以下のファイルが必要です:\n"] = new[] { "The following files are required in the package's Templates/ folder:\n", "软件包的Templates/文件夹中需要以下文件：\n", "패키지의 Templates/ 폴더에 다음 파일이 필요합니다:\n" },
            ["アバターの複製に失敗しました。"] = new[] { "Failed to duplicate the avatar.", "复制角色失败。", "아바타 복제에 실패했습니다." },
            ["複製したアバターにVRCAvatarDescriptorが見つかりませんでした。"] = new[] { "VRCAvatarDescriptor could not be found on the duplicated avatar.", "在复制的角色上未找到VRCAvatarDescriptor。", "복제된 아바타에서 VRCAvatarDescriptor를 찾을 수 없었습니다." },
            ["複製したアバター上でFace SMRが見つかりませんでした。"] = new[] { "Face SMR could not be found on the duplicated avatar.", "在复制的角色上未找到Face SMR。", "복제된 아바타에서 Face SMR을 찾을 수 없었습니다." },
            ["複製したアバター上でEye SMRが見つかりませんでした。"] = new[] { "Eye SMR could not be found on the duplicated avatar.", "在复制的角色上未找到Eye SMR。", "복제된 아바타에서 Eye SMR을 찾을 수 없었습니다." },
            ["FXテンプレートのコピーに失敗しました。"] = new[] { "Failed to copy the FX template.", "复制FX模板失败。", "FX 템플릿 복사에 실패했습니다." },
            ["コピーしたFXを読み込めませんでした。"] = new[] { "Could not load the copied FX.", "无法读取已复制的FX。", "복사한 FX를 불러올 수 없었습니다." },
            ["無効"] = new[] { "Disabled", "无效", "비활성화" },
            ["スキップ(LipSyncがVisemeBlendShapeではありません)"] = new[] { "Skipped (LipSync is not VisemeBlendShape)", "跳过(LipSync不是VisemeBlendShape)", "건너뜀 (LipSync가 VisemeBlendShape가 아님)" },
            ["スキップ(Viseme SMR未設定)"] = new[] { "Skipped (Viseme SMR not set)", "跳过(未设置Viseme SMR)", "건너뜀 (Viseme SMR 미설정)" },
            ["生成済み"] = new[] { "Generated", "已生成", "생성됨" },
            ["スキップ(生成条件を満たしません)"] = new[] { "Skipped (generation conditions not met)", "跳过(不满足生成条件)", "건너뜀 (생성 조건을 충족하지 않음)" },
            ["Menuテンプレートのコピーに失敗しました。"] = new[] { "Failed to copy the Menu template.", "复制Menu模板失败。", "Menu 템플릿 복사에 실패했습니다." },
            ["Parametersテンプレートのコピーに失敗しました。"] = new[] { "Failed to copy the Parameters template.", "复制Parameters模板失败。", "Parameters 템플릿 복사에 실패했습니다." },
            ["生成したMenuまたはParametersを読み込めませんでした。"] = new[] { "Could not load the generated Menu or Parameters.", "无法读取生成的Menu或Parameters。", "생성된 Menu 또는 Parameters를 불러올 수 없었습니다." },
            ["Modular Avatar用Prefabの保存に失敗しました。"] = new[] { "Failed to save the Modular Avatar Prefab.", "保存Modular Avatar用Prefab失败。", "Modular Avatar용 Prefab 저장에 실패했습니다." },
            ["保存したModular Avatar用Prefabを読み込めませんでした。"] = new[] { "Could not load the saved Modular Avatar Prefab.", "无法读取已保存的Modular Avatar用Prefab。", "저장한 Modular Avatar용 Prefab을 불러올 수 없었습니다." },
            ["Modular Avatar用Prefabの配置に失敗しました。"] = new[] { "Failed to place the Modular Avatar Prefab.", "放置Modular Avatar用Prefab失败。", "Modular Avatar용 Prefab 배치에 실패했습니다." },
            ["有効(強度{0:0.0}x)"] = new[] { "Enabled (strength {0:0.0}x)", "有效(强度{0:0.0}倍)", "활성화(강도 {0:0.0}배)" },
            ["未指定"] = new[] { "Not specified", "未指定", "미지정" },
            [" / 強さ {0:0.00}"] = new[] { " / strength {0:0.00}", " / 强度 {0:0.00}", " / 세기 {0:0.00}" },
            ["有効 / 強度 {0:P0}"] = new[] { "Enabled / strength {0:P0}", "有效 / 强度 {0:P0}", "활성화 / 강도 {0:P0}" },
            ["無効化 / FT OFF時は自動目線なし・自動まばたきなし"] = new[] { "Disabled / When FT is OFF: no auto eye-look, no auto-blink", "禁用 / FT OFF时无自动视线・无自动眨眼", "비활성화 / FT OFF 시 자동 시선 없음・자동 눈 깜빡임 없음" },
            ["維持 / Compatibility"] = new[] { "Kept / Compatibility", "保持 / Compatibility", "유지 / Compatibility" },
            ["  ⚠ 超過（既存 {0} + FT {1}）"] = new[] { "  ⚠ Over budget (existing {0} + FT {1})", "  ⚠ 超出(现有 {0} + FT {1})", "  ⚠ 초과(기존 {0} + FT {1})" },
            ["空/未検出 {0}件 → 同期OFF {1}件"] = new[] { "Empty/undetected: {0} → Sync OFF: {1}", "空/未检测到 {0}个 → 同步OFF {1}个", "비어있음/미검출 {0}건 → 동기화 OFF {1}건" },
            ["インストールに失敗しました。途中生成物は可能な範囲で削除します。\n\n"] = new[] { "Install failed. Intermediate generated files will be removed where possible.\n\n", "安装失败。将尽可能删除中途生成的产物。\n\n", "설치에 실패했습니다. 도중에 생성된 산출물은 가능한 범위에서 삭제합니다.\n\n" },
            ["まばたきエフェクトを追加"] = new[] { "Add Blink Effect", "添加眨眼特效", "눈 깜빡임 이펙트 추가" },
            ["OFFにすると、まばたき時のおまけ演出そのものを含めずにインストールします。"] = new[] { "Turning this OFF installs without the bonus blink effect at all.", "关闭后，安装时将完全不包含眨眼时的附加演出。", "OFF으로 하면 눈 깜빡임 시의 보너스 연출 자체를 포함하지 않고 설치합니다." },
            ["エフェクトクリップ (任意)"] = new[] { "Effect Clip (Optional)", "特效片段(可选)", "이펙트 클립 (선택)" },
            ["まばたきエフェクト"] = new[] { "Blink Effect", "眨眼特效", "눈 깜빡임 이펙트" },
            ["指定したクリップをまばたき時のおまけ演出として使用します。"] = new[] { "Uses the specified clip as the bonus effect when blinking.", "将指定的片段用作眨眼时的附加演出。", "지정한 클립을 눈 깜빡임 시의 보너스 연출로 사용합니다。" },
            ["まばたきエフェクトなし"] = new[] { "No Blink Effect", "无眨眼特效", "눈 깜빡임 이펙트 없음" },
            ["まばたき時のおまけ演出を含めずにインストールします。"] = new[] { "Installs without the bonus blink effect.", "安装时不包含眨眼时的附加演出。", "눈 깜빡임 시 보너스 연출을 포함하지 않고 설치합니다." },
            ["なし"] = new[] { "None", "无", "없음" },
            ["再生後は自動的に元の状態へ戻ります(Write Defaultsによる汎用リセット)。\nただしARKit標準のシェイプキー(まばたきや口の動きなど)を同じクリップで動かすと、\nフェイストラッキングの値と一瞬競合することがあるため、専用の演出用シェイプキーを\n使うことをおすすめします。"] = new[] { "After playback, it automatically returns to the prior state (a generic reset via Write Defaults).\nHowever, if the same clip also animates standard ARKit shape keys (blinking, mouth movement, etc.),\nit may briefly conflict with Face Tracking values, so we recommend using shape keys dedicated\nto this effect.", "播放结束后会自动恢复原状(通过Write Defaults实现的通用重置)。\n但如果同一片段还操作了标准ARKit形态键(眨眼、嘴部动作等)，\n可能会与FaceTracking的数值产生短暂冲突，建议使用专用于该演出的形态键。", "재생 후에는 자동으로 원래 상태로 돌아갑니다(Write Defaults를 통한 범용 리셋).\n다만 동일한 클립으로 표준 ARKit 쉐이프 키(눈 깜빡임이나 입 움직임 등)를 함께 움직이면\n페이스 트래킹 값과 순간적으로 충돌할 수 있으므로, 전용 연출용 쉐이프 키를\n사용하는 것을 권장합니다." },
            ["おまけ機能"] = new[] { "Bonus Feature", "附加功能", "보너스 기능" },
            ["まばたきするたびに発動するアニメーションクリップを設定できます。\nテンプレートFXに既定の演出は同梱されていません(アバターによってシェイプキーの構成が\n異なり、汎用的な演出クリップを用意できないためです)。アバター固有のシェイプキーで\n構成したエフェクト用のAnimationClipを別途ご用意のうえ、下欄で指定してください。"] = new[] { "You can set an animation clip that plays each time you blink.\nNo default effect is bundled with the template FX (since shape keys differ between avatars,\nno generic effect clip can be provided). Please prepare your own AnimationClip built from\nyour avatar's own shape keys and specify it below.", "可以设置一个每次眨眼时都会播放的动画剪辑。\n模板FX中并未内置默认演出(因为不同角色的形态键构成各不相同，\n无法提供通用的演出片段)。请另行准备使用你自己角色专属形态键制作的\nAnimationClip，并在下方指定。", "눈을 깜빡일 때마다 재생되는 애니메이션 클립을 설정할 수 있습니다.\n템플릿 FX에는 기본 연출이 포함되어 있지 않습니다(아바타마다 쉐이프 키 구성이\n달라 범용적인 연출 클립을 제공할 수 없기 때문입니다). 아바타 고유의 쉐이프 키로\n구성한 이펙트용 AnimationClip을 별도로 준비하여 아래에서 지정해 주세요." },
            ["アバター固有のシェイプキーで構成したAnimationClipを指定してください。\n未指定のままだと、まばたき時に何も再生されません。"] = new[] { "Specify an AnimationClip built from your avatar's own shape keys.\nIf left unspecified, nothing will play when blinking.", "请指定使用你角色专属形态键制作的AnimationClip。\n如果保持未指定，眨眼时将不会播放任何内容。", "아바타 고유의 쉐이프 키로 구성한 AnimationClip을 지정하세요.\n지정하지 않으면 눈을 깜빡일 때 아무것도 재생되지 않습니다." },
            ["まばたきで揺れる瞳ハイライトのアニメーションなどを設定してください。"] = new[] { "Set something like an animation of the eye highlight swaying with each blink.", "请设置例如随眨眼摆动的瞳孔高光动画等演出。", "눈을 깜빡일 때 흔들리는 눈동자 하이라이트 애니메이션 등을 설정해 주세요." },
            ["有効 / クリップ設定済み ({0})"] = new[] { "Enabled / clip set ({0})", "有效 / 已设置片段 ({0})", "활성화 / 클립 설정됨 ({0})" },
            ["有効 / クリップ未設定(演出は再生されません)"] = new[] { "Enabled / no clip set (nothing will play)", "有效 / 未设置片段(不会播放演出)", "활성화 / 클립 미설정 (연출이 재생되지 않음)" },
            ["⚠ エフェクトクリップ未設定"] = new[] { "⚠ No effect clip set", "⚠ 未设置特效片段", "⚠ 이펙트 클립 미설정" },
            ["クリップが未指定のため、まばたき時に演出は再生されません。"] = new[] { "Since no clip is specified, nothing will play when blinking.", "由于未指定片段，眨眼时不会播放任何演出。", "클립이 지정되지 않아 눈을 깜빡일 때 연출이 재생되지 않습니다." },
            ["識別タグ (任意)"] = new[] { "Match Tag (Optional)", "识别标签(可选)", "식별 태그 (선택)" },
            ["このProfileを自動選択する際の目印となる文字列。Avatar名にこの文字列が含まれていれば、\nバージョン名や接頭辞・接尾辞が付いていても最優先でこのProfileが選ばれる。\nカンマ区切りで複数指定できる(いずれか1つでも一致すれば選ばれる)。複数のProfileやタグが\n同時に一致した場合は、Avatar名との完全一致を最優先し、次により長いタグ、最後にタグの\n登録順(先に書いたもの)で決まる。これにより「Sumiya」と「miya」のような包含関係も区別できる。\n空の場合はファイル名からの推測にフォールバックする。\n「保存」ボタンで他の設定と一緒に保存される。"] = new[] { "A string used as a marker for automatically selecting this Profile. If the Avatar's name\ncontains this string, this Profile is chosen first, even with version numbers or a\nprefix/suffix attached.\nMultiple tags can be given, separated by commas (a match on any one of them is enough). If\nmultiple Profiles or tags match, an exact Avatar-name match wins first, then the longer\nmatching tag, then the tag written earlier. This distinguishes contained names such as\n\"Sumiya\" and \"miya\".\nIf left blank, matching falls back to guessing from the file name.\nSaved together with the other settings via the \"Save\" button.", "用于自动选择该Profile的标记字符串。只要Avatar名称中包含此字符串，\n即使带有版本号或前后缀，也会优先选中该Profile。\n可用逗号分隔指定多个标签(只要其中任意一个匹配即可)。当多个Profile或标签同时匹配时，\n优先选择与Avatar名称完全一致的标签，其次选择更长的标签，最后按标签的书写顺序决定。\n这样可以区分“Sumiya”和“miya”这类包含关系。\n留空时将回退为根据文件名进行推测。\n会通过“保存”按钮与其他设置一起保存。", "이 Profile을 자동으로 선택할 때 기준이 되는 문자열입니다. Avatar 이름에 이 문자열이\n포함되어 있으면, 버전 이름이나 접두사・접미사가 붙어 있어도 이 Profile이\n최우선으로 선택됩니다.\n쉼표로 구분하여 여러 개를 지정할 수 있습니다(그 중 하나라도 일치하면 선택됩니다).\n여러 Profile이나 태그가 동시에 일치하면 Avatar 이름과 완전히 일치하는 태그를 먼저,\n그다음 더 긴 태그를, 마지막으로 먼저 작성한 태그를 우선합니다. 이를 통해\n\"Sumiya\"와 \"miya\"처럼 포함 관계인 이름도 구분할 수 있습니다.\n비워두면 파일 이름 기반 추측으로 대체됩니다.\n\"저장\" 버튼으로 다른 설정과 함께 저장됩니다." },
            ["Profileに保存されたFace Meshが現在のアバター上に見つかりませんでした(アバターの階層が変わった可能性があります)。下のFace Meshを確認し、必要なら選び直してください。"] = new[] { "The Face Mesh saved in the Profile could not be found on the current avatar (its hierarchy may have changed). Please check the Face Mesh below and reselect it if needed.", "在当前角色上未找到Profile中保存的Face Mesh(角色的层级结构可能已发生变化)。请确认下方的Face Mesh，如有需要请重新选择。", "Profile에 저장된 Face Mesh를 현재 아바타에서 찾을 수 없습니다(아바타의 계층 구조가 변경되었을 수 있습니다). 아래의 Face Mesh를 확인하고 필요하면 다시 선택해 주세요." },
            ["キャンセル"] = new[] { "Cancel", "取消", "취소" },
            ["Profileの内容が現在のアバターと一致しない可能性があります"] = new[] { "This Profile's contents may not match the current avatar", "该Profile的内容可能与当前角色不匹配", "이 Profile의 내용이 현재 아바타와 일치하지 않을 수 있습니다" },
            ["適用する"] = new[] { "Apply", "应用", "적용" },
            ["適用しない"] = new[] { "Don't Apply", "不应用", "적용 안 함" },
            ["選択したProfile('{0}')に保存されているFace Mesh('{1}')が、\n現在選択中のアバターには見つかりません。\n\nこのまま適用すると、現在のアバターに対する作業内容がこのProfileの設定で\n上書きされます。"] = new[] { "The Face Mesh ('{1}') saved in the selected Profile ('{0}') could not be found on the\ncurrently selected avatar.\n\nApplying it now will overwrite your current work on this avatar with this Profile's settings.", "所选Profile(“{0}”)中保存的Face Mesh(“{1}”)在当前选择的角色上未找到。\n\n如果继续应用，当前角色的作业内容将被该Profile的设置覆盖。", "선택한 Profile('{0}')에 저장된 Face Mesh('{1}')를 현재 선택된 아바타에서\n찾을 수 없습니다.\n\n지금 적용하면 현재 아바타에 대한 작업 내용이 이 Profile의 설정으로\n덮어써집니다." },
            ["まずアバターを選択してください。名前や識別タグが対応するProfileがあれば自動的に選択されます。"] = new[] { "First select an avatar. A matching Profile will be selected automatically if its name or match tag corresponds.", "请先选择一个角色。如果存在名称或识别标签对应的Profile，将会自动选中。", "먼저 아바타를 선택하세요. 이름이나 식별 태그가 일치하는 Profile이 있으면 자동으로 선택됩니다." },
            ["見つからない場合は「新規Profile」で作成、または「既存Profile」から選び直せます。"] = new[] { "If none is found, create one with \"New Profile\", or choose one manually from \"Existing Profile\".", "如果找不到，可以通过“新建Profile”创建，或从“已有Profile”中重新选择。", "찾을 수 없는 경우 \"새 Profile\"로 만들거나 \"기존 Profile\"에서 다시 선택할 수 있습니다." },
            ["⚠ Parameter超過のおそれ {0}/{1}bit"] = new[] { "⚠ Parameter may exceed budget {0}/{1}bit", "⚠ Parameter可能超出预算 {0}/{1}bit", "⚠ Parameter 예산 초과 우려 {0}/{1}bit" },
            ["既存 {0}bit + FT追加分(最適化前) {1}bit = {2}bit で、上限{3}bitを超える可能性があります。\n「空 / 未検出シェイプの同期をオフにする」を有効にすると実際の使用量を抑えられます。\n正確な値はインストール実行時に確定します。\n\n注意: Modular Avatar / NDMFの非破壊コンポーネント(トグル等)がパラメータを追加する場合、\nそれらはVRChat SDKでのビルド時に初めて確定するため、Manual Bake前はこの見積りに\n含まれません。Manual Bake後の値が最も正確です。"] = new[] { "Existing {0}bit + FT addition (before optimization) {1}bit = {2}bit, which may exceed the {3}bit limit.\nEnabling \"Disable sync for empty / undetected shapes\" can reduce actual usage.\nThe exact value is determined when you run Install.\n\nNote: If Modular Avatar / NDMF non-destructive components (such as toggles) add parameters,\nthose are only finalized when VRChat SDK builds the avatar, so they are not included in this\nestimate before Manual Bake. The value is most accurate after Manual Bake.", "现有 {0}bit + FT新增(优化前) {1}bit = {2}bit，可能超出 {3}bit 的上限。\n启用“关闭空/未检测到的形态键的同步”可以降低实际使用量。\n准确数值将在执行安装时确定。\n\n注意：如果Modular Avatar / NDMF的非破坏性组件(如开关等)会添加参数，\n这些参数只有在VRChat SDK构建时才会确定，因此在Manual Bake之前不会计入本估算。\nManual Bake之后的数值最为准确。", "기존 {0}bit + FT 추가분(최적화 전) {1}bit = {2}bit로, 상한 {3}bit를 초과할 가능성이 있습니다.\n\"비어 있음/미검출 쉐이프의 동기화 끄기\"를 활성화하면 실제 사용량을 줄일 수 있습니다.\n정확한 값은 설치를 실행할 때 확정됩니다.\n\n주의: Modular Avatar / NDMF의 비파괴 컴포넌트(토글 등)가 파라미터를 추가하는 경우,\n이는 VRChat SDK 빌드 시에 비로소 확정되므로 Manual Bake 이전에는 이 견적에\n포함되지 않습니다. Manual Bake 이후의 값이 가장 정확합니다." },
            ["ARKit標準名のシェイプキーが見つからない場合、VRCFaceTrackingの\n「Unified Expressions」側の代替名(例: cheekPuff → CheekPuff、またはCheekPuffLeft/Right)\nでも検索します。左右分割のシェイプキーが見つかった場合は、同じカーブを両方へ\n複製設定します(1つのパラメータで両方が同時に動きます)。"] = new[] { "If a shape key with the standard ARKit name can't be found, this also searches for the\nequivalent name on VRCFaceTracking's \"Unified Expressions\" side (e.g. cheekPuff → CheekPuff,\nor CheekPuffLeft/Right). If a left/right split pair is found, the same curve is duplicated\nonto both (so a single parameter drives both at once).", "如果找不到标准ARKit名称的形态键，也会搜索VRCFaceTracking\n“Unified Expressions”一侧的对应名称(例如cheekPuff → CheekPuff，\n或CheekPuffLeft/Right)。如果找到左右分开的形态键，会将同一条曲线\n复制设置到两者(即用同一个参数同时驱动两者)。", "표준 ARKit 이름의 쉐이프 키를 찾을 수 없는 경우, VRCFaceTracking의\n\"Unified Expressions\" 쪽 대체 이름(예: cheekPuff → CheekPuff, 또는\nCheekPuffLeft/Right)으로도 검색합니다. 좌우로 분리된 쉐이프 키가 발견되면\n동일한 커브를 양쪽에 복제 설정합니다(하나의 파라미터로 양쪽이 동시에 움직입니다)." },
            ["UE代替 {0}件"] = new[] { "UE fallback {0}", "UE替代 {0}个", "UE 대체 {0}건" },
            ["ARKit標準名では見つからなかったが、UE代替名で解決できたシェイプ:\n"] = new[] { "Shapes not found under the standard ARKit name, but resolved via UE fallback names:\n", "标准ARKit名称下未找到、但通过UE替代名称解决的形态键：\n", "표준 ARKit 이름으로는 찾을 수 없었지만 UE 대체 이름으로 해결된 쉐이프:\n" },
            ["UE代替名で解決: {0}件"] = new[] { "Resolved via UE fallback: {0}", "通过UE替代名称解决：{0}个", "UE 대체 이름으로 해결: {0}건" },
            ["UEのシェイプキーを流用※試験的"] = new[] { "Divert UE shape keys ※Experimental", "挪用UE形态键 ※实验性功能", "UE 쉐이프 키 전용 ※실험적" },
            ["アバター側がUnified Expressionsに対応しているとき、一部のシェイプキーをARKit向けに流用する試験的な機能です。\n目の瞳孔径やあご・舌の詳細な動き等、ARKitでサポートしないシェイプキーは動作しません。"] = new[] { "An experimental feature that, when the avatar supports Unified Expressions, diverts some of its\nshape keys for use with ARKit. Shape keys that ARKit doesn't support (such as pupil dilation or\ndetailed jaw/tongue movement) will not work.", "当角色支持Unified Expressions时，将其一部分形态键挪用于ARKit的实验性功能。\n瞳孔缩放、下颚/舌头的细致动作等ARKit不支持的形态键将不会起作用。", "아바타가 Unified Expressions를 지원할 때, 일부 쉐이프 키를 ARKit용으로 전용하는 실험적인 기능입니다.\n눈동자 크기, 턱・혀의 세밀한 움직임 등 ARKit이 지원하지 않는 쉐이프 키는 동작하지 않습니다." },
            ["複製元となる標準ARKitシェイプキー(browInnerUp等)の検索には、\nFACEカードの「UEのシェイプキーを流用※試験的」設定がそのまま適用されます。"] = new[] { "Searching for the source standard ARKit shape keys (browInnerUp, etc.) uses the same\n\"Divert UE shape keys ※Experimental\" setting from the FACE card.", "查找作为复制来源的标准ARKit形态键(browInnerUp等)时，将直接沿用\nFACE卡片中的“挪用UE形态键 ※实验性功能”设置。", "복제 원본이 되는 표준 ARKit 쉐이프 키(browInnerUp 등)를 검색할 때는\nFACE 카드의 \"UE 쉐이프 키 전용 ※실험적\" 설정이 그대로 적용됩니다." },
            ["不足しているシェイプキーに対応するFXレイヤーは正しく動作しません。"] = new[] { "The FX layers corresponding to the missing shape keys will not work correctly.", "缺失的形态键所对应的FX图层将无法正常工作。", "부족한 쉐이프 키에 대응하는 FX 레이어는 올바르게 동작하지 않습니다." },
            ["⚠ 不足シェイプキー"] = new[] { "⚠ Missing shape keys", "⚠ 缺失的形态键", "⚠ 부족한 쉐이프 키" },
            ["{0}件: {1}"] = new[] { "{0}: {1}", "{0}个：{1}", "{0}건: {1}" },

            // 舌アシスト(2026-08 UI簡素化・多言語対応)
            ["舌が下唇を貫通するのを防ぐシェイプキーを生成"] = new[] { "Generate a shape key that keeps the tongue from poking through the lower lip", "生成防止舌头穿透下唇的形态键", "혀가 아랫입술을 뚫고 나가지 않도록 하는 쉐이프 키 생성" },
            ["tongueOutの動きから舌の頂点を自動検出し(mouthPucker等の唇系シェイプキーが\n動く頂点は除外)、持ち上げた形状とtongueOut本体を0.5ずつミックスしたピーク形状を\n生成します。Install時、tongueOutが0%→100%へ変化する遷移の50%地点(唇を越える\nタイミング)でこのピーク形状が最大になるよう、標準の駆動カーブが組み替えられます\n(0%・100%は従来通り、50%だけ持ち上げが最大になる自然な変化になります)。"] = new[] { "Automatically detects tongue vertices from the movement of tongueOut (vertices moved by\nlip-related shape keys such as mouthPucker are excluded), and generates a peak shape that mixes\nthe lifted shape with tongueOut itself at a 0.5 ratio. At Install time, the standard driving\ncurve is restructured so this peak shape reaches its maximum at the 50% point of the\ntongueOut 0%→100% transition (the moment it passes the lip), while 0% and 100% stay as before,\nfor a natural change with the lift peaking only in the middle.", "根据tongueOut的动作自动检测舌头顶点(排除mouthPucker等嘴唇类形态键\n所影响的顶点)，生成将抬起形状与tongueOut本体按0.5比例混合的峰值形态。\n安装时，标准驱动曲线会被重新组织，使该峰值形态在tongueOut从0%→100%\n变化过程的50%位置(越过嘴唇的时刻)达到最大值，0%和100%保持不变，\n形成仅在中间抬起达到峰值的自然变化。", "tongueOut의 움직임으로부터 혀 정점을 자동 검출하고(mouthPucker 등\n입술 계열 쉐이프 키가 움직이는 정점은 제외), 들어올린 형태와 tongueOut\n본체를 0.5씩 믹스한 피크 형태를 생성합니다. Install 시, tongueOut이\n0%→100%로 변화하는 전환의 50% 지점(입술을 넘는 타이밍)에서 이 피크\n형태가 최대가 되도록 표준 구동 커브가 재구성됩니다(0%·100%는 기존\n그대로 유지되며, 50%에서만 들어올림이 최대가 되는 자연스러운 변화가\n됩니다)." },
            ["ARKitのシェイプキーなどから舌の頂点を自動検出します。各設定項目について、\n詳しくはツールチップをご確認ください。"] = new[] { "Automatically detects tongue vertices from ARKit shape keys and others. For details on each\nsetting, please check the tooltips.", "根据ARKit形态键等自动检测舌头顶点。关于各设置项目的详细说明，\n请查看各自的工具提示。", "ARKit 쉐이프 키 등으로부터 혀 정점을 자동 검출합니다. 각 설정 항목에 대한\n자세한 내용은 툴팁을 확인해 주세요." },
            ["舌を持ち上げる形状のソース"] = new[] { "Tongue lift shape source", "舌头抬起形状来源", "혀 들어올리기 형태 소스" },
            ["「持ち上げ」形状の作り方。\n既存シェイプキー(既定・推奨): アバターが既に持っている舌持ち上げ用シェイプキーを\n指定して流用します。頂点検出を行わないため、歯やまつげの誤検出を心配する必要が\n無く、アバター側で作り込まれた自然な形状をそのまま活かせます。\n自動検知: tongueOut等の動きから舌頂点を自動検出し、指定軸方向へ一律に持ち上げます\n(既存の持ち上げシェイプキーを持たないアバター向け、従来方式)。"] = new[] { "How the \"lift\" shape is created.\nExisting Shape Key (default, recommended): Reuses a tongue-lift shape key your avatar already\nhas. No vertex detection is performed, so there's no risk of misdetecting teeth or eyelashes,\nand you get to keep the natural shape your avatar's creator built.\nAuto Detect: Automatically detects tongue vertices from the movement of tongueOut, etc., and\nlifts them uniformly along the specified axis (for avatars without an existing lift shape key,\nthe legacy method).", "「抬起」形状的生成方式。\n既存形态键(默认·推荐): 复用角色本身已有的舌头抬起形态键。\n不进行顶点检测，因此无需担心误检测到牙齿或睫毛，并能保留角色\n制作者精心制作的自然形状。\n自动检测: 根据tongueOut等的动作自动检测舌头顶点，并沿指定轴方向\n统一抬起(适用于没有现成抬起形态键的角色，传统方式)。", "\"들어올리기\" 형태를 만드는 방법.\n기존 쉐이프 키(기본값·권장): 아바타가 이미 가지고 있는 혀 들어올리기용\n쉐이프 키를 그대로 사용합니다. 정점 검출을 하지 않으므로 치아나 속눈썹의\n오검출을 걱정할 필요가 없고, 아바타 쪽에서 만들어진 자연스러운 형태를 그대로\n활용할 수 있습니다.\n자동 검지: tongueOut 등의 움직임으로부터 혀 정점을 자동 검출하여 지정한 축\n방향으로 일률적으로 들어올립니다(기존 들어올리기 쉐이프 키가 없는 아바타용,\n기존 방식)." },
            ["既存シェイプキー"] = new[] { "Existing Shape Key", "既存形态键", "기존 쉐이프 키" },
            ["自動検知"] = new[] { "Auto Detect", "自动检测", "자동 검지" },
            ["持ち上げ量"] = new[] { "Lift Amount", "抬起量", "들어올림 양" },
            ["検出した舌頂点を持ち上げる距離(メッシュのローカル空間、単位はアバターの\nスケールに依存)。値を大きくするほど舌が下唇をしっかり回避しますが、大きすぎると\n不自然に持ち上がって見えることがあります。Scene Viewのプレビューを見ながら\n少しずつ調整してください。"] = new[] { "Distance to lift the detected tongue vertices (in the mesh's local space; the unit depends on\nyour avatar's scale). A larger value makes the tongue avoid the lower lip more reliably, but too\nlarge a value can look unnaturally lifted. Adjust it gradually while watching the Scene View preview.", "抬起检测到的舌头顶点的距离(网格的本地空间，单位取决于角色的比例)。\n数值越大，舌头躲避下唇的效果越可靠，但过大会显得不自然。\n请一边观察Scene View预览一边逐步调整。", "검출된 혀 정점을 들어올리는 거리(메쉬의 로컬 공간, 단위는 아바타의\n스케일에 따라 다름). 값을 크게 할수록 혀가 아랫입술을 확실히 피하지만,\n너무 크면 부자연스럽게 들어올려진 것처럼 보일 수 있습니다. Scene View\n미리보기를 보면서 조금씩 조정해 주세요." },
            ["移動方向"] = new[] { "Lift Axis", "移动方向", "이동 방향" },
            ["検出しきい値 (mm)"] = new[] { "Detect Threshold (mm)", "检测阈值 (mm)", "검출 임계값 (mm)" },
            ["舌のシェイプキーで、これ以上動いた頂点を「舌の一部」として検出するための\nしきい値(実寸mm)。値を下げるほど検出される頂点が増えます。検出頂点数が\n0のままの場合は下げてみてください。"] = new[] { "The threshold (in real-world mm) above which a vertex moved by a tongue shape key is detected as\n\"part of the tongue.\" Lowering the value increases the number of detected vertices. If the detected\nvertex count stays at 0, try lowering it.", "舌头形态键中，超过该阈值(实际尺寸mm)移动的顶点将被检测为「舌头的一部分」。\n数值越小，检测到的顶点越多。如果检测到的顶点数始终为0，请尝试调低该值。", "혀 쉐이프 키에서 이 값 이상 움직인 정점을 「혀의 일부」로 검출하기 위한\n임계값(실제 치수 mm). 값을 낮출수록 검출되는 정점이 늘어납니다. 검출된\n정점 수가 계속 0이라면 낮춰 보세요." },
            ["唇除外しきい値 (mm)"] = new[] { "Lip Exclude Threshold (mm)", "排除嘴唇阈值 (mm)", "입술 제외 임계값 (mm)" },
            ["唇・頬・歯等、舌ではない部位の頂点を誤って検出しないよう除外するための\nしきい値(実寸mm)。ARKit標準の'cheekPuff'を主要な除外シグナルとして使い、\nmouthRollUpper/Lower等と合わせて判定します(mouthPucker・mouthFunnel・\nmouthCloseは口を閉じる/すぼめる動きで舌も一緒に動かすため除外対象から\n外しています)。値を上げるほど除外が弱まり、残る頂点が増えます。\n舌の頂点が消えすぎている(検出数が極端に少ない)場合は上げてみてください。"] = new[] { "The threshold (in real-world mm) for excluding vertices from non-tongue areas such as the lips,\ncheeks, and teeth, so they aren't mistakenly detected. Uses ARKit's standard 'cheekPuff' as the\nmain exclusion signal, judged together with mouthRollUpper/Lower, etc. (mouthPucker, mouthFunnel,\nand mouthClose are excluded from this list since they move the tongue too while closing/pursing\nthe mouth). Raising the value weakens the exclusion, leaving more vertices. If too many tongue\nvertices are being excluded (an extremely low detected count), try raising it.", "为防止误检测嘴唇、脸颊、牙齿等非舌头部位的顶点而设置的排除阈值(实际尺寸mm)。\n以ARKit标准的'cheekPuff'作为主要排除信号，并结合mouthRollUpper/Lower等\n一起判断(mouthPucker、mouthFunnel、mouthClose会在闭口/撅嘴动作时连带\n移动舌头，因此已从排除对象中排除)。数值越大，排除效果越弱，保留的顶点\n越多。如果舌头顶点被过度排除(检测数量极少)，请尝试调高该值。", "입술·볼·치아 등 혀가 아닌 부위의 정점을 오검출하지 않도록 제외하기 위한\n임계값(실제 치수 mm). ARKit 표준의 'cheekPuff'를 주요 제외 신호로 사용하며,\nmouthRollUpper/Lower 등과 함께 판정합니다(mouthPucker・mouthFunnel・\nmouthClose는 입을 다물거나 오므리는 동작으로 혀도 함께 움직이기 때문에\n제외 대상에서 뺐습니다). 값을 높일수록 제외가 약해져 남는 정점이 늘어납니다.\n혀 정점이 너무 많이 제외되고 있다면(검출 수가 극단적으로 적음) 높여 보세요." },
            ["単位スケール変換係数を手動指定"] = new[] { "Manually specify unit scale conversion factor", "手动指定单位缩放换算系数", "단위 스케일 변환 계수 수동 지정" },
            ["OFF(既定): mm→メッシュ空間への変換係数を自動推定します(バウンディングボックス比)。\nON: 自動推定を使わず、右の数値を変換係数として強制的に使います。\n自動推定がアバターによって大きく外れ、検出頂点数が0のまま/意図しない部位\n(まつげ等)を拾ってしまう場合にONにしてください。"] = new[] { "OFF (default): Automatically estimates the mm-to-mesh-space conversion factor (bounding box ratio).\nON: Skips auto-estimation and forcibly uses the value on the right as the conversion factor.\nTurn this ON if auto-estimation is significantly off for a particular avatar, causing the detected\nvertex count to stay at 0 or pick up unintended areas (such as eyelashes).", "OFF(默认): 自动估算mm到网格空间的换算系数(边界框比例)。\nON: 不使用自动估算，强制使用右侧数值作为换算系数。\n如果自动估算在某个角色上明显偏离，导致检测到的顶点数始终为0，\n或误检测到意外部位(如睫毛)，请开启此选项。", "OFF(기본값): mm→메쉬 공간 변환 계수를 자동으로 추정합니다(바운딩 박스 비율).\nON: 자동 추정을 사용하지 않고 오른쪽 숫자를 변환 계수로 강제 사용합니다.\n자동 추정이 특정 아바타에서 크게 어긋나 검출된 정점 수가 계속 0이거나\n의도치 않은 부위(속눈썹 등)를 잡는 경우 켜 주세요." },
            ["単位スケール変換係数"] = new[] { "Unit Scale Conversion Factor", "单位缩放换算系数", "단위 스케일 변환 계수" },
            ["mm→メッシュ空間への変換係数(手動指定時のみ使用)。\n下の「実際に使われている係数」を見ながら、検出頂点数が正しくなるよう調整してください。\n目安: 係数を大きくするとmm指定に対するメッシュ空間しきい値が小さくなり、検出が増えます。"] = new[] { "The mm-to-mesh-space conversion factor (used only when manually specified).\nAdjust it while watching the \"factor actually in use\" value below, so the detected vertex count\ncomes out correctly. Rule of thumb: increasing the factor lowers the mesh-space threshold for a given\nmm value, increasing detection.", "mm到网格空间的换算系数(仅在手动指定时使用)。\n请一边观察下方「实际使用的系数」，一边调整以使检测到的顶点数正确。\n参考: 系数越大，对应mm值的网格空间阈值就越小，检测到的顶点也会增多。", "mm→메쉬 공간 변환 계수(수동 지정 시에만 사용).\n아래의 「실제로 사용 중인 계수」를 보면서 검출된 정점 수가 올바르게\n나오도록 조정해 주세요. 기준: 계수를 크게 하면 mm 지정에 대한 메쉬\n공간 임계값이 작아져 검출이 늘어납니다." },

            // EyeLook競合対策(Stableモードの案内、2026-08)
            ["✓ TrackingControl競合候補 {0}件\n"] = new[] { "✓ {0} TrackingControl conflict candidate(s)\n", "✓ {0} 个TrackingControl冲突候选项\n", "✓ TrackingControl 충돌 후보 {0}건\n" },
            ["\nStableモードが有効なため、TrackingControlが発火しても実害はありません。"] = new[] { "\nSince Stable mode is enabled, there is no actual harm even if TrackingControl fires.", "\n由于已启用Stable模式，即使TrackingControl触发也不会造成实际影响。", "\nStable 모드가 활성화되어 있으므로 TrackingControl이 발동해도 실제 피해는 없습니다." },
            ["\nStableモードを選択すると、VRChat標準Eye Lookとの競合を根本的に回避できます。"] = new[] { "\nSelecting Stable mode fundamentally avoids conflicts with VRChat standard Eye Look.", "\n选择Stable模式可从根本上避免与VRChat标准Eye Look的冲突。", "\nStable 모드를 선택하면 VRChat 표준 Eye Look과의 충돌을 근본적으로 피할 수 있습니다." },
            ["まばたきを安定化させるために、デフォルトでは一定の閾値で左右の目の開きが同期します\n(この場合もウインクは可能です)。同期が不要の場合は左右のまばたきを独立させることができます。"] = new[] { "By default, the left and right eye openness are synchronized at a certain threshold to stabilize\nblinking (winking is still possible in this case). If synchronization isn't needed, you can make\nthe left and right blinks fully independent.", "为了使眨眼更加稳定，默认情况下左右眼的睁开程度会在一定阈值内同步\n(此时依然可以做出眨单眼的动作)。如果不需要同步，可以将左右眨眼设为完全独立。", "눈 깜빡임을 안정화하기 위해 기본적으로는 일정한 임계값에서 좌우 눈의 뜨임이\n동기화됩니다(이 경우에도 윙크는 가능합니다). 동기화가 필요 없다면 좌우\n눈 깜빡임을 완전히 독립시킬 수 있습니다." },
            ["OFF(既定): Blink2D。v2/EyeLidLeft・Rightを2D Freeformでブレンドし、左右がある程度連動します。\nON: Blink Simple 1D。v2/EyeLidLeft・Rightをそれぞれ独立したSimple1Dで駆動し、片目だけの\nウィンクにも対応できます。"] = new[] { "OFF (default): Blink2D. Blends v2/EyeLidLeft/Right with a 2D Freeform, so the two eyes are somewhat linked.\nON: Blink Simple 1D. Drives v2/EyeLidLeft/Right independently with separate Simple 1D blends,\nsupporting a wink with just one eye.", "OFF(默认): Blink2D。通过2D Freeform混合v2/EyeLidLeft·Right，左右眼在一定程度上联动。\nON: Blink Simple 1D。分别用独立的Simple1D驱动v2/EyeLidLeft·Right，\n也支持只眨一只眼的动作。", "OFF(기본값): Blink2D. v2/EyeLidLeft·Right를 2D Freeform으로 블렌드하여 좌우가 어느 정도 연동됩니다.\nON: Blink Simple 1D. v2/EyeLidLeft·Right를 각각 독립된 Simple1D로 구동하여 한쪽 눈만\n윙크하는 것도 가능합니다." },
            ["フェイストラッキング実行中は、ジェスチャーやExpressionMenuで動く\n表情が動かないように設定できます。混ざってほしくないFXレイヤーをすべて選択してください。"] = new[] { "You can set it up so that expressions driven by gestures or the Expression Menu don't move\nwhile Face Tracking is running. Select all FX layers you don't want mixed in.", "在执行FaceTracking期间，可以设置为不让由手势或ExpressionMenu驱动的\n表情发生变化。请选择所有不希望混入的FX图层。", "페이스 트래킹 실행 중에는 제스처나 ExpressionMenu로 움직이는\n표정이 움직이지 않도록 설정할 수 있습니다. 섞이길 원하지 않는 FX 레이어를 모두 선택해 주세요." },
            ["各レイヤーの「口」「目」チェックボックスで、どちらのトラッキング状態のときに\nそのレイヤーを抑制するかを個別に選べます。両方チェックすれば、どちらか一方でも\n有効なら抑制されます。ジェスチャー中も目を動かし続けたいレイヤーは「口」だけに、\nアイトラッキング中は常に抑制したいレイヤー(視線制御を含むもの等)は「目」も\nチェックしてください。"] = new[] { "The \"Mouth\" and \"Eyes\" checkboxes on each layer let you individually choose which tracking\nstate suppresses that layer. Check both to suppress it whenever either one is active. For\nlayers where you want eyes to keep moving during a gesture, check only \"Mouth\"; for layers you\nwant suppressed whenever Eye Tracking is active (such as ones containing gaze control), also\ncheck \"Eyes\".", "通过各图层的「口」「目」复选框，可以分别选择在哪种追踪状态下抑制该图层。\n两者都勾选的话，只要其中一个处于有效状态就会被抑制。如果希望在做手势时\n眼睛仍能持续转动，只勾选「口」；如果希望在AiTracking期间始终抑制某图层\n(例如包含视线控制的图层)，请同时勾选「目」。", "각 레이어의 \"입\" \"눈\" 체크박스로, 어느 트래킹 상태일 때 해당 레이어를\n억제할지 개별적으로 선택할 수 있습니다. 둘 다 체크하면 둘 중 하나라도\n활성화되면 억제됩니다. 제스처 중에도 눈을 계속 움직이고 싶은 레이어는 \"입\"만,\n아이트래킹 중에는 항상 억제하고 싶은 레이어(시선 제어를 포함하는 것 등)는\n\"눈\"도 체크해 주세요." },
            ["口"] = new[] { "Mouth", "口", "입" },
            ["MouthTracking(音声リップシンク相当)が有効なとき、このレイヤーを抑制します。\n「口」「目」の少なくとも一方はチェックしてください(両方外すと抑制されなくなります)。"] = new[] { "Suppresses this layer while MouthTracking (equivalent to voice lip-sync) is active.\nPlease check at least one of \"Mouth\" or \"Eyes\" (unchecking both means it won't be suppressed).", "在MouthTracking(相当于语音对口型)有效时抑制此图层。\n请至少勾选「口」「目」中的一个(两者都不勾选将不会被抑制)。", "MouthTracking(음성 립싱크에 해당)이 활성화되어 있을 때 이 레이어를 억제합니다.\n\"입\" \"눈\" 중 최소 하나는 체크해 주세요(둘 다 해제하면 억제되지 않습니다)." },
            ["目"] = new[] { "Eyes", "目", "눈" },
            ["EyeTrackingが有効なとき、このレイヤーを抑制します。視線制御のTrackingControlを\n含むレイヤー等、アイトラッキング中は常に抑制したい場合にチェックしてください。\n「口」「目」の少なくとも一方はチェックしてください(両方外すと抑制されなくなります)。"] = new[] { "Suppresses this layer while EyeTracking is active. Check this for layers you want suppressed\nwhenever Eye Tracking is active, such as ones containing gaze-control TrackingControl.\nPlease check at least one of \"Mouth\" or \"Eyes\" (unchecking both means it won't be suppressed).", "在EyeTracking有效时抑制此图层。对于希望在AiTracking期间始终抑制的图层\n(例如包含视线控制TrackingControl的图层)，请勾选此项。\n请至少勾选「口」「目」中的一个(两者都不勾选将不会被抑制)。", "EyeTracking이 활성화되어 있을 때 이 레이어를 억제합니다. 시선 제어\nTrackingControl을 포함하는 레이어 등, 아이트래킹 중에는 항상 억제하고\n싶은 경우에 체크해 주세요. \"입\" \"눈\" 중 최소 하나는 체크해 주세요\n(둘 다 해제하면 억제되지 않습니다)." },

            ["抑制レイヤーに対して、FT有効時にはレイヤーのWeightを0にします。\nなお、Weightが0でもTrackingControlの変更自体は発火します。\nEyeのTrackingControl競合を根本的に避けたい場合は、Stable Eye Mode（AvatarDescriptor Eye Lookを無効化）を使用してください。"] = new[] { "For suppressed layers, the layer Weight is set to 0 while FT is active.\nNote that TrackingControl changes can still fire even when Weight is 0.\nTo fundamentally avoid Eye TrackingControl conflicts, use Stable Eye Mode (Disable AvatarDescriptor Eye Look).", "对于抑制图层，FT有效时会将该图层的Weight设为0。\n需要注意的是，即使Weight为0，TrackingControl的更改本身仍会触发。\n若要从根本上避免Eye TrackingControl冲突，请使用Stable Eye Mode（禁用AvatarDescriptor Eye Look）。", "억제 레이어에 대해 FT가 활성화되어 있을 때 레이어 Weight를 0으로 만듭니다.\nWeight가 0이어도 TrackingControl 변경 자체는 발동할 수 있습니다.\nEye TrackingControl 충돌을 근본적으로 피하려면 Stable Eye Mode(AvatarDescriptor Eye Look 비활성화)를 사용하세요." },
            ["選択したレイヤーは、MouthTracking(音声リップシンク相当)が有効な間、Weightを0にして\n抑制します。目のTrackingControl競合はweight抑制では解決できないため、\nStable Eye Mode（AvatarDescriptor Eye Lookを無効化）で対応してください。\nアイトラッキングのみを有効にすることで、ジェスチャー/メニュー表情とアイトラッキングを\n併用できます。（ただし、目を閉じる表情とアイトラッキングのまばたきは重なって\n破綻してしまいますのでご注意ください。写真撮影での活用をおすすめします。）"] = new[] { "The selected layers have their Weight set to 0 to suppress them while MouthTracking (equivalent\nto voice lip-sync) is active. Eye TrackingControl conflicts can't be resolved by weight\nsuppression, so please use Stable Eye Mode (disable AvatarDescriptor Eye Look) for that instead.\nBy enabling only Eye Tracking, you can use gesture/menu expressions together with eye tracking.\n(Note, however, that an expression that closes the eyes will collide with eye-tracking blinks and\nlook broken. We recommend using this for photography.)", "所选图层会在MouthTracking(相当于语音对口型)有效期间将Weight设为0以进行\n抑制。眼睛的TrackingControl冲突无法通过weight抑制解决，请使用Stable Eye\nMode(禁用AvatarDescriptor Eye Look)来应对。\n只启用EyeTracking，即可将手势/菜单表情与眼动追踪同时使用。\n(不过，闭眼类表情会与眼动追踪的眨眼动作重叠而显得不自然，请注意。\n建议用于拍照场景。)", "선택한 레이어는 MouthTracking(음성 립싱크에 해당)이 활성화되어 있는 동안\nWeight를 0으로 하여 억제합니다. 눈의 TrackingControl 충돌은 weight 억제로는\n해결할 수 없으므로, Stable Eye Mode(AvatarDescriptor Eye Look 비활성화)로\n대응해 주세요.\n아이트래킹만 활성화하면 제스처/메뉴 표정과 아이트래킹을 함께 사용할 수\n있습니다. (다만 눈을 감는 표정과 아이트래킹의 깜빡임이 겹쳐 부자연스러워질\n수 있으니 주의해 주세요. 사진 촬영 시 활용을 권장합니다.)" },
            ["mm(実寸)とメッシュ内部の座標単位は縮尺が異なるため、しきい値を比較する前に\n単位を揃える変換が必要です。この変換に使う「1mmがメッシュ空間でいくつに\nあたるか」という縮尺の値が単位スケール変換係数です。\nOFF(既定): SMRのバウンディングボックス比から自動推定します。\nON: 自動推定を使わず、右の数値を単位スケール変換係数として強制的に使います。\n自動推定がアバターによって大きく外れ、検出頂点数が0のまま/意図しない部位\n(まつげ等)を拾ってしまう場合にONにしてください。"] = new[] { "Real-world mm and the mesh's internal coordinate units are on different scales, so the threshold\nvalues need to be converted to a common scale before they can be compared. The unit scale\nconversion factor is the scale value used for this conversion: \"how many mesh-space units\ncorrespond to 1mm.\"\nOFF (default): Automatically estimated from the SMR's bounding box ratio.\nON: Skips auto-estimation and forcibly uses the value on the right as the unit scale conversion\nfactor. Turn this ON if auto-estimation is significantly off for a particular avatar, causing the\ndetected vertex count to stay at 0 or pick up unintended areas (such as eyelashes).", "由于mm(实际尺寸)与网格内部坐标单位的缩放比例不同，在比较阈值之前需要先将\n单位统一进行换算。用于此换算的「1mm相当于网格空间中多少」这一缩放数值，\n就是单位缩放换算系数。\nOFF(默认): 根据SMR的边界框比例自动估算。\nON: 不使用自动估算，强制使用右侧数值作为单位缩放换算系数。\n如果自动估算在某个角色上明显偏离，导致检测到的顶点数始终为0，\n或误检测到意外部位(如睫毛)，请开启此选项。", "mm(실제 치수)와 메쉬 내부 좌표 단위는 축척이 다르기 때문에, 임계값을\n비교하기 전에 단위를 맞추는 변환이 필요합니다. 이 변환에 사용하는\n「1mm가 메쉬 공간에서 얼마에 해당하는가」라는 축척 값이 단위 스케일\n변환 계수입니다.\nOFF(기본값): SMR의 바운딩 박스 비율로부터 자동 추정합니다.\nON: 자동 추정을 사용하지 않고 오른쪽 숫자를 단위 스케일 변환 계수로\n강제 사용합니다. 자동 추정이 특정 아바타에서 크게 어긋나 검출된 정점\n수가 계속 0이거나 의도치 않은 부위(속눈썹 등)를 잡는 경우 켜 주세요." },
            ["1mm(実寸)が、メッシュ内部の座標単位でいくつにあたるかを表す縮尺の値です\n(手動指定時のみ使用)。下の「実際に使われている係数」を見ながら、検出頂点数が\n正しくなるよう調整してください。\n目安: 値を大きくするとmm指定に対するメッシュ空間しきい値が小さくなり、検出が増えます。"] = new[] { "The scale value representing how many mesh-internal coordinate units correspond to 1mm\n(real-world). Used only when manually specified. Adjust it while watching the \"factor actually\nin use\" value below, so the detected vertex count comes out correctly.\nRule of thumb: increasing the value lowers the mesh-space threshold for a given mm value,\nincreasing detection.", "表示1mm(实际尺寸)相当于网格内部坐标单位中多少的缩放数值\n(仅在手动指定时使用)。请一边观察下方「实际使用的系数」，一边调整以使检测到\n的顶点数正确。\n参考: 数值越大，对应mm值的网格空间阈值就越小，检测到的顶点也会增多。", "1mm(실제 치수)가 메쉬 내부 좌표 단위로 얼마에 해당하는지를 나타내는\n축척 값입니다(수동 지정 시에만 사용). 아래의 「실제로 사용 중인 계수」를\n보면서 검출된 정점 수가 올바르게 나오도록 조정해 주세요.\n기준: 값을 크게 하면 mm 지정에 대한 메쉬 공간 임계값이 작아져 검출이\n늘어납니다." },
            ["FT OFF時にはVRChat標準の目線へ戻ります。アバターによってはFT中に競合する場合があります。"] = new[] { "When FT is OFF, it reverts to VRChat's standard gaze. Depending on the avatar, conflicts may occur while FT is active.", "FT OFF时会恢复为VRChat标准的视线。根据角色的不同，在FT有效期间可能会发生冲突。", "FT가 OFF일 때는 VRChat 표준 시선으로 돌아갑니다. 아바타에 따라 FT 중에 충돌이 발생할 수 있습니다." },
            ["ショップ名/作者名"] = new[] { "Shop/Author Name", "商店名/作者名", "샵 이름/제작자 이름" },
            ["表示専用のメタデータで、自動選択のマッチングには一切使用されない。\n複数のProfileを見比べる際、どの作者/ショップが配布したものかを目視で\n確認しやすくするためのもの。「保存」ボタンで他の設定と一緒に保存される。"] = new[] { "Display-only metadata that is never used for automatic matching.\nHelps you visually identify which author/shop distributed a Profile when comparing\nmultiple Profiles. Saved together with the other settings via the \"Save\" button.", "仅用于显示的元数据，完全不参与自动匹配。\n在比较多个Profile时，方便通过肉眼确认是由哪位作者/哪个商店发布的。\n会通过“保存”按钮与其他设置一起保存。", "표시 전용 메타데이터이며, 자동 선택 매칭에는 전혀 사용되지 않습니다.\n여러 Profile을 비교할 때 어느 제작자/샵이 배포한 것인지 육안으로\n확인하기 쉽게 하기 위한 것입니다. \"저장\" 버튼으로 다른 설정과 함께 저장됩니다." },
            ["バージョン名"] = new[] { "Version Name", "版本名", "버전 이름" },
            ["表示専用のメタデータで、自動選択のマッチングには一切使用されない。\n例: \"v1.2\"。アバター本体の更新に合わせてProfileを複製・更新した際に、\nどのバージョン向けかを目視で確認しやすくするためのもの。\n「保存」ボタンで他の設定と一緒に保存される。"] = new[] { "Display-only metadata that is never used for automatic matching.\nExample: \"v1.2\". Helps you visually identify which version a Profile is intended for when\nyou duplicate/update Profiles alongside avatar updates.\nSaved together with the other settings via the \"Save\" button.", "仅用于显示的元数据，完全不参与自动匹配。\n例如：“v1.2”。当配合角色本体的更新而复制/更新Profile时，方便通过肉眼\n确认该Profile对应哪个版本。\n会通过“保存”按钮与其他设置一起保存。", "표시 전용 메타데이터이며, 자동 선택 매칭에는 전혀 사용되지 않습니다.\n예: \"v1.2\". 아바타 본체 업데이트에 맞춰 Profile을 복제・업데이트할 때,\n어느 버전용인지 육안으로 확인하기 쉽게 하기 위한 것입니다.\n\"저장\" 버튼으로 다른 설정과 함께 저장됩니다." },
            ["目線シェイプキー生成時に追加シェイプキーを有効化"] = new[] { "Enable Additional Shape Keys During Eye-Look Generation", "生成视线形态键时启用附加形态键", "시선 쉐이프 키 생성 시 추가 쉐이프 키 활성화" },
            ["目のハイライト・瞳孔等、サブメッシュを手前に移動させるシェイプキーを持つ\nアバターの場合、ここでそのシェイプキーを指定してください。目線シェイプキー生成\n(ボーン回転のベイク)の間だけ、指定したシェイプキーの重みを100にした状態で\n計算します(生成後、元の重みに戻します)。指定しない場合、サブメッシュが奥にある\n状態を基準に計算されるため、実際に手前へ出した状態で目線を動かすと、回転による\n移動量が不足して眼球メッシュを貫通することがあります。"] = new[] { "If your avatar has a shape key that moves a sub-mesh (such as an eye highlight or pupil)\nforward, specify it here. Only for the duration of eye-look shape key generation (baking bone\nrotation), the specified shape key's weight is set to 100 for the calculation (restored\nafterward). If left unspecified, the calculation is based on the sub-mesh being in its back\nposition, so moving the gaze after actually bringing the sub-mesh forward can result in\ninsufficient rotational movement and the sub-mesh poking through the eyeball mesh.", "如果角色拥有将眼睛高光、瞳孔等子网格向前移动的形态键，请在此指定。仅在生成视线\n形态键(烘焙骨骼旋转)期间，会将指定形态键的权重设为100进行计算(生成后会恢复\n原来的权重)。如果不指定，计算将以子网格处于靠后位置为基准，因此在实际将子网格\n移到前方后再移动视线时，可能会因旋转移动量不足而导致子网格穿透眼球网格。", "아바타에 눈 하이라이트・동공 등 서브 메쉬를 앞으로 이동시키는 쉐이프 키가 있는\n경우, 여기서 해당 쉐이프 키를 지정해 주세요. 시선 쉐이프 키 생성(본 회전 베이크)\n동안에만, 지정한 쉐이프 키의 가중치를 100으로 설정하여 계산합니다(생성 후 원래\n가중치로 되돌립니다). 지정하지 않으면 서브 메쉬가 뒤에 있는 상태를 기준으로\n계산되므로, 실제로 서브 메쉬를 앞으로 낸 상태에서 시선을 움직이면 회전에 의한\n이동량이 부족해 안구 메쉬를 뚫고 나올 수 있습니다." },
            ["左右まばたきの安定化"] = new[] { "Left/Right Blink Stabilization", "左右眨眼稳定化", "좌우 눈 깜빡임 안정화" },
            ["左右の目の開きを一定の閾値で揃える"] = new[] { "Align left/right eye openness at a fixed threshold", "以固定阈值统一左右眼睁开程度", "좌우 눈 뜨임을 일정 임계값으로 맞춤" },
            ["ON(既定): Blink2D。v2/EyeLidLeft・Rightを2D Freeformでブレンドし、左右がある程度連動します。\nOFF: Blink Simple 1D。v2/EyeLidLeft・Rightをそれぞれ独立したSimple1Dで駆動し、片目だけの\nウィンクにも対応できます。"] = new[] { "ON (default): Blink2D. Blends v2/EyeLidLeft/Right with a 2D Freeform, so the two eyes are somewhat linked.\nOFF: Blink Simple 1D. Drives v2/EyeLidLeft/Right independently with separate Simple 1D blends,\nsupporting a wink with just one eye.", "ON(默认): Blink2D。通过2D Freeform混合v2/EyeLidLeft·Right，左右眼在一定程度上联动。\nOFF: Blink Simple 1D。分别用独立的Simple1D驱动v2/EyeLidLeft·Right，\n也支持只眨一只眼的动作。", "ON(기본값): Blink2D. v2/EyeLidLeft·Right를 2D Freeform으로 블렌드하여 좌우가 어느 정도 연동됩니다.\nOFF: Blink Simple 1D. v2/EyeLidLeft·Right를 각각 독립된 Simple1D로 구동하여 한쪽 눈만\n윙크하는 것도 가능합니다." },
            ["左右の目の開きが不揃いになるのを避けるため、一定の閾値で同期させます。\n綺麗にウインクするには、反対の目が一定以上開いている必要があります。"] = new[] { "To prevent the left and right eyes from looking uneven, they are synced at a fixed threshold.\nFor a clean wink, the opposite eye needs to stay open beyond a certain amount.", "为避免左右眼睁开程度不一致，会以固定阈值进行同步。\n要做出干净利落的眨单眼动作，另一只眼睛需要保持在一定程度以上的睁开状态。", "좌우 눈 뜨임이 고르지 않게 되는 것을 방지하기 위해, 일정한 임계값으로 동기화합니다.\n깔끔하게 윙크하려면 반대쪽 눈이 일정 이상 떠 있어야 합니다." },
            ["詳細設定"] = new[] { "Advanced Settings", "详细设置", "상세 설정" },
            ["検出のしきい値や単位変換など、既存Profileを使うだけであれば通常は\n触る必要のない設定です。検出頂点数が0のまま等、うまく検出できない場合のみ\n開いて調整してください。"] = new[] { "Settings such as detection thresholds and unit conversion. If you're just using an existing\nProfile, you normally won't need to touch these. Open this only if detection isn't working well,\nsuch as the detected vertex count staying at 0.", "检测阈值、单位换算等设置。如果只是使用现有的Profile，通常不需要\n触碰这些设置。仅在检测效果不佳时(例如检测到的顶点数始终为0)才\n展开并进行调整。", "검출 임계값이나 단위 변환 등의 설정입니다. 기존 Profile을 사용하는\n것뿐이라면 보통 건드릴 필요가 없습니다. 검출된 정점 수가 계속 0인\n경우 등, 검출이 잘 되지 않을 때만 열어서 조정해 주세요." },
            ["舌アシスト"] = new[] { "Tongue Assist", "舌头辅助", "혀 어시스트" },
            ["Scene Viewにプレビュー表示"] = new[] { "Show Preview in Scene View", "在Scene View中显示预览", "Scene View에 미리보기 표시" },
            ["実際に使われている変換係数: {0:0.######}{1}(自動推定値: {2:0.######})"] = new[] { "Factor actually in use: {0:0.######}{1} (auto-estimated: {2:0.######})", "实际使用的换算系数: {0:0.######}{1}(自动推测值: {2:0.######})", "실제로 사용 중인 변환 계수: {0:0.######}{1}(자동 추정값: {2:0.######})" },
            ["Blendshapeの接頭辞 (カンマ区切りで複数可)"] = new[] { "Blendshape Prefix (comma-separated for multiple)", "Blendshape前缀(可用逗号分隔指定多个)", "Blendshape 접두사 (쉼표로 구분하여 여러 개 지정 가능)" },
            ["tongueOut自体にも歯除外を適用"] = new[] { "Also apply teeth exclusion to tongueOut itself", "对tongueOut本身也应用牙齿排除", "tongueOut 자체에도 치아 제외를 적용" },
            ["検出頂点数: {0}"] = new[] { "Detected vertex count: {0}", "检测到的顶点数: {0}", "검출된 정점 수: {0}" },
            ["(未選択)"] = new[] { "(None)", "(未选择)", "(선택 안 함)" },
            ["(メッシュのRead/Writeが無効なためプレビューできません)"] = new[] { "(Cannot preview because Read/Write is disabled on the mesh)", "(网格的Read/Write已禁用，无法预览)", "(메쉬의 Read/Write가 비활성화되어 있어 미리보기를 할 수 없습니다)" },
            ["OFF(既定): tongueOut自体は歯除外の対象外にします。\nON: tongueOut自体が動かす頂点でも、歯シェイプキーと重なるものは除外します\n(tongueOutが歯を動かす正当な理由は無いはずという前提で、歯の誤検出を防ぎます)。\nアバターによっては、歯とキーワード一致するシェイプキーが舌の可動域と大きく\n重なっており、ONにすると本来検出されるべき舌の頂点まで巻き込んで消えてしまい、\n検出頂点数が0になることがあります(Consoleに「歯除外が原因の可能性が高いです」\nという警告が出た場合はOFFのままにしてください)。"] = new[] { "OFF (default): tongueOut itself is excluded from teeth exclusion.\nON: Even vertices moved by tongueOut itself are excluded if they overlap with teeth shape keys\n(assuming tongueOut has no legitimate reason to move teeth, this prevents false detection).\nOn some avatars, a shape key that keyword-matches \"teeth\" overlaps significantly with the\ntongue's range of motion; turning this ON can end up excluding tongue vertices that should\nhave been detected, causing the detected count to drop to 0 (if the Console shows a warning\nlikely caused by teeth exclusion, keep this OFF).", "OFF(默认): tongueOut本身不作为牙齿排除的对象。\nON: 即使是tongueOut本身移动的顶点，若与牙齿形态键重叠也会被排除\n(基于tongueOut没有移动牙齿的正当理由这一前提，以防止牙齿误检测)。\n根据角色的不同，与“牙齿”关键词匹配的形态键可能与舌头的可动范围大幅\n重叠，开启ON后可能会连本应检测到的舌头顶点也一并排除消失，\n导致检测顶点数变为0(如果Console中出现“很可能是牙齿排除导致的”\n警告，请保持OFF)。", "OFF(기본값): tongueOut 자체는 치아 제외 대상에서 제외합니다.\nON: tongueOut 자체가 움직이는 정점이라도 치아 쉐이프 키와 겹치는 것은\n제외합니다(tongueOut이 치아를 움직일 정당한 이유는 없다는 전제로,\n치아 오검출을 방지합니다). 아바타에 따라서는 치아와 키워드가 일치하는\n쉐이프 키가 혀의 가동 범위와 크게 겹쳐 있어, ON으로 하면 본래 검출되어야\n할 혀 정점까지 함께 사라져 검출된 정점 수가 0이 되는 경우가 있습니다\n(Console에 \"치아 제외가 원인일 가능성이 높습니다\"라는 경고가 나오면\nOFF 상태로 유지해 주세요)." },
            ["OFF(既定): 上書き方式のみ。ジェスチャーのモーション(表情等)はFT有効中も\n通常通り再生されます。\nON: このレイヤーへゲートを注入し、FT有効中はTrackingControlを含むステートへ\n一切侵入させません。ジェスチャーのモーションも一切再生されなくなりますが、\n上書き方式だけでは競合を解消しきれない場合に有効です。"] = new[] { "OFF (default): Override method only. The gesture's motion (expression, etc.) still plays\nnormally while FT is active.\nON: Injects a gate into this layer, so while FT is active it never enters any state containing\na TrackingControl. The gesture's motion won't play at all either, but this helps when the\noverride method alone can't fully resolve the conflict.", "OFF(默认): 仅使用覆盖方式。即使FT有效期间，手势的动作(表情等)\n仍会正常播放。\nON: 向该图层注入门控，FT有效期间将完全不会进入包含TrackingControl的\n状态。手势的动作也将完全不再播放，但当仅靠覆盖方式无法彻底解决\n冲突时，此选项很有效。", "OFF(기본값): 덮어쓰기 방식만 사용. 제스처의 모션(표정 등)은 FT가\n활성화되어 있는 동안에도 평소대로 재생됩니다.\nON: 이 레이어에 게이트를 주입하여, FT가 활성화되어 있는 동안\nTrackingControl을 포함한 상태로 전혀 진입하지 않도록 합니다. 제스처의\n모션도 전혀 재생되지 않게 되지만, 덮어쓰기 방식만으로는 충돌을 완전히\n해소할 수 없는 경우에 유효합니다." },
            ["[手動指定] "] = new[] { "[Manual] ", "[手动指定] ", "[수동 지정] " },
            ["指定したシェイプキーの頂点移動量をそのまま(Weight%でスケールして)使います。\n頂点の自動検出は行わないため、歯やまつげ等の誤検出の心配がありません。\nこの持ち上げ形状は、v2/TongueOutが0%→100%へ遷移する間、50%地点(唇を越える\nタイミング)で最大になるよう自動的に組み込まれます(0%・100%では持ち上げ無し)。\nリモートでの同期時にv2/TongueOutが粗く量子化される運用でも安定するよう、\nピーク位置は50%に固定しています。"] = new[] { "Uses the specified shape key's vertex displacement as-is (scaled by Weight%).\nSince no automatic vertex detection is performed, there's no risk of misdetecting teeth,\neyelashes, etc. This lift shape is automatically built in so it reaches its maximum at the 50%\npoint (the moment it passes the lip), while v2/TongueOut transitions from 0% to 100% (no lift at\n0% or 100%). The peak position is fixed at 50% so it stays stable even when v2/TongueOut gets\ncoarsely quantized for remote sync.", "直接使用指定形态键的顶点位移量(按Weight%缩放)。\n由于不进行顶点自动检测，因此无需担心误检测到牙齿、睫毛等。\n该抬起形状会在v2/TongueOut从0%→100%变化的过程中自动组合，使其在50%\n位置(越过嘴唇的时刻)达到最大值(0%、100%时不抬起)。\n为了在远程同步时v2/TongueOut被粗略量化的情况下依然保持稳定，\n峰值位置固定为50%。", "지정한 쉐이프 키의 정점 이동량을 그대로(Weight%로 스케일하여) 사용합니다.\n정점의 자동 검출을 하지 않으므로 치아나 속눈썹 등의 오검출을 걱정할\n필요가 없습니다. 이 들어올리기 형태는 v2/TongueOut이 0%→100%로 전환되는\n동안 50% 지점(입술을 넘는 타이밍)에서 최대가 되도록 자동으로 구성됩니다\n(0%・100%에서는 들어올리지 않음). 리모트 동기화 시 v2/TongueOut이\n거칠게 양자화되는 환경에서도 안정적이도록, 피크 위치는 50%로 고정되어\n있습니다." },
            ["検出頂点を持ち上げる方向(ローカル空間)。既定はY+(上)ですが、メッシュによっては「上」がY+軸ではなくZ軸(奥行方向)等になっている場合があります。\nTongue Up Amountを上げたときに舌が奥や横に動いてしまう場合は、Scene Viewのプレビュー(白→赤の球)を見ながら正しい方向に切り替えてください。"] = new[] { "The direction (local space) in which detected vertices are lifted. The default is Y+ (up), but\ndepending on the mesh, \"up\" may not be the Y+ axis and could instead be the Z axis (depth), etc.\nIf the tongue moves backward or sideways when you increase the Tongue Up Amount, switch to the\ncorrect direction while watching the Scene View preview (white → red spheres).", "检测顶点被抬起的方向(本地空间)。默认为Y+(上)，但根据网格的不同，\n「上」有时并非Y+轴，而是Z轴(纵深方向)等。\n如果增大Tongue Up Amount时舌头向后方或侧方移动，请一边观察Scene View\n预览(白→红球)一边切换到正确的方向。", "검출된 정점을 들어올리는 방향(로컬 공간)입니다. 기본값은 Y+(위)이지만,\n메쉬에 따라서는 「위」가 Y+ 축이 아니라 Z 축(깊이 방향) 등인 경우가\n있습니다.\nTongue Up Amount를 올렸을 때 혀가 안쪽이나 옆쪽으로 움직여 버리는 경우,\nScene View 미리보기(흰색→빨간색 구)를 보면서 올바른 방향으로 전환해\n주세요." },
            ["複数の接頭辞を混在させたい場合は、カンマ区切りで指定できます(例: 'FT.,facial.')。\n各ARKitシェイプキーごとに、指定した候補のうち実際にメッシュ上に存在する接頭辞を\n自動的に選んで使います。どの候補でも見つからない場合は、先頭の候補を使います。"] = new[] { "If you want to mix multiple prefixes, you can specify them separated by commas\n(e.g. 'FT.,facial.'). For each ARKit shape key, the prefix that actually exists on the mesh\namong the specified candidates is automatically chosen and used. If none of the candidates are\nfound, the first candidate is used.", "如果想混用多个前缀，可以用逗号分隔指定(例如：'FT.,facial.')。\n对于每个ARKit形态键，会从指定的候选中自动选择实际存在于网格上的前缀\n并使用。如果所有候选都未找到，则使用第一个候选。", "여러 접두사를 혼용하고 싶은 경우, 쉼표로 구분하여 지정할 수 있습니다\n(예: 'FT.,facial.'). 각 ARKit 쉐이프 키마다, 지정한 후보 중 실제로 메쉬에\n존재하는 접두사를 자동으로 선택하여 사용합니다. 어떤 후보도 찾지 못한\n경우 첫 번째 후보를 사용합니다." },
            ["アバターが既に持っている、舌を持ち上げるシェイプキーを一覧から選択してください。\nこのシェイプキーの頂点差分を、下のWeightで指定した強度でそのまま流用します。"] = new[] { "Select a tongue-lifting shape key your avatar already has from the list.\nThis shape key's vertex displacement is used as-is, at the strength specified by the Weight below.", "请从列表中选择角色本身已有的舌头抬起形态键。\n将直接使用该形态键的顶点位移，强度按下方的Weight指定。", "아바타가 이미 가지고 있는 혀 들어올리기 쉐이프 키를 목록에서 선택해 주세요.\n이 쉐이프 키의 정점 변위를, 아래의 Weight로 지정한 강도로 그대로 사용합니다." },
            ["⚠ Profile「{0}」を読み込み済みですが、このアバターにはFace Meshが見つからないか、ARKit標準シェイプキーが1つも検出されませんでした。Avatar/Face Meshの選択をご確認ください。"] = new[] { "⚠ Profile \"{0}\" has been loaded, but this avatar's Face Mesh could not be found, or no ARKit\nstandard shape keys were detected at all. Please check your Avatar/Face Mesh selection.", "⚠ 已加载Profile“{0}”，但未能在此角色上找到Face Mesh，或未检测到任何ARKit标准\n形态键。请确认Avatar/Face Mesh的选择。", "⚠ Profile \"{0}\"을(를) 불러왔지만, 이 아바타에서 Face Mesh를 찾을 수 없거나 ARKit\n표준 쉐이프 키가 하나도 검출되지 않았습니다. Avatar/Face Mesh 선택을 확인해 주세요." },
            ["✓ Profile「{0}」を読み込み済みです。ARKit標準シェイプキー{1}個を検出しました。Installできます。"] = new[] { "✓ Profile \"{0}\" has been loaded. Detected {1} ARKit standard shape keys. You can Install.", "✓ 已加载Profile“{0}”。检测到{1}个ARKit标准形态键。可以进行Install。", "✓ Profile \"{0}\"을(를) 불러왔습니다. ARKit 표준 쉐이프 키 {1}개를 검출했습니다. Install할 수 있습니다." },
            ["⚠ EyeLook未設定"] = new[] { "⚠ EyeLook Not Configured", "⚠ EyeLook未设置", "⚠ EyeLook 미설정" },
            ["⚠ Viseme未設定"] = new[] { "⚠ Viseme Not Configured", "⚠ Viseme未设置", "⚠ Viseme 미설정" },
            ["AvatarDescriptorにLeft Eye/Right Eyeボーンが設定されていません。"] = new[] { "AvatarDescriptor does not have Left Eye/Right Eye bones configured.", "AvatarDescriptor未设置Left Eye/Right Eye骨骼。", "AvatarDescriptor에 Left Eye/Right Eye 본이 설정되어 있지 않습니다." },
            ["AvatarDescriptorのEyeLookボーンは設定されていますが、上下左右いずれの方向も\n回転量が実質0のままです(目線が動くように設定されていません)。"] = new[] { "AvatarDescriptor's EyeLook bones are configured, but the rotation amount is essentially 0 in\nevery direction (up/down/left/right). The gaze isn't set up to actually move.", "AvatarDescriptor的EyeLook骨骼虽已设置，但上下左右各方向的旋转量\n实质上均为0(视线未被设置为可以移动)。", "AvatarDescriptor의 EyeLook 본은 설정되어 있지만, 상하좌우 모든 방향의\n회전량이 실질적으로 0인 상태입니다(시선이 움직이도록 설정되어 있지 않습니다)." },
            ["AvatarDescriptorのLip SyncがViseme Blend Shapeに設定されていません。"] = new[] { "AvatarDescriptor's Lip Sync is not set to Viseme Blend Shape.", "AvatarDescriptor的Lip Sync未设置为Viseme Blend Shape。", "AvatarDescriptor의 Lip Sync가 Viseme Blend Shape로 설정되어 있지 않습니다." },
            ["AvatarDescriptorにVisemeシェイプキーが1つも設定されていません。"] = new[] { "AvatarDescriptor does not have a single Viseme shape key configured.", "AvatarDescriptor未设置任何Viseme形态键。", "AvatarDescriptor에 Viseme 쉐이프 키가 하나도 설정되어 있지 않습니다." },
            ["口内にあり通常は隠れて見えない検出頂点を、顔メッシュ越しに透過表示します。\n白い球=移動前、赤い球=移動後の位置です。FACEカードでFace SMRを選択している必要があります。\n※Scene上に配置されたモデルでのみ機能します。Project内のPrefabアセットを直接指定した\n場合は、Scene上に表示されないため、この機能は動作しません。"] = new[] { "Displays the detected vertices, which are inside the mouth and normally hidden, semi-transparently\nthrough the face mesh. White sphere = position before moving, red sphere = position after moving.\nYou need to have a Face SMR selected in the FACE card.\n* Only works for a model placed in the Scene. If you've directly specified a Project prefab\nasset, this won't work since it isn't shown in the Scene.", "将口腔内通常隐藏不可见的检测顶点，透过面部网格以半透明方式显示。\n白色球=移动前的位置，红色球=移动后的位置。需要在FACE卡片中选择了Face SMR。\n※仅对放置在Scene中的模型有效。如果直接指定了Project内的Prefab\n资源，由于未显示在Scene中，此功能将无法工作。", "입 안에 있어 평소에는 가려져 보이지 않는 검출 정점을, 얼굴 메쉬 너머로\n반투명하게 표시합니다. 흰색 구=이동 전, 빨간색 구=이동 후의 위치입니다.\nFACE 카드에서 Face SMR을 선택해 두어야 합니다.\n※Scene에 배치된 모델에서만 작동합니다. Project 내의 Prefab 에셋을\n직접 지정한 경우, Scene에 표시되지 않으므로 이 기능은 작동하지 않습니다." },
            ["選択中のAvatarはProject内のPrefabアセットのため、Scene Viewへのプレビュー表示は機能しません。\nSceneにモデルを配置してから選択し直してください。"] = new[] { "The selected Avatar is a Prefab asset inside the Project, so the Scene View preview won't work.\nPlease place the model in the Scene and select it again.", "当前选择的Avatar是Project内的Prefab资源，因此Scene View预览功能无法工作。\n请先将模型放置到Scene中，然后重新选择。", "선택된 Avatar는 Project 내의 Prefab 에셋이므로 Scene View 미리보기가 작동하지\n않습니다. Scene에 모델을 배치한 후 다시 선택해 주세요." },
        };
    }
}

namespace hinzka.FaceTracking.Editor
{
    /// <summary>
    /// ARKit FaceTracking汎用インストーラー。
    /// 元アセット非破壊。必要な複製Mesh / FX / ExpressionParameters / Menu を生成・注入する。
    /// </summary>
    public class ARKitFaceTrackingInstallerWindow : EditorWindow
    {
        // ── Profile ───────────────────────────────────────
        private ARKitFTProfile _profile;
        private string _avatarMatchTag = "";
        // 表示専用メタデータ(マッチングには使用しない)。複数のProfileを見比べる際、
        // どの作者/ショップの・どのバージョン向けのProfileかを目視で確認しやすくするためのもの。
        private string _profileShopName = "";
        private string _profileVersionName = "";

        // ── 入力 ──────────────────────────────────────────
        private GameObject _avatarPrefab;

        // 顔SMR
        private int _smrIndex = 0;
        private string[] _smrPaths = Array.Empty<string>();
        private SkinnedMeshRenderer[] _smrs = Array.Empty<SkinnedMeshRenderer>();

        // にっこり目シェイプキー (複数選択可)
        private List<int> _squintShapeIndices = new List<int>();
        private string[] _shapeNames = Array.Empty<string>();
        private string _squintSearchQuery = "";

        // ジェスチャーレイヤー (FXから取得したレイヤー名で選択)
        private List<int> _gestureLayerIndices = new List<int>();
        private List<string> _lastEyeLookEmptyDeltaShapes = new List<string>();
        // Install()の一連の処理(Viseme縮小・逆Viseme生成・EyeLook・眉アシスト・舌アシスト)は、
        // 各ステップが直前のメッシュを元に新しいメッシュアセットを生成し、Face SMRへ順に
        // 差し替えていく方式になっている。そのため、最終的に採用されるのは一番最後に
        // 生成されたメッシュだけであり、途中のメッシュは出力フォルダに残り続ける不要な
        // 中間生成物になる。Install()の開始時にクリアし、各生成関数がここへ自身の出力パスを
        // 登録する。Install完了時、最終的に採用されたメッシュ以外をここから削除する。
        // 複数の生成関数(static/インスタンス双方)から共通してアクセスするためstaticにしている。
        private static List<string> _installGeneratedMeshPaths = new List<string>();
        private bool _eyeSmrSeparate = false;
        private int _eyeSmrIndex = 0;
        private bool _eyeUsesConstraint = false;
        private Transform _leftEyeConstraintTarget;
        private Transform _rightEyeConstraintTarget;
        private bool _disableNativeEyeLook = false; // false=標準Eye Lookを維持、true=無効化(ラジオボタンで選択)
        private float _eyeLookIntensity = 1f;
        // 目線シェイプキー生成(ボーン回転のベイク)時、あらかじめ重み100で有効にしておく
        // 追加シェイプキー(_shapeNamesのインデックス、複数選択可)。目のハイライト・瞳孔等の
        // サブメッシュを手前に移動させるシェイプキーを持つアバターで、そのシェイプキーを
        // 有効にした状態を基準にベイクしないと、サブメッシュの回転移動量が不足し、
        // 目線を動かした際に眼球メッシュを貫通してしまうことがあるための対策。
        private List<int> _eyeLookBakeShapeIndices = new List<int>();
        private string _eyeLookBakeSearchQuery = "";
        // 「目線シェイプキー生成時に有効化する追加シェイプキー」機能自体のON/OFF。
        // OFF(既定)の場合、シェイプキーを選択していても一切ベイクに反映しない。
        private bool _useEyeLookBakeShapes = false;
        private string _arkitShapePrefix = "";
        private bool _hasBlendshapePrefix = false;
        // ARKit標準シェイプ名が見つからない場合、UE(Unified Expressions)側の代替名も
        // 検索するかどうか。ONの場合、_missingArkitShapesのうちUE代替名で解決できたものは
        // 「不足」から除外し、_ueFallbackResolvedShapesに記録する(Install時に実際の
        // カーブ複製もこの情報を使って行う)。
        private bool _ueFallbackEnabled = false;
        private Dictionary<string, string[]> _ueFallbackResolvedShapes = new Dictionary<string, string[]>();
        // AvatarDescriptorのEyeLookボーンが設定されており、かつ実際に動きが設定されているか
        // (Left/Right Eyeボーン未設定、または全方向の回転量が実質0のままだと、目線シェイプキー
        // 生成が実質的に無意味になるため、事前に警告できるようにする)。
        private bool _avatarEyeLookConfigured = true;
        private string _avatarEyeLookProblem = "";
        // AvatarDescriptorにVisemeが1つ以上設定されているか(LipSyncがVisemeBlendShapeで、
        // かつVisemeBlendShapesのいずれかが実際にシェイプキー名を持っているか)。
        private bool _avatarVisemeConfigured = true;
        private string _avatarVisemeProblem = "";
        // Profileに保存されたfaceSMRPathが現在のアバター上で解決できなかった場合にtrue。
        // 解決失敗時、_smrIndexは(誤解を招く形で)0番目の候補へ静かにフォールバックして
        // しまうため、その旨をUIで明示するためのフラグ。
        private bool _faceSmrPathMismatch = false;
        // アバター読み込み時点でのExpression Parameters bit予算の見積り
        // (テンプレートFXの同期最適化前の生の値との合算。実際にInstallすると
        // 「空/未検出シェイプの同期をオフ」等でこれより減ることがあるが、早期警告のため
        // あえて保守的(悪い方)の見積りにしている)。
        private int _estimatedTotalParamBits = 0;
        private int _estimatedExistingParamBits = 0;
        private int _estimatedFtParamBits = 0;
        private bool _estimatedParamBitsOverBudget = false;
        private bool _disableSyncForEmptyShapes = false;
        private ArkitShapeParameterMap _shapeParameterMap;
        private string[] _fxLayerNames = Array.Empty<string>();
        private List<string> _eyeTrackingControlLayerNames = new List<string>();
        private string _gestureSearchQuery = "";

        // Viseme補償
        private bool _generateVisemeCompensation = true;
        private float _visemeScale = 1f;

        // EyeLook自動生成
        private bool _generateEyeLookShapes = true;

        // まばたき制御方式(Blink2D / Blink Simple 1D)。テンプレートFXに両方式が同梱されている
        // 場合、Install時に選択されなかった方を無効化する。
        private BlinkControlMode _blinkControlMode = BlinkControlMode.TwoD;

        // 眉アシスト
        private bool _generateBrowAssistShapes = false;
        private float _browAssistIntensity = 0.5f;

        // 舌アシスト(舌頂点の自動検出 → 持ち上げシェイプ生成 → tongueOutとのミックス)
        private bool _generateTongueAssistShapes = false;
        // 「持ち上げ」形状の作り方。既定はExistingShapeKey(アバターが既に持っている舌持ち上げ
        // シェイプキーを流用する方が、頂点検出による一律持ち上げより自然な形状になりやすいため)。
        private TongueLiftSource _tongueLiftSource = TongueLiftSource.ExistingShapeKey;
        private string _tongueExistingLiftShapeName = "";
        private float _tongueExistingLiftShapeWeight = 100f;
        private string _tongueExistingLiftSearchQuery = "";
        private float _tongueMoveAmount = 0.01f; // 検出頂点をtongueLiftAxis方向へ持ち上げる移動量(ローカル空間)。AutoDetect時のみ使用
        private TongueLiftAxis _tongueLiftAxis = TongueLiftAxis.PlusY;
        private bool _showTonguePreview = true;
        // Scene Viewプレビューの球の大きさ(HandleUtility.GetHandleSizeに対する倍率)。
        // 既定値を他社製ツールのサンプル程度に大きめにしている。
        private float _tonguePreviewPointScale = 0.08f;
        // 舌検出の閾値。ワールド空間の実寸(mm)で指定する(メッシュ空間の生の値ではない)。
        // アーマチュアのスケールがアバターごとに異なっても、MeshUnitToWorldで自動的に
        // メッシュ空間の値へ変換されるため、同じmm値がどのアバターでも同じ意味を持つ。
        private float _tongueDetectThresholdMm = TONGUE_DETECT_THRESHOLD_MM_DEFAULT;
        private float _tongueLipExcludeThresholdMm = TONGUE_LIP_EXCLUDE_THRESHOLD_MM_DEFAULT;
        // 歯除外を主判定(tongueOut)にも適用するかどうか。既定はfalse。歯とキーワード一致する
        // シェイプキーが舌の可動域と大きく重なっているアバターでは、trueだと本来検出されるべき
        // 舌の頂点まで巻き込んで消えてしまうことがあるため、まずはOFFの状態を試してもらい、
        // 必要な場合にのみONへ切り替えられるようにする。
        private bool _tongueExcludeTeethFromPrimary = false;
        // MeshUnitToWorldの自動推定がアバターによっては大きく外れることがあるため、
        // 手動で上書きできるようにしている。0以下なら自動推定を使う。
        private float _tongueUnitOverride = 0f;
        // Scene Viewプレビュー用キャッシュ(選択中のFace SMRに対するもの)
        private Vector3[] _tonguePreviewBaseVertices;
        private HashSet<int> _tonguePreviewVertexIndices;

        // まばたきエフェクト(おまけ機能)
        private bool _addBlinkEffect = false;
        private AnimationClip _blinkEffectClip;

        // ARKitシェイプキーチェック結果
        private List<string> _missingArkitShapes = new List<string>();
        private List<string> _emptyArkitShapes = new List<string>();
        private string _arkitCheckSmrPath = "";

        // 出力先
        private string _outputFolder = "Assets/NK_Installer_Generated";


        // ── UI Toolkit ──────────────────────────────────────
        private bool _uiReady;
        private VisualElement _uiHeaderHost;
        private VisualElement _uiPageHost;
        private VisualElement _uiFooterHost;
        private VisualElement _uiBasicPage;
        private VisualElement _uiTrackingPage;
        private VisualElement _uiAdvancedPage;
        private Button _uiBasicTabButton;
        private Button _uiTrackingTabButton;
        private Button _uiAdvancedTabButton;
        private int _uiSelectedTab = 0;
        private ObjectField _uiProfileField;
        private TextField _uiAvatarMatchTagField;
        private VisualElement _uiAvatarMatchTagRow;
        private TextField _uiProfileShopNameField;
        private TextField _uiProfileVersionNameField;
        private VisualElement _uiProfileMetaRow;
        private VisualElement _uiProfileReadyBanner;
        private Label _uiProfileReadyBannerLabel;
        private ObjectField _uiAvatarField;
        private DropdownField _uiFaceSmrField;
        private TextField _uiArkitPrefixField;
        private Toggle _uiHasBlendshapePrefixToggle;
        private Toggle _uiUeFallbackToggle;
        private VisualElement _uiUeFallbackDetail;
        private VisualElement _uiBlendshapePrefixDetail;
        private Toggle _uiDisableEmptySyncToggle;
        private VisualElement _uiDisableEmptySyncHint;
        private Toggle _uiEyeSeparateToggle;
        private DropdownField _uiEyeSmrField;
        private VisualElement _uiEyeSmrRow;
        private VisualElement _uiFaceDetail;
        private VisualElement _uiFaceSmrPathMismatchHint;
        private VisualElement _uiFaceAvatarGate;
        private VisualElement _uiFaceNoAvatarHint;
        private VisualElement _uiAvatarStatusRow;
        private VisualElement _uiFaceStatusRow;

        private TextField _uiSquintSearchField;
        private VisualElement _uiSquintList;
        private Button _uiSquintAddButton;
        private VisualElement _uiSquintAvatarGate;
        private VisualElement _uiSquintNoAvatarHint;

        private TextField _uiGestureSearchField;
        private VisualElement _uiGestureList;
        private Button _uiGestureAddButton;
        private VisualElement _uiGestureSuppressDetail;
        private VisualElement _uiGestureAvatarGate;
        private VisualElement _uiGestureNoAvatarHint;

        private Toggle _uiVisemeToggle;
        private Slider _uiVisemeSlider;
        private FloatField _uiVisemeValue;
        private VisualElement _uiVisemeDetail;
        private VisualElement _uiVisemeConfigWarningHint;
        private VisualElement _uiTonguePreviewAssetWarningHint;
        private VisualElement _uiEyeLookConfigWarningHint;

        private VisualElement _uiBlinkModeCard;
        private Toggle _uiBlinkSimple1DToggle;
        private VisualElement _uiBlinkModeHint;

        private Toggle _uiEyeLookToggle;
        private Slider _uiEyeLookSlider;
        private FloatField _uiEyeLookValue;
        private VisualElement _uiEyeLookDetail;
        private Toggle _uiEyeConstraintToggle;
        private VisualElement _uiEyeConstraintFields;
        private Toggle _uiEyeLookBakeToggle;
        private VisualElement _uiEyeLookBakeDetail;
        private TextField _uiEyeLookBakeSearchField;
        private VisualElement _uiEyeLookBakeShapeList;
        private Button _uiEyeLookBakeAddButton;
        private ObjectField _uiLeftEyeField;
        private ObjectField _uiRightEyeField;
        private VisualElement _uiEyeConflictBox;
        private Label _uiEyeConflictText;
        private VisualElement _uiEyeCompatCard;
        private VisualElement _uiEyeStableCard;

        private Toggle _uiBrowToggle;
        private Slider _uiBrowSlider;
        private FloatField _uiBrowValue;
        private VisualElement _uiBrowDetail;

        private VisualElement _uiTongueCard;
        private Toggle _uiTongueToggle;
        private PopupField<TongueLiftSource> _uiTongueLiftSourceField;
        private VisualElement _uiTongueExistingShapeDetail;
        private TextField _uiTongueExistingShapeSearchField;
        private VisualElement _uiTongueExistingShapePopup;
        private PopupField<int> _uiTongueExistingShapeField;
        private VisualElement _uiTongueExistingShapeHint;
        private Slider _uiTongueExistingShapeWeightSlider;
        private FloatField _uiTongueExistingShapeWeightValue;
        private VisualElement _uiTongueAutoDetectDetail;
        private Slider _uiTongueMoveSlider;
        private FloatField _uiTongueMoveValue;
        private VisualElement _uiTongueDetail;
        private Toggle _uiTonguePreviewToggle;
        private Slider _uiTonguePreviewSizeSlider;
        private FloatField _uiTonguePreviewSizeValue;
        private Label _uiTongueDetectedCountLabel;
        private Slider _uiTongueDetectThresholdSlider;
        private FloatField _uiTongueDetectThresholdValue;
        private Slider _uiTongueLipExcludeThresholdSlider;
        private FloatField _uiTongueLipExcludeThresholdValue;
        private EnumField _uiTongueLiftAxisField;
        private Toggle _uiTongueExcludeTeethFromPrimaryToggle;
        private Toggle _uiTongueUnitOverrideToggle;
        private FloatField _uiTongueUnitOverrideField;
        private Label _uiTongueUnitInfoLabel;

        private Toggle _uiBlinkEffectToggle;
        private ObjectField _uiBlinkEffectClipField;
        private VisualElement _uiBlinkEffectDetail;
        private VisualElement _uiBlinkEffectCard;

        private TextField _uiOutputField;
        private VisualElement _uiReadyStatusRow;
        private Label _uiReadyTitle;
        private Button _uiInstallButton;
        private Button _uiProfileSaveButton;
        private VisualElement _uiInstallResultCard;
        private VisualElement _uiAvatarCard;
        private VisualElement _uiFaceCard;
        private VisualElement _uiSquintCard;
        private VisualElement _uiGestureCard;
        private VisualElement _uiMouthCard;
        private VisualElement _uiEyesCard;
        private VisualElement _uiAssistCard;
        private VisualElement _uiInstallResultBody;
        private InstallResultSummary _lastInstallResult;

        private sealed class InstallResultSummary
        {
            public GameObject avatar;
            public string avatarName;
            public string faceSmr;
            public string eyeSmr;
            public string squint;
            public string gestures;
            public string viseme;
            public string eyeLook;
            public string brow;
            public string blinkEffect;
            public string nativeEyeLook;
            public string parameters;
            public string emptySync;
            public string ueFallback;
            public string missingShapes;
            public string outputFolder;
            public bool parametersOverBudget;
        }

        // テンプレートパス (このスクリプトと同じフォルダの Templates/ 以下)
        private const string TEMPLATE_FX_GUID_PREF   = "hinzka_ARKitFT_TemplateFxPath";
        private const string TEMPLATE_MENU_GUID_PREF = "hinzka_ARKitFT_TemplateMenuPath";
        private const string TEMPLATE_PARAM_GUID_PREF= "hinzka_ARKitFT_TemplateParamPath";
        private const string TEMPLATE_SMR_PATH        = "Body"; // テンプレートFX内のデフォルトSMRパス

        // Apple ARFaceAnchor準拠 (52個・文頭小文字、FXが参照する表記に合わせる)
        private static readonly string[] ARKIT_SHAPE_NAMES =
        {
            "browDownLeft", "browDownRight", "browInnerUp", "browOuterUpLeft", "browOuterUpRight",
            "cheekPuff", "cheekSquintLeft", "cheekSquintRight",
            "eyeBlinkLeft", "eyeBlinkRight",
            "eyeLookDownLeft", "eyeLookDownRight", "eyeLookInLeft", "eyeLookInRight",
            "eyeLookOutLeft", "eyeLookOutRight", "eyeLookUpLeft", "eyeLookUpRight",
            "eyeSquintLeft", "eyeSquintRight", "eyeWideLeft", "eyeWideRight",
            "jawForward", "jawLeft", "jawOpen", "jawRight",
            "mouthClose",
            "mouthDimpleLeft", "mouthDimpleRight",
            "mouthFrownLeft", "mouthFrownRight",
            "mouthFunnel", "mouthLeft",
            "mouthLowerDownLeft", "mouthLowerDownRight",
            "mouthPressLeft", "mouthPressRight",
            "mouthPucker", "mouthRight",
            "mouthRollLower", "mouthRollUpper",
            "mouthShrugLower", "mouthShrugUpper",
            "mouthSmileLeft", "mouthSmileRight",
            "mouthStretchLeft", "mouthStretchRight",
            "mouthUpperUpLeft", "mouthUpperUpRight",
            "noseSneerLeft", "noseSneerRight",
            "tongueOut",
        };

        /// <summary>
        /// ARKit標準シェイプ名が(接頭辞込みでも)メッシュ上に見つからなかった場合の、
        /// VRCFaceTracking「Unified Expressions」側の代替名候補。
        ///
        /// 1つのARKit名につき、優先順に複数の「候補グループ」を持つ。各グループは
        /// 1つ以上のUEシェイプ名からなり、グループ内の名前が全てメッシュ上に存在する
        /// 場合にそのグループを採用する(例: cheekPuffなら、まず単一のBlended Shape
        /// "CheekPuff" を探し、無ければ左右分割の "CheekPuffLeft"+"CheekPuffRight" の
        /// 両方を探す)。採用されたグループの全シェイプに、元のARKitパラメータと同じ
        /// カーブを複製設定することで、1つのOSC値で複数のBlendShapeを同時に駆動する。
        ///
        /// アバターによってBase(左右個別)とBlended(左右統合)のどちらを持っているかが
        /// バラバラであるため、実在するアバターでの検証結果をもとに現実的な組み合わせを
        /// 採用している。
        /// </summary>
        private static readonly Dictionary<string, string[][]> ARKIT_TO_UE_FALLBACK =
            new Dictionary<string, string[][]>
        {
            ["browDownLeft"]        = new[] { new[] { "BrowDownLeft" }, new[] { "BrowLowererLeft", "BrowPinchLeft" } },
            ["browDownRight"]       = new[] { new[] { "BrowDownRight" }, new[] { "BrowLowererRight", "BrowPinchRight" } },
            ["browInnerUp"]         = new[] { new[] { "BrowInnerUp" }, new[] { "BrowInnerUpLeft", "BrowInnerUpRight" } },
            ["browOuterUpLeft"]     = new[] { new[] { "BrowOuterUpLeft" } },
            ["browOuterUpRight"]    = new[] { new[] { "BrowOuterUpRight" } },
            ["cheekPuff"]           = new[] { new[] { "CheekPuff" }, new[] { "CheekPuffLeft", "CheekPuffRight" } },
            ["cheekSquintLeft"]     = new[] { new[] { "CheekSquintLeft" } },
            ["cheekSquintRight"]    = new[] { new[] { "CheekSquintRight" } },
            ["eyeBlinkLeft"]        = new[] { new[] { "EyeClosedLeft" } },
            ["eyeBlinkRight"]       = new[] { new[] { "EyeClosedRight" } },
            ["eyeLookDownLeft"]     = new[] { new[] { "EyeLookDownLeft" } },
            ["eyeLookDownRight"]    = new[] { new[] { "EyeLookDownRight" } },
            ["eyeLookInLeft"]       = new[] { new[] { "EyeLookInLeft" } },
            ["eyeLookInRight"]      = new[] { new[] { "EyeLookInRight" } },
            ["eyeLookOutLeft"]      = new[] { new[] { "EyeLookOutLeft" } },
            ["eyeLookOutRight"]     = new[] { new[] { "EyeLookOutRight" } },
            ["eyeLookUpLeft"]       = new[] { new[] { "EyeLookUpLeft" } },
            ["eyeLookUpRight"]      = new[] { new[] { "EyeLookUpRight" } },
            ["eyeSquintLeft"]       = new[] { new[] { "EyeSquintLeft" } },
            ["eyeSquintRight"]      = new[] { new[] { "EyeSquintRight" } },
            ["eyeWideLeft"]         = new[] { new[] { "EyeWideLeft" } },
            ["eyeWideRight"]        = new[] { new[] { "EyeWideRight" } },
            ["jawForward"]          = new[] { new[] { "JawForward" } },
            ["jawLeft"]             = new[] { new[] { "JawLeft" } },
            ["jawOpen"]             = new[] { new[] { "JawOpen" } },
            ["jawRight"]            = new[] { new[] { "JawRight" } },
            ["mouthClose"]          = new[] { new[] { "MouthClosed" } },
            ["mouthDimpleLeft"]     = new[] { new[] { "MouthDimpleLeft" } },
            ["mouthDimpleRight"]    = new[] { new[] { "MouthDimpleRight" } },
            ["mouthFrownLeft"]      = new[] { new[] { "MouthFrownLeft" } },
            ["mouthFrownRight"]     = new[] { new[] { "MouthFrownRight" } },
            ["mouthFunnel"]         = new[] { new[] { "LipFunnel" },
                new[] { "LipFunnelUpperLeft", "LipFunnelUpperRight", "LipFunnelLowerLeft", "LipFunnelLowerRight" } },
            ["mouthLeft"]           = new[] { new[] { "MouthLeft" }, new[] { "MouthUpperLeft", "MouthLowerLeft" } },
            ["mouthLowerDownLeft"]  = new[] { new[] { "MouthLowerDownLeft" } },
            ["mouthLowerDownRight"] = new[] { new[] { "MouthLowerDownRight" } },
            ["mouthPressLeft"]      = new[] { new[] { "MouthPressLeft" } },
            ["mouthPressRight"]     = new[] { new[] { "MouthPressRight" } },
            ["mouthPucker"]         = new[] { new[] { "LipPucker" },
                new[] { "LipPuckerUpperLeft", "LipPuckerUpperRight", "LipPuckerLowerLeft", "LipPuckerLowerRight" } },
            ["mouthRight"]          = new[] { new[] { "MouthRight" }, new[] { "MouthUpperRight", "MouthLowerRight" } },
            ["mouthRollLower"]      = new[] { new[] { "LipSuckLower" }, new[] { "LipSuckLowerLeft", "LipSuckLowerRight" } },
            ["mouthRollUpper"]      = new[] { new[] { "LipSuckUpper" }, new[] { "LipSuckUpperLeft", "LipSuckUpperRight" } },
            ["mouthShrugLower"]     = new[] { new[] { "MouthRaiserLower" } },
            ["mouthShrugUpper"]     = new[] { new[] { "MouthRaiserUpper" } },
            ["mouthSmileLeft"]      = new[] { new[] { "MouthSmileLeft" }, new[] { "MouthCornerPullLeft", "MouthCornerSlantLeft" } },
            ["mouthSmileRight"]     = new[] { new[] { "MouthSmileRight" }, new[] { "MouthCornerPullRight", "MouthCornerSlantRight" } },
            ["mouthStretchLeft"]    = new[] { new[] { "MouthStretchLeft" } },
            ["mouthStretchRight"]   = new[] { new[] { "MouthStretchRight" } },
            ["mouthUpperUpLeft"]    = new[] { new[] { "MouthUpperUpLeft" } },
            ["mouthUpperUpRight"]   = new[] { new[] { "MouthUpperUpRight" } },
            ["noseSneerLeft"]       = new[] { new[] { "NoseSneerLeft" } },
            ["noseSneerRight"]      = new[] { new[] { "NoseSneerRight" } },
            ["tongueOut"]           = new[] { new[] { "TongueOut" } },
        };


        // 優先SMR名リスト
        private static readonly string[] PRIORITY_SMR_NAMES =
            { "Body", "body", "Face", "face", "Head", "head" };

        // ── 舌アシスト ────────────────────────────────────
        // 舌検出の閾値(ローカル空間での頂点移動量)。tongue系シェイプキーでこれを超えて
        // 動く頂点を「舌候補」とし、唇系シェイプキーでこれを超えて動く頂点は除外する。
        private const float TONGUE_DETECT_THRESHOLD_MM_DEFAULT = 1.0f;
        private const float TONGUE_LIP_EXCLUDE_THRESHOLD_MM_DEFAULT = 0.5f;
        // 舌候補から除外する「唇・頬」シェイプキー(ARKit標準名。接頭辞は呼び出し側で解決する)。
        // mouthPucker・mouthFunnel・mouthCloseは口を閉じる/すぼめる動きで舌も一緒に動かして
        // しまうため、除外シグナルとしては不適切と判断し対象から外している。cheekPuffは
        // 頬を膨らませた際に口内壁の頂点も動くことがあり、舌の誤検出源になりやすいため
        // 主要な除外シグナルとして加えている。
        private static readonly string[] TONGUE_LIP_EXCLUDE_SHAPES =
            { "mouthRollUpper", "mouthRollLower", "cheekPuff" };
        // TongueOutSteps_BT配下の専用クリップ名の接頭辞。標準の汎用Binding
        // (hinzkaUE_Bind_v2_TongueOut等)と区別するために使う。
        private const string TONGUE_STEP_CLIP_PREFIX = "hinzkaUE_TongueStep_";
        private const string TONGUE_GAIN_TREE_NAME = "hinzkaUE_Gain_v2_TongueOut";
        // 標準の舌駆動BlendTree(UEFxGeneratorが全ARKitシェイプ共通で生成する、汎用の
        // "Gain_v2_<パラメータ名>"命名規則のtongueOut版)の固定名。持ち上げエンベロープを
        // 組み込む際、このBlendTreeをfx内から名前で探して直接組み替える。

        // ── まばたき制御方式(Blink2D / Blink Simple 1D) ──────────────────
        // UEFxGeneratorのUEFxGenConfigデフォルト命名規則に合わせた、専用レイヤー名。
        // LegacySeparateLayer配置の場合、この名前のレイヤーが見つかれば無効化対象にする。
        private const string BLINK_2D_LAYER_NAME = "UE_Blink2D";
        private const string BLINK_SIMPLE1D_LAYER_NAME = "UE_BlinkSimple1D";
        // InMainDriverDirect配置(Direct BlendTreeへの直接注入)の場合、Direct BlendTreeの
        // 子Motionの名前にこの文字列が含まれていれば無効化対象にする
        // (BrowModeSwitch/Modulationでラップされていない、素の状態でのみ確実に検出できる)。
        private const string BLINK_2D_MOTION_NAME_SUFFIX = "Blink2D_BT";
        private const string BLINK_SIMPLE1D_MOTION_NAME_SUFFIX = "BlinkSimple1D_Combined";

        [MenuItem("Tools/NK Installer")]
        public static void Open()
        {
            var w = GetWindow<ARKitFaceTrackingInstallerWindow>("NK Installer");
            w.minSize = new Vector2(640, 460);
        }

        private void OnEnable()
        {
            // ARKitシェイプ⇔OSCmoothパラメータの対応表(開発者側でFXジェネレータから生成・配置済み)を
            // 自動で読み込む。エンドユーザーが指定する必要はない。見つからなければnullのままで、
            // その場合は部分一致による判定にフォールバックする。
            _shapeParameterMap = FindTemplate<ArkitShapeParameterMap>("ARKit_FT_ShapeParamMap.asset");
            SceneView.duringSceneGui += OnTongueSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnTongueSceneGUI;
        }

        public void CreateGUI()
        {
            _uiReady = false;
            rootVisualElement.Clear();

            var script = MonoScript.FromScriptableObject(this);
            var scriptPath = AssetDatabase.GetAssetPath(script);
            var scriptDir = Path.GetDirectoryName(scriptPath)?.Replace('\\', '/');

            VisualTreeAsset visualTree = null;
            StyleSheet styleSheet = null;
            if (!string.IsNullOrEmpty(scriptDir))
            {
                visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    scriptDir + "/UI/ARKitFTInstaller.uxml");
                styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    scriptDir + "/UI/ARKitFTInstaller.uss");
            }

            if (visualTree != null)
                visualTree.CloneTree(rootVisualElement);
            else
                BuildToolkitFallbackRoot();

            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet);

            _uiHeaderHost = rootVisualElement.Q<VisualElement>("header-host");
            _uiPageHost = rootVisualElement.Q<VisualElement>("page-host");
            _uiFooterHost = rootVisualElement.Q<VisualElement>("footer-host");

            if (_uiHeaderHost == null || _uiPageHost == null || _uiFooterHost == null)
            {
                rootVisualElement.Clear();
                BuildToolkitFallbackRoot();
                _uiHeaderHost = rootVisualElement.Q<VisualElement>("header-host");
                _uiPageHost = rootVisualElement.Q<VisualElement>("page-host");
                _uiFooterHost = rootVisualElement.Q<VisualElement>("footer-host");
            }

            var scroll = rootVisualElement.Q<ScrollView>("main-scroll");
            if (scroll != null)
                scroll.mode = ScrollViewMode.Vertical;

            BuildToolkitHeader();
            BuildToolkitTabs();
            BuildToolkitPages();

            BuildToolkitAvatarCard();
            BuildToolkitFaceCard();
            BuildToolkitSquintCard();
            BuildToolkitGestureCard();

            BuildToolkitMouthCard();
            BuildToolkitEyesCard();
            BuildToolkitBlinkModeCard();
            BuildToolkitAssistCard();
            BuildToolkitTongueCard();
            BuildToolkitBlinkEffectCard();

            BuildToolkitAdvancedCard();
            BuildToolkitInstallFooter();

            SelectToolkitTab(_uiSelectedTab);
            _uiReady = true;
            RefreshToolkitUI();
        }

        private void BuildToolkitFallbackRoot()
        {
            var appRoot = new VisualElement { name = "app-root" };
            appRoot.AddToClassList("app-root");

            var headerHost = new VisualElement { name = "header-host" };
            headerHost.AddToClassList("header-host");
            appRoot.Add(headerHost);

            var tabHost = new VisualElement { name = "tab-host" };
            tabHost.AddToClassList("tab-host");
            appRoot.Add(tabHost);

            var scroll = new ScrollView(ScrollViewMode.Vertical) { name = "main-scroll" };
            scroll.AddToClassList("main-scroll");
            var pageHost = new VisualElement { name = "page-host" };
            pageHost.AddToClassList("page-host");
            scroll.Add(pageHost);
            appRoot.Add(scroll);

            var footerHost = new VisualElement { name = "footer-host" };
            footerHost.AddToClassList("footer-host");
            appRoot.Add(footerHost);

            rootVisualElement.Add(appRoot);
        }

        private void BuildToolkitTabs()
        {
            var tabHost = rootVisualElement.Q<VisualElement>("tab-host");
            if (tabHost == null) return;
            tabHost.Clear();

            _uiBasicTabButton = MakeTabButton(ArkitFTLoc.T("基本設定"), 0);
            _uiTrackingTabButton = MakeTabButton(ArkitFTLoc.T("トラッキング"), 1);
            _uiAdvancedTabButton = MakeTabButton(ArkitFTLoc.T("詳細 / 結果"), 2);

            tabHost.Add(_uiBasicTabButton);
            tabHost.Add(_uiTrackingTabButton);
            tabHost.Add(_uiAdvancedTabButton);
        }

        private Button MakeTabButton(string text, int tabIndex)
        {
            var button = new Button(() => SelectToolkitTab(tabIndex)) { text = text };
            button.AddToClassList("tab-button");
            return button;
        }

        private void BuildToolkitPages()
        {
            _uiPageHost.Clear();

            _uiBasicPage = new VisualElement { name = "basic-page" };
            _uiTrackingPage = new VisualElement { name = "tracking-page" };
            _uiAdvancedPage = new VisualElement { name = "advanced-page" };

            _uiBasicPage.AddToClassList("tab-page");
            _uiTrackingPage.AddToClassList("tab-page");
            _uiAdvancedPage.AddToClassList("tab-page");

            _uiPageHost.Add(_uiBasicPage);
            _uiPageHost.Add(_uiTrackingPage);
            _uiPageHost.Add(_uiAdvancedPage);
        }

        private void SelectToolkitTab(int tabIndex)
        {
            _uiSelectedTab = Mathf.Clamp(tabIndex, 0, 2);

            if (_uiBasicPage != null)
                _uiBasicPage.style.display = _uiSelectedTab == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiTrackingPage != null)
                _uiTrackingPage.style.display = _uiSelectedTab == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiAdvancedPage != null)
                _uiAdvancedPage.style.display = _uiSelectedTab == 2 ? DisplayStyle.Flex : DisplayStyle.None;

            SetTabButtonSelected(_uiBasicTabButton, _uiSelectedTab == 0);
            SetTabButtonSelected(_uiTrackingTabButton, _uiSelectedTab == 1);
            SetTabButtonSelected(_uiAdvancedTabButton, _uiSelectedTab == 2);
        }

        private static void SetTabButtonSelected(Button button, bool selected)
        {
            if (button == null) return;
            if (selected) button.AddToClassList("tab-button-selected");
            else button.RemoveFromClassList("tab-button-selected");
        }

        private void BuildToolkitHeader()
        {
            var hero = new VisualElement();
            hero.AddToClassList("hero");

            var topRow = new VisualElement();
            topRow.AddToClassList("hero-top-row");

            var eyebrow = new Label("100% AVATAR-DERIVED");
            eyebrow.AddToClassList("hero-eyebrow");
            topRow.Add(eyebrow);

            // 言語切替。選択するとUI全体を再構築して即座に反映する。
            var langChoices = new List<string>
            {
                ArkitFTLoc.DisplayName(ArkitFTLoc.Lang.Japanese),
                ArkitFTLoc.DisplayName(ArkitFTLoc.Lang.English),
                ArkitFTLoc.DisplayName(ArkitFTLoc.Lang.ChineseSimplified),
                ArkitFTLoc.DisplayName(ArkitFTLoc.Lang.Korean),
            };
            var langField = new DropdownField(langChoices, (int)ArkitFTLoc.Current);
            langField.AddToClassList("lang-field");
            langField.RegisterValueChangedCallback(evt =>
            {
                int idx = langChoices.IndexOf(evt.newValue);
                if (idx < 0) return;
                ArkitFTLoc.Current = (ArkitFTLoc.Lang)idx;
                CreateGUI(); // 言語切替はUI全体を作り直して反映する
            });
            topRow.Add(langField);

            hero.Add(topRow);

            var title = new Label("NK Installer");
            title.AddToClassList("hero-title");
            title.tooltip = ArkitFTLoc.T(
                "NK = Native Key。アバター作者が作った「そのままの(Native)」シェイプキーを使い、\n" +
                "作り直したり別物に差し替えたりしません。");
            hero.Add(title);

            // "Native Shape Key" の頭文字 N/K だけ色を変え、"NK" の由来がさりげなく
            // 伝わるようにする(UI ToolkitのLabelは既定でリッチテキストのcolorタグに対応)。
            var tagline = new Label(
                "<color=#E780AE>N</color>ative Shape <color=#E780AE>K</color>ey " +
                "ARKit FaceTracking Installer");
            tagline.enableRichText = true;
            tagline.AddToClassList("hero-tagline");
            hero.Add(tagline);

            var subtitle = new Label(ArkitFTLoc.T("アバター作者がつくった表情で、そのままフェイストラッキング"));
            subtitle.AddToClassList("hero-subtitle");
            hero.Add(subtitle);

            // Avatar選択はProfileより先に行う設定なので、案内文とあわせてProfileより上に置く。
            // (以前はProfileの方が先に目に入る配置になっており、「まずProfileに何か
            // 入れなければ」と誤解させてしまっていたため)
            var avatarGuide = MakeHint(
                ArkitFTLoc.T("まずアバターを選択してください。名前や識別タグが対応するProfileがあれば自動的に選択されます。"),
                "profile");
            avatarGuide.AddToClassList("profile-guide");
            hero.Add(avatarGuide);

            var avatarRow = new VisualElement();
            avatarRow.AddToClassList("toolbar-row");

            _uiAvatarField = new ObjectField("Avatar")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = true
            };
            _uiAvatarField.AddToClassList("grow-field");
            _uiAvatarField.RegisterValueChangedCallback(evt =>
            {
                _avatarPrefab = evt.newValue as GameObject;
                ReloadAvatarState();
            });
            // ObjectFieldは「値が実際に変化した場合」しかChangeEventを発火しないため、
            // 同じAvatarを(FXだけアバター側で差し替えた後などに)再度ドラッグ&ドロップしても、
            // 参照自体は変わらず検知されない。ドラッグ&ドロップの完了自体を直接検知することで、
            // 同一アバターの再ドロップでも確実に再チェックされるようにする
            // (値が変わった場合はChangeEvent側でもReloadAvatarStateが呼ばれ二重になるが、
            // 常に最新状態へ揃えるだけの処理なので害はない)。
            _uiAvatarField.RegisterCallback<DragPerformEvent>(evt =>
            {
                if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
                    ReloadAvatarState();
            });
            avatarRow.Add(_uiAvatarField);

            var reloadButton = new Button(() => ReloadAvatarState()) { text = ArkitFTLoc.T("再読込") };
            reloadButton.tooltip =
                ArkitFTLoc.T("同じAvatarを選択したままFX等をアバター側で変更した場合、\n" +
                "ObjectFieldの値自体は変化しないため自動では再検出されません。\n" +
                "このボタンでAvatar情報(SMR / ARKitチェック / FXレイヤー)を強制的に再読み込みします。");
            reloadButton.AddToClassList("mini-button");
            avatarRow.Add(reloadButton);
            hero.Add(avatarRow);

            var profileGuide = MakeHint(
                ArkitFTLoc.T("見つからない場合は「新規Profile」で作成、または「既存Profile」から選び直せます。"),
                "profile");
            profileGuide.AddToClassList("profile-guide");
            hero.Add(profileGuide);

            var profileRow = new VisualElement();
            profileRow.AddToClassList("toolbar-row");

            _uiProfileField = new ObjectField(ArkitFTLoc.T("Profile（任意）"))
            {
                objectType = typeof(ARKitFTProfile),
                allowSceneObjects = false
            };
            _uiProfileField.AddToClassList("grow-field");
            _uiProfileField.tooltip = ArkitFTLoc.T("初回は空欄で構いません。保存済みの設定を再利用するときだけProfileを指定します。");
            _uiProfileField.RegisterValueChangedCallback(evt =>
            {
                SetCurrentProfile(evt.newValue as ARKitFTProfile, true);
            });

            var loadButton = new Button(() =>
            {
                ShowExistingProfileMenu();
            }) { text = ArkitFTLoc.T("既存Profile") };
            loadButton.tooltip = ArkitFTLoc.T("プロジェクト内のARKit FT Profileから選択して読み込みます。");
            loadButton.AddToClassList("mini-button");

            var newButton = new Button(() =>
            {
                CreateNewProfile();
                ExpandAllCollapsibleCards();
                RefreshToolkitUI();
            }) { text = ArkitFTLoc.T("新規Profile") };
            newButton.tooltip = ArkitFTLoc.T("現在の設定を保存する新しいProfileを作成します。");
            newButton.AddToClassList("mini-button");

            _uiProfileSaveButton = new Button(() =>
            {
                SaveToProfile();
                RefreshToolkitUI();
            }) { text = ArkitFTLoc.T("保存") };
            _uiProfileSaveButton.AddToClassList("mini-button");

            profileRow.Add(_uiProfileField);
            profileRow.Add(loadButton);
            profileRow.Add(newButton);
            profileRow.Add(_uiProfileSaveButton);
            hero.Add(profileRow);

            // アバター名からの自動選択精度を上げるための識別タグ。
            // バージョン名や接頭辞・接尾辞に左右されない本質的な文字列を登録しておける。
            // カンマ区切りで複数指定可能。
            var tagRow = new VisualElement();
            tagRow.AddToClassList("toolbar-row");
            _uiAvatarMatchTagField = new TextField(ArkitFTLoc.T("識別タグ (任意)"));
            _uiAvatarMatchTagField.AddToClassList("grow-field");
            _uiAvatarMatchTagField.tooltip = ArkitFTLoc.T(
                "このProfileを自動選択する際の目印となる文字列。Avatar名にこの文字列が含まれていれば、\n" +
                "バージョン名や接頭辞・接尾辞が付いていても最優先でこのProfileが選ばれる。\n" +
                "カンマ区切りで複数指定できる(いずれか1つでも一致すれば選ばれる)。複数のProfileやタグが\n" +
                "同時に一致した場合は、Avatar名との完全一致を最優先し、次により長いタグ、最後にタグの\n" +
                "登録順(先に書いたもの)で決まる。これにより「Sumiya」と「miya」のような包含関係も区別できる。\n" +
                "空の場合はファイル名からの推測にフォールバックする。\n" +
                "「保存」ボタンで他の設定と一緒に保存される。");
            _uiAvatarMatchTagField.RegisterValueChangedCallback(evt => { _avatarMatchTag = evt.newValue ?? ""; });
            tagRow.Add(_uiAvatarMatchTagField);
            _uiAvatarMatchTagRow = tagRow;
            hero.Add(tagRow);

            // 表示専用メタデータ(ショップ名・バージョン名)。マッチングには一切使用しない。
            var metaRow = new VisualElement();
            metaRow.AddToClassList("toolbar-row");
            _uiProfileShopNameField = new TextField(ArkitFTLoc.T("ショップ名/作者名"));
            _uiProfileShopNameField.AddToClassList("grow-field");
            _uiProfileShopNameField.tooltip =
                ArkitFTLoc.T("表示専用のメタデータで、自動選択のマッチングには一切使用されない。\n" +
                "複数のProfileを見比べる際、どの作者/ショップが配布したものかを目視で\n" +
                "確認しやすくするためのもの。「保存」ボタンで他の設定と一緒に保存される。");
            _uiProfileShopNameField.RegisterValueChangedCallback(evt => { _profileShopName = evt.newValue ?? ""; });
            metaRow.Add(_uiProfileShopNameField);

            _uiProfileVersionNameField = new TextField(ArkitFTLoc.T("バージョン名"));
            _uiProfileVersionNameField.AddToClassList("grow-field");
            _uiProfileVersionNameField.tooltip =
                ArkitFTLoc.T("表示専用のメタデータで、自動選択のマッチングには一切使用されない。\n" +
                "例: \"v1.2\"。アバター本体の更新に合わせてProfileを複製・更新した際に、\n" +
                "どのバージョン向けかを目視で確認しやすくするためのもの。\n" +
                "「保存」ボタンで他の設定と一緒に保存される。");
            _uiProfileVersionNameField.RegisterValueChangedCallback(evt => { _profileVersionName = evt.newValue ?? ""; });
            metaRow.Add(_uiProfileVersionNameField);

            _uiProfileMetaRow = metaRow;
            hero.Add(metaRow);

            _uiHeaderHost.Add(hero);

            // Profileが読み込まれている場合、「このままInstallできます」という目立つ帯を
            // 表示し、大きなInstallボタンを併設する。初めて使う人が上から順にすべての
            // Profileが読み込まれている場合、その旨を伝える控えめな帯を表示する。
            // 「そのままInstallできる」と断定してしまうと、ユーザーが作りかけの未完成な
            // Profileを保存していた場合に誤解を招くため、あくまで「読み込み済みである」
            // という事実のみを伝え、判断はユーザーに委ねる(Installボタン自体は置かない)。
            _uiProfileReadyBanner = new VisualElement();
            _uiProfileReadyBanner.AddToClassList("profile-ready-banner");
            _uiProfileReadyBanner.style.paddingTop = 10;
            _uiProfileReadyBanner.style.paddingBottom = 10;
            _uiProfileReadyBanner.style.paddingLeft = 14;
            _uiProfileReadyBanner.style.paddingRight = 14;
            _uiProfileReadyBanner.style.marginTop = 8;
            _uiProfileReadyBanner.style.marginBottom = 8;
            _uiProfileReadyBanner.style.backgroundColor = new Color(0.16f, 0.32f, 0.28f);
            _uiProfileReadyBanner.style.borderTopLeftRadius = 6;
            _uiProfileReadyBanner.style.borderTopRightRadius = 6;
            _uiProfileReadyBanner.style.borderBottomLeftRadius = 6;
            _uiProfileReadyBanner.style.borderBottomRightRadius = 6;

            _uiProfileReadyBannerLabel = new Label();
            _uiProfileReadyBannerLabel.style.whiteSpace = WhiteSpace.Normal;
            _uiProfileReadyBanner.Add(_uiProfileReadyBannerLabel);

            _uiHeaderHost.Add(_uiProfileReadyBanner);
        }

        private void BuildToolkitAvatarCard()
        {
            var card = MakeCard("AVATAR", ArkitFTLoc.T("対象アバター"), "accent-primary");
            _uiAvatarCard = card;

            // Avatarの選択自体はタイトル(hero)エリアで行う。このカードは選択結果の
            // 詳細(状態チップ・注意事項)だけを表示する。
            _uiAvatarStatusRow = new VisualElement();
            _uiAvatarStatusRow.AddToClassList("chip-row");
            card.Add(_uiAvatarStatusRow);

            var hint = MakeHint(ArkitFTLoc.T("元のアバターは変更しません。インストール時に複製して処理します。"), "info");
            card.Add(hint);

            // 対象アバターの状態は常に確認してほしい最重要カードなので、既定で開いておく。
            MakeCardCollapsible(card, "AVATAR", defaultExpanded: true);
            _uiBasicPage.Add(card);
        }

        private void BuildToolkitFaceCard()
        {
            var card = MakeCard("FACE", ArkitFTLoc.T("顔メッシュ設定"), "accent-primary");
            _uiFaceCard = card;

            _uiFaceSmrPathMismatchHint = MakeHint(
                ArkitFTLoc.T("Profileに保存されたFace Meshが現在のアバター上に見つかりませんでした" +
                "(アバターの階層が変わった可能性があります)。下のFace Meshを確認し、必要なら選び直してください。"),
                "warning");
            card.Add(_uiFaceSmrPathMismatchHint);

            _uiFaceSmrField = new DropdownField("Face Mesh");
            _uiFaceSmrField.RegisterValueChangedCallback(evt =>
            {
                int idx = Array.IndexOf(_smrPaths, evt.newValue);
                if (idx >= 0)
                {
                    _smrIndex = idx;
                    _faceSmrPathMismatch = false; // 手動で選び直したので警告は解除する
                    RefreshShapeList();
                    RefreshToolkitUI();
                }
            });
            card.Add(_uiFaceSmrField);

            // アバター未選択の段階では、この先の項目(ARKit Prefix以下)はすべて
            // 「何に対する設定か」が定まらず意味を持たないため、まとめて隠せる
            // ゲートコンテナに入れる。代わりに一言だけの案内を表示する。
            _uiFaceAvatarGate = new VisualElement();
            _uiFaceNoAvatarHint = MakeHint(ArkitFTLoc.T("アバターを選択すると顔メッシュの設定ができます。"), "soft");
            card.Add(_uiFaceNoAvatarHint);

            _uiHasBlendshapePrefixToggle = new Toggle(ArkitFTLoc.T("Blendshapeに接頭辞がある"));
            _uiHasBlendshapePrefixToggle.RegisterValueChangedCallback(evt =>
            {
                _hasBlendshapePrefix = evt.newValue;
                if (!_hasBlendshapePrefix) _arkitShapePrefix = "";
                RefreshArkitCheck();
                RefreshToolkitUI();
            });
            _uiFaceAvatarGate.Add(_uiHasBlendshapePrefixToggle);

            _uiBlendshapePrefixDetail = new VisualElement();

            _uiArkitPrefixField = new TextField(ArkitFTLoc.T("Blendshapeの接頭辞 (カンマ区切りで複数可)"));
            _uiArkitPrefixField.tooltip =
                ArkitFTLoc.T("複数の接頭辞を混在させたい場合は、カンマ区切りで指定できます(例: 'FT.,facial.')。\n" +
                "各ARKitシェイプキーごとに、指定した候補のうち実際にメッシュ上に存在する接頭辞を\n" +
                "自動的に選んで使います。どの候補でも見つからない場合は、先頭の候補を使います。");
            _uiArkitPrefixField.RegisterValueChangedCallback(evt =>
            {
                _arkitShapePrefix = evt.newValue ?? "";
                RefreshArkitCheck();
                RefreshToolkitUI();
            });
            _uiBlendshapePrefixDetail.Add(_uiArkitPrefixField);
            _uiBlendshapePrefixDetail.Add(MakeHint(
                ArkitFTLoc.T("アバターによって、Blendshape名に接頭辞が付いているためにARKitのシェイプキーを\n" +
                "正しく検出できないことがあります。接頭辞を指定すると、該当の文字列を除去して検索します。\n" +
                "1つのアバター内で複数の異なる接頭辞が混在している場合(例: ARKit系は'FT.'、独自の表情\n" +
                "シェイプ系は'facial.'等)は、カンマ区切りで複数指定してください。シェイプキーごとに、\n" +
                "実際にメッシュ上へ存在する接頭辞が自動的に選ばれます。"),
                "soft"));
            _uiFaceAvatarGate.Add(_uiBlendshapePrefixDetail);

            _uiUeFallbackToggle = new Toggle(ArkitFTLoc.T("UEのシェイプキーを流用※試験的"));
            _uiUeFallbackToggle.tooltip =
                ArkitFTLoc.T("ARKit標準名のシェイプキーが見つからない場合、VRCFaceTrackingの\n" +
                "「Unified Expressions」側の代替名(例: cheekPuff → CheekPuff、またはCheekPuffLeft/Right)\n" +
                "でも検索します。左右分割のシェイプキーが見つかった場合は、同じカーブを両方へ\n" +
                "複製設定します(1つのパラメータで両方が同時に動きます)。");
            _uiUeFallbackToggle.RegisterValueChangedCallback(evt =>
            {
                _ueFallbackEnabled = evt.newValue;
                RefreshArkitCheck();
                RefreshToolkitUI();
            });
            _uiFaceAvatarGate.Add(_uiUeFallbackToggle);

            _uiUeFallbackDetail = new VisualElement();
            _uiUeFallbackDetail.Add(MakeHint(
                ArkitFTLoc.T("アバター側がUnified Expressionsに対応しているとき、一部のシェイプキーをARKit向けに流用する試験的な機能です。\n" +
                "目の瞳孔径やあご・舌の詳細な動き等、ARKitでサポートしないシェイプキーは動作しません。"),
                "warning"));
            _uiFaceAvatarGate.Add(_uiUeFallbackDetail);

            _uiFaceStatusRow = new VisualElement();
            _uiFaceStatusRow.AddToClassList("chip-row");
            _uiFaceAvatarGate.Add(_uiFaceStatusRow);

            // Face Meshが未検出のときは、これらの詳細設定を出しても意味がない
            // (何に対する「同期オフ」「メッシュ分離」なのか判断できない)ため、
            // まとめて非表示にできるコンテナに入れる。
            _uiFaceDetail = new VisualElement();

            _uiDisableEmptySyncToggle = new Toggle(ArkitFTLoc.T("空 / 未検出シェイプの同期をオフにする"));
            _uiDisableEmptySyncToggle.tooltip =
                ArkitFTLoc.T("見た目に影響しないARKitシェイプに対応するNetwork Syncedパラメータをオフにし、bit予算を節約します。");
            _uiDisableEmptySyncToggle.RegisterValueChangedCallback(evt =>
            {
                _disableSyncForEmptyShapes = evt.newValue;
                RefreshToolkitUI();
            });
            _uiFaceDetail.Add(_uiDisableEmptySyncToggle);
            _uiDisableEmptySyncHint = MakeHint(
                ArkitFTLoc.T("空 / 未検出のシェイプキーを検出した場合、対応する同期パラメータをオフにして節約します。"),
                "soft");
            _uiFaceDetail.Add(_uiDisableEmptySyncHint);

            _uiEyeSeparateToggle = new Toggle(ArkitFTLoc.T("表情メッシュと目メッシュが別々"));
            _uiEyeSeparateToggle.RegisterValueChangedCallback(evt =>
            {
                _eyeSmrSeparate = evt.newValue;
                RefreshEyeLookBakeShapeNamesCache();
                RefreshToolkitUI();
            });
            _uiFaceDetail.Add(_uiEyeSeparateToggle);

            _uiEyeSmrRow = new VisualElement();
            _uiEyeSmrRow.AddToClassList("nested-panel");
            _uiEyeSmrField = new DropdownField("Eye Mesh");
            _uiEyeSmrField.RegisterValueChangedCallback(evt =>
            {
                int idx = Array.IndexOf(_smrPaths, evt.newValue);
                if (idx >= 0) _eyeSmrIndex = idx;
                RefreshEyeLookBakeShapeNamesCache();
                RefreshToolkitUI();
            });
            _uiEyeSmrRow.Add(_uiEyeSmrField);
            _uiFaceDetail.Add(_uiEyeSmrRow);
            _uiFaceAvatarGate.Add(_uiFaceDetail);
            card.Add(_uiFaceAvatarGate);

            MakeCardCollapsible(card, "FACE", defaultExpanded: false);
            _uiBasicPage.Add(card);
        }

        private void BuildToolkitSquintCard()
        {
            var card = MakeCard("SMILE EYES", ArkitFTLoc.T("にっこり目"), "accent-primary");
            _uiSquintCard = card;
            card.Add(MakeHint(
                ArkitFTLoc.T("任意のシェイプキーを「にっこり目」として指定できます。\n" +
                "未指定の場合はARKitのeyeSquintLeft・eyeSquintRightが設定されます。"),
                "soft"));

            _uiSquintNoAvatarHint = MakeHint(ArkitFTLoc.T("アバターを選択するとShape Keyを指定できます。"), "soft");
            card.Add(_uiSquintNoAvatarHint);

            _uiSquintAvatarGate = new VisualElement();

            _uiSquintSearchField = new TextField(ArkitFTLoc.T("検索"));
            StyleAsCompactSearchField(_uiSquintSearchField);
            _uiSquintSearchField.RegisterValueChangedCallback(evt =>
            {
                _squintSearchQuery = evt.newValue ?? "";
                RebuildSquintList();
            });
            _uiSquintAvatarGate.Add(_uiSquintSearchField);

            _uiSquintList = new VisualElement();
            _uiSquintList.AddToClassList("selection-list");
            _uiSquintAvatarGate.Add(_uiSquintList);

            _uiSquintAddButton = new Button(() =>
            {
                var filtered = GetFilteredShapeIndices();
                if (filtered.Count > 0)
                {
                    _squintShapeIndices.Add(filtered[0]);
                    RebuildSquintList();
                    RefreshReadyToInstallChips();
                    RefreshCardAccents(_avatarPrefab != null);
                }
            }) { text = ArkitFTLoc.T("＋ Shape Keyを追加") };
            _uiSquintAddButton.AddToClassList("add-button");
            _uiSquintAvatarGate.Add(_uiSquintAddButton);
            card.Add(_uiSquintAvatarGate);

            MakeCardCollapsible(card, "SMILE EYES", defaultExpanded: false);
            _uiBasicPage.Add(card);
        }

        private void BuildToolkitGestureCard()
        {
            var card = MakeCard("GESTURE", ArkitFTLoc.T("ジェスチャー/メニュー表情の抑制"), "accent-primary");
            _uiGestureCard = card;
            card.Add(MakeHint(
                ArkitFTLoc.T("フェイストラッキング実行中は、ジェスチャーやExpressionMenuで動く\n" +
                "表情が動かないように設定できます。混ざってほしくないFXレイヤーをすべて選択してください。"),
                "soft"));

            _uiGestureNoAvatarHint = MakeHint(ArkitFTLoc.T("アバターを選択するとFXレイヤーを指定できます。"), "soft");
            card.Add(_uiGestureNoAvatarHint);

            _uiGestureAvatarGate = new VisualElement();

            _uiGestureSearchField = new TextField(ArkitFTLoc.T("検索"));
            StyleAsCompactSearchField(_uiGestureSearchField);
            _uiGestureSearchField.RegisterValueChangedCallback(evt =>
            {
                _gestureSearchQuery = evt.newValue ?? "";
                RebuildGestureList();
            });
            _uiGestureAvatarGate.Add(_uiGestureSearchField);

            _uiGestureList = new VisualElement();
            _uiGestureList.AddToClassList("selection-list");
            _uiGestureAvatarGate.Add(_uiGestureList);

            _uiGestureAddButton = new Button(() =>
            {
                var filtered = GetFilteredGestureIndices();
                if (filtered.Count > 0)
                {
                    _gestureLayerIndices.Add(filtered[0]);
                    // 目トリガーには追加しない(既定は口)
                    RebuildGestureList();
                    RefreshReadyToInstallChips();
                    RefreshCardAccents(_avatarPrefab != null);
                }
            }) { text = ArkitFTLoc.T("＋ Layerを追加") };
            _uiGestureAddButton.AddToClassList("add-button");
            _uiGestureAvatarGate.Add(_uiGestureAddButton);

            // 抑制レイヤーが1つも登録されていない段階では、この設定は意味を持たないため
            // まとめて非表示にできるコンテナに入れる(RebuildGestureListで表示制御)。
            _uiGestureSuppressDetail = new VisualElement();
            _uiGestureSuppressDetail.Add(MakeHint(
                ArkitFTLoc.T("選択したレイヤーは、MouthTracking(音声リップシンク相当)が有効な間、Weightを0にして\n" +
                "抑制します。目のTrackingControl競合はweight抑制では解決できないため、\n" +
                "Stable Eye Mode（AvatarDescriptor Eye Lookを無効化）で対応してください。\n" +
                "アイトラッキングのみを有効にすることで、ジェスチャー/メニュー表情とアイトラッキングを\n" +
                "併用できます。（ただし、目を閉じる表情とアイトラッキングのまばたきは重なって\n" +
                "破綻してしまいますのでご注意ください。写真撮影での活用をおすすめします。）"),
                "soft"));

            _uiGestureAvatarGate.Add(_uiGestureSuppressDetail);
            card.Add(_uiGestureAvatarGate);

            MakeCardCollapsible(card, "GESTURE", defaultExpanded: false);
            _uiBasicPage.Add(card);
        }

        private void BuildToolkitMouthCard()
        {
            var card = MakeCard("MOUTH", ArkitFTLoc.T("音声リップシンク形状の抑制"), "accent-primary");
            _uiMouthCard = card;

            _uiVisemeConfigWarningHint = MakeHint("", "warning");
            _uiVisemeConfigWarningHint.style.display = DisplayStyle.None;
            card.Add(_uiVisemeConfigWarningHint);

            _uiVisemeToggle = new Toggle(ArkitFTLoc.T("Viseme打消しシェイプキーを生成"));
            _uiVisemeToggle.RegisterValueChangedCallback(evt =>
            {
                _generateVisemeCompensation = evt.newValue;
                RefreshToolkitUI();
            });
            card.Add(_uiVisemeToggle);

            // OFF時は「機能を使っていない」ことが一目でわかるよう、詳細部分ごと非表示にする。
            _uiVisemeDetail = new VisualElement();

            var sliderRow = MakeFloatSlider(
                "Viseme Strength", 0.3f, 1f,
                out _uiVisemeSlider, out _uiVisemeValue,
                value => _visemeScale = value);
            _uiVisemeDetail.Add(sliderRow);

            _uiVisemeDetail.Add(MakeHint(ArkitFTLoc.T("発話中のVisemeとFaceTrackingの口形状が重なるのを補正します。"), "soft"));
            card.Add(_uiVisemeDetail);

            _uiTrackingPage.Add(card);
        }

        private void BuildToolkitEyesCard()
        {
            var card = MakeCard("EYES", ArkitFTLoc.T("目線とまばたきの制御"), "accent-primary");
            _uiEyesCard = card;

            _uiEyeLookConfigWarningHint = MakeHint("", "warning");
            _uiEyeLookConfigWarningHint.style.display = DisplayStyle.None;
            card.Add(_uiEyeLookConfigWarningHint);

            _uiEyeLookToggle = new Toggle(ArkitFTLoc.T("アイトラッキング用シェイプキーを生成"));
            _uiEyeLookToggle.tooltip =
                ArkitFTLoc.T("フェイストラッキングで動く目線のシェイプキーを、アバターのEyeLook設定から自動生成します。\n" +
                "EyeLook Strengthを大きくするとわずかな動きにも敏感に反応します。\n" +
                "目の可動範囲そのものを調整したい場合はEyeLook設定を調整してください。");
            _uiEyeLookToggle.RegisterValueChangedCallback(evt =>
            {
                _generateEyeLookShapes = evt.newValue;
                RefreshToolkitUI();
            });
            card.Add(_uiEyeLookToggle);

            // OFF時は「EyeLookシェイプキーを生成していない」ことが一目でわかるよう、
            // 強度スライダー・Constraint設定・Eye Look競合対策セクションをまとめて非表示にする。
            _uiEyeLookDetail = new VisualElement();

            var eyeSliderRow = MakeFloatSlider(
                "EyeLook Strength", 0.5f, 2f,
                out _uiEyeLookSlider, out _uiEyeLookValue,
                value => _eyeLookIntensity = value);
            eyeSliderRow.name = "eye-strength-row";
            _uiEyeLookDetail.Add(eyeSliderRow);

            _uiEyeLookDetail.Add(MakeHint(
                ArkitFTLoc.T("フェイストラッキングで動く目線のシェイプキーを、アバターのEyeLook設定から自動生成します。\n" +
                "EyeLook Strengthを大きくするとわずかな動きにも敏感に反応します。\n" +
                "目の可動範囲そのものを調整したい場合はEyeLook設定を調整してください。"),
                "soft"));

            // Constraint方式の目線制御を使っているアバター向けの設定。
            // 以前は「Advanced Eye Bone Settingsを開く」→「Constraint経由をON」の
            // 2段階だったが、1つのトグルに統合して手順を短縮する。
            _uiEyeConstraintToggle = new Toggle(ArkitFTLoc.T("アバターの目線制御がConstraint方式"));
            _uiEyeConstraintToggle.RegisterValueChangedCallback(evt =>
            {
                _eyeUsesConstraint = evt.newValue;
                RefreshToolkitUI();
            });
            _uiEyeLookDetail.Add(_uiEyeConstraintToggle);

            _uiEyeConstraintFields = new VisualElement();
            _uiEyeConstraintFields.AddToClassList("nested-panel");
            _uiEyeConstraintFields.Add(MakeHint(
                ArkitFTLoc.T("アバターの目線制御がConstraint方式の場合、実際に目メッシュにウエイトが乗っている" +
                "ボーンを指定してください。"),
                "soft"));

            _uiLeftEyeField = new ObjectField("Left Eye")
            {
                objectType = typeof(Transform),
                allowSceneObjects = true
            };
            _uiLeftEyeField.RegisterValueChangedCallback(evt =>
                _leftEyeConstraintTarget = evt.newValue as Transform);

            _uiRightEyeField = new ObjectField("Right Eye")
            {
                objectType = typeof(Transform),
                allowSceneObjects = true
            };
            _uiRightEyeField.RegisterValueChangedCallback(evt =>
                _rightEyeConstraintTarget = evt.newValue as Transform);

            _uiEyeConstraintFields.Add(_uiLeftEyeField);
            _uiEyeConstraintFields.Add(_uiRightEyeField);
            _uiEyeLookDetail.Add(_uiEyeConstraintFields);

            // 上の「アバターの目線制御がConstraint方式」欄と詰まって見えないよう、
            // 区切り線で明確にセクションを分ける。
            var bakeDivider = new VisualElement();
            bakeDivider.AddToClassList("divider");
            _uiEyeLookDetail.Add(bakeDivider);

            // 目線シェイプキー生成時に、あらかじめ有効にしておく追加シェイプキー。
            // 目のハイライト・瞳孔等のサブメッシュを手前に移動させるシェイプキーを持つ
            // アバターで、そのシェイプキーを有効にした状態を基準にベイクしないと、
            // サブメッシュの回転移動量が不足して眼球メッシュを貫通してしまうことがあるための対策。
            // 該当するアバターは一部のため、既定OFFのオプション機能として提供する。
            _uiEyeLookBakeToggle = new Toggle(ArkitFTLoc.T("目線シェイプキー生成時に追加シェイプキーを有効化"));
            _uiEyeLookBakeToggle.RegisterValueChangedCallback(evt =>
            {
                _useEyeLookBakeShapes = evt.newValue;
                RefreshToolkitUI();
            });
            _uiEyeLookDetail.Add(_uiEyeLookBakeToggle);

            // OFF時は「この機能を使っていない」ことが一目でわかるよう、詳細部分ごと非表示にする。
            _uiEyeLookBakeDetail = new VisualElement();

            _uiEyeLookBakeSearchField = new TextField(ArkitFTLoc.T("検索"));
            StyleAsCompactSearchField(_uiEyeLookBakeSearchField);
            _uiEyeLookBakeSearchField.RegisterValueChangedCallback(evt =>
            {
                _eyeLookBakeSearchQuery = evt.newValue ?? "";
                RebuildEyeLookBakeShapeList();
            });
            _uiEyeLookBakeDetail.Add(_uiEyeLookBakeSearchField);

            _uiEyeLookBakeShapeList = new VisualElement();
            _uiEyeLookBakeShapeList.AddToClassList("selection-list");
            _uiEyeLookBakeDetail.Add(_uiEyeLookBakeShapeList);

            _uiEyeLookBakeAddButton = new Button(() =>
            {
                var filtered = GetFilteredEyeLookBakeShapeIndices();
                if (filtered.Count > 0)
                {
                    _eyeLookBakeShapeIndices.Add(filtered[0]);
                    RebuildEyeLookBakeShapeList();
                    RefreshReadyToInstallChips();
                    RefreshCardAccents(_avatarPrefab != null);
                }
            }) { text = ArkitFTLoc.T("＋ Shape Keyを追加") };
            _uiEyeLookBakeAddButton.AddToClassList("add-button");
            _uiEyeLookBakeDetail.Add(_uiEyeLookBakeAddButton);

            _uiEyeLookBakeDetail.Add(MakeHint(
                ArkitFTLoc.T("目のハイライト・瞳孔等、サブメッシュを手前に移動させるシェイプキーを持つ\n" +
                "アバターの場合、ここでそのシェイプキーを指定してください。目線シェイプキー生成\n" +
                "(ボーン回転のベイク)の間だけ、指定したシェイプキーの重みを100にした状態で\n" +
                "計算します(生成後、元の重みに戻します)。指定しない場合、サブメッシュが奥にある\n" +
                "状態を基準に計算されるため、実際に手前へ出した状態で目線を動かすと、回転による\n" +
                "移動量が不足して眼球メッシュを貫通することがあります。"),
                "soft"));

            _uiEyeLookDetail.Add(_uiEyeLookBakeDetail);

            var divider = new VisualElement();
            divider.AddToClassList("divider");
            _uiEyeLookDetail.Add(divider);

            var modeTitle = new Label(ArkitFTLoc.T("Eye Look競合対策"));
            modeTitle.AddToClassList("subheading");
            _uiEyeLookDetail.Add(modeTitle);

            _uiEyeConflictBox = new VisualElement();
            _uiEyeConflictBox.AddToClassList("warning-card");
            _uiEyeConflictText = new Label();
            _uiEyeConflictText.AddToClassList("warning-text");
            _uiEyeConflictBox.Add(_uiEyeConflictText);
            _uiEyeLookDetail.Add(_uiEyeConflictBox);

            var modeGrid = new VisualElement();
            modeGrid.AddToClassList("mode-grid");

            // モード選択はラジオボタンではなく、カード自体のクリックで切り替える。
            // 選択中のモードは枠線(mode-card-selected)で示し、「RECOMMENDED」バッジは
            // あくまで推奨の目印であって選択状態を表すものではないことを明確にする。
            _uiEyeCompatCard = new VisualElement();
            _uiEyeCompatCard.AddToClassList("mode-card");
            var compatName = new Label("Compatibility");
            compatName.AddToClassList("mode-name");
            _uiEyeCompatCard.Add(compatName);
            _uiEyeCompatCard.Add(MakeModeDescription(
                ArkitFTLoc.T("標準Eye Lookを維持"),
                ArkitFTLoc.T("FT OFF時にはVRChat標準の目線へ戻ります。アバターによってはFT中に競合する場合があります。")));
            _uiEyeCompatCard.RegisterCallback<MouseDownEvent>(_ =>
            {
                _disableNativeEyeLook = false;
                RefreshToolkitUI();
            });

            _uiEyeStableCard = new VisualElement();
            _uiEyeStableCard.AddToClassList("mode-card");
            var stableName = new Label("Stable");
            stableName.AddToClassList("mode-name");
            _uiEyeStableCard.Add(stableName);
            _uiEyeStableCard.Add(MakeModeDescription(
                ArkitFTLoc.T("AvatarDescriptor Eye Lookを無効化"),
                ArkitFTLoc.T("VRChat標準Eye Lookとの競合を根本的に回避します。\nFT OFF時はVRChat標準の自動目線および自動まばたきが動作しません。")));
            _uiEyeStableCard.RegisterCallback<MouseDownEvent>(_ =>
            {
                _disableNativeEyeLook = true;
                RefreshToolkitUI();
            });

            modeGrid.Add(_uiEyeCompatCard);
            modeGrid.Add(_uiEyeStableCard);
            _uiEyeLookDetail.Add(modeGrid);

            card.Add(_uiEyeLookDetail);

            _uiTrackingPage.Add(card);
        }

        private void BuildToolkitBlinkModeCard()
        {
            var card = MakeCard("EYES", ArkitFTLoc.T("左右まばたきの安定化"), "accent-primary");
            _uiBlinkModeCard = card;

            // チェックボックスは「同期(Blink2D)を有効にするか」を表す(既定ON)。
            // OFFにすると左右のまばたきが完全に独立する(Blink Simple 1D)。
            _uiBlinkSimple1DToggle = new Toggle(ArkitFTLoc.T("左右の目の開きを一定の閾値で揃える"));
            _uiBlinkSimple1DToggle.tooltip =
                ArkitFTLoc.T("ON(既定): Blink2D。v2/EyeLidLeft・Rightを2D Freeformでブレンドし、左右がある程度連動します。\n" +
                "OFF: Blink Simple 1D。v2/EyeLidLeft・Rightをそれぞれ独立したSimple1Dで駆動し、片目だけの\n" +
                "ウィンクにも対応できます。");
            _uiBlinkSimple1DToggle.RegisterValueChangedCallback(evt =>
            {
                _blinkControlMode = evt.newValue ? BlinkControlMode.TwoD : BlinkControlMode.OneD;
                RefreshToolkitUI();
            });
            card.Add(_uiBlinkSimple1DToggle);

            _uiBlinkModeHint = MakeHint(
                ArkitFTLoc.T("左右の目の開きが不揃いになるのを避けるため、一定の閾値で同期させます。\n" +
                "綺麗にウインクするには、反対の目が一定以上開いている必要があります。"),
                "soft");
            card.Add(_uiBlinkModeHint);

            _uiTrackingPage.Add(card);
        }

        private void BuildToolkitAssistCard()
        {
            var card = MakeCard("ASSIST", ArkitFTLoc.T("眉アシスト"), "accent-primary");
            _uiAssistCard = card;

            _uiBrowToggle = new Toggle(ArkitFTLoc.T("まばたきに連動して眉を動かすシェイプキーを生成"));
            _uiBrowToggle.tooltip =
                ArkitFTLoc.T("眉のトラッキングに非対応のデバイス向けに、まばたきに連動して眉を動かす補助シェイプキーを追加します。\n" +
                "動きすぎる場合はBrow Strengthを小さくすると弱められます。");
            _uiBrowToggle.RegisterValueChangedCallback(evt =>
            {
                _generateBrowAssistShapes = evt.newValue;
                RefreshToolkitUI();
            });
            card.Add(_uiBrowToggle);

            // OFF時は機能を使っていないことが一目でわかるよう、詳細部分ごと非表示にする。
            _uiBrowDetail = new VisualElement();
            var browSliderRow = MakeFloatSlider(
                "Brow Strength", 0f, 1f,
                out _uiBrowSlider, out _uiBrowValue,
                value => _browAssistIntensity = value);
            browSliderRow.name = "brow-strength-row";
            _uiBrowDetail.Add(browSliderRow);
            _uiBrowDetail.Add(MakeHint(
                ArkitFTLoc.T("眉のトラッキングに非対応のデバイス向けに、まばたきに連動して眉を動かす補助シェイプキーを追加します。\n" +
                "動きすぎる場合はBrow Strengthを小さくすると弱められます。"),
                "soft"));
            _uiBrowDetail.Add(MakeHint(
                ArkitFTLoc.T("複製元となる標準ARKitシェイプキー(browInnerUp等)の検索には、\n" +
                "FACEカードの「UEのシェイプキーを流用※試験的」設定がそのまま適用されます。"),
                "soft"));
            card.Add(_uiBrowDetail);

            _uiTrackingPage.Add(card);
        }

        private void BuildToolkitTongueCard()
        {
            var card = MakeCard("ASSIST", ArkitFTLoc.T("舌アシスト"), "accent-primary");
            _uiTongueCard = card;
            // Foldoutが折りたたみ状態でもカード下端で見切れる不具合の対策。settings-cardクラスが
            // 角丸のためoverflow:hiddenを持っている可能性があるため、このカードだけ明示的に
            // overflow:Visibleへ上書きする(スペーサー挿入では改善しなかったため、こちらを試す)。
            card.style.overflow = Overflow.Visible;

            _uiTongueToggle = new Toggle(ArkitFTLoc.T("舌が下唇を貫通するのを防ぐシェイプキーを生成"));
            _uiTongueToggle.tooltip =
                ArkitFTLoc.T("tongueOutの動きから舌の頂点を自動検出し(mouthPucker等の唇系シェイプキーが\n" +
                "動く頂点は除外)、持ち上げた形状とtongueOut本体を0.5ずつミックスしたピーク形状を\n" +
                "生成します。Install時、tongueOutが0%→100%へ変化する遷移の50%地点(唇を越える\n" +
                "タイミング)でこのピーク形状が最大になるよう、標準の駆動カーブが組み替えられます\n" +
                "(0%・100%は従来通り、50%だけ持ち上げが最大になる自然な変化になります)。");
            _uiTongueToggle.RegisterValueChangedCallback(evt =>
            {
                _generateTongueAssistShapes = evt.newValue;
                RefreshToolkitUI();
            });
            card.Add(_uiTongueToggle);

            // OFF時は機能を使っていないことが一目でわかるよう、詳細部分ごと非表示にする。
            _uiTongueDetail = new VisualElement();

            _uiTongueDetail.Add(MakeHint(
                ArkitFTLoc.T("ARKitのシェイプキーなどから舌の頂点を自動検出します。各設定項目について、\n" +
                "詳しくはツールチップをご確認ください。"),
                "soft"));

            var tongueLiftSourceChoices = new List<TongueLiftSource>
            { TongueLiftSource.ExistingShapeKey, TongueLiftSource.AutoDetect };
            _uiTongueLiftSourceField = new PopupField<TongueLiftSource>(
                ArkitFTLoc.T("舌を持ち上げる形状のソース"), tongueLiftSourceChoices, _tongueLiftSource,
                formatSelectedValueCallback: FormatTongueLiftSourceLabel,
                formatListItemCallback: FormatTongueLiftSourceLabel);
            _uiTongueLiftSourceField.tooltip =
                ArkitFTLoc.T("「持ち上げ」形状の作り方。\n" +
                "既存シェイプキー(既定・推奨): アバターが既に持っている舌持ち上げ用シェイプキーを\n" +
                "指定して流用します。頂点検出を行わないため、歯やまつげの誤検出を心配する必要が\n" +
                "無く、アバター側で作り込まれた自然な形状をそのまま活かせます。\n" +
                "自動検知: tongueOut等の動きから舌頂点を自動検出し、指定軸方向へ一律に持ち上げます\n" +
                "(既存の持ち上げシェイプキーを持たないアバター向け、従来方式)。");
            _uiTongueLiftSourceField.AddToClassList("grow-field");
            // 親要素(_uiTongueDetail)がCSSクラス無しの素のVisualElementのため、"grow-field"クラス
            // だけでは幅・高さが確保されない場合がある(以前_uiTongueExistingShapeFieldで実機にて
            // 幅0・高さ0になり見えなくなる不具合を確認済み)。クラスに頼らず、インラインスタイルで
            // 明示的に幅・高さを確保する。
            _uiTongueLiftSourceField.style.flexGrow = 1;
            _uiTongueLiftSourceField.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            _uiTongueLiftSourceField.style.minHeight = 20;
            _uiTongueLiftSourceField.RegisterValueChangedCallback(evt =>
            {
                _tongueLiftSource = evt.newValue;
                RefreshToolkitUI();
                UpdateTonguePreview();
            });
            _uiTongueDetail.Add(_uiTongueLiftSourceField);

            // ── Existing Shape Key モード用UI ──────────────────────
            _uiTongueExistingShapeDetail = new VisualElement();

            _uiTongueExistingShapeSearchField = new TextField(ArkitFTLoc.T("検索"));
            StyleAsCompactSearchField(_uiTongueExistingShapeSearchField);
            _uiTongueExistingShapeSearchField.RegisterValueChangedCallback(evt =>
            {
                _tongueExistingLiftSearchQuery = evt.newValue ?? "";
                RebuildTongueExistingShapePopup();
            });
            _uiTongueExistingShapeDetail.Add(_uiTongueExistingShapeSearchField);

            _uiTongueExistingShapePopup = new VisualElement();
            // にっこり目・ジェスチャー抑制と同じ"selection-row"の見た目に揃える。
            _uiTongueExistingShapePopup.AddToClassList("selection-row");
            // "selection-row"クラスだけでは幅が確保されない場合がある(実機で幅0・高さ0になる
            // 不具合を確認)。クラスに頼らず、インラインスタイルでも明示的に幅を確保しておく。
            _uiTongueExistingShapePopup.style.flexDirection = FlexDirection.Row;
            _uiTongueExistingShapePopup.style.width = new StyleLength(new Length(100, LengthUnit.Percent));

            _uiTongueExistingShapeField = new PopupField<int>(
                ArkitFTLoc.T("Shape"), new List<int> { -1 }, -1,
                formatSelectedValueCallback: i => i < 0
                    ? ArkitFTLoc.T("(未選択)")
                    : (i < _shapeNames.Length ? _shapeNames[i] : "?"),
                formatListItemCallback: i => i < 0
                    ? ArkitFTLoc.T("(未選択)")
                    : (i < _shapeNames.Length ? _shapeNames[i] : "?"));
            _uiTongueExistingShapeField.tooltip =
                ArkitFTLoc.T("アバターが既に持っている、舌を持ち上げるシェイプキーを一覧から選択してください。\n" +
                "このシェイプキーの頂点差分を、下のWeightで指定した強度でそのまま流用します。");
            _uiTongueExistingShapeField.AddToClassList("grow-field");
            _uiTongueExistingShapeField.style.flexGrow = 1;
            _uiTongueExistingShapeField.style.minHeight = 20;
            _uiTongueExistingShapeField.RegisterValueChangedCallback(evt =>
            {
                _tongueExistingLiftShapeName = evt.newValue >= 0 && evt.newValue < _shapeNames.Length
                    ? _shapeNames[evt.newValue] : "";
            });
            _uiTongueExistingShapePopup.Add(_uiTongueExistingShapeField);
            _uiTongueExistingShapeDetail.Add(_uiTongueExistingShapePopup);

            _uiTongueExistingShapeHint = MakeHint("", "soft");
            _uiTongueExistingShapeHint.style.display = DisplayStyle.None;
            _uiTongueExistingShapeDetail.Add(_uiTongueExistingShapeHint);

            var tongueExistingWeightRow = MakeFloatSlider(
                "Weight (%)", 0f, 100f,
                out _uiTongueExistingShapeWeightSlider, out _uiTongueExistingShapeWeightValue,
                value => { _tongueExistingLiftShapeWeight = value; });
            tongueExistingWeightRow.name = "tongue-existing-weight-row";
            _uiTongueExistingShapeDetail.Add(tongueExistingWeightRow);

            _uiTongueExistingShapeDetail.Add(MakeHint(
                ArkitFTLoc.T("指定したシェイプキーの頂点移動量をそのまま(Weight%でスケールして)使います。\n" +
                "頂点の自動検出は行わないため、歯やまつげ等の誤検出の心配がありません。\n" +
                "この持ち上げ形状は、v2/TongueOutが0%→100%へ遷移する間、50%地点(唇を越える\n" +
                "タイミング)で最大になるよう自動的に組み込まれます(0%・100%では持ち上げ無し)。\n" +
                "リモートでの同期時にv2/TongueOutが粗く量子化される運用でも安定するよう、\n" +
                "ピーク位置は50%に固定しています。"),
                "soft"));

            _uiTongueDetail.Add(_uiTongueExistingShapeDetail);

            // ── Auto Detect モード用UI ─────────────────────────────
            _uiTongueAutoDetectDetail = new VisualElement();

            var tongueCountAndPreviewRow = new VisualElement();
            tongueCountAndPreviewRow.style.flexDirection = FlexDirection.Row;
            tongueCountAndPreviewRow.style.alignItems = Align.Center;

            var tonguePreviewToggleContainer = new VisualElement();
            tonguePreviewToggleContainer.style.width = new StyleLength(new Length(50, LengthUnit.Percent));
            tonguePreviewToggleContainer.style.flexDirection = FlexDirection.Row;

            _uiTonguePreviewToggle = new Toggle(ArkitFTLoc.T("Scene Viewにプレビュー表示"));
            _uiTonguePreviewToggle.tooltip =
                ArkitFTLoc.T("口内にあり通常は隠れて見えない検出頂点を、顔メッシュ越しに透過表示します。\n" +
                "白い球=移動前、赤い球=移動後の位置です。FACEカードでFace SMRを選択している必要があります。\n" +
                "※Scene上に配置されたモデルでのみ機能します。Project内のPrefabアセットを直接指定した\n" +
                "場合は、Scene上に表示されないため、この機能は動作しません。");
            _uiTonguePreviewToggle.RegisterValueChangedCallback(evt =>
            {
                _showTonguePreview = evt.newValue;
                UpdateTonguePreview();
            });
            tonguePreviewToggleContainer.Add(_uiTonguePreviewToggle);
            tongueCountAndPreviewRow.Add(tonguePreviewToggleContainer);

            _uiTonguePreviewAssetWarningHint = MakeHint("", "warning");
            _uiTonguePreviewAssetWarningHint.style.display = DisplayStyle.None;
            _uiTongueAutoDetectDetail.Add(_uiTonguePreviewAssetWarningHint);

            // 「検出頂点数」は右端ではなく、行の中央(50%位置)からちょうど始まるように配置する
            // (トグル側も同じ50%幅のコンテナに収めることで、間が開きすぎて見えるのを避ける)。
            _uiTongueDetectedCountLabel = new Label();
            _uiTongueDetectedCountLabel.AddToClassList("hint-text");
            _uiTongueDetectedCountLabel.style.width = new StyleLength(new Length(50, LengthUnit.Percent));
            _uiTongueDetectedCountLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            tongueCountAndPreviewRow.Add(_uiTongueDetectedCountLabel);

            _uiTongueAutoDetectDetail.Add(tongueCountAndPreviewRow);

            // 検出のしきい値・移動方向・単位変換・プレビュー等は、既存Profileを使うだけの
            // ユーザーにはほとんど関係が無く、常時表示すると項目過多でUIが煩雑になる。また、
            // 「持ち上げ量」も誤って触ってしまう事故を防ぐため、ここにまとめて含める。
            // 「診断・技術情報」と同じ折りたたみ(Foldout・既定で閉)にまとめ、実際に検出結果を
            // 調整したい場合だけ開いてもらう構成にする。
            var tongueAdvancedFoldout = new Foldout
            {
                text = ArkitFTLoc.T("詳細設定"),
                value = false
            };
            tongueAdvancedFoldout.AddToClassList("sub-foldout");
            // 「sub-foldout」クラスがヘッダー行の高さを詰めすぎており、見出し文字列自体が
            // 縦方向に見切れる不具合が起きていたため、Foldout内部のトグル(見出し行)を
            // 直接取得し、高さを明示的に確保する。
            var tongueFoldoutToggle = tongueAdvancedFoldout.Q<Toggle>(className: "unity-foldout__toggle");
            if (tongueFoldoutToggle != null)
            {
                tongueFoldoutToggle.style.minHeight = 20;
                tongueFoldoutToggle.style.height = new StyleLength(StyleKeyword.Auto);
            }
            tongueAdvancedFoldout.style.minHeight = new StyleLength(StyleKeyword.Auto);
            tongueAdvancedFoldout.tooltip =
                ArkitFTLoc.T("検出のしきい値や単位変換など、既存Profileを使うだけであれば通常は\n" +
                "触る必要のない設定です。検出頂点数が0のまま等、うまく検出できない場合のみ\n" +
                "開いて調整してください。");

            var tongueLiftRow = MakeFloatSlider(
                ArkitFTLoc.T("持ち上げ量"), 0f, 0.05f,
                out _uiTongueMoveSlider, out _uiTongueMoveValue,
                value =>
                {
                    _tongueMoveAmount = value;
                    UpdateTonguePreview();
                });
            tongueLiftRow.name = "tongue-move-amount-row";
            tongueLiftRow.tooltip =
                ArkitFTLoc.T("検出した舌頂点を持ち上げる距離(メッシュのローカル空間、単位はアバターの\n" +
                "スケールに依存)。値を大きくするほど舌が下唇をしっかり回避しますが、大きすぎると\n" +
                "不自然に持ち上がって見えることがあります。Scene Viewのプレビューを見ながら\n" +
                "少しずつ調整してください。");
            tongueAdvancedFoldout.Add(tongueLiftRow);

            _uiTongueLiftAxisField = new EnumField(ArkitFTLoc.T("移動方向"), _tongueLiftAxis);
            // プルダウンがウィンドウ幅いっぱいに広がって目立ちすぎるため、幅を抑えて控えめにする。
            _uiTongueLiftAxisField.style.width = new StyleLength(new Length(50, LengthUnit.Percent));
            _uiTongueLiftAxisField.tooltip =
                ArkitFTLoc.T("検出頂点を持ち上げる方向(ローカル空間)。既定はY+(上)ですが、メッシュによっては" +
                "「上」がY+軸ではなくZ軸(奥行方向)等になっている場合があります。\n" +
                "Tongue Up Amountを上げたときに舌が奥や横に動いてしまう場合は、Scene Viewの" +
                "プレビュー(白→赤の球)を見ながら正しい方向に切り替えてください。");
            _uiTongueLiftAxisField.RegisterValueChangedCallback(evt =>
            {
                _tongueLiftAxis = (TongueLiftAxis)evt.newValue;
                UpdateTonguePreview();
            });
            tongueAdvancedFoldout.Add(_uiTongueLiftAxisField);

            var tongueDetectThresholdRow = MakeFloatSlider(
                ArkitFTLoc.T("検出しきい値 (mm)"), 0f, 5f,
                out _uiTongueDetectThresholdSlider, out _uiTongueDetectThresholdValue,
                value =>
                {
                    _tongueDetectThresholdMm = value;
                    UpdateTonguePreview();
                });
            tongueDetectThresholdRow.name = "tongue-detect-threshold-row";
            tongueDetectThresholdRow.tooltip =
                ArkitFTLoc.T("舌のシェイプキーで、これ以上動いた頂点を「舌の一部」として検出するための\n" +
                "しきい値(実寸mm)。値を下げるほど検出される頂点が増えます。検出頂点数が\n" +
                "0のままの場合は下げてみてください。");
            tongueAdvancedFoldout.Add(tongueDetectThresholdRow);

            var tongueLipExcludeThresholdRow = MakeFloatSlider(
                ArkitFTLoc.T("唇除外しきい値 (mm)"), 0f, 5f,
                out _uiTongueLipExcludeThresholdSlider, out _uiTongueLipExcludeThresholdValue,
                value =>
                {
                    _tongueLipExcludeThresholdMm = value;
                    UpdateTonguePreview();
                });
            tongueLipExcludeThresholdRow.name = "tongue-lip-exclude-threshold-row";
            tongueLipExcludeThresholdRow.tooltip =
                ArkitFTLoc.T("唇・頬・歯等、舌ではない部位の頂点を誤って検出しないよう除外するための\n" +
                "しきい値(実寸mm)。ARKit標準の'cheekPuff'を主要な除外シグナルとして使い、\n" +
                "mouthRollUpper/Lower等と合わせて判定します(mouthPucker・mouthFunnel・\n" +
                "mouthCloseは口を閉じる/すぼめる動きで舌も一緒に動かすため除外対象から\n" +
                "外しています)。値を上げるほど除外が弱まり、残る頂点が増えます。\n" +
                "舌の頂点が消えすぎている(検出数が極端に少ない)場合は上げてみてください。");
            tongueAdvancedFoldout.Add(tongueLipExcludeThresholdRow);

            _uiTongueExcludeTeethFromPrimaryToggle = new Toggle(ArkitFTLoc.T("tongueOut自体にも歯除外を適用"));
            _uiTongueExcludeTeethFromPrimaryToggle.tooltip =
                ArkitFTLoc.T("OFF(既定): tongueOut自体は歯除外の対象外にします。\n" +
                "ON: tongueOut自体が動かす頂点でも、歯シェイプキーと重なるものは除外します\n" +
                "(tongueOutが歯を動かす正当な理由は無いはずという前提で、歯の誤検出を防ぎます)。\n" +
                "アバターによっては、歯とキーワード一致するシェイプキーが舌の可動域と大きく\n" +
                "重なっており、ONにすると本来検出されるべき舌の頂点まで巻き込んで消えてしまい、\n" +
                "検出頂点数が0になることがあります(Consoleに「歯除外が原因の可能性が高いです」\n" +
                "という警告が出た場合はOFFのままにしてください)。");
            _uiTongueExcludeTeethFromPrimaryToggle.RegisterValueChangedCallback(evt =>
            {
                _tongueExcludeTeethFromPrimary = evt.newValue;
                UpdateTonguePreview();
            });
            tongueAdvancedFoldout.Add(_uiTongueExcludeTeethFromPrimaryToggle);

            _uiTongueUnitOverrideToggle = new Toggle(ArkitFTLoc.T("単位スケール変換係数を手動指定"));
            _uiTongueUnitOverrideToggle.tooltip =
                ArkitFTLoc.T("mm(実寸)とメッシュ内部の座標単位は縮尺が異なるため、しきい値を比較する前に\n" +
                "単位を揃える変換が必要です。この変換に使う「1mmがメッシュ空間でいくつに\n" +
                "あたるか」という縮尺の値が単位スケール変換係数です。\n" +
                "OFF(既定): SMRのバウンディングボックス比から自動推定します。\n" +
                "ON: 自動推定を使わず、右の数値を単位スケール変換係数として強制的に使います。\n" +
                "自動推定がアバターによって大きく外れ、検出頂点数が0のまま/意図しない部位\n" +
                "(まつげ等)を拾ってしまう場合にONにしてください。");
            _uiTongueUnitOverrideToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    // ONにした瞬間、値が0以下(=自動推定扱い)のままだと矛盾するため、
                    // 妥当な初期値(1)を入れておく。
                    if (_tongueUnitOverride <= 0f) _tongueUnitOverride = 1f;
                }
                else
                {
                    _tongueUnitOverride = 0f; // 0以下=自動推定に戻す
                }
                RefreshToolkitUI();
                UpdateTonguePreview();
            });
            tongueAdvancedFoldout.Add(_uiTongueUnitOverrideToggle);

            _uiTongueUnitOverrideField = new FloatField(ArkitFTLoc.T("単位スケール変換係数"));
            _uiTongueUnitOverrideField.tooltip =
                ArkitFTLoc.T("1mm(実寸)が、メッシュ内部の座標単位でいくつにあたるかを表す縮尺の値です\n" +
                "(手動指定時のみ使用)。下の「実際に使われている係数」を見ながら、検出頂点数が\n" +
                "正しくなるよう調整してください。\n" +
                "目安: 値を大きくするとmm指定に対するメッシュ空間しきい値が小さくなり、検出が増えます。");
            _uiTongueUnitOverrideField.RegisterValueChangedCallback(evt =>
            {
                _tongueUnitOverride = Mathf.Max(evt.newValue, 0.0001f); // ONの間は0以下にしない
                UpdateTonguePreview();
            });
            tongueAdvancedFoldout.Add(_uiTongueUnitOverrideField);

            _uiTongueUnitInfoLabel = new Label();
            _uiTongueUnitInfoLabel.AddToClassList("hint-text");
            tongueAdvancedFoldout.Add(_uiTongueUnitInfoLabel);

            var tonguePreviewSizeRow = MakeFloatSlider(
                "Preview Point Size", 0.02f, 0.2f,
                out _uiTonguePreviewSizeSlider, out _uiTonguePreviewSizeValue,
                value =>
                {
                    _tonguePreviewPointScale = value;
                    SceneView.RepaintAll();
                });
            tonguePreviewSizeRow.name = "tongue-preview-size-row";
            tongueAdvancedFoldout.Add(tonguePreviewSizeRow);

            _uiTongueAutoDetectDetail.Add(tongueAdvancedFoldout);

            _uiTongueDetail.Add(_uiTongueAutoDetectDetail);

            card.Add(_uiTongueDetail);

            _uiTrackingPage.Add(card);
        }

        private void BuildToolkitBlinkEffectCard()
        {
            var card = MakeCard("EXTRA", ArkitFTLoc.T("おまけ機能"), "accent-primary");
            _uiBlinkEffectCard = card;

            _uiBlinkEffectToggle = new Toggle(ArkitFTLoc.T("まばたきエフェクトを追加"));
            _uiBlinkEffectToggle.tooltip =
                ArkitFTLoc.T("OFFにすると、まばたき時のおまけ演出そのものを含めずにインストールします。");
            _uiBlinkEffectToggle.RegisterValueChangedCallback(evt =>
            {
                _addBlinkEffect = evt.newValue;
                RefreshToolkitUI();
            });
            card.Add(_uiBlinkEffectToggle);

            // OFF時は機能を使っていないことが一目でわかるよう、説明文も含めて詳細部分ごと非表示にする。
            _uiBlinkEffectDetail = new VisualElement();

            _uiBlinkEffectDetail.Add(MakeHint(
                ArkitFTLoc.T("まばたきするたびに発動するアニメーションクリップを設定できます。\n" +
                "テンプレートFXに既定の演出は同梱されていません(アバターによってシェイプキーの構成が\n" +
                "異なり、汎用的な演出クリップを用意できないためです)。アバター固有のシェイプキーで\n" +
                "構成したエフェクト用のAnimationClipを別途ご用意のうえ、下欄で指定してください。"),
                "soft"));

            _uiBlinkEffectClipField = new ObjectField(ArkitFTLoc.T("エフェクトクリップ (任意)"))
            {
                objectType = typeof(AnimationClip),
                allowSceneObjects = false
            };
            _uiBlinkEffectClipField.tooltip =
                ArkitFTLoc.T("アバター固有のシェイプキーで構成したAnimationClipを指定してください。\n" +
                "未指定のままだと、まばたき時に何も再生されません。");
            _uiBlinkEffectClipField.RegisterValueChangedCallback(evt =>
            {
                _blinkEffectClip = evt.newValue as AnimationClip;
                RefreshToolkitUI();
            });
            _uiBlinkEffectDetail.Add(_uiBlinkEffectClipField);

            _uiBlinkEffectDetail.Add(MakeHint(
                ArkitFTLoc.T("まばたきで揺れる瞳ハイライトのアニメーションなどを設定してください。"),
                "soft"));
            _uiBlinkEffectDetail.Add(MakeHint(
                ArkitFTLoc.T("再生後は自動的に元の状態へ戻ります(Write Defaultsによる汎用リセット)。\n" +
                "ただしARKit標準のシェイプキー(まばたきや口の動きなど)を同じクリップで動かすと、\n" +
                "フェイストラッキングの値と一瞬競合することがあるため、専用の演出用シェイプキーを\n" +
                "使うことをおすすめします。"),
                "warning"));
            card.Add(_uiBlinkEffectDetail);

            _uiTrackingPage.Add(card);
        }

        private void BuildToolkitAdvancedCard()
        {
            var outputCard = MakeCard("OUTPUT", ArkitFTLoc.T("出力設定"), "accent-primary");

            var outputRow = new VisualElement();
            outputRow.AddToClassList("inline-row");

            _uiOutputField = new TextField("Output Folder");
            _uiOutputField.AddToClassList("grow-field");
            _uiOutputField.RegisterValueChangedCallback(evt =>
            {
                _outputFolder = evt.newValue ?? "";
                RefreshToolkitUI();
            });

            var browse = new Button(() =>
            {
                var path = EditorUtility.SaveFolderPanel(
                    "Output Folder",
                    _outputFolder.StartsWith("Assets/") ? _outputFolder : "Assets", "");
                if (string.IsNullOrEmpty(path)) return;

                if (path.StartsWith(Application.dataPath, StringComparison.Ordinal))
                {
                    _outputFolder = ("Assets" + path.Substring(Application.dataPath.Length))
                        .Replace('\\', '/');
                    RefreshToolkitUI();
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Output Folder",
                        ArkitFTLoc.T("UnityプロジェクトのAssetsフォルダ内を選択してください。"), "OK");
                }
            }) { text = "…" };
            browse.AddToClassList("browse-button");

            outputRow.Add(_uiOutputField);
            outputRow.Add(browse);
            outputCard.Add(outputRow);

            var diagnostics = new Foldout
            {
                text = ArkitFTLoc.T("診断・技術情報"),
                value = false
            };
            diagnostics.AddToClassList("sub-foldout");
            diagnostics.Add(MakeHint(
                ArkitFTLoc.T("ARKitシェイプやAnimatorの詳細診断はConsoleにも出力されます。通常は確認しなくてもインストールできます。"),
                "soft"));
            outputCard.Add(diagnostics);
            _uiAdvancedPage.Add(outputCard);

            _uiInstallResultCard = MakeCard("RESULT", ArkitFTLoc.T("インストール結果"), "accent-primary");
            _uiInstallResultBody = new VisualElement();
            _uiInstallResultBody.AddToClassList("result-body");
            _uiInstallResultCard.Add(_uiInstallResultBody);
            _uiAdvancedPage.Add(_uiInstallResultCard);

            RefreshInstallResultUI();
        }

        private void RefreshInstallResultUI()
        {
            if (_uiInstallResultBody == null) return;
            _uiInstallResultBody.Clear();

            if (_lastInstallResult == null)
            {
                var empty = MakeHint(
                    ArkitFTLoc.T("まだインストール結果はありません。\nINSTALLが完了すると、生成内容・Parameter使用量・出力先などをここにまとめて表示します。"),
                    "soft");
                empty.AddToClassList("result-empty");
                _uiInstallResultBody.Add(empty);
                return;
            }

            var complete = new VisualElement();
            complete.AddToClassList("result-complete");
            var completeTitle = new Label(ArkitFTLoc.T("✓ インストール完了"));
            completeTitle.AddToClassList("result-complete-title");
            var avatarTitle = new Label(_lastInstallResult.avatarName ?? "");
            avatarTitle.AddToClassList("result-avatar-name");
            complete.Add(completeTitle);
            complete.Add(avatarTitle);
            _uiInstallResultBody.Add(complete);

            var grid = new VisualElement();
            grid.AddToClassList("result-grid");
            AddResultRow(grid, "Face Mesh", _lastInstallResult.faceSmr);
            if (!string.IsNullOrEmpty(_lastInstallResult.eyeSmr))
                AddResultRow(grid, "Eye Mesh", _lastInstallResult.eyeSmr);
            AddResultRow(grid, ArkitFTLoc.T("にっこり目"), _lastInstallResult.squint);
            AddResultRow(grid, "Gesture", _lastInstallResult.gestures);
            AddResultRow(grid, ArkitFTLoc.T("Viseme補償"), _lastInstallResult.viseme);
            AddResultRow(grid, ArkitFTLoc.T("EyeLook"), _lastInstallResult.eyeLook);
            AddResultRow(grid, ArkitFTLoc.T("眉アシスト"), _lastInstallResult.brow);
            AddResultRow(grid, ArkitFTLoc.T("まばたきエフェクト"), _lastInstallResult.blinkEffect);
            AddResultRow(grid, ArkitFTLoc.T("標準Eye Look"), _lastInstallResult.nativeEyeLook);
            AddResultRow(grid, "Parameters", _lastInstallResult.parameters,
                _lastInstallResult.parametersOverBudget ? "warning" : "ok");
            if (!string.IsNullOrEmpty(_lastInstallResult.emptySync))
                AddResultRow(grid, ArkitFTLoc.T("同期最適化"), _lastInstallResult.emptySync);
            if (!string.IsNullOrEmpty(_lastInstallResult.ueFallback))
                AddResultRow(grid, "UE Fallback", _lastInstallResult.ueFallback);
            if (!string.IsNullOrEmpty(_lastInstallResult.missingShapes))
                AddResultRow(grid, ArkitFTLoc.T("⚠ 不足シェイプキー"), _lastInstallResult.missingShapes, "warning");
            _uiInstallResultBody.Add(grid);

            var pathBox = new VisualElement();
            pathBox.AddToClassList("result-path-box");
            var pathTitle = new Label(ArkitFTLoc.T("出力先"));
            pathTitle.AddToClassList("result-path-title");
            var path = new Label(_lastInstallResult.outputFolder ?? "");
            path.AddToClassList("result-path");
            pathBox.Add(pathTitle);
            pathBox.Add(path);
            _uiInstallResultBody.Add(pathBox);

            var actions = new VisualElement();
            actions.AddToClassList("result-actions");

            var selectAvatar = new Button(() =>
            {
                if (_lastInstallResult?.avatar == null) return;
                Selection.activeGameObject = _lastInstallResult.avatar;
                EditorGUIUtility.PingObject(_lastInstallResult.avatar);
            }) { text = ArkitFTLoc.T("生成アバターを選択") };
            selectAvatar.AddToClassList("result-action-button");
            selectAvatar.SetEnabled(_lastInstallResult.avatar != null);

            var showFolder = new Button(() =>
            {
                if (_lastInstallResult == null || string.IsNullOrEmpty(_lastInstallResult.outputFolder)) return;
                var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_lastInstallResult.outputFolder);
                if (folder != null)
                {
                    Selection.activeObject = folder;
                    EditorGUIUtility.PingObject(folder);
                }
            }) { text = ArkitFTLoc.T("出力フォルダを表示") };
            showFolder.AddToClassList("result-action-button");

            actions.Add(selectAvatar);
            actions.Add(showFolder);
            _uiInstallResultBody.Add(actions);
        }

        private static void AddResultRow(VisualElement parent, string label, string value, string status = null)
        {
            var row = new VisualElement();
            row.AddToClassList("result-row");

            var key = new Label(label);
            key.AddToClassList("result-key");
            var val = new Label(string.IsNullOrEmpty(value) ? "-" : value);
            val.AddToClassList("result-value");
            if (!string.IsNullOrEmpty(status)) val.AddToClassList("result-value-" + status);

            row.Add(key);
            row.Add(val);
            parent.Add(row);
        }

        private void BuildToolkitInstallFooter()
        {
            var footer = new VisualElement();
            footer.AddToClassList("install-footer");

            var title = new Label("Ready to install");
            title.AddToClassList("install-title");
            footer.Add(title);
            _uiReadyTitle = title;

            _uiReadyStatusRow = new VisualElement();
            _uiReadyStatusRow.AddToClassList("chip-row");
            _uiReadyStatusRow.AddToClassList("centered-row");
            footer.Add(_uiReadyStatusRow);

            _uiInstallButton = new Button(() => Install()) { text = "INSTALL" };
            _uiInstallButton.AddToClassList("install-button");
            footer.Add(_uiInstallButton);

            _uiFooterHost.Add(footer);
        }

        private VisualElement MakeCard(string kicker, string title, string accentClass)
        {
            var card = new VisualElement();
            card.AddToClassList("settings-card");
            card.AddToClassList(accentClass);

            var header = new VisualElement();
            header.AddToClassList("section-header");

            var kickerLabel = new Label(kicker);
            kickerLabel.AddToClassList("section-kicker");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("section-title");

            header.Add(kickerLabel);
            header.Add(titleLabel);
            card.Add(header);
            return card;
        }

        // カードの開閉状態は、VisualElementのインスタンス参照ではなく「kicker文字列」
        // (AVATAR・FACE等、MakeCardに渡すカード種別のキー)で記憶する。言語切り替え等で
        // UI全体が再構築されると、カード自体が新しいVisualElementインスタンスとして
        // 作り直されるため、インスタンス参照で記憶していると全て失われてしまう。
        // kickerは再構築されても変わらない安定したキーなので、これを使うことで
        // 開閉状態が再構築をまたいで保持されるようにする。
        private readonly Dictionary<string, bool> _cardExpandedState = new Dictionary<string, bool>();
        // ユーザーが手動で閉じたカードのkicker。これを覚えておかないと、警告が出るたびに
        // (RefreshCardAccents等の再描画のたびに)ユーザーが明示的に閉じたカードを
        // 強制的に開き直してしまい、鬱陶しい挙動になってしまう。
        private readonly HashSet<string> _userCollapsedCardKeys = new HashSet<string>();
        // 現在表示中のカード(VisualElement)ごとのコンテンツコンテナ・シェブロン・kickerへの
        // 参照。RefreshCardAccents側で「警告があれば自動的に開く」ために保持する。
        // UI再構築のたびにMakeCardCollapsibleで上書きされるため、こちらはインスタンス
        // 参照のままで問題ない(常に「今表示されている」カードだけを指すため)。
        private readonly Dictionary<VisualElement, (VisualElement content, Label chevron, string key)> _collapsibleCards =
            new Dictionary<VisualElement, (VisualElement, Label, string)>();

        /// <summary>
        /// MakeCardで作った完成済みのカードを折りたたみ可能にする。ヘッダー(先頭の子要素、
        /// MakeCardの実装上必ずkicker+titleを含む)はそのまま残し、それ以降に追加された
        /// 全ての子要素を、折りたたみ可能なコンテンツコンテナへまとめて移動する。
        /// ヘッダー全体をクリックすると開閉できる(シェブロン▶/▼付き)。
        ///
        /// 初めてNK Installerを開いた人にとって、全カードが常に全展開された状態だと
        /// 情報量が多すぎて「難しそう」という印象を与えてしまう。既定で折りたたんでおき、
        /// 実際に注意が必要なカード(Missing・Eye競合等の警告があるカード)だけ
        /// RefreshCardAccents側から自動的に開くようにすることで、初見の分かりやすさと
        /// 警告の見落とし防止を両立させる。
        ///
        /// cardKeyには、そのカードのMakeCard呼び出しで使ったkicker文字列(例:"FACE")を
        /// そのまま渡す。以前にこのkickerで開閉状態が記録されていれば、defaultExpandedより
        /// そちらを優先する(=言語切り替え等でカードが再構築されても、直前の開閉状態を
        /// 引き継げるようにするため)。
        /// </summary>
        private void MakeCardCollapsible(VisualElement card, string cardKey, bool defaultExpanded)
        {
            if (card == null || card.childCount == 0) return;
            var header = card[0];

            var content = new VisualElement();
            content.AddToClassList("card-collapsible-content");
            var toMove = card.Children().Skip(1).ToList();
            foreach (var child in toMove)
            {
                card.Remove(child);
                content.Add(child);
            }
            card.Add(content);

            bool startExpanded = _cardExpandedState.TryGetValue(cardKey, out var savedExpanded)
                ? savedExpanded
                : defaultExpanded;
            _cardExpandedState[cardKey] = startExpanded;

            var chevron = new Label(startExpanded ? "▼" : "▶");
            chevron.AddToClassList("card-collapse-chevron");
            // 実際のスタイルシートにこのクラスの定義が無い場合でも見た目が崩れないよう、
            // 最低限のインラインスタイルを直接指定しておく。
            chevron.style.marginRight = 6;
            chevron.style.unityTextAlign = TextAnchor.MiddleCenter;
            chevron.style.width = 14;
            chevron.style.opacity = 0.7f;
            header.Insert(0, chevron);
            header.AddToClassList("card-header-clickable");
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;

            content.style.display = startExpanded ? DisplayStyle.Flex : DisplayStyle.None;

            header.RegisterCallback<ClickEvent>(evt =>
            {
                bool nowExpanded = content.style.display == DisplayStyle.None;
                content.style.display = nowExpanded ? DisplayStyle.Flex : DisplayStyle.None;
                chevron.text = nowExpanded ? "▼" : "▶";
                _cardExpandedState[cardKey] = nowExpanded;
                // ユーザーが手動で操作した場合、以降はRefreshCardAccentsによる自動展開の
                // 対象から外す(ユーザーが意図的に閉じたカードを、警告のたびに勝手に
                // 開き直すのを防ぐため)。ただし、ユーザーが手動で開いた場合は自動展開と
                // 競合しないため、閉じた場合のみ記録すれば十分。
                if (!nowExpanded) _userCollapsedCardKeys.Add(cardKey);
                else _userCollapsedCardKeys.Remove(cardKey);
            });

            _collapsibleCards[card] = (content, chevron, cardKey);
        }

        /// <summary>
        /// 指定したカードを、警告がある場合にだけ自動的に開く。警告が無い場合は何もしない
        /// (現在の開閉状態を維持する)。ここでカードを畳む方向の処理をしてしまうと、
        /// ExpandAllCollapsibleCardsで開いたばかりのカードや、ユーザーが手動で開いた
        /// カードまで、警告条件を満たさなくなった瞬間に勝手に閉じてしまうため、
        /// 「開く」動作のみに限定している。ユーザーが手動で閉じたカードは対象外にする
        /// (MakeCardCollapsibleのコールバック参照)。
        /// </summary>
        private void SetCardAutoExpanded(VisualElement card, bool shouldExpand)
        {
            if (!shouldExpand) return;
            if (card == null) return;
            if (!_collapsibleCards.TryGetValue(card, out var pair)) return;
            if (_userCollapsedCardKeys.Contains(pair.key)) return; // ユーザーの意思を尊重する
            pair.content.style.display = DisplayStyle.Flex;
            pair.chevron.text = "▼";
            _cardExpandedState[pair.key] = true;
        }

        /// <summary>
        /// 全ての折りたたみ可能カードを展開する。Profileが存在しないアバターを選択した
        /// 場合や、新規Profileを作成した場合は、ユーザーが「使い方マニュアル」の
        /// 「Profile未作成アバター」の手順に沿って、上から順に設定していく状況にあたる。
        /// この場合はガイド代わりに全カードを開いておく必要があるため、通常は尊重する
        /// はずの「ユーザーが手動で閉じた」という記憶(_userCollapsedCardKeys)もあえて
        /// クリアする(以前に別のアバターで畳んだ記憶を、事実上の初回セットアップである
        /// この状況にまで引きずらないようにするため)。
        /// </summary>
        private void ExpandAllCollapsibleCards()
        {
            _userCollapsedCardKeys.Clear();
            foreach (var kv in _collapsibleCards)
            {
                kv.Value.content.style.display = DisplayStyle.Flex;
                kv.Value.chevron.text = "▼";
                _cardExpandedState[kv.Value.key] = true;
            }
        }

        /// <summary>
        /// TongueLiftSource enumのPopupField表示ラベルをローカライズする
        /// (ExistingShapeKey→既存シェイプキー、AutoDetect→自動検知)。
        /// </summary>
        private static string FormatTongueLiftSourceLabel(TongueLiftSource value)
        {
            switch (value)
            {
                case TongueLiftSource.ExistingShapeKey: return ArkitFTLoc.T("既存シェイプキー");
                case TongueLiftSource.AutoDetect: return ArkitFTLoc.T("自動検知");
                default: return value.ToString();
            }
        }

        private static VisualElement MakeHint(string text, string kind)
        {
            var box = new VisualElement();
            box.AddToClassList("hint-box");
            box.AddToClassList("hint-" + kind);
            var label = new Label(text);
            label.AddToClassList("hint-text");
            box.Add(label);
            return box;
        }

        /// <summary>
        /// 検索欄を、入力必須な項目に見えないよう控えめなサイズ(幅50%)・左寄せにする。
        /// にっこり目・ジェスチャー抑制・舌アシストの3か所の検索欄で共通して使う。
        /// </summary>
        private static void StyleAsCompactSearchField(VisualElement field)
        {
            field.style.width = new StyleLength(new Length(50, LengthUnit.Percent));
            field.style.alignSelf = Align.FlexStart;
        }

        private static VisualElement MakeModeDescription(string title, string text)
        {
            var body = new VisualElement();
            body.AddToClassList("mode-body");
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("mode-title");
            var desc = new Label(text);
            desc.AddToClassList("mode-description");
            body.Add(titleLabel);
            body.Add(desc);
            return body;
        }

        private VisualElement MakeFloatSlider(
            string label, float min, float max,
            out Slider slider, out FloatField valueField,
            Action<float> setter)
        {
            var row = new VisualElement();
            row.AddToClassList("slider-row");

            var labelElement = new Label(label);
            labelElement.AddToClassList("slider-label");
            row.Add(labelElement);

            slider = new Slider(min, max);
            slider.AddToClassList("slider-control");
            row.Add(slider);

            valueField = new FloatField();
            valueField.AddToClassList("slider-value");
            row.Add(valueField);

            var localSlider = slider;
            var localField = valueField;

            localSlider.RegisterValueChangedCallback(evt =>
            {
                float value = Mathf.Clamp(evt.newValue, min, max);
                setter(value);
                localField.SetValueWithoutNotify(value);
            });
            localField.RegisterValueChangedCallback(evt =>
            {
                float value = Mathf.Clamp(evt.newValue, min, max);
                setter(value);
                localSlider.SetValueWithoutNotify(value);
                localField.SetValueWithoutNotify(value);
            });

            return row;
        }

        private VisualElement MakeIntSlider(
            string label, int min, int max,
            out SliderInt slider, out IntegerField valueField,
            Action<int> setter)
        {
            var row = new VisualElement();
            row.AddToClassList("slider-row");

            var labelElement = new Label(label);
            labelElement.AddToClassList("slider-label");
            row.Add(labelElement);

            slider = new SliderInt(min, max);
            slider.AddToClassList("slider-control");
            row.Add(slider);

            valueField = new IntegerField();
            valueField.AddToClassList("slider-value");
            row.Add(valueField);

            var localSlider = slider;
            var localField = valueField;

            localSlider.RegisterValueChangedCallback(evt =>
            {
                int value = Mathf.Clamp(evt.newValue, min, max);
                setter(value);
                localField.SetValueWithoutNotify(value);
            });
            localField.RegisterValueChangedCallback(evt =>
            {
                int value = Mathf.Clamp(evt.newValue, min, max);
                setter(value);
                localSlider.SetValueWithoutNotify(value);
                localField.SetValueWithoutNotify(value);
            });

            return row;
        }

        private Label MakeChip(string text, string kind, string tooltip = null)
        {
            var label = new Label(text);
            label.AddToClassList("status-chip");
            label.AddToClassList("chip-" + kind);
            if (!string.IsNullOrEmpty(tooltip)) label.tooltip = tooltip;
            return label;
        }

        private List<int> GetFilteredShapeIndices()
        {
            var result = new List<int>();
            for (int i = 0; i < _shapeNames.Length; i++)
            {
                if (string.IsNullOrEmpty(_squintSearchQuery) ||
                    _shapeNames[i].IndexOf(_squintSearchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add(i);
            }
            return result;
        }

        private List<int> GetFilteredGestureIndices()
        {
            var result = new List<int>();
            for (int i = 0; i < _fxLayerNames.Length; i++)
            {
                if (string.IsNullOrEmpty(_gestureSearchQuery) ||
                    _fxLayerNames[i].IndexOf(_gestureSearchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add(i);
            }
            return result;
        }

        /// <summary>
        /// 舌アシストのExisting Shape Key(持ち上げ形状)の選択肢を、検索語で絞り込んだ内容で
        /// 更新する。にっこり目の複数選択リストと同じ「Face SMRのシェイプキー名一覧を検索語で
        /// 絞り込む」考え方を、単一選択向けに使う。現在選択中の値が検索に一致しない場合も、
        /// 選択自体が失われないよう一覧に残す(にっこり目のPopupFieldと同じ配慮)。
        ///
        /// PopupField自体はBuildToolkitTongueCardで一度だけ生成し、以降はここで
        /// choices/valueだけを更新する(キー入力のたびに要素を破棄・再生成すると、
        /// UI Toolkit側の描画が追いつかず表示されなくなることがあるため)。
        /// RefreshToolkitUIから、_shapeNamesが更新されるたびに呼ばれる。
        /// </summary>
        private void RebuildTongueExistingShapePopup()
        {
            if (_uiTongueExistingShapeField == null) return;

            if (_shapeNames.Length == 0)
            {
                _uiTongueExistingShapeField.style.display = DisplayStyle.None;
                if (_uiTongueExistingShapeHint != null)
                {
                    ((Label)_uiTongueExistingShapeHint.hierarchy[0]).text =
                        ArkitFTLoc.T("Face Meshを選択するとShape Key一覧が表示されます。");
                    _uiTongueExistingShapeHint.style.display = DisplayStyle.Flex;
                }
                return;
            }

            var filtered = new List<int>();
            for (int i = 0; i < _shapeNames.Length; i++)
                if (string.IsNullOrEmpty(_tongueExistingLiftSearchQuery) ||
                    _shapeNames[i].IndexOf(_tongueExistingLiftSearchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    filtered.Add(i);

            // 現在保存されている値(Profile経由の場合、現在のFace SMRに無い可能性もある)を
            // 名前で検索する。見つかった場合、検索語に一致していなくても一覧に残す。
            int currentIndex = Array.FindIndex(_shapeNames, n => n == _tongueExistingLiftShapeName);

            var choices = new List<int> { -1 }; // -1 = 未選択
            choices.AddRange(filtered);
            if (currentIndex >= 0 && !choices.Contains(currentIndex))
                choices.Insert(1, currentIndex);

            _uiTongueExistingShapeField.choices = choices;
            _uiTongueExistingShapeField.SetValueWithoutNotify(currentIndex >= 0 ? currentIndex : -1);
            _uiTongueExistingShapeField.style.display = DisplayStyle.Flex;

            if (_uiTongueExistingShapeHint != null)
            {
                if (filtered.Count == 0 && currentIndex < 0 && !string.IsNullOrEmpty(_tongueExistingLiftSearchQuery))
                {
                    ((Label)_uiTongueExistingShapeHint.hierarchy[0]).text =
                        string.Format(ArkitFTLoc.T("「{0}」に一致するShape Keyがありません。"), _tongueExistingLiftSearchQuery);
                    _uiTongueExistingShapeHint.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _uiTongueExistingShapeHint.style.display = DisplayStyle.None;
                }
            }
        }

        /// <summary>
        /// 目線シェイプキー生成のベイク対象となるSMR(表情メッシュと目メッシュが別々の場合は
        /// Eye SMR、そうでなければFace SMR)のシェイプキー名一覧を、_shapeNamesと同様に
        /// キャッシュとして保持する。_smrIndex/_eyeSmrIndex/_eyeSmrSeparateが変化する
        /// タイミングでのみ再構築し、それ以外のUI更新(Save・行追加等)では再計算しない。
        /// 呼び出すたびに再計算する設計だと、他の処理の実行順序次第でSMR一覧が一時的に
        /// 不安定な状態のまま参照されてしまい、選択中の行が意図せず先頭のシェイプキーに
        /// リセットされたように見える不具合の原因になっていたため、この方式に変更した。
        /// </summary>
        private string[] _eyeLookBakeShapeNamesCache = Array.Empty<string>();

        private void RefreshEyeLookBakeShapeNamesCache()
        {
            int idx = _eyeSmrSeparate ? _eyeSmrIndex : _smrIndex;
            if (idx < 0 || idx >= _smrs.Length)
            {
                _eyeLookBakeShapeNamesCache = Array.Empty<string>();
                return;
            }
            var smr = _smrs[idx];
            if (smr == null || smr.sharedMesh == null)
            {
                _eyeLookBakeShapeNamesCache = Array.Empty<string>();
                return;
            }
            var mesh = smr.sharedMesh;
            var names = new string[mesh.blendShapeCount];
            for (int i = 0; i < mesh.blendShapeCount; i++)
                names[i] = mesh.GetBlendShapeName(i);
            _eyeLookBakeShapeNamesCache = names;
        }

        private List<int> GetFilteredEyeLookBakeShapeIndices()
        {
            var shapeNames = _eyeLookBakeShapeNamesCache;
            var result = new List<int>();
            for (int i = 0; i < shapeNames.Length; i++)
            {
                if (string.IsNullOrEmpty(_eyeLookBakeSearchQuery) ||
                    shapeNames[i].IndexOf(_eyeLookBakeSearchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add(i);
            }
            return result;
        }

        /// <summary>
        /// 目線シェイプキー生成時に、あらかじめ有効にしておく追加シェイプキーの選択リストを
        /// 再構築する。にっこり目の複数選択リストと全く同じ考え方(検索語で絞り込み、
        /// 実インデックスで選択肢を保持する)。
        /// </summary>
        private void RebuildEyeLookBakeShapeList()
        {
            if (_uiEyeLookBakeShapeList == null) return;
            _uiEyeLookBakeShapeList.Clear();

            var shapeNames = _eyeLookBakeShapeNamesCache;
            var filteredIndices = GetFilteredEyeLookBakeShapeIndices();

            if (shapeNames.Length == 0)
            {
                _uiEyeLookBakeShapeList.Add(MakeHint(ArkitFTLoc.T("Face Meshを選択するとShape Key一覧が表示されます。"), "soft"));
                _uiEyeLookBakeAddButton?.SetEnabled(false);
                return;
            }

            if (filteredIndices.Count == 0)
            {
                _uiEyeLookBakeShapeList.Add(MakeHint(string.Format(ArkitFTLoc.T("「{0}」に一致するShape Keyがありません。"), _eyeLookBakeSearchQuery), "soft"));
                _uiEyeLookBakeAddButton?.SetEnabled(false);
                return;
            }

            for (int rowIndex = 0; rowIndex < _eyeLookBakeShapeIndices.Count; rowIndex++)
            {
                int capturedRow = rowIndex;
                int selectedIndex = _eyeLookBakeShapeIndices[rowIndex];
                if (selectedIndex < 0 || selectedIndex >= shapeNames.Length)
                    selectedIndex = filteredIndices[0];

                // 選択肢は「実インデックス(int)」で保持する(にっこり目と同じ理由。
                // 同名のShape Keyが複数存在するケースでの誤選択を防ぐ)。
                var choices = new List<int>(filteredIndices);
                if (!choices.Contains(selectedIndex)) choices.Insert(0, selectedIndex);

                var row = new VisualElement();
                row.AddToClassList("selection-row");

                var popup = new PopupField<int>(
                    $"Shape {rowIndex + 1}", choices, selectedIndex,
                    formatSelectedValueCallback: i => (i >= 0 && i < shapeNames.Length) ? shapeNames[i] : "?",
                    formatListItemCallback: i => (i >= 0 && i < shapeNames.Length) ? shapeNames[i] : "?");
                popup.AddToClassList("grow-field");
                popup.RegisterValueChangedCallback(evt =>
                {
                    if (capturedRow < _eyeLookBakeShapeIndices.Count)
                        _eyeLookBakeShapeIndices[capturedRow] = evt.newValue;
                });

                var remove = new Button(() =>
                {
                    if (capturedRow < _eyeLookBakeShapeIndices.Count)
                    {
                        _eyeLookBakeShapeIndices.RemoveAt(capturedRow);
                        RebuildEyeLookBakeShapeList();
                        RefreshReadyToInstallChips();
                        RefreshCardAccents(_avatarPrefab != null);
                    }
                }) { text = "×" };
                remove.AddToClassList("remove-button");

                row.Add(popup);
                row.Add(remove);
                _uiEyeLookBakeShapeList.Add(row);
            }

            _uiEyeLookBakeAddButton?.SetEnabled(true);
        }

        private void RebuildSquintList()
        {
            if (_uiSquintList == null) return;
            _uiSquintList.Clear();

            var filteredIndices = GetFilteredShapeIndices();

            if (_shapeNames.Length == 0)
            {
                _uiSquintList.Add(MakeHint(ArkitFTLoc.T("Face Meshを選択するとShape Key一覧が表示されます。"), "soft"));
                _uiSquintAddButton?.SetEnabled(false);
                return;
            }

            if (filteredIndices.Count == 0)
            {
                _uiSquintList.Add(MakeHint(string.Format(ArkitFTLoc.T("「{0}」に一致するShape Keyがありません。"), _squintSearchQuery), "soft"));
                _uiSquintAddButton?.SetEnabled(false);
                return;
            }

            for (int rowIndex = 0; rowIndex < _squintShapeIndices.Count; rowIndex++)
            {
                int capturedRow = rowIndex;
                int selectedIndex = _squintShapeIndices[rowIndex];
                if (selectedIndex < 0 || selectedIndex >= _shapeNames.Length)
                    selectedIndex = filteredIndices[0];

                // 選択肢は「実インデックス(int)」で保持する。名前(string)をキーにすると、
                // 同名のShape Keyが複数存在する場合にArray.IndexOf等で常に最初の一致に
                // 解決されてしまい、2つ目以降を選んでも見た目と実データがズレる
                // (エラーも出ず静かに誤動作するため危険)。
                var choices = new List<int>(filteredIndices);
                if (!choices.Contains(selectedIndex)) choices.Insert(0, selectedIndex);

                var row = new VisualElement();
                row.AddToClassList("selection-row");

                var popup = new PopupField<int>(
                    $"Shape {rowIndex + 1}", choices, selectedIndex,
                    formatSelectedValueCallback: i => (i >= 0 && i < _shapeNames.Length) ? _shapeNames[i] : "?",
                    formatListItemCallback: i => (i >= 0 && i < _shapeNames.Length) ? _shapeNames[i] : "?");
                popup.AddToClassList("grow-field");
                popup.RegisterValueChangedCallback(evt =>
                {
                    if (capturedRow < _squintShapeIndices.Count)
                        _squintShapeIndices[capturedRow] = evt.newValue;
                });

                var remove = new Button(() =>
                {
                    if (capturedRow < _squintShapeIndices.Count)
                    {
                        _squintShapeIndices.RemoveAt(capturedRow);
                        RebuildSquintList();
                        RefreshReadyToInstallChips();
                        RefreshCardAccents(_avatarPrefab != null);
                    }
                }) { text = "×" };
                remove.AddToClassList("remove-button");

                row.Add(popup);
                row.Add(remove);
                _uiSquintList.Add(row);
            }

            _uiSquintAddButton?.SetEnabled(true);
        }

        private void RebuildGestureList()
        {
            if (_uiGestureList == null) return;
            _uiGestureList.Clear();

            if (_uiGestureSuppressDetail != null)
                _uiGestureSuppressDetail.style.display =
                    _gestureLayerIndices.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            var filteredIndices = GetFilteredGestureIndices();

            if (_fxLayerNames.Length == 0)
            {
                _uiGestureList.Add(MakeHint(ArkitFTLoc.T("AvatarDescriptorのFXが見つかりません。"), "warning"));
                _uiGestureAddButton?.SetEnabled(false);
                return;
            }

            if (filteredIndices.Count == 0)
            {
                _uiGestureList.Add(MakeHint(string.Format(ArkitFTLoc.T("「{0}」に一致するLayerがありません。"), _gestureSearchQuery), "soft"));
                _uiGestureAddButton?.SetEnabled(false);
                return;
            }

            // 見出し行(1回だけ表示。各行はコントロールのみにして、対応関係を分かりやすくする)。
            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
            var headerBadgeSpacer = new VisualElement();
            headerBadgeSpacer.style.width = 32; // バッジ(#3等)の幅ぶんの空白
            headerRow.Add(headerBadgeSpacer);
            var headerSpacer = new VisualElement();
            headerSpacer.style.flexGrow = 1;
            headerRow.Add(headerSpacer);
            var headerRemoveSpacer = new VisualElement();
            headerRemoveSpacer.style.width = 28; // 削除ボタン(×)の幅ぶんの空白
            headerRow.Add(headerRemoveSpacer);
            _uiGestureList.Add(headerRow);

            for (int rowIndex = 0; rowIndex < _gestureLayerIndices.Count; rowIndex++)
            {
                int capturedRow = rowIndex;
                int selectedIndex = _gestureLayerIndices[rowIndex];
                if (selectedIndex < 0 || selectedIndex >= _fxLayerNames.Length)
                    selectedIndex = filteredIndices[0];

                // にっこり目リストと同じ理由で、選択肢は名前ではなく実インデックス(int)で保持する
                // (同名のFXレイヤーが複数存在するケースでの誤選択を防ぐ)。
                var choices = new List<int>(filteredIndices);
                if (!choices.Contains(selectedIndex)) choices.Insert(0, selectedIndex);

                // 1レイヤー = 1行。チェックボックスにはラベルを付けず、見出し行の対応する列幅と
                // 揃えることで、チェックボックスとラベルの対応関係を分かりやすくする。
                var row = new VisualElement();
                row.AddToClassList("selection-row");

                var popup = new PopupField<int>(
                    choices, selectedIndex,
                    formatSelectedValueCallback: i => (i >= 0 && i < _fxLayerNames.Length) ? _fxLayerNames[i] : "?",
                    formatListItemCallback: i => (i >= 0 && i < _fxLayerNames.Length) ? _fxLayerNames[i] : "?");
                // 詰め込みすぎると幅0になって見えなくなる不具合を確認済みのため、クラスに頼らず
                // インラインスタイルで明示的に幅・高さを確保する。
                popup.AddToClassList("grow-field");
                popup.style.flexGrow = 1;
                popup.style.minHeight = 20;

                var badge = new Label($"#{selectedIndex}");
                badge.AddToClassList("index-badge");
                badge.style.width = 32; // 左端に固定幅で配置し、レイヤー名側の表示幅を圧迫しないようにする
                badge.style.unityTextAlign = TextAnchor.MiddleCenter;

                popup.RegisterValueChangedCallback(evt =>
                {
                    if (capturedRow < _gestureLayerIndices.Count)
                        _gestureLayerIndices[capturedRow] = evt.newValue;
                    // #番号バッジは行構築時の値で固定されるため、選択変更時に表示を追従させる。
                    badge.text = $"#{evt.newValue}";
                });

                var remove = new Button(() =>
                {
                    if (capturedRow < _gestureLayerIndices.Count)
                    {
                        _gestureLayerIndices.RemoveAt(capturedRow);
                        RebuildGestureList();
                        RefreshReadyToInstallChips();
                        RefreshCardAccents(_avatarPrefab != null);
                    }
                }) { text = "×" };
                remove.AddToClassList("remove-button");

                row.Add(badge);
                row.Add(popup);
                row.Add(remove);
                _uiGestureList.Add(row);
            }

            _uiGestureAddButton?.SetEnabled(true);
        }

        private void RefreshToolkitUI()
        {
            if (!_uiReady) return;

            // アバター未選択の段階では、FACE/SMILE EYES/GESTUREの詳細設定は
            // 何に対する設定か定まらず意味を持たないため、まとめて隠して
            // 案内ヒントに差し替える。
            bool hasAvatar = _avatarPrefab != null;
            if (_uiFaceAvatarGate != null)
                _uiFaceAvatarGate.style.display = hasAvatar ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiFaceNoAvatarHint != null)
                _uiFaceNoAvatarHint.style.display = hasAvatar ? DisplayStyle.None : DisplayStyle.Flex;
            if (_uiSquintAvatarGate != null)
                _uiSquintAvatarGate.style.display = hasAvatar ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiSquintNoAvatarHint != null)
                _uiSquintNoAvatarHint.style.display = hasAvatar ? DisplayStyle.None : DisplayStyle.Flex;
            if (_uiGestureAvatarGate != null)
                _uiGestureAvatarGate.style.display = hasAvatar ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiGestureNoAvatarHint != null)
                _uiGestureNoAvatarHint.style.display = hasAvatar ? DisplayStyle.None : DisplayStyle.Flex;

            // カード左端の色: そのカードで何も値が設定されていない(=まだ手を付けていない)
            // 場合はグレー(accent-unset)にし、何か設定されているカードとの見分けを付ける。
            RefreshCardAccents(hasAvatar);

            _uiProfileField?.SetValueWithoutNotify(_profile);
            SimplifyObjectFieldDisplay(_uiProfileField, _profile);
            _uiProfileSaveButton?.SetEnabled(_profile != null);
            _uiAvatarMatchTagField?.SetValueWithoutNotify(_avatarMatchTag ?? "");
            _uiProfileShopNameField?.SetValueWithoutNotify(_profileShopName ?? "");
            _uiProfileVersionNameField?.SetValueWithoutNotify(_profileVersionName ?? "");
            if (_uiAvatarMatchTagRow != null)
                _uiAvatarMatchTagRow.style.display = _profile != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiProfileMetaRow != null)
                _uiProfileMetaRow.style.display = _profile != null ? DisplayStyle.Flex : DisplayStyle.None;
            _uiAvatarField?.SetValueWithoutNotify(_avatarPrefab);

            // Profileが読み込まれていれば、上部の帯で状態を案内する。ダミーファイル等、
            // 顔まわりのデータが実質何も無いアバターの場合は「このままInstallできます」と
            // 案内してしまうと誤解を招くため、警告表示に切り替える。
            // アバター未選択の間はInstall自体ができないため、Profileがあっても帯は出さない。
            if (_uiProfileReadyBanner != null)
            {
                bool showBanner = _profile != null && _avatarPrefab != null;
                _uiProfileReadyBanner.style.display = showBanner ? DisplayStyle.Flex : DisplayStyle.None;
                if (showBanner && _uiProfileReadyBannerLabel != null)
                {
                    const string DISPLAY_PREFIX = "ARKitFTProfile_";
                    string profileLabel = _profile.name.StartsWith(DISPLAY_PREFIX, StringComparison.Ordinal)
                        ? _profile.name.Substring(DISPLAY_PREFIX.Length)
                        : _profile.name;

                    // Face Meshが1つも見つからない、または見つかったメッシュ上にARKit標準
                    // シェイプキーもUE代替シェイプキーも1つも無い場合は、フェイストラッキング
                    // として機能する見込みが無いダミーファイル等の可能性が高いため警告する。
                    bool noUsableFaceData = _smrPaths.Length == 0 ||
                        (_missingArkitShapes.Count >= ARKIT_SHAPE_NAMES.Length && _ueFallbackResolvedShapes.Count == 0);

                    if (noUsableFaceData)
                    {
                        _uiProfileReadyBanner.style.backgroundColor = new Color(0.36f, 0.24f, 0.10f);
                        _uiProfileReadyBannerLabel.text =
                            string.Format(ArkitFTLoc.T("⚠ Profile「{0}」を読み込み済みですが、このアバターにはFace Meshが" +
                            "見つからないか、ARKit標準シェイプキーが1つも検出されませんでした。Avatar/Face Meshの" +
                            "選択をご確認ください。"), profileLabel);
                    }
                    else
                    {
                        _uiProfileReadyBanner.style.backgroundColor = new Color(0.16f, 0.32f, 0.28f);
                        int detectedCount = ARKIT_SHAPE_NAMES.Length - _missingArkitShapes.Count;
                        _uiProfileReadyBannerLabel.text =
                            string.Format(ArkitFTLoc.T("✓ Profile「{0}」を読み込み済みです。ARKit標準シェイプキー{1}個を" +
                            "検出しました。Installできます。"), profileLabel, detectedCount);
                    }
                }
            }

            if (_uiFaceSmrField != null)
            {
                _uiFaceSmrField.choices = _smrPaths.ToList();
                _uiFaceSmrField.SetValueWithoutNotify(
                    _smrIndex >= 0 && _smrIndex < _smrPaths.Length ? _smrPaths[_smrIndex] : "");
                _uiFaceSmrField.SetEnabled(_smrPaths.Length > 0);
            }

            _uiArkitPrefixField?.SetValueWithoutNotify(_arkitShapePrefix ?? "");
            _uiHasBlendshapePrefixToggle?.SetValueWithoutNotify(_hasBlendshapePrefix);
            _uiUeFallbackToggle?.SetValueWithoutNotify(_ueFallbackEnabled);
            if (_uiUeFallbackDetail != null)
                _uiUeFallbackDetail.style.display = _ueFallbackEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiBlendshapePrefixDetail != null)
                _uiBlendshapePrefixDetail.style.display = _hasBlendshapePrefix ? DisplayStyle.Flex : DisplayStyle.None;

            if (_uiFaceSmrPathMismatchHint != null)
                _uiFaceSmrPathMismatchHint.style.display = _faceSmrPathMismatch ? DisplayStyle.Flex : DisplayStyle.None;

            if (_uiFaceDetail != null)
                _uiFaceDetail.style.display = _smrPaths.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;

            _uiDisableEmptySyncToggle?.SetValueWithoutNotify(_disableSyncForEmptyShapes);
            if (_uiDisableEmptySyncHint != null)
                _uiDisableEmptySyncHint.style.display =
                    _disableSyncForEmptyShapes ? DisplayStyle.Flex : DisplayStyle.None;
            _uiEyeSeparateToggle?.SetValueWithoutNotify(_eyeSmrSeparate);

            if (_uiEyeSmrField != null)
            {
                _uiEyeSmrField.choices = _smrPaths.ToList();
                _uiEyeSmrField.SetValueWithoutNotify(
                    _eyeSmrIndex >= 0 && _eyeSmrIndex < _smrPaths.Length ? _smrPaths[_eyeSmrIndex] : "");
            }
            if (_uiEyeSmrRow != null)
                _uiEyeSmrRow.style.display =
                    (_smrPaths.Length > 0 && _eyeSmrSeparate) ? DisplayStyle.Flex : DisplayStyle.None;

            RefreshStatusChips();

            _uiEyeLookBakeToggle?.SetValueWithoutNotify(_useEyeLookBakeShapes);
            if (_uiEyeLookBakeDetail != null)
                _uiEyeLookBakeDetail.style.display = _useEyeLookBakeShapes ? DisplayStyle.Flex : DisplayStyle.None;
            _uiEyeLookBakeSearchField?.SetValueWithoutNotify(_eyeLookBakeSearchQuery ?? "");
            RebuildEyeLookBakeShapeList();

            _uiSquintSearchField?.SetValueWithoutNotify(_squintSearchQuery ?? "");
            RebuildSquintList();

            _uiGestureSearchField?.SetValueWithoutNotify(_gestureSearchQuery ?? "");
            RebuildGestureList();

            _uiVisemeToggle?.SetValueWithoutNotify(_generateVisemeCompensation);
            _uiVisemeSlider?.SetValueWithoutNotify(_visemeScale);
            _uiVisemeValue?.SetValueWithoutNotify(_visemeScale);
            if (_uiVisemeDetail != null)
                _uiVisemeDetail.style.display = _generateVisemeCompensation ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiVisemeConfigWarningHint != null)
            {
                bool showVisemeWarning = _avatarPrefab != null && !_avatarVisemeConfigured;
                _uiVisemeConfigWarningHint.style.display = showVisemeWarning ? DisplayStyle.Flex : DisplayStyle.None;
                if (showVisemeWarning && _uiVisemeConfigWarningHint.childCount > 0 &&
                    _uiVisemeConfigWarningHint[0] is Label visemeWarningLabel)
                    visemeWarningLabel.text = _avatarVisemeProblem;
            }

            _uiEyeLookToggle?.SetValueWithoutNotify(_generateEyeLookShapes);
            _uiEyeLookSlider?.SetValueWithoutNotify(_eyeLookIntensity);
            _uiEyeLookValue?.SetValueWithoutNotify(_eyeLookIntensity);
            if (_uiEyeLookDetail != null)
                _uiEyeLookDetail.style.display = _generateEyeLookShapes ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiEyeLookConfigWarningHint != null)
            {
                bool showEyeLookWarning = _avatarPrefab != null && !_avatarEyeLookConfigured;
                _uiEyeLookConfigWarningHint.style.display = showEyeLookWarning ? DisplayStyle.Flex : DisplayStyle.None;
                if (showEyeLookWarning && _uiEyeLookConfigWarningHint.childCount > 0 &&
                    _uiEyeLookConfigWarningHint[0] is Label eyeLookWarningLabel)
                    eyeLookWarningLabel.text = _avatarEyeLookProblem;
            }

            _uiBlinkSimple1DToggle?.SetValueWithoutNotify(_blinkControlMode == BlinkControlMode.TwoD);
            if (_uiBlinkModeHint != null)
                _uiBlinkModeHint.style.display = (_blinkControlMode == BlinkControlMode.TwoD) ? DisplayStyle.Flex : DisplayStyle.None;

            _uiEyeConstraintToggle?.SetValueWithoutNotify(_eyeUsesConstraint);
            _uiEyeConstraintToggle?.SetEnabled(_generateEyeLookShapes);
            if (_uiEyeConstraintFields != null)
                _uiEyeConstraintFields.style.display =
                    (_generateEyeLookShapes && _eyeUsesConstraint) ? DisplayStyle.Flex : DisplayStyle.None;
            _uiLeftEyeField?.SetValueWithoutNotify(_leftEyeConstraintTarget);
            _uiRightEyeField?.SetValueWithoutNotify(_rightEyeConstraintTarget);

            if (_uiEyeCompatCard != null)
                _uiEyeCompatCard.EnableInClassList("mode-card-selected", !_disableNativeEyeLook);
            if (_uiEyeStableCard != null)
                _uiEyeStableCard.EnableInClassList("mode-card-selected", _disableNativeEyeLook);
            RefreshEyeConflictCard();

            _uiBrowToggle?.SetValueWithoutNotify(_generateBrowAssistShapes);
            _uiBrowSlider?.SetValueWithoutNotify(_browAssistIntensity);
            _uiBrowValue?.SetValueWithoutNotify(_browAssistIntensity);
            if (_uiBrowDetail != null)
                _uiBrowDetail.style.display = _generateBrowAssistShapes ? DisplayStyle.Flex : DisplayStyle.None;

            _uiTongueToggle?.SetValueWithoutNotify(_generateTongueAssistShapes);
            _uiTongueLiftSourceField?.SetValueWithoutNotify(_tongueLiftSource);
            _uiTongueExistingShapeSearchField?.SetValueWithoutNotify(_tongueExistingLiftSearchQuery);
            RebuildTongueExistingShapePopup();
            _uiTongueExistingShapeWeightSlider?.SetValueWithoutNotify(_tongueExistingLiftShapeWeight);
            _uiTongueExistingShapeWeightValue?.SetValueWithoutNotify(_tongueExistingLiftShapeWeight);
            _uiTongueMoveSlider?.SetValueWithoutNotify(_tongueMoveAmount);
            _uiTongueMoveValue?.SetValueWithoutNotify(_tongueMoveAmount);
            _uiTongueLiftAxisField?.SetValueWithoutNotify(_tongueLiftAxis);
            _uiTongueDetectThresholdSlider?.SetValueWithoutNotify(_tongueDetectThresholdMm);
            _uiTongueDetectThresholdValue?.SetValueWithoutNotify(_tongueDetectThresholdMm);
            _uiTongueLipExcludeThresholdSlider?.SetValueWithoutNotify(_tongueLipExcludeThresholdMm);
            _uiTongueLipExcludeThresholdValue?.SetValueWithoutNotify(_tongueLipExcludeThresholdMm);
            _uiTongueExcludeTeethFromPrimaryToggle?.SetValueWithoutNotify(_tongueExcludeTeethFromPrimary);
            _uiTongueUnitOverrideToggle?.SetValueWithoutNotify(_tongueUnitOverride > 0f);
            _uiTongueUnitOverrideField?.SetValueWithoutNotify(_tongueUnitOverride);
            if (_uiTongueUnitOverrideField != null)
                _uiTongueUnitOverrideField.style.display = _tongueUnitOverride > 0f ? DisplayStyle.Flex : DisplayStyle.None;
            _uiTonguePreviewToggle?.SetValueWithoutNotify(_showTonguePreview);
            _uiTonguePreviewSizeSlider?.SetValueWithoutNotify(_tonguePreviewPointScale);
            _uiTonguePreviewSizeValue?.SetValueWithoutNotify(_tonguePreviewPointScale);
            if (_uiTongueDetail != null)
                _uiTongueDetail.style.display = _generateTongueAssistShapes ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiTongueExistingShapeDetail != null)
                _uiTongueExistingShapeDetail.style.display =
                    _tongueLiftSource == TongueLiftSource.ExistingShapeKey ? DisplayStyle.Flex : DisplayStyle.None;
            if (_uiTongueAutoDetectDetail != null)
                _uiTongueAutoDetectDetail.style.display =
                    _tongueLiftSource == TongueLiftSource.AutoDetect ? DisplayStyle.Flex : DisplayStyle.None;
            UpdateTonguePreview();

            _uiBlinkEffectToggle?.SetValueWithoutNotify(_addBlinkEffect);
            _uiBlinkEffectClipField?.SetValueWithoutNotify(_blinkEffectClip);
            if (_uiBlinkEffectDetail != null)
                _uiBlinkEffectDetail.style.display = _addBlinkEffect ? DisplayStyle.Flex : DisplayStyle.None;

            _uiOutputField?.SetValueWithoutNotify(_outputFolder ?? "");
            RefreshInstallResultUI();

            bool canInstall = _avatarPrefab != null
                              && _smrPaths.Length > 0
                              && _shapeNames.Length > 0
                              && IsValidAssetsFolder(_outputFolder);
            _uiInstallButton?.SetEnabled(canInstall);

            RefreshReadyToInstallChips();

            // Unityエディタのテーマ(Dark/Light)を切り替えると、ObjectField/TextField/PopupField
            // 等の組み込みコントロールが持つ「フィールド名ラベル」部分(Avatar・Profile等)が、
            // エディタのテーマに応じた色を自動的に継承してしまう。NK Installer自体は常に固定の
            // 暗い背景色を使っているため、Lightテーマ時にラベルの文字色が黒に近くなり、
            // 暗い背景に溶け込んで読みにくくなる。動的に追加される行(にっこり目・ジェスチャー等)
            // にも同じ問題が起こりうるため、リフレッシュのたびに再適用する。
            ForceThemeIndependentTextColors();
        }

        /// <summary>
        /// Unityエディタのテーマ(Dark/Light)に依存せず、NK Installer自身の固定の暗い背景色に
        /// 対して常に読みやすい色になるよう、対象要素の文字色を明示的に上書きする。
        /// </summary>
        private void ForceThemeIndependentTextColors()
        {
            var labelColor = new Color(0.82f, 0.82f, 0.85f); // 固定の背景色に対して常に読みやすい明るいグレー
            // ObjectField/TextField/PopupField等、BaseField系コントロールが内部に持つ
            // 「フィールド名」ラベル部分は、すべてUnity標準の"unity-base-field__label"クラスを持つ。
            var fieldLabels = rootVisualElement.Query<Label>(className: "unity-base-field__label").ToList();
            foreach (var label in fieldLabels)
                label.style.color = labelColor;

            // Foldout(「詳細設定」「診断・技術情報」等)は、内部的にBaseFieldではなく専用の
            // クラスでタイトル文字を表示しているため、上記とは別に対応が必要。Unityのバージョンに
            // よって内部クラス名が異なる可能性があるため、想定される候補をまとめて対象にする。
            foreach (var className in new[] { "unity-foldout__text", "unity-toggle__text" })
            {
                var foldoutLabels = rootVisualElement.Query<Label>(className: className).ToList();
                foreach (var label in foldoutLabels)
                    label.style.color = labelColor;
            }

            // MakeCardCollapsibleで自前追加している開閉シェブロン(▶/▼)も、色を明示していないと
            // 同様にテーマ依存の暗い色になってしまうため、あわせて固定する。
            var chevrons = rootVisualElement.Query<Label>(className: "card-collapse-chevron").ToList();
            foreach (var chevron in chevrons)
                chevron.style.color = labelColor;

            // Unity標準Foldoutの開閉矢印アイコン(「詳細設定」「診断・技術情報」等)は、文字色
            // ではなく背景画像のtintColorで色が決まるため、上記のLabel向けの対応とは別に
            // 対応が必要。Unityのバージョンによって内部クラス名が異なる可能性があるため、
            // 想定される候補をまとめて対象にする。
            foreach (var className in new[] { "unity-foldout__checkmark", "unity-toggle__checkmark" })
            {
                var checkmarks = rootVisualElement.Query<VisualElement>(className: className).ToList();
                foreach (var checkmark in checkmarks)
                    checkmark.style.unityBackgroundImageTintColor = labelColor;
            }

            if (_uiProfileReadyBannerLabel != null)
                _uiProfileReadyBannerLabel.style.color = labelColor;
        }

        /// <summary>
        /// フッターの「Ready to install」タグ行を再構築する。にっこり目・ジェスチャー抑制
        /// リストの追加/削除、各種トグルの変更など、footerタグに影響しうる操作の後は
        /// 必ずこのメソッドを直接呼ぶ(RefreshToolkitUI全体を経由しなくても最新化されるように)。
        /// </summary>
        /// <summary>
        /// カード左端の色を、そのカードで値が設定されているかどうかに応じて
        /// accent-primary(水色)/accent-unset(グレー)に切り替える。
        /// にっこり目・ジェスチャーの追加/削除ボタンなど、RefreshToolkitUI全体を
        /// 経由しない操作の後にも直接呼べるよう独立したメソッドにしている。
        /// </summary>
        /// <summary>
        /// UI ToolkitのObjectFieldは値を選択すると既定で「ファイル名 (型名)」という
        /// 表示になる。ファイル名自体に既にProfileだと分かる文字列(ARKitFTProfile_...)が
        /// 含まれているため、値が選択されている間は末尾の型名表記を消し、ファイル名だけを
        /// 表示する。値が空(None)のときの「None (型名)」表示はそのまま残す。
        /// ObjectField内部の表示用Labelを直接書き換えるため、値が変わるたびに
        /// (RefreshToolkitUIの中で)呼び直す必要がある。
        /// </summary>
        private static void SimplifyObjectFieldDisplay(ObjectField field, UnityEngine.Object value)
        {
            if (field == null || value == null) return;
            var label = field.Q<Label>(className: "unity-object-field-display__label");
            if (label != null) label.text = value.name;
        }

        private void RefreshCardAccents(bool hasAvatar)
        {
            _uiAvatarCard?.EnableInClassList("accent-unset", !hasAvatar);
            _uiFaceCard?.EnableInClassList("accent-unset", _smrPaths.Length == 0);
            _uiSquintCard?.EnableInClassList("accent-unset", _squintShapeIndices.Count == 0);
            _uiGestureCard?.EnableInClassList("accent-unset", _gestureLayerIndices.Count == 0);
            _uiMouthCard?.EnableInClassList("accent-unset", !_generateVisemeCompensation);
            _uiEyesCard?.EnableInClassList("accent-unset", !_generateEyeLookShapes);
            _uiAssistCard?.EnableInClassList("accent-unset", !_generateBrowAssistShapes);
            _uiTongueCard?.EnableInClassList("accent-unset", !_generateTongueAssistShapes);
            _uiBlinkEffectCard?.EnableInClassList("accent-unset", !_addBlinkEffect);
            _uiInstallResultCard?.EnableInClassList("accent-unset", _lastInstallResult == null);

            // 初見の分かりやすさのため各カードは既定で折りたたんでいるが、実際に確認・対応が
            // 必要な警告(不足シェイプキー・未選択の抑制レイヤー等)がある場合は、見落とし防止の
            // ため自動的に開く(ユーザーが手動で閉じたカードは対象外。SetCardAutoExpanded参照)。
            SetCardAutoExpanded(_uiFaceCard, hasAvatar && _missingArkitShapes.Count > 0);
            SetCardAutoExpanded(_uiGestureCard, hasAvatar && _gestureLayerIndices.Count == 0);
        }

        private void RefreshReadyToInstallChips()
        {
            if (_uiReadyStatusRow == null) return;

            // アバター未選択の段階ではARKit判定やEyeLook/Viseme等の設定内容がまだ意味を
            // 持たない(すべて既定値の見かけ上の表示になってしまう)ため、誤解を招く
            // 個別タグは出さず、「まずアバターを選んでください」という案内のみ表示する。
            if (_avatarPrefab == null)
            {
                if (_uiReadyTitle != null) _uiReadyTitle.text = ArkitFTLoc.T("Avatarを選択してください");
                _uiReadyStatusRow.Clear();
                _uiReadyStatusRow.Add(MakeChip(ArkitFTLoc.T("Avatar未選択"), "neutral",
                    ArkitFTLoc.T("インストール対象のAvatarをまだ選択していません。")));
                return;
            }

            if (_uiReadyTitle != null) _uiReadyTitle.text = "Ready to install";

            _uiReadyStatusRow.Clear();
            if (_smrPaths.Length == 0)
            {
                // Face Meshが見つかっていない段階では、ARKitシェイプの不足チェック自体が
                // 行われていない(_missingArkitShapes.Count==0はチェック未実施を意味するに
                // すぎない)。ここで判定してしまうと誤って緑色の「✓ ARKit ready」表示に
                // なってしまうため、専用の警告チップを出す。
                _uiReadyStatusRow.Add(MakeChip(ArkitFTLoc.T("⚠ Face Mesh未検出"), "warning",
                    ArkitFTLoc.T("アバターにSkinnedMeshRendererが見つかりません。FACEカードでFace Meshを確認してください。")));
            }
            else if (!_ueFallbackEnabled)
            {
                // UEフォールバックがONの場合は、最終的にUE代替で解決されるため
                // (「UE代替 N件」チップと重複するので)ここでは表示しない。
                var arkitReadyChip = MakeChip(
                    _missingArkitShapes.Count == 0 ? "✓ ARKit ready" : $"⚠ Missing {_missingArkitShapes.Count}",
                    _missingArkitShapes.Count == 0 ? "ok" : "warning",
                    _missingArkitShapes.Count == 0
                        ? ArkitFTLoc.T("ARKit標準52シェイプキーがすべてメッシュ上に存在します。")
                        : null);
                if (_missingArkitShapes.Count > 0)
                    arkitReadyChip.tooltip = ArkitFTLoc.T("メッシュに存在しないARKitシェイプキー:\n") + string.Join("\n", _missingArkitShapes);
                _uiReadyStatusRow.Add(arkitReadyChip);
            }
            else if (_ueFallbackEnabled)
            {
                // ARKit検出数の代わりに、UE代替で解決できた件数を表示する。
                var ueReadyChip = MakeChip(
                    string.Format(ArkitFTLoc.T("UE代替 {0}件"), _ueFallbackResolvedShapes.Count),
                    _ueFallbackResolvedShapes.Count > 0 ? "ok" : "neutral",
                    _ueFallbackResolvedShapes.Count > 0
                        ? ArkitFTLoc.T("ARKit標準名では見つからなかったが、UE代替名で解決できたシェイプ:\n") +
                          string.Join("\n", _ueFallbackResolvedShapes.Select(kv =>
                              $"{kv.Key} → {string.Join(" + ", kv.Value)}"))
                        : null);
                _uiReadyStatusRow.Add(ueReadyChip);
            }
            if (!_ueFallbackEnabled && _emptyArkitShapes.Count > 0)
            {
                var emptyChip = MakeChip($"Empty {_emptyArkitShapes.Count}", "face");
                // 具体的なシェイプ名はチップ上のツールチップで確認できるようにする
                // (フッターは常に狭いスペースのため、詳細は名前一覧をホバー表示に留める)。
                emptyChip.tooltip = ArkitFTLoc.T("中身が空 / 未検出のARKitシェイプキー:\n") + string.Join("\n", _emptyArkitShapes);
                _uiReadyStatusRow.Add(emptyChip);
            }

            if (_estimatedParamBitsOverBudget)
            {
                var paramChip = MakeChip(
                    string.Format(ArkitFTLoc.T("⚠ Parameter超過のおそれ {0}/{1}bit"),
                        _estimatedTotalParamBits, VRC_PARAM_BIT_BUDGET),
                    "warning");
                paramChip.tooltip = string.Format(ArkitFTLoc.T(
                    "既存 {0}bit + FT追加分(最適化前) {1}bit = {2}bit で、上限{3}bitを超える可能性があります。\n" +
                    "「空 / 未検出シェイプの同期をオフにする」を有効にすると実際の使用量を抑えられます。\n" +
                    "正確な値はインストール実行時に確定します。\n\n" +
                    "注意: Modular Avatar / NDMFの非破壊コンポーネント(トグル等)がパラメータを追加する場合、\n" +
                    "それらはVRChat SDKでのビルド時に初めて確定するため、Manual Bake前はこの見積りに\n" +
                    "含まれません。Manual Bake後の値が最も正確です。"),
                    _estimatedExistingParamBits, _estimatedFtParamBits,
                    _estimatedTotalParamBits, VRC_PARAM_BIT_BUDGET);
                _uiReadyStatusRow.Add(paramChip);
            }

            if (!_avatarEyeLookConfigured)
            {
                var eyeLookChip = MakeChip(ArkitFTLoc.T("⚠ EyeLook未設定"), "warning", _avatarEyeLookProblem);
                _uiReadyStatusRow.Add(eyeLookChip);
            }

            if (!_avatarVisemeConfigured)
            {
                var visemeChip = MakeChip(ArkitFTLoc.T("⚠ Viseme未設定"), "warning", _avatarVisemeProblem);
                _uiReadyStatusRow.Add(visemeChip);
            }

            // にっこり目: 未指定でも動作はする(ARKit eyeSquintへフォールバック)ため
            // warningではなくneutralで「未設定」を伝える。件数はカードで確認できるため
            // フッターでは表示しない(タグは一目で状態が分かることを優先)。
            _uiReadyStatusRow.Add(_squintShapeIndices.Count > 0
                ? MakeChip(ArkitFTLoc.T("にっこり目"), "smile", ArkitFTLoc.T("「にっこり目」シェイプキーが指定されています。"))
                : MakeChip(ArkitFTLoc.T("にっこり目未設定 (EyeSquintで代替)"), "neutral",
                    ArkitFTLoc.T("未指定のため、ARKitのeyeSquintLeft・eyeSquintRightがそのまま使われます。")));

            // ジェスチャー抑制レイヤー: 未設定だとFT中にジェスチャー表情が混入し続けるため、
            // 本来は設定してほしい項目としてwarningで明示する。
            _uiReadyStatusRow.Add(_gestureLayerIndices.Count > 0
                ? MakeChip(ArkitFTLoc.T("ジェスチャー表情抑制"), "gesture", ArkitFTLoc.T("指定したFXレイヤーをフェイストラッキング中は抑制します。"))
                : MakeChip(ArkitFTLoc.T("⚠ 抑制レイヤー未選択"), "warning",
                    ArkitFTLoc.T("抑制レイヤーが未選択です。フェイストラッキング中もジェスチャー/メニュー表情が混ざります。")));

            if (_generateEyeLookShapes)
            {
                _uiReadyStatusRow.Add(MakeChip(ArkitFTLoc.T("EyeLook"), "eyes", ArkitFTLoc.T("EyeLookシェイプキーを自動生成します。")));
                // Eye Look競合対策(Compatibility/Stable)はEyeLookシェイプキー生成が
                // ONのときだけカード内に表示されるため、フッタータグも連動させる。
                _uiReadyStatusRow.Add(MakeChip(
                    _disableNativeEyeLook ? "Stable Eye Mode" : "Compatibility Eye Mode",
                    _disableNativeEyeLook ? "ok" : "neutral",
                    _disableNativeEyeLook
                        ? ArkitFTLoc.T("AvatarDescriptorのEye Lookを無効化し、VRChat標準との競合を回避します。")
                        : ArkitFTLoc.T("VRChat標準のEye Lookを維持します。アバターによってはFT中に競合する場合があります。")));
            }
            if (_generateVisemeCompensation)
                _uiReadyStatusRow.Add(MakeChip(ArkitFTLoc.T("Viseme"), "mouth", ArkitFTLoc.T("逆Viseme補償シェイプキーを生成します。")));
            if (_generateBrowAssistShapes)
                _uiReadyStatusRow.Add(MakeChip(ArkitFTLoc.T("眉アシスト"), "assist", ArkitFTLoc.T("まばたき連動の眉アシストシェイプキーを生成します。")));
            if (!_addBlinkEffect)
            {
                _uiReadyStatusRow.Add(MakeChip(ArkitFTLoc.T("まばたきエフェクトなし"), "neutral",
                    ArkitFTLoc.T("まばたき時のおまけ演出を含めずにインストールします。")));
            }
            else if (_blinkEffectClip != null)
            {
                _uiReadyStatusRow.Add(MakeChip(ArkitFTLoc.T("まばたきエフェクト"), "eyes",
                    ArkitFTLoc.T("指定したクリップをまばたき時のおまけ演出として使用します。")));
            }
            else
            {
                _uiReadyStatusRow.Add(MakeChip(ArkitFTLoc.T("⚠ エフェクトクリップ未設定"), "warning",
                    ArkitFTLoc.T("クリップが未指定のため、まばたき時に演出は再生されません。")));
            }
        }

        private void RefreshStatusChips()
        {
            if (_uiAvatarStatusRow != null)
            {
                _uiAvatarStatusRow.Clear();
                if (_avatarPrefab == null)
                {
                    _uiAvatarStatusRow.Add(MakeChip(ArkitFTLoc.T("Avatar未選択"), "neutral",
                        ArkitFTLoc.T("インストール対象のAvatarをまだ選択していません。")));
                }
                else
                {
                    _uiAvatarStatusRow.Add(MakeChip(ArkitFTLoc.T("✓ Avatar"), "ok", ArkitFTLoc.T("Avatarが選択されています。")));
                    if (_missingArkitShapes.Count == 0 && _smrPaths.Length > 0)
                        _uiAvatarStatusRow.Add(MakeChip("✓ ARKit 52", "ok",
                            ArkitFTLoc.T("ARKit標準52シェイプキーがすべてメッシュ上に存在します。")));
                    else if (_smrPaths.Length > 0)
                    {
                        var missingChip = MakeChip($"⚠ Missing {_missingArkitShapes.Count}", "warning");
                        missingChip.tooltip = ArkitFTLoc.T("メッシュに存在しないARKitシェイプキー:\n") + string.Join("\n", _missingArkitShapes);
                        _uiAvatarStatusRow.Add(missingChip);
                    }

                    if (_eyeTrackingControlLayerNames.Count > 0)
                    {
                        var eyeConflictChip = MakeChip(
                            string.Format(ArkitFTLoc.T("⚠ Eye競合 {0}"), _eyeTrackingControlLayerNames.Count), "warning");
                        eyeConflictChip.tooltip =
                            ArkitFTLoc.T("EyesをTrackingへ切り替えるFXレイヤー") +
                            ArkitFTLoc.T("(ジェスチャー切替のたびに再発火し、VRChat標準の目線制御へ一時的に") +
                            ArkitFTLoc.T("戻ってしまう可能性があります):\n") +
                            string.Join("\n", _eyeTrackingControlLayerNames);
                        _uiAvatarStatusRow.Add(eyeConflictChip);
                    }

                    if (!_avatarEyeLookConfigured)
                    {
                        var eyeLookChip = MakeChip(ArkitFTLoc.T("⚠ EyeLook未設定"), "warning", _avatarEyeLookProblem);
                        _uiAvatarStatusRow.Add(eyeLookChip);
                    }

                    if (!_avatarVisemeConfigured)
                    {
                        var visemeChip = MakeChip(ArkitFTLoc.T("⚠ Viseme未設定"), "warning", _avatarVisemeProblem);
                        _uiAvatarStatusRow.Add(visemeChip);
                    }

                    if (_estimatedParamBitsOverBudget)
                    {
                        var paramChip = MakeChip(
                            string.Format(ArkitFTLoc.T("⚠ Parameter超過のおそれ {0}/{1}bit"),
                                _estimatedTotalParamBits, VRC_PARAM_BIT_BUDGET),
                            "warning");
                        paramChip.tooltip = string.Format(ArkitFTLoc.T(
                            "既存 {0}bit + FT追加分(最適化前) {1}bit = {2}bit で、上限{3}bitを超える可能性があります。\n" +
                            "「空 / 未検出シェイプの同期をオフにする」を有効にすると実際の使用量を抑えられます。\n" +
                            "正確な値はインストール実行時に確定します。\n\n" +
                            "注意: Modular Avatar / NDMFの非破壊コンポーネント(トグル等)がパラメータを追加する場合、\n" +
                            "それらはVRChat SDKでのビルド時に初めて確定するため、Manual Bake前はこの見積りに\n" +
                            "含まれません。Manual Bake後の値が最も正確です。"),
                            _estimatedExistingParamBits, _estimatedFtParamBits,
                            _estimatedTotalParamBits, VRC_PARAM_BIT_BUDGET);
                        _uiAvatarStatusRow.Add(paramChip);
                    }
                }
            }

            if (_uiFaceStatusRow != null)
            {
                _uiFaceStatusRow.Clear();
                if (_smrPaths.Length == 0)
                {
                    _uiFaceStatusRow.Add(MakeChip(ArkitFTLoc.T("Face Mesh未検出"), "warning",
                        ArkitFTLoc.T("アバターにSkinnedMeshRendererが見つかりません。")));
                    return;
                }

                // UEフォールバックがONの場合、最終的にはUE代替で解決されるため
                // (_missingArkitShapesはUE代替で解決できたものを除外済み)、
                // このARKit検出数チップは「UE代替 N件」チップと重複する情報になり
                // 意味が薄れる。ONのときは非表示にする。
                if (!_ueFallbackEnabled)
                {
                    var faceMissingChip = MakeChip(
                        _missingArkitShapes.Count == 0 ? "✓ 52 shapes" : $"Missing {_missingArkitShapes.Count}",
                        _missingArkitShapes.Count == 0 ? "ok" : "warning",
                        _missingArkitShapes.Count == 0
                            ? ArkitFTLoc.T("ARKit標準52シェイプキーがすべてメッシュ上に存在します。")
                            : null);
                    if (_missingArkitShapes.Count > 0)
                        faceMissingChip.tooltip = ArkitFTLoc.T("メッシュに存在しないARKitシェイプキー:\n") + string.Join("\n", _missingArkitShapes);
                    _uiFaceStatusRow.Add(faceMissingChip);
                }

                if (!_ueFallbackEnabled && _emptyArkitShapes.Count > 0)
                {
                    var emptyStatusChip = MakeChip(string.Format(ArkitFTLoc.T("Empty / 未検出 {0}"), _emptyArkitShapes.Count), "neutral");
                    emptyStatusChip.tooltip = ArkitFTLoc.T("中身が空 / 未検出のARKitシェイプキー:\n") + string.Join("\n", _emptyArkitShapes);
                    _uiFaceStatusRow.Add(emptyStatusChip);
                }

                bool readable = _smrIndex < _smrs.Length
                                && _smrs[_smrIndex]?.sharedMesh != null
                                && _smrs[_smrIndex].sharedMesh.isReadable;
                if (!readable)
                    _uiFaceStatusRow.Add(MakeChip("Read/Write OFF", "warning",
                        ArkitFTLoc.T("対象メッシュのRead/Writeが無効なため、「中身が空」のシェイプキー検出ができません。\n" +
                        "Import SettingsでRead/Writeを有効にしてください。")));

                if (_ueFallbackEnabled && _ueFallbackResolvedShapes.Count > 0)
                {
                    var ueChip = MakeChip(
                        string.Format(ArkitFTLoc.T("UE代替 {0}件"), _ueFallbackResolvedShapes.Count),
                        "ok",
                        ArkitFTLoc.T("ARKit標準名では見つからなかったが、UE代替名で解決できたシェイプ:\n") +
                        string.Join("\n", _ueFallbackResolvedShapes.Select(kv =>
                            $"{kv.Key} → {string.Join(" + ", kv.Value)}")));
                    _uiFaceStatusRow.Add(ueChip);
                }

                if (_disableSyncForEmptyShapes)
                    _uiFaceStatusRow.Add(MakeChip(
                        _shapeParameterMap != null ? "✓ Param Map" : "△ Partial Match",
                        _shapeParameterMap != null ? "ok" : "warning",
                        _shapeParameterMap != null
                            ? ArkitFTLoc.T("対応表(ARKit_FT_ShapeParamMap.asset)を参照し、1対1対応が確認できたシェイプのみ同期をオフにします。")
                            : ArkitFTLoc.T("対応表が見つからないため、パラメータ名の部分一致で判定します(やや不正確です)。")));
            }
        }

        private void RefreshEyeConflictCard()
        {
            if (_uiEyeConflictBox == null || _uiEyeConflictText == null) return;

            _uiEyeConflictBox.RemoveFromClassList("warning-card-safe");
            _uiEyeConflictBox.RemoveFromClassList("warning-card-alert");

            if (_eyeTrackingControlLayerNames.Count > 0)
            {
                _uiEyeConflictBox.style.display = DisplayStyle.Flex;

                if (_disableNativeEyeLook)
                {
                    // StableモードではAvatarDescriptor Eye Look自体を無効化するため、
                    // 元FX側のEyes=Trackingが発火してもVRChat標準Eye Lookとの競合は発生しない。
                    _uiEyeConflictBox.AddToClassList("warning-card-safe");
                    _uiEyeConflictText.text =
                        string.Format(ArkitFTLoc.T("✓ TrackingControl競合候補 {0}件\n"), _eyeTrackingControlLayerNames.Count)
                        + string.Join(" / ", _eyeTrackingControlLayerNames)
                        + ArkitFTLoc.T("\nStableモードが有効なため、TrackingControlが発火しても実害はありません。");
                }
                else
                {
                    _uiEyeConflictBox.AddToClassList("warning-card-alert");
                    _uiEyeConflictText.text =
                        string.Format(ArkitFTLoc.T("⚠ TrackingControl競合候補 {0}件\n"), _eyeTrackingControlLayerNames.Count)
                        + string.Join(" / ", _eyeTrackingControlLayerNames)
                        + ArkitFTLoc.T("\nStableモードを選択すると、VRChat標準Eye Lookとの競合を根本的に回避できます。");
                }
            }
            else if (_fxLayerNames.Length > 0)
            {
                _uiEyeConflictBox.style.display = DisplayStyle.Flex;
                _uiEyeConflictBox.AddToClassList("warning-card-safe");
                _uiEyeConflictText.text =
                    ArkitFTLoc.T("✓ Eye TrackingControlの競合候補は見つかりませんでした。");
            }
            else
            {
                _uiEyeConflictBox.style.display = DisplayStyle.None;
            }
        }

        // ── Profile 読み書き ─────────────────────────────

        /// <summary>
        /// 現在の _profile の内容を、現在の状態(_avatarPrefab / _smrPaths / _shapeNames等)に対して適用する。
        /// Profile変更時とAvatar変更時の両方から呼べるように共通化している
        /// (どちらを先に設定しても、後から設定した方の変更でもう片方の選択がリセットされないようにするため)。
        /// </summary>
        private void ApplyProfileSelections()
        {
            if (_profile == null) return;

            _avatarMatchTag = _profile.avatarMatchTag ?? "";
            _profileShopName = _profile.shopName ?? "";
            _profileVersionName = _profile.versionName ?? "";

            // アバターに依存しない項目は常に適用
            _generateVisemeCompensation = _profile.generateVisemeCompensation;
            _visemeScale = _profile.visemeScale;
            _generateEyeLookShapes = _profile.generateEyeLookShapes;
            _blinkControlMode = _profile.blinkControlMode;
            _generateBrowAssistShapes = _profile.generateBrowAssistShapes;
            _browAssistIntensity = _profile.browAssistIntensity;
            _generateTongueAssistShapes = _profile.generateTongueAssistShapes;
            _tongueLiftSource = _profile.tongueLiftSource;
            _tongueExistingLiftShapeName = _profile.tongueExistingLiftShapeName;
            _tongueExistingLiftShapeWeight = _profile.tongueExistingLiftShapeWeight;
            _tongueMoveAmount = _profile.tongueMoveAmount;
            _tongueLiftAxis = _profile.tongueLiftAxis;
            _tongueDetectThresholdMm = _profile.tongueDetectThresholdMm;
            _tongueLipExcludeThresholdMm = _profile.tongueLipExcludeThresholdMm;
            _tongueExcludeTeethFromPrimary = _profile.tongueExcludeTeethFromPrimary;
            _tongueUnitOverride = _profile.tongueUnitOverride;
            _showTonguePreview = _profile.showTonguePreview;
            _tonguePreviewPointScale = _profile.tonguePreviewPointScale;
            _addBlinkEffect = _profile.addBlinkEffect;
            _blinkEffectClip = _profile.blinkEffectClip;
            _eyeLookIntensity = _profile.eyeLookIntensity;
            _disableNativeEyeLook = _profile.disableNativeEyeLook;
            _outputFolder = _profile.outputFolder;

            if (_avatarPrefab == null) return;

            // SMRパスからSMRリスト内のインデックスを解決
            int idx = Array.IndexOf(_smrPaths, _profile.faceSMRPath);
            if (idx >= 0)
            {
                _smrIndex = idx;
                _faceSmrPathMismatch = false;
                RefreshShapeList(); // ここで _squintShapeIndices は一旦空にリセットされる
            }
            else if (!string.IsNullOrEmpty(_profile.faceSMRPath))
            {
                // Profileにパスは保存されているのに、現在のアバター上には見つからなかった
                // (アバターの階層が変わった等)。ここで黙って0番目の候補にフォールバック
                // すると、無関係なメッシュが誤って選ばれたまま気づかれない恐れがあるため、
                // はっきり警告してユーザーに手動選択を促す。
                _faceSmrPathMismatch = true;
                Debug.LogWarning($"[hinzka ARKit FT] Profileに保存されたFace Mesh " +
                                  $"('{_profile.faceSMRPath}')が現在のアバター上に見つかりませんでした。" +
                                  "アバターの階層が変わった可能性があります。FACEカードでFace Meshを選び直してください。");
            }
            else
            {
                _faceSmrPathMismatch = false;
            }

            _arkitShapePrefix = _profile.arkitShapePrefix ?? "";
            _hasBlendshapePrefix = !string.IsNullOrEmpty(_arkitShapePrefix);
            _ueFallbackEnabled = _profile.ueFallbackEnabled;
            _disableSyncForEmptyShapes = _profile.disableSyncForEmptyShapes;
            RefreshArkitCheck();

            _leftEyeConstraintTarget = ResolveTransformFromPath(_profile.leftEyeConstraintPath);
            _rightEyeConstraintTarget = ResolveTransformFromPath(_profile.rightEyeConstraintPath);
            if (_leftEyeConstraintTarget != null || _rightEyeConstraintTarget != null)
                _eyeUsesConstraint = true;

            _eyeSmrSeparate = _profile.eyeSmrSeparate;
            if (_eyeSmrSeparate && !string.IsNullOrEmpty(_profile.eyeSMRPath))
            {
                int eyeIdx = Array.IndexOf(_smrPaths, _profile.eyeSMRPath);
                if (eyeIdx >= 0) _eyeSmrIndex = eyeIdx;
            }

            // にっこり目: シェイプキー名 → インデックスに変換
            _squintShapeIndices.Clear();
            if (_profile.squintShapeNames != null)
            {
                foreach (var name in _profile.squintShapeNames)
                {
                    int shapeIdx = Array.IndexOf(_shapeNames, name);
                    if (shapeIdx >= 0) _squintShapeIndices.Add(shapeIdx);
                    else Debug.LogWarning($"[hinzka ARKit FT] Profileのにっこり目シェイプキー '{name}' は現在のFace SMRに見つからないためスキップしました。");
                }
            }
            // 目線シェイプキー生成時に有効化する追加シェイプキー: シェイプキー名 → インデックスに変換
            // (Face SMRとEye SMRが別々の場合、実際にベイクが行われるSMR側の一覧を使う)
            _useEyeLookBakeShapes = _profile.useEyeLookBakeShapes;
            RefreshEyeLookBakeShapeNamesCache(); // 呼び出し順序に依存せず必ず最新の状態を参照する
            _eyeLookBakeShapeIndices.Clear();
            if (_profile.eyeLookBakeShapeNames != null)
            {
                var eyeLookBakeShapeNamesForLookup = _eyeLookBakeShapeNamesCache;
                foreach (var name in _profile.eyeLookBakeShapeNames)
                {
                    int shapeIdx = Array.IndexOf(eyeLookBakeShapeNamesForLookup, name);
                    if (shapeIdx >= 0) _eyeLookBakeShapeIndices.Add(shapeIdx);
                    else Debug.LogWarning($"[hinzka ARKit FT] Profileの目線シェイプキー生成用の追加シェイプキー '{name}' は現在のSMRに見つからないためスキップしました。");
                }
            }
            // ジェスチャーレイヤー: 新Profileでは名前で復元。旧Profileはindexへフォールバック。
            _gestureLayerIndices.Clear();
            var savedGestureNames = GetProfileGestureLayerNames(_profile);
            if (savedGestureNames.Count > 0)
            {
                foreach (var layerName in savedGestureNames)
                {
                    int layerIdx = Array.IndexOf(_fxLayerNames, layerName);
                    if (layerIdx >= 0) _gestureLayerIndices.Add(layerIdx);
                    else Debug.LogWarning($"[hinzka ARKit FT] Profileのジェスチャーレイヤー '{layerName}' は現在のFXに見つからないためスキップしました。");
                }
            }
            else
            {
                var legacyIndices = _profile.gestureLayerIndices ?? new List<int>();
                _gestureLayerIndices = new List<int>(legacyIndices
                    .Where(i => i >= 0 && i < _fxLayerNames.Length)
                    .Distinct());
            }
        }

        private void SaveToProfile()
        {
            if (_profile == null) return;

            _profile.avatarMatchTag = _avatarMatchTag ?? "";
            _profile.shopName = _profileShopName ?? "";
            _profile.versionName = _profileVersionName ?? "";

            _profile.faceSMRPath = _smrIndex < _smrPaths.Length ? _smrPaths[_smrIndex] : "";
            _profile.arkitShapePrefix = _arkitShapePrefix;
            _profile.ueFallbackEnabled = _ueFallbackEnabled;
            _profile.disableSyncForEmptyShapes = _disableSyncForEmptyShapes;
            _profile.leftEyeConstraintPath = (_avatarPrefab != null && _leftEyeConstraintTarget != null)
                ? GetRelativePath(_avatarPrefab.transform, _leftEyeConstraintTarget) : "";
            _profile.rightEyeConstraintPath = (_avatarPrefab != null && _rightEyeConstraintTarget != null)
                ? GetRelativePath(_avatarPrefab.transform, _rightEyeConstraintTarget) : "";
            _profile.eyeSmrSeparate = _eyeSmrSeparate;
            _profile.eyeSMRPath = (_eyeSmrSeparate && _eyeSmrIndex < _smrPaths.Length) ? _smrPaths[_eyeSmrIndex] : "";

            // にっこり目: インデックス → シェイプキー名に変換して保存
            _profile.squintShapeNames = _squintShapeIndices
                .Where(i => i >= 0 && i < _shapeNames.Length)
                .Select(i => _shapeNames[i])
                .Distinct()
                .ToList();

            // 目線シェイプキー生成時に有効化する追加シェイプキー: インデックス → シェイプキー名に変換して保存
            // (Face SMRとEye SMRが別々の場合、実際にベイクが行われるSMR側の一覧を使う)
            _profile.useEyeLookBakeShapes = _useEyeLookBakeShapes;
            var eyeLookBakeShapeNamesForSave = _eyeLookBakeShapeNamesCache;
            _profile.eyeLookBakeShapeNames = _eyeLookBakeShapeIndices
                .Where(i => i >= 0 && i < eyeLookBakeShapeNamesForSave.Length)
                .Select(i => eyeLookBakeShapeNamesForSave[i])
                .Distinct()
                .ToList();

            _profile.gestureLayerIndices = new List<int>(_gestureLayerIndices.Distinct()); // 旧Profile互換
            SetProfileGestureLayerNames(_profile, _gestureLayerIndices
                .Where(i => i >= 0 && i < _fxLayerNames.Length)
                .Select(i => _fxLayerNames[i])
                .Distinct()
                .ToList());
            _profile.generateVisemeCompensation = _generateVisemeCompensation;
            _profile.visemeScale = _visemeScale;
            _profile.generateEyeLookShapes = _generateEyeLookShapes;
            _profile.blinkControlMode = _blinkControlMode;
            _profile.generateBrowAssistShapes = _generateBrowAssistShapes;
            _profile.browAssistIntensity = _browAssistIntensity;
            _profile.generateTongueAssistShapes = _generateTongueAssistShapes;
            _profile.tongueLiftSource = _tongueLiftSource;
            _profile.tongueExistingLiftShapeName = _tongueExistingLiftShapeName;
            _profile.tongueExistingLiftShapeWeight = _tongueExistingLiftShapeWeight;
            _profile.tongueMoveAmount = _tongueMoveAmount;
            _profile.tongueLiftAxis = _tongueLiftAxis;
            _profile.tongueDetectThresholdMm = _tongueDetectThresholdMm;
            _profile.tongueLipExcludeThresholdMm = _tongueLipExcludeThresholdMm;
            _profile.tongueExcludeTeethFromPrimary = _tongueExcludeTeethFromPrimary;
            _profile.tongueUnitOverride = _tongueUnitOverride;
            _profile.showTonguePreview = _showTonguePreview;
            _profile.tonguePreviewPointScale = _tonguePreviewPointScale;
            _profile.addBlinkEffect = _addBlinkEffect;
            _profile.blinkEffectClip = _blinkEffectClip;
            _profile.eyeLookIntensity = _eyeLookIntensity;
            _profile.disableNativeEyeLook = _disableNativeEyeLook;
            _profile.outputFolder = _outputFolder;

            EditorUtility.SetDirty(_profile);
            AssetDatabase.SaveAssets();
            Debug.Log($"[hinzka ARKit FT] Profile saved: {AssetDatabase.GetAssetPath(_profile)}");
        }

        /// <summary>
        /// プロジェクト内に存在する全ARKitFTProfileアセットをドロップダウンメニューとして表示し、
        /// 選択したものをそのまま読み込む。ファイル選択ダイアログ(LoadExistingProfile)より
        /// 素早く切り替えられるよう、こちらを既存Profileボタンの既定動作にしている。
        ///
        /// ショップ名/作者名(表示専用メタデータ)が設定されているProfileは、その名前で
        /// サブメニューとしてグループ化して表示する(GenericMenuは項目名中の"/"をメニュー階層の
        /// 区切りとして扱う仕様を利用している)。このため、ショップ名自体に"/"が含まれている
        /// 場合は、意図しない階層分割を防ぐため全角スラッシュ("／")に置き換えてから使う。
        /// ショップ名が未設定のProfileは、グループ化せずメニュー直下に並べる。
        /// 各項目はクリックすると即座にそのProfileを読み込む(以前あった「識別タグを編集...」
        /// というサブメニューは、利用頻度が低いため廃止した)。
        /// </summary>
        private void ShowExistingProfileMenu()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(ARKitFTProfile));
            var menu = new GenericMenu();

            if (guids == null || guids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent(ArkitFTLoc.T("プロジェクト内にProfileが見つかりません")));
            }
            else
            {
                var entries = guids
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Select(path => (path, name: Path.GetFileNameWithoutExtension(path)))
                    .OrderBy(e => e.name, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var entry in entries)
                {
                    string capturedPath = entry.path;
                    bool isCurrent = _profile != null &&
                                      AssetDatabase.GetAssetPath(_profile) == capturedPath;

                    // ショップ名・バージョン名(表示専用メタデータ)を読み取る。
                    var previewProfile = AssetDatabase.LoadAssetAtPath<ARKitFTProfile>(capturedPath);
                    string shopName = previewProfile != null ? (previewProfile.shopName ?? "").Trim() : "";
                    string versionName = previewProfile != null ? (previewProfile.versionName ?? "").Trim() : "";

                    // ファイル名自体は変更せず、メニュー上の表示だけ定型の接頭辞を省略する
                    // (見やすさのため。ファイル名から接頭辞を外すとProfile名からのアバター名
                    // 推測ロジック(TryAutoSelectProfileForAvatar)に影響するため、表示専用の対応にとどめる)。
                    const string DISPLAY_PREFIX = "ARKitFTProfile_";
                    string profileLabel = entry.name.StartsWith(DISPLAY_PREFIX, StringComparison.Ordinal)
                        ? entry.name.Substring(DISPLAY_PREFIX.Length)
                        : entry.name;
                    if (string.IsNullOrEmpty(profileLabel)) profileLabel = entry.name; // 万一空になった場合のフォールバック
                    if (!string.IsNullOrEmpty(versionName))
                        profileLabel += $" [{SanitizeMenuPathSegment(versionName)}]";

                    string itemPath = string.IsNullOrEmpty(shopName)
                        ? SanitizeMenuPathSegment(profileLabel)
                        : $"{SanitizeMenuPathSegment(shopName)}/{SanitizeMenuPathSegment(profileLabel)}";

                    menu.AddItem(new GUIContent(itemPath), isCurrent, () =>
                    {
                        var loaded = AssetDatabase.LoadAssetAtPath<ARKitFTProfile>(capturedPath);
                        if (loaded != null) SetCurrentProfile(loaded, true);
                    });
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent(ArkitFTLoc.T("ファイルから選択...")), false, LoadExistingProfile);

            menu.ShowAsContext();
        }

        /// <summary>
        /// GenericMenuの項目名に使う文字列から、メニュー階層の区切りとして誤解釈されてしまう
        /// "/"を、見た目の近い全角スラッシュ("／")へ置き換える。
        /// </summary>
        private static string SanitizeMenuPathSegment(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace('/', '／');
        }

        /// <summary>
        /// 既存のARKitFTProfileアセットをファイル選択ダイアログから読み込む。
        /// ドロップダウンメニュー(ShowExistingProfileMenu)の「ファイルから選択...」から、
        /// または通常の検索範囲外にあるアセットを明示的に指定したい場合に使う。
        /// </summary>
        private void LoadExistingProfile()
        {
            // OpenFilePanelWithFilters はUnity/OSの組み合わせによって挙動差が出ることがあるため、
            // 単純な .asset 選択ダイアログを使い、FileUtilでProject相対パスへ変換する。
            string absolutePath = EditorUtility.OpenFilePanel(
                ArkitFTLoc.T("ARKit FT Profileを選択"),
                Application.dataPath,
                "asset");

            if (string.IsNullOrEmpty(absolutePath)) return;

            absolutePath = absolutePath.Replace('\\', '/');
            string assetPath = FileUtil.GetProjectRelativePath(absolutePath)?.Replace('\\', '/');

            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog(
                    ArkitFTLoc.T("Profileを読み込めません"),
                    ArkitFTLoc.T("UnityプロジェクトのAssetsフォルダ内にある .asset を選択してください。"),
                    "OK");
                return;
            }

            // まずMainAssetとして読み、型情報も確認する。
            // ScriptableObjectのscript GUIDが失われている場合も、ここで分かりやすく案内する。
            var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            var loaded = mainAsset as ARKitFTProfile;
            if (loaded == null)
            {
                string typeName = mainAsset != null ? mainAsset.GetType().FullName : ArkitFTLoc.T("読み込み不可 / Missing Scriptの可能性あり");
                EditorUtility.DisplayDialog(
                    ArkitFTLoc.T("Profileを読み込めません"),
                    ArkitFTLoc.T("選択した .asset をARKit FT Profileとして認識できませんでした。\n\n") +
                    $"Asset: {assetPath}\n" +
                    $"Detected Type: {typeName}\n\n" +
                    ArkitFTLoc.T("既存Profileでこの表示になる場合は、ARKitFTProfile.cs の .meta が作り直されていないか確認してください。") +
                    ArkitFTLoc.T("ScriptableObjectは .meta のGUIDでスクリプトと結び付いているため、.metaを変更すると既存ProfileがMissing Scriptになります。"),
                    ArkitFTLoc.T("OK"));
                return;
            }

            SetCurrentProfile(loaded, true);
        }

        /// <summary>
        /// UI / ファイル選択 / 新規作成のどの経路からでも、Profile設定処理を1か所に集約する。
        /// </summary>
        private void SetCurrentProfile(ARKitFTProfile profile, bool applySelections)
        {
            var previousProfile = _profile;
            _profile = profile;

            if (_profile == null)
            {
                // Profileが空欄に戻された場合、「適用済み」の記録も一緒にクリアする。
                // これを残したままだと、後で同じアバター×同じProfileの組み合わせが
                // 再度選択されたときに「既に適用済み」と誤判定され、実際には設定が
                // 何も反映されないまま(見た目だけProfileが選択された状態に)なってしまう。
                _lastAppliedProfile = null;
                _lastAppliedProfileAvatar = null;
            }

            if (_profile != null && applySelections)
            {
                // 保存されているFace Meshが現在のアバターと明らかに一致しない場合、
                // 「複数のProfileを次々開いて別の作業(タグ付け直しなど)をしていたら、
                // 意図せず今のアバターへ別アバター用の設定一式を適用してしまった」という
                // 事故を防ぐため、適用前に一度確認する。
                if (ShouldConfirmBeforeApplyingProfile(_profile, out string warningMessage))
                {
                    bool proceed = EditorUtility.DisplayDialog(
                        ArkitFTLoc.T("Profileの内容が現在のアバターと一致しない可能性があります"),
                        warningMessage,
                        ArkitFTLoc.T("適用する"), ArkitFTLoc.T("適用しない"));
                    if (proceed)
                    {
                        ApplyProfileSelections();
                        _lastAppliedProfile = _profile;
                        _lastAppliedProfileAvatar = _avatarPrefab;
                    }
                    else
                    {
                        // 適用を見送った場合は選択自体も元に戻す。ここで_profileをこのまま
                        // 残すと、後で「保存」を押したときに無関係な(現在の作業内容の)設定で
                        // このProfileを上書きしてしまう恐れがあるため。
                        _profile = previousProfile;
                        if (_uiProfileField != null) _uiProfileField.SetValueWithoutNotify(_profile);
                        RefreshToolkitUI();
                        return;
                    }
                }
                else
                {
                    ApplyProfileSelections();
                    _lastAppliedProfile = _profile;
                    _lastAppliedProfileAvatar = _avatarPrefab;
                }
            }

            if (_uiProfileField != null)
                _uiProfileField.SetValueWithoutNotify(_profile);

            RefreshToolkitUI();

            if (_profile != null)
            {
                Selection.activeObject = _profile;
                EditorGUIUtility.PingObject(_profile);
            }
        }

        /// <summary>
        /// 選択されたProfileのfaceSMRPathが、現在選択中のアバター上のどのSMRパスとも
        /// 一致しない場合にtrueを返す(比較しようがない場合はfalse=警告なし)。
        /// </summary>
        private bool ShouldConfirmBeforeApplyingProfile(ARKitFTProfile profile, out string message)
        {
            message = null;
            if (_avatarPrefab == null || _smrPaths.Length == 0) return false;
            if (string.IsNullOrEmpty(profile.faceSMRPath)) return false;
            if (Array.IndexOf(_smrPaths, profile.faceSMRPath) >= 0) return false;

            message = string.Format(ArkitFTLoc.T(
                "選択したProfile('{0}')に保存されているFace Mesh('{1}')が、\n" +
                "現在選択中のアバターには見つかりません。\n\n" +
                "このまま適用すると、現在のアバターに対する作業内容がこのProfileの設定で\n" +
                "上書きされます。"),
                profile.name, profile.faceSMRPath);
            return true;
        }

        private void CreateNewProfile()
        {
            EnsureAssetFolder(PROFILE_DEFAULT_FOLDER);

            // アバターが選択済みなら、ファイル名の候補にもアバター名を反映しておく
            // (「ARKitFTProfile_Kirishima」のような命名規則に自然と揃う)。
            string defaultName = _avatarPrefab != null
                ? $"ARKitFTProfile_{SanitizeFileName(_avatarPrefab.name)}"
                : "ARKitFTProfile";

            var path = EditorUtility.SaveFilePanelInProject(
                "Save ARKit FT Profile", defaultName, "asset",
                ArkitFTLoc.T("プロファイルの保存先を選択してください"), PROFILE_DEFAULT_FOLDER);
            if (string.IsNullOrEmpty(path)) return;

            var profile = CreateInstance<ARKitFTProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();

            // 識別タグ・メタデータは、前に読み込んでいた別Profileの値を誤って引き継がないよう、
            // 現在選択中のアバター名から新たに設定し直す(なければ空のまま)。
            _avatarMatchTag = _avatarPrefab != null ? _avatarPrefab.name : "";
            _profileShopName = "";
            _profileVersionName = "";

            SetCurrentProfile(profile, false);
            SaveToProfile(); // 現在の設定をそのまま書き込む
            RefreshToolkitUI();
        }

        /// <summary>
        /// ファイル名として使えない文字を取り除く(アバター名をProfileファイル名に
        /// 流用する際の安全策)。
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Avatar";
            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.Where(c => !invalid.Contains(c)).ToArray();
            var result = new string(chars).Trim();
            return string.IsNullOrEmpty(result) ? "Avatar" : result;
        }

        // ── SMR / シェイプキーリスト更新 ─────────────────

        // 「このアバター」に「このProfile」を最後に適用したかどうかの記録。
        // ReloadAvatarStateが呼ばれるたびに無条件でProfileを再適用すると、
        // 手動でFace Mesh等を編集し直しても「再読込」やアバター再選択のたびに
        // 元のProfile内容へ静かに巻き戻されてしまう事故になるため、
        // 同じ組み合わせへの重複適用を避けるためのガードとして使う。
        private ARKitFTProfile _lastAppliedProfile;
        private GameObject _lastAppliedProfileAvatar;
        // RefreshSmrListが最後に処理したアバター。これと現在の_avatarPrefabが同じなら
        // 「同じアバターの再読込」とみなし、Blendshape Prefix等のアバター構造に紐づく
        // 設定は保持する(異なるアバターへ切り替わったときだけリセットする)。
        private GameObject _lastRefreshedAvatar;

        /// <summary>
        /// 現在の_avatarPrefabに対してSMR / ARKitチェック / FXレイヤー情報を
        /// 強制的に再取得する。ObjectFieldの値変更コールバックと「再読込」ボタンの
        /// 両方から呼ばれる共通処理(同一オブジェクトの再選択はUI ToolkitのObjectFieldでは
        /// 値変更イベントが発火しないため、再読込ボタン経由でも同じ処理を呼べるようにしている)。
        ///
        /// Profileの適用(ApplyProfileSelections)は、「このアバター×このProfile」の組み合わせが
        /// まだ一度も適用されていない場合だけ行う。既に適用済みの組み合わせであれば、
        /// (アバター名タグによる自動選択が働いた場合も含めて)再読込のたびに手動での
        /// Face Mesh変更などが上書きされてしまう事故を防ぐため、あえて何もしない。
        /// 保存済みの内容へ明示的に戻したい場合は、「既存Profile」メニューから
        /// 該当Profileを選び直せば(確認ダイアログを経て)再適用できる。
        /// </summary>
        private void ReloadAvatarState()
        {
            RefreshSmrList();
            RefreshAvatarDescriptorChecks();
            TryAutoSelectProfileForAvatar();

            if (_profile != null &&
                (_profile != _lastAppliedProfile || _avatarPrefab != _lastAppliedProfileAvatar))
            {
                ApplyProfileSelections();
                _lastAppliedProfile = _profile;
                _lastAppliedProfileAvatar = _avatarPrefab;
            }
            else if (_profile == null && _avatarPrefab != null)
            {
                // Profileが存在しない(=対応するものが見つからなかった)アバターを選択した
                // 場合、ユーザーはこれから「使い方マニュアル」の「Profile未作成アバター」の
                // 手順に沿って、上から順に設定していくことになる。折りたたまれたままだと
                // 何から手を付ければよいか分かりづらいため、ガイド代わりに全カードを開く。
                ExpandAllCollapsibleCards();
            }

            RefreshToolkitUI();
        }

        /// <summary>
        /// アバター名の文字列をもとに、プロジェクト内のARKitFTProfileアセットから
        /// 名前が一致するものを自動選択する。見つからない場合や、既に同じProfileが
        /// 選択済みの場合は何もしない(手動で選んだ別のProfileを勝手に上書きすることはない)。
        ///
        /// マッチングは精度の高い順に4段階で評価する:
        ///   0. 識別タグ一致: Profile.avatarMatchTag(カンマ区切りで複数指定可)のいずれかが
        ///      Avatar名に含まれる(最優先。バージョン名や接頭辞・接尾辞が付いたAvatar名でも、
        ///      タグさえ登録しておけばマッチする)。複数のタグ・複数のProfileが同時に一致した場合は、
        ///      まずAvatar名との完全一致を優先し、次に一致タグが長いもの、最後にタグの登録順
        ///      (カンマ区切りで先に書かれているものほど優先)で決める。
        ///      これにより「Sumiya」と「miya」のような包含関係でも、より具体的なProfileを選べる。
        ///   1. 完全一致: Profile名が「ARKitFTProfile_&lt;アバター名&gt;」そのもの
        ///   2. 単語単位一致: Profile名を _ / - / スペース で区切った単語のいずれかが
        ///      アバター名と完全に一致する
        ///   3. 部分一致: 上記に該当するものがない場合のみ、従来通りの部分一致
        ///      (単純な部分一致だけだと、例えばアバター名「Ai」がProfile名
        ///      「ARKitFTProfile_Kai」に誤ってマッチしてしまうため、他の段階が
        ///      優先される)
        /// </summary>
        private void TryAutoSelectProfileForAvatar()
        {
            if (_avatarPrefab == null) return;

            // 既にこのアバターに対してProfileが適用・確定済みの場合は、再読込のたびに
            // 自動選択で上書きしない(手動で別Profileへ切り替えた後の「再読込」で、
            // タグ一致するProfileへ勝手に戻ってしまうと、意図した選択が保持できないため)。
            if (_profile != null && _avatarPrefab == _lastAppliedProfileAvatar) return;

            // シーン上のインスタンスは名前に "(Clone)" や重複時の " (1)" が付くことが
            // あるため、比較前に取り除いておく。
            string avatarName = _avatarPrefab.name;
            avatarName = Regex.Replace(avatarName, @"\s*\(Clone\)\s*$", "", RegexOptions.IgnoreCase);
            avatarName = Regex.Replace(avatarName, @"\s*\(\d+\)\s*$", "");
            avatarName = avatarName.Trim();
            if (string.IsNullOrEmpty(avatarName)) return;

            var guids = AssetDatabase.FindAssets("t:" + nameof(ARKitFTProfile));
            if (guids == null || guids.Length == 0) return;

            var allProfiles = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrEmpty(p))
                .Select(p => (path: p, name: Path.GetFileNameWithoutExtension(p)))
                .ToList();

            // 段階0: 識別タグ一致(最優先)。avatarMatchTagはカンマ区切りで複数指定できるため、
            // 分割したタグのいずれか1つでもAvatar名に含まれていればマッチとみなす。
            // 各ProfileアセットのavatarMatchTagを直接読みに行く必要があるため、
            // 他の段階と違いここだけAssetDatabase読み込みが伴う。
            //
            // 複数Profile / 複数タグが同時に一致する場合は、次の順で具体性を判定する。
            //   1) Avatar名との完全一致
            //   2) 一致したタグが長いもの (例: Sumiya > miya)
            //   3) 同条件ならタグの登録順 (カンマ区切りで先に書いたもの)
            // 短い正当なアバター名(U / Mao等)も、他により具体的な競合候補が無ければ通常通り選ばれる。
            var tier0 = allProfiles
                .Select(e => (e.path, e.name, profile: AssetDatabase.LoadAssetAtPath<ARKitFTProfile>(e.path)))
                .Where(e => e.profile != null && !string.IsNullOrWhiteSpace(e.profile.avatarMatchTag))
                .Select(e =>
                {
                    var tags = e.profile.avatarMatchTag.Split(',').Select(t => t.Trim()).ToArray();
                    int bestIndex = -1;
                    string matchedTag = "";
                    bool exactMatch = false;

                    for (int i = 0; i < tags.Length; i++)
                    {
                        string tag = tags[i];
                        if (tag.Length == 0) continue;
                        if (avatarName.IndexOf(tag, StringComparison.OrdinalIgnoreCase) < 0) continue;

                        bool isExact = string.Equals(avatarName, tag, StringComparison.OrdinalIgnoreCase);

                        // このProfile内でも「完全一致 → 長いタグ → 登録順」の順で最良タグを選ぶ。
                        if (bestIndex < 0 ||
                            (isExact && !exactMatch) ||
                            (isExact == exactMatch && tag.Length > matchedTag.Length))
                        {
                            bestIndex = i;
                            matchedTag = tag;
                            exactMatch = isExact;
                        }
                    }

                    return (e.path, e.name, bestIndex, matchedTag, exactMatch);
                })
                .Where(e => e.bestIndex >= 0)
                // Profile間でも同じ優先順位で比較する。
                .OrderByDescending(e => e.exactMatch)
                .ThenByDescending(e => e.matchedTag.Length)
                .ThenBy(e => e.bestIndex)
                // ここまで同条件ならAssetDatabaseの列挙順に依存しないようパスで安定化する。
                .ThenBy(e => e.path, StringComparer.OrdinalIgnoreCase)
                .Select(e => (e.path, e.name))
                .ToList();

            string exactExpected = "ARKitFTProfile_" + avatarName;

            // 段階1: 完全一致(「ARKitFTProfile_<アバター名>」そのもの)
            var tier1 = tier0.Count > 0 ? tier0 : allProfiles
                .Where(e => string.Equals(e.name, exactExpected, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // 段階2: 単語単位一致(_ / - / スペースで区切った単語のいずれかが完全一致)
            var tier2 = tier1.Count > 0 ? tier1 : allProfiles
                .Where(e => e.name.Split('_', '-', ' ')
                                  .Any(token => string.Equals(token, avatarName, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(e => e.name.Length)
                .ToList();

            // 段階3: 部分一致(フォールバック。誤検出のリスクがあるため最後の手段)
            var candidates = tier2.Count > 0 ? tier2 : allProfiles
                .Where(e => e.name.IndexOf(avatarName, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(e => e.name.Length)
                .ToList();

            if (candidates.Count == 0) return;

            var matched = AssetDatabase.LoadAssetAtPath<ARKitFTProfile>(candidates[0].path);
            if (matched == null || matched == _profile) return;

            _profile = matched;
            _uiProfileField?.SetValueWithoutNotify(_profile);

            Debug.Log($"[hinzka ARKit FT] Avatar名 '{avatarName}' に一致するProfileを自動選択しました: " +
                      $"{candidates[0].path}" +
                      (candidates.Count > 1 ? $" (他に{candidates.Count - 1}件の候補がありました)" : ""));
        }

        private void RefreshSmrList()
        {
            // アバターの(再)読み込みが行われたら、別のアバター(または古い状態)に対する
            // インストール結果を表示したままにしないよう、結果カードもクリアする。
            _lastInstallResult = null;

            // 「本当に別のアバターへ切り替わったのか」を、直前にこの関数が処理した
            // アバターとの比較で判定する。同じアバターの単なる再読込(「再読込」ボタン等)
            // であれば、Blendshape Prefixやにっこり目/Eye Constraintの選択といった
            // アバター固有の設定を保持する。以前はこの区別がなく、再読込のたびに
            // 無条件でリセットされ、かつProfile再適用も(重複適用防止のガードにより)
            // 行われないケースがあったため、設定が消えたまま戻らない不具合があった。
            bool avatarChanged = _avatarPrefab != _lastRefreshedAvatar;
            _lastRefreshedAvatar = _avatarPrefab;

            if (_avatarPrefab == null)
            {
                // アバターが空欄に戻された場合も、SetCurrentProfileの場合と同じ理由で
                // 「適用済み」の記録をクリアする。
                _lastAppliedProfile = null;
                _lastAppliedProfileAvatar = null;
            }

            // 再構築で失われる前に、現在の選択状態を「名前」で覚えておく。
            // インデックスはSMR/シェイプキー一覧が再構築されると無効になるため、
            // Profileを再適用しなくても、同じ名前のものが再構築後にも存在すれば
            // 選択状態を維持できるようにする。
            string previousFaceSmrPath = _smrIndex < _smrPaths.Length ? _smrPaths[_smrIndex] : null;
            var previousSquintNames = _squintShapeIndices
                .Where(i => i >= 0 && i < _shapeNames.Length)
                .Select(i => _shapeNames[i])
                .ToList();
            string previousArkitShapePrefix = _arkitShapePrefix;
            bool previousHasBlendshapePrefix = _hasBlendshapePrefix;
            bool previousUeFallbackEnabled = _ueFallbackEnabled;
            int previousEyeSmrIndex = _eyeSmrIndex;
            bool previousEyeSmrSeparate = _eyeSmrSeparate;
            bool previousEyeUsesConstraint = _eyeUsesConstraint;
            Transform previousLeftEyeConstraintTarget = _leftEyeConstraintTarget;
            Transform previousRightEyeConstraintTarget = _rightEyeConstraintTarget;

            _smrPaths = Array.Empty<string>();
            _smrs = Array.Empty<SkinnedMeshRenderer>();
            _shapeNames = Array.Empty<string>();
            _smrIndex = 0;
            _squintShapeIndices = new List<int>();
            _faceSmrPathMismatch = false;

            if (avatarChanged)
            {
                // Eye SMR/Constraint関連は、アバターごとの階層に紐づく「発見情報」であり、
                // Transform参照を含むため、別アバターへ切り替わった場合は必ずリセットする。
                // Profileが読み込まれている場合は直後のApplyProfileSelectionsが
                // 新アバター上で解決し直して上書きする。
                _eyeSmrIndex = 0;
                _eyeSmrSeparate = false;
                _eyeUsesConstraint = false;
                _leftEyeConstraintTarget = null;
                _rightEyeConstraintTarget = null;

                // ARKit接頭辞も特定アバターの命名規則に依存する設定のため、残ったままだと
                // 別アバターで全シェイプが「不足」と誤判定される。同様にリセットする。
                _arkitShapePrefix = "";
                _hasBlendshapePrefix = false;
                _ueFallbackEnabled = false;
            }
            else
            {
                // 同じアバターの再読込なので、これらの設定はそのまま保持する。
                _eyeSmrIndex = previousEyeSmrIndex;
                _eyeSmrSeparate = previousEyeSmrSeparate;
                _eyeUsesConstraint = previousEyeUsesConstraint;
                _leftEyeConstraintTarget = previousLeftEyeConstraintTarget;
                _rightEyeConstraintTarget = previousRightEyeConstraintTarget;
                _arkitShapePrefix = previousArkitShapePrefix;
                _hasBlendshapePrefix = previousHasBlendshapePrefix;
                _ueFallbackEnabled = previousUeFallbackEnabled;
            }

            if (_avatarPrefab != null)
            {
                var all = _avatarPrefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (all.Length > 0)
                {
                    // 優先名を先頭に並べる
                    var priority = all
                        .Where(s => PRIORITY_SMR_NAMES.Contains(s.name))
                        .OrderBy(s => Array.IndexOf(PRIORITY_SMR_NAMES, s.name))
                        .ToList();
                    var rest = all.Where(s => !PRIORITY_SMR_NAMES.Contains(s.name)).ToList();
                    var sorted = priority.Concat(rest).ToArray();

                    _smrs = sorted;
                    _smrPaths = sorted.Select(s => GetRelativePath(_avatarPrefab.transform, s.transform)).ToArray();

                    // 直前に選んでいたFace Meshが再構築後も存在するなら、それを優先して復元する。
                    // (Profileが読み込まれていない場合や、まだ再適用されていない場合でも、
                    // 選択状態が勝手に「優先名リストの先頭」へ巻き戻らないようにするため)
                    if (!string.IsNullOrEmpty(previousFaceSmrPath))
                    {
                        int restoredIdx = Array.IndexOf(_smrPaths, previousFaceSmrPath);
                        if (restoredIdx >= 0) _smrIndex = restoredIdx;
                    }

                    RefreshShapeList(); // 内部でRefreshArkitCheck()も呼ばれる(_squintShapeIndicesもここで一旦空になる)

                    // にっこり目シェイプキーを名前で復元する。
                    if (previousSquintNames.Count > 0)
                    {
                        foreach (var name in previousSquintNames)
                        {
                            int shapeIdx = Array.IndexOf(_shapeNames, name);
                            if (shapeIdx >= 0 && !_squintShapeIndices.Contains(shapeIdx))
                                _squintShapeIndices.Add(shapeIdx);
                        }
                    }
                }
            }

            // SMRが見つからない場合(アバター未選択・SMRなしメッシュ双方)でも、
            // ARKitチェック結果は必ずクリアし直す(RefreshShapeListの経路を通らないため)。
            if (_smrs.Length == 0)
                RefreshArkitCheck();

            // FXレイヤー/Eye競合情報は_smrsの有無に関わらず常に再取得する。
            // 以前はSMRが見つからない場合にこの呼び出しがスキップされ、前のアバターの
            // FXレイヤー名やEye競合情報が残ってしまう不具合があった。
            RefreshFxLayerNames(avatarChanged);

            // Parameters bit予算の見積りも、アバター読み込みの時点で早期警告できるよう
            // ここで更新する(実際の詳細な計算はInstall実行時のみ可能だが、大まかな
            // 超過の可能性はここで分かる)。
            RefreshParameterBudgetEstimate();
        }

        /// <summary>
        /// 現在のアバターが既に持っているExpression Parametersと、ARKit FTテンプレートが
        /// 追加するExpression Parametersを合算し、256bit上限を超えそうかどうかを
        /// アバター読み込みの時点で見積もる。「空/未検出シェイプの同期をオフ」等の最適化は
        /// Install実行時にしか正確な値が分からないため、ここでは最適化前(悪い方)の
        /// 見積りにしてある。実際にInstallするとこれより少ないbit数になることはあっても、
        /// 多くなることはない。
        /// </summary>
        private void RefreshParameterBudgetEstimate()
        {
            _estimatedTotalParamBits = 0;
            _estimatedExistingParamBits = 0;
            _estimatedFtParamBits = 0;
            _estimatedParamBitsOverBudget = false;

            if (_avatarPrefab == null) return;

            var desc = _avatarPrefab.GetComponentInChildren<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>(true);
            if (desc != null)
                _estimatedExistingParamBits = ComputeVrcParameterBits(desc.expressionParameters);

            var templateParams = FindTemplate<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>(
                "ARKit_FT_Parameters.asset");
            if (templateParams != null)
                _estimatedFtParamBits = ComputeVrcParameterBits(templateParams);

            _estimatedTotalParamBits = _estimatedExistingParamBits + _estimatedFtParamBits;
            _estimatedParamBitsOverBudget = _estimatedTotalParamBits > VRC_PARAM_BIT_BUDGET;
        }

        private void RefreshFxLayerNames(bool avatarChanged = true)
        {
            // 再構築で失われる前に、現在選ばれているジェスチャーレイヤーを名前で覚えておく
            // (同じアバターの再読込であれば、再構築後も同じ名前で復元する)。
            var previousGestureLayerNames = _gestureLayerIndices
                .Where(i => i >= 0 && i < _fxLayerNames.Length)
                .Select(i => _fxLayerNames[i])
                .ToList();

            _fxLayerNames = Array.Empty<string>();
            _gestureLayerIndices = new List<int>();
            _gestureSearchQuery = "";
            _eyeTrackingControlLayerNames = new List<string>();
            if (_avatarPrefab == null) return;

            // アバターのAvatarDescriptorからFXレイヤーを取得
            var desc = _avatarPrefab.GetComponentInChildren<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (desc == null)
            {
                Debug.Log("[hinzka ARKit FT][DIAG] VRCAvatarDescriptorが見つかりません。");
                return;
            }

            Debug.Log($"[hinzka ARKit FT][DIAG] baseAnimationLayers.Length = {desc.baseAnimationLayers?.Length ?? -1}");
            if (desc.baseAnimationLayers != null)
            {
                foreach (var l in desc.baseAnimationLayers)
                    Debug.Log($"[hinzka ARKit FT][DIAG]   type={l.type}, isDefault={l.isDefault}, " +
                              $"controller={(l.animatorController != null ? l.animatorController.name : "null")}");
            }

            int fxIdxDiag = ResolveFxLayerIndex(desc.baseAnimationLayers);
            var fxController = fxIdxDiag >= 0
                ? desc.baseAnimationLayers[fxIdxDiag].animatorController as AnimatorController
                : null;
            Debug.Log($"[hinzka ARKit FT][DIAG] 選択されたfxIdx={fxIdxDiag}, " +
                      $"fxController={(fxController != null ? fxController.name : "null")}");
            if (fxController == null) return;

            _fxLayerNames = fxController.layers.Select(l => l.name).ToArray();
            Debug.Log($"[hinzka ARKit FT][DIAG] _fxLayerNames = [{string.Join(", ", _fxLayerNames)}]");

            _eyeTrackingControlLayerNames = ScanEyeTrackingControlLayers(fxController);

            // 同じアバターの再読込であれば、直前に選んでいたジェスチャーレイヤーを
            // 名前で復元する(異なるアバターへ切り替わった場合は復元しない。
            // Profileが読み込まれていれば直後のApplyProfileSelectionsが改めて解決する)。
            if (!avatarChanged && previousGestureLayerNames.Count > 0)
            {
                foreach (var name in previousGestureLayerNames)
                {
                    int layerIdx = Array.IndexOf(_fxLayerNames, name);
                    if (layerIdx >= 0 && !_gestureLayerIndices.Contains(layerIdx))
                        _gestureLayerIndices.Add(layerIdx);
                }
            }
        }

        /// <summary>
        /// 指定レイヤー内のVRCAnimatorTrackingControlについて、指定フィールドだけを
        /// NoChange(enum値0)へ変更する。Behaviour自体や他フィールド、Parameter Driver、
        /// State/Transitionは残すため、AutoStop等の本来のロジックを壊さずTrackingControlの
        /// 副作用だけを止めたい場合に使う。
        ///
        /// Stable Eye ModeではAvatarDescriptor Eye Look自体を無効化するため、
        /// UE_FT_AutoStop_EyesからEyes=Animation/Trackingを再主張する必要がない。
        /// レイヤー丸ごとの削除ではUEFx/FT_EnableEyesのウォッチドッグまで失われるので、
        /// trackingEyesのみNoChangeにする。
        /// </summary>
        private static int NeutralizeTrackingControlFieldInLayer(
            AnimatorController fxController, string layerName, string trackingFieldName)
        {
            if (fxController == null || string.IsNullOrEmpty(layerName) ||
                string.IsNullOrEmpty(trackingFieldName)) return 0;

            Type trackingControlType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name?.Contains("VRCSDK") != true) continue;
                trackingControlType = asm.GetType("VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl");
                if (trackingControlType != null) break;
            }
            if (trackingControlType == null) return 0;

            var field = trackingControlType.GetField(trackingFieldName);
            if (field == null) return 0;

            int changed = 0;
            foreach (var layer in fxController.layers)
            {
                if (!string.Equals(layer.name, layerName, StringComparison.Ordinal)) continue;
                changed += NeutralizeTrackingControlFieldInStateMachine(
                    layer.stateMachine, trackingControlType, field);
            }

            if (changed > 0)
            {
                EditorUtility.SetDirty(fxController);
                Debug.Log($"[hinzka ARKit FT] Stable Eye Mode: レイヤー '{layerName}' の" +
                          $" {trackingFieldName} TrackingControlを{changed}件NoChangeへ変更しました。" +
                          " AutoStopのParameter Driver / リセット処理は維持されます。");
            }
            return changed;
        }

        private static int NeutralizeTrackingControlFieldInStateMachine(
            AnimatorStateMachine sm, Type trackingControlType, FieldInfo field)
        {
            if (sm == null) return 0;

            int changed = 0;
            foreach (var child in sm.states)
            {
                var state = child.state;
                if (state == null || state.behaviours == null) continue;

                foreach (var behaviour in state.behaviours)
                {
                    if (behaviour == null || !trackingControlType.IsInstanceOfType(behaviour)) continue;

                    var current = field.GetValue(behaviour);
                    if (current == null || Convert.ToInt32(current) == 0) continue;

                    // trackingEyes等はenum。型を保ったままNoChange(0)を書き込む。
                    object noChange = field.FieldType.IsEnum
                        ? Enum.ToObject(field.FieldType, 0)
                        : Convert.ChangeType(0, field.FieldType);
                    field.SetValue(behaviour, noChange);
                    EditorUtility.SetDirty(behaviour);
                    changed++;
                }
            }

            foreach (var sub in sm.stateMachines)
                changed += NeutralizeTrackingControlFieldInStateMachine(
                    sub.stateMachine, trackingControlType, field);

            return changed;
        }

        /// <summary>
        /// UE_FT_AutoStop_Eyes の Idle_A / Idle_B の無条件反復を単一Idleへ整理する。
        /// 実機検証で、AvatarDescriptor Eye Lookの有効/無効どちらでもA/B反復を削除して
        /// AutoStop・Eye Look復帰とも問題がないことを確認したため、両モード共通で適用する。
        ///
        /// Idle_A側のMotion / Parameter Driver / TrackingControlはそのまま残し、
        /// Idle_A→Idle_B遷移を削除、Idle_BをStateMachineから除去してIdle_AをIdleへリネームする。
        /// AnyStateからIdle_Aへ向いていた復帰遷移やDefaultStateは同じStateオブジェクトを
        /// 参照しているため、そのまま有効。StoppedやAutoStop条件には触れない。
        /// </summary>
        private static bool CollapseEyeAutoStopIdleLoop(
            AnimatorController fxController, string layerName)
        {
            if (fxController == null || string.IsNullOrEmpty(layerName)) return false;

            foreach (var layer in fxController.layers)
            {
                if (!string.Equals(layer.name, layerName, StringComparison.Ordinal)) continue;
                var sm = layer.stateMachine;
                if (sm == null) return false;

                AnimatorState idleA = null;
                AnimatorState idleB = null;
                foreach (var child in sm.states)
                {
                    if (child.state == null) continue;
                    if (child.state.name.EndsWith("_Idle_A", StringComparison.Ordinal)) idleA = child.state;
                    else if (child.state.name.EndsWith("_Idle_B", StringComparison.Ordinal)) idleB = child.state;
                }

                // テンプレート構造が想定と違う場合は安全のため何もしない。
                if (idleA == null || idleB == null)
                {
                    Debug.LogWarning($"[hinzka ARKit FT] レイヤー '{layerName}' の " +
                                     "Idle_A / Idle_B が見つからないため単一Idle化をスキップしました。");
                    return false;
                }

                // Idle_AからIdle_Bへの反復遷移だけを明示的に除去する。
                // 他の遷移が将来追加されても巻き込まない。
                foreach (var transition in idleA.transitions.ToArray())
                {
                    if (transition != null && transition.destinationState == idleB)
                        idleA.RemoveTransition(transition);
                }

                // Idle_BはA/B反復専用。StateMachineから取り除く。
                // Idle_AはAnyState復帰先・DefaultStateとしてそのまま利用する。
                sm.RemoveState(idleB);

                const string suffix = "_Idle_A";
                if (idleA.name.EndsWith(suffix, StringComparison.Ordinal))
                    idleA.name = idleA.name.Substring(0, idleA.name.Length - suffix.Length) + "_Idle";

                EditorUtility.SetDirty(idleA);
                EditorUtility.SetDirty(sm);
                EditorUtility.SetDirty(fxController);

                Debug.Log($"[hinzka ARKit FT] レイヤー '{layerName}' の " +
                          "Idle_A ↔ Idle_B 反復を削除し、単一Idleへ整理しました。");
                return true;
            }

            Debug.LogWarning($"[hinzka ARKit FT] レイヤー '{layerName}' が見つからないため " +
                             "単一Idle化をスキップしました。");
            return false;
        }

        /// <summary>
        /// FX内の各レイヤーを走査し、VRCAnimatorTrackingControlでEyes(目)の設定が
        /// NoChange以外(Tracking/Animation)になっているものを含むレイヤー名の一覧を返す
        /// (重複なし)。ネイティブ目線制御をオフにするかどうかの判断材料として使う。
        /// </summary>
        private static List<string> ScanEyeTrackingControlLayers(AnimatorController fxController)
        {
            var result = new List<string>();
            if (fxController == null) return result;

            Type trackingControlType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name?.Contains("VRCSDK") != true) continue;
                trackingControlType = asm.GetType("VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl");
                if (trackingControlType != null) break;
            }
            if (trackingControlType == null) return result;

            var eyesField = trackingControlType.GetField("trackingEyes");
            if (eyesField == null) return result;

            foreach (var layer in fxController.layers)
            {
                if (layer.stateMachine == null) continue;
                if (StateMachineHasEyeTrackingControl(layer.stateMachine, trackingControlType, eyesField))
                    result.Add(layer.name);
            }
            return result;
        }

        private static bool StateMachineHasEyeTrackingControl(
            AnimatorStateMachine sm, Type trackingControlType, FieldInfo eyesField)
        {
            if (sm == null) return false;

            foreach (var s in sm.states)
            {
                var state = s.state;
                if (state == null || state.behaviours == null) continue;

                foreach (var b in state.behaviours)
                {
                    if (b == null || !trackingControlType.IsInstanceOfType(b)) continue;
                    var val = eyesField.GetValue(b);
                    // Eye Look競合レポートとして検出したいのは、VRChat標準Eye Lookを再度
                    // 有効化してしまう trackingEyes = Tracking(1) だけに限定する。
                    // trackingEyes = Animation(2) はVRChat標準Eye Trackingへ戻す指定では
                    // ないため、この意味での競合候補には該当しない
                    // (Animation切替の検出・ガードはScanTrackingAnimationTriggers側の
                    // 別の仕組みが目的別に担っており、本関数はそちらには一切影響しない)。
                    // enum値0=NoChange、1=Tracking、2=Animationという前提(他機能で確認済みの並び)。
                    if (val != null && Convert.ToInt32(val) == 1) return true;
                }
            }

            foreach (var sub in sm.stateMachines)
                if (StateMachineHasEyeTrackingControl(sub.stateMachine, trackingControlType, eyesField))
                    return true;

            return false;
        }

        // ── Tracking Animation ガード(逆Viseme・EyeLook等がジェスチャーの ─────
        // ── Mouth/Eyes=Animationと衝突しないよう複製するための条件抽出 ─────

        /// <summary>
        /// アバター本来のFX内を走査し、「VRCAnimatorTrackingControlで指定フィールド
        /// (trackingMouth/trackingEyes)がAnimation(enum値2)に設定されているState」へ
        /// 実際に入るための遷移条件を抽出する。
        ///
        /// 1つのStateに対して複数の遷移(AnyState経由・同一階層の他Stateからの遷移)が
        /// あり得るため、それぞれを独立した「トリガーグループ」(グループ内はAND条件)として
        /// 返す(グループ間はOR)。例えばGestureLeft==2でOpenステートに入るなら、
        /// [[GestureLeft Equals 2]]という1グループを返す。
        ///
        /// 条件抽出できない特殊な遷移(Exit遷移・Entry遷移等)は無視する。取得したいのは
        /// あくまで「このパラメータの組み合わせのとき、ジェスチャーがMouth/Eyesを
        /// Animationにしている」という実用的な近似であり、100%網羅ではない。
        /// </summary>
        private static List<List<AnimatorCondition>> ScanTrackingAnimationTriggers(
            AnimatorController avatarFx, string trackingFieldName)
        {
            var result = new List<List<AnimatorCondition>>();
            if (avatarFx == null) return result;

            Type trackingControlType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name?.Contains("VRCSDK") != true) continue;
                trackingControlType = asm.GetType("VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl");
                if (trackingControlType != null) break;
            }
            if (trackingControlType == null) return result;

            var targetField = trackingControlType.GetField(trackingFieldName);
            if (targetField == null) return result;

            foreach (var layer in avatarFx.layers)
            {
                if (layer.stateMachine == null) continue;
                CollectTrackingAnimationTriggersInStateMachine(
                    layer.stateMachine, trackingControlType, targetField, result);
            }
            return result;
        }

        private static void CollectTrackingAnimationTriggersInStateMachine(
            AnimatorStateMachine sm, Type trackingControlType, FieldInfo targetField,
            List<List<AnimatorCondition>> result)
        {
            if (sm == null) return;

            // このステートマシン直下の各Stateについて、Animation指定を持つものを探す
            var animationStates = new HashSet<AnimatorState>();
            foreach (var s in sm.states)
            {
                var state = s.state;
                if (state == null || state.behaviours == null) continue;
                foreach (var b in state.behaviours)
                {
                    if (b == null || !trackingControlType.IsInstanceOfType(b)) continue;
                    var val = targetField.GetValue(b);
                    // enum値: 0=NoChange, 1=Tracking, 2=Animation(他機能で確認済みの並び)
                    if (val != null && Convert.ToInt32(val) == 2)
                        animationStates.Add(state);
                }
            }

            if (animationStates.Count > 0)
            {
                // AnyState遷移のうち、対象Stateへ向かうものを集める
                foreach (var t in sm.anyStateTransitions)
                {
                    if (t == null || t.destinationState == null) continue;
                    if (!animationStates.Contains(t.destinationState)) continue;
                    if (t.conditions != null && t.conditions.Length > 0)
                        result.Add(new List<AnimatorCondition>(t.conditions));
                }
                // 同一階層内の他Stateからの遷移のうち、対象Stateへ向かうものも集める
                foreach (var s in sm.states)
                {
                    if (s.state == null || s.state.transitions == null) continue;
                    foreach (var t in s.state.transitions)
                    {
                        if (t == null || t.destinationState == null) continue;
                        if (!animationStates.Contains(t.destinationState)) continue;
                        if (t.conditions != null && t.conditions.Length > 0)
                            result.Add(new List<AnimatorCondition>(t.conditions));
                    }
                }
            }

            foreach (var sub in sm.stateMachines)
                CollectTrackingAnimationTriggersInStateMachine(sub.stateMachine, trackingControlType, targetField, result);
        }

        /// <summary>
        /// AnimatorConditionModeの否定を返す。Equals/NotEqual/If/IfNotは厳密に否定できるが、
        /// Greater/Lessは「以上/以下」に相当するモードがAnimatorConditionModeに存在しないため、
        /// 反対方向の比較(同じ閾値)で近似する(境界値ちょうどの場合にわずかな誤差があり得る)。
        /// </summary>
        private static AnimatorConditionMode NegateConditionMode(AnimatorConditionMode mode)
        {
            switch (mode)
            {
                case AnimatorConditionMode.Equals: return AnimatorConditionMode.NotEqual;
                case AnimatorConditionMode.NotEqual: return AnimatorConditionMode.Equals;
                case AnimatorConditionMode.If: return AnimatorConditionMode.IfNot;
                case AnimatorConditionMode.IfNot: return AnimatorConditionMode.If;
                case AnimatorConditionMode.Greater: return AnimatorConditionMode.Less; // 近似
                case AnimatorConditionMode.Less: return AnimatorConditionMode.Greater; // 近似
                default: return mode;
            }
        }
        /// <summary>
        /// ジェスチャーレイヤー等がVRCAnimatorTrackingControlでtrackingMouth/trackingEyesを
        /// Animationへ切り替える条件(triggerGroups、ScanTrackingAnimationTriggersで抽出済み)と
        /// 同じ条件が成立している間、指定シェイプキー(逆Viseme等)を0に固定する専用レイヤーを追加する。
        ///
        /// 逆Viseme・EyeLook等はNK Installer自身が生成するFXレイヤーであり、VRCAnimatorTrackingControlの
        /// 管轄(Mouth/Eyesがtracking/animationのどちらで駆動されるか)とは無関係に常時動作し続ける。
        /// そのため、ジェスチャーがMouth/Eyesの制御をAnimation側へ奪ったとき、打ち消す相手(標準の
        /// Viseme/EyeLook)がAnimation側に切り替わっているにも関わらず、打ち消しシェイプキー側だけが
        /// 動き続けてしまい、表情が破綻することがある。この専用レイヤーで同じ条件を監視し、該当中は
        /// 打ち消しシェイプキーを0に固定することでこれを防ぐ。
        ///
        /// 復帰条件(Inactiveへ戻る条件)は、全トリガーグループの個々の条件をそれぞれ否定した上で
        /// AND連結したものを使う(ド・モルガンの法則: NOT(A or B or ...) = NOT A and NOT B and ...)。
        ///
        /// 該当レイヤーが既に存在する場合は削除して作り直す。triggerGroups・shapeNamesが
        /// 空なら何もしない。
        /// </summary>
        private static void AddTrackingAnimationGuardLayer(
            AnimatorController fx, List<List<AnimatorCondition>> triggerGroups, string smrPath,
            List<string> shapeNames, string layerName)
        {
            if (fx == null || string.IsNullOrEmpty(layerName)) return;
            if (triggerGroups == null || triggerGroups.Count == 0) return;
            if (shapeNames == null || shapeNames.Count == 0) return;

            // 既に同名レイヤーが存在する場合は削除して作り直す(再Install時の重複防止)。
            var existingLayers = fx.layers.ToList();
            var existingIdx = existingLayers.FindIndex(l => l.name == layerName);
            if (existingIdx >= 0) fx.RemoveLayer(existingIdx);

            // 参照しているパラメータをfx側にも確保する(Modular Avatarのマージ時に解決されるが、
            // 単体のAnimatorControllerとして見ても不整合が出ないよう、念のためここで追加する)。
            var neededParams = triggerGroups.SelectMany(g => g).Select(c => c.parameter).Distinct();
            foreach (var pName in neededParams)
            {
                if (string.IsNullOrEmpty(pName)) continue;
                if (fx.parameters.Any(p => p.name == pName)) continue;
                fx.AddParameter(new AnimatorControllerParameter
                { name = pName, type = AnimatorControllerParameterType.Int, defaultInt = 0 });
            }

            var sm = new AnimatorStateMachine { name = layerName + "_SM" };
            AssetDatabase.AddObjectToAsset(sm, fx);
            HideGeneratedSubAsset(sm);

            var emptyClip = new AnimationClip { name = layerName + "_Empty" };
            AssetDatabase.AddObjectToAsset(emptyClip, fx);
            HideGeneratedSubAsset(emptyClip);

            var zeroClip = new AnimationClip { name = layerName + "_Zero" };
            AssetDatabase.AddObjectToAsset(zeroClip, fx);
            HideGeneratedSubAsset(zeroClip);
            foreach (var shapeName in shapeNames)
                SetCurve(zeroClip, smrPath, shapeName, 0f);

            var inactiveState = sm.AddState(layerName + "_Inactive", new Vector3(200f, 80f,  0f));
            var activeState   = sm.AddState(layerName + "_Active",   new Vector3(200f, 200f, 0f));
            inactiveState.motion = emptyClip;
            activeState.motion   = zeroClip;
            sm.defaultState       = inactiveState;

            // 各トリガーグループ(AND条件)ごとに、Any State→Activeの遷移を追加する(グループ間はOR)。
            foreach (var group in triggerGroups)
            {
                if (group == null || group.Count == 0) continue;
                var t = sm.AddAnyStateTransition(activeState);
                t.hasExitTime = false; t.duration = 0f; t.hasFixedDuration = true; t.canTransitionToSelf = false;
                foreach (var cond in group)
                    t.AddCondition(cond.mode, cond.threshold, cond.parameter);
            }

            // 全トリガーグループの否定をANDした遷移 → Inactive
            // (ド・モルガンの法則: NOT(A or B or ...) = NOT A and NOT B and ...)
            var allConditions = triggerGroups.Where(g => g != null).SelectMany(g => g).ToList();
            if (allConditions.Count > 0)
            {
                var tOff = sm.AddAnyStateTransition(inactiveState);
                tOff.hasExitTime = false; tOff.duration = 0f; tOff.hasFixedDuration = true; tOff.canTransitionToSelf = false;
                foreach (var cond in allConditions)
                    tOff.AddCondition(NegateConditionMode(cond.mode), cond.threshold, cond.parameter);
            }

            fx.AddLayer(new AnimatorControllerLayer
            {
                name          = layerName,
                stateMachine  = sm,
                defaultWeight = 1f,
                blendingMode  = AnimatorLayerBlendingMode.Override,
            });

            EditorUtility.SetDirty(sm);
            EditorUtility.SetDirty(fx);
            Debug.Log($"[hinzka ARKit FT] '{layerName}'レイヤーを追加しました" +
                      $"(トリガー{triggerGroups.Count}件 / 対象シェイプ{shapeNames.Count}件)。");
        }


        /// <summary>
        /// baseAnimationLayers配列からFXレイヤーのインデックスを解決する。
        /// type==FXで単純検索すると、配列が壊れている(type重複等)アバターで誤ったエントリを
        /// 拾うことが実例で確認されたため、VRC標準の並び順(Base, Additive, Gesture, Action, FX)の
        /// 固定インデックス4を優先し、それが該当しなければ「type==FXの中で最後に見つかったもの」に
        /// フォールバックする(Unity標準のAvatarDescriptor Inspectorの実際の挙動に基づく)。
        /// 見つからなければ-1を返す。
        /// </summary>
        private static int ResolveFxLayerIndex(
            VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.CustomAnimLayer[] layers)
        {
            if (layers == null) return -1;
            if (layers.Length > 4 && layers[4].type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX)
                return 4;
            for (int i = layers.Length - 1; i >= 0; i--)
                if (layers[i].type == VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX)
                    return i;
            return -1;
        }

        /// <summary>
        /// _avatarPrefabのルートからの相対パス文字列(GetRelativePathの逆)からTransformを解決する。
        /// 見つからない場合はnullを返す(アバターを付け替えた・階層が変わった等)。
        /// </summary>
        private Transform ResolveTransformFromPath(string relativePath)
        {
            if (_avatarPrefab == null || string.IsNullOrEmpty(relativePath)) return null;
            return _avatarPrefab.transform.Find(relativePath);
        }

        /// <summary>
        /// AvatarDescriptorのEyeLook・Visemeが実際に使える状態で設定されているかを確認する。
        /// アバターの選択・再読込のたびに呼び出す(Face Meshの選択状態には依存しないため、
        /// RefreshArkitCheckとは独立して呼ぶ)。
        /// </summary>
        private void RefreshAvatarDescriptorChecks()
        {
            _avatarEyeLookConfigured = true;
            _avatarEyeLookProblem = "";
            _avatarVisemeConfigured = true;
            _avatarVisemeProblem = "";

            if (_avatarPrefab == null) return;
            var desc = _avatarPrefab.GetComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            if (desc == null) return;

            // EyeLook: Left/Right Eyeボーンが未設定、または設定されていても全方向の回転量が
            // 実質0(=見た目上ボーンが一切動かない)場合は、目線シェイプキーを生成しても
            // 意味が無いため警告する。
            var eyeSettings = desc.customEyeLookSettings;
            if (eyeSettings.leftEye == null || eyeSettings.rightEye == null)
            {
                _avatarEyeLookConfigured = false;
                _avatarEyeLookProblem = ArkitFTLoc.T("AvatarDescriptorにLeft Eye/Right Eyeボーンが設定されていません。");
            }
            else
            {
                // 各方向の目標回転が、基準姿勢(Quaternion.identity)からどれだけ離れているかを
                // 角度で見る。全方向で1度未満しか動かない場合、実質「未設定」と同じ状態と判断する。
                const float EYE_LOOK_ANGLE_EPSILON_DEG = 1f;
                float maxAngle = 0f;
                foreach (var rotSet in new[]
                         {
                             eyeSettings.eyesLookingUp, eyeSettings.eyesLookingDown,
                             eyeSettings.eyesLookingLeft, eyeSettings.eyesLookingRight
                         })
                {
                    maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(Quaternion.identity, rotSet.left));
                    maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(Quaternion.identity, rotSet.right));
                }
                if (maxAngle < EYE_LOOK_ANGLE_EPSILON_DEG)
                {
                    _avatarEyeLookConfigured = false;
                    _avatarEyeLookProblem = ArkitFTLoc.T(
                        "AvatarDescriptorのEyeLookボーンは設定されていますが、上下左右いずれの方向も\n" +
                        "回転量が実質0のままです(目線が動くように設定されていません)。");
                }
            }

            // Viseme: LipSyncがVisemeBlendShape以外、またはVisemeBlendShapesが1つも
            // 実在するシェイプキー名を持っていない場合は、Viseme打消しシェイプキーの
            // 生成対象が無いため警告する。
            if (desc.lipSync != VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
            {
                _avatarVisemeConfigured = false;
                _avatarVisemeProblem = ArkitFTLoc.T("AvatarDescriptorのLip SyncがViseme Blend Shapeに設定されていません。");
            }
            else
            {
                var visemeShapes = desc.VisemeBlendShapes;
                bool anyConfigured = visemeShapes != null && visemeShapes.Any(s => !string.IsNullOrWhiteSpace(s));
                if (!anyConfigured)
                {
                    _avatarVisemeConfigured = false;
                    _avatarVisemeProblem = ArkitFTLoc.T("AvatarDescriptorにVisemeシェイプキーが1つも設定されていません。");
                }
            }
        }

        private void RefreshArkitCheck()
        {
            // 対応表(ARKit_FT_ShapeParamMap.asset)は起動時(OnEnable)に一度だけ探すため、
            // ウィンドウを開いたまま後からアセットを追加・配置した場合は反映されない。
            // 未検出のままの間はここでも再探索し、次に見つかったタイミングで自動的に拾う。
            if (_shapeParameterMap == null)
                _shapeParameterMap = FindTemplate<ArkitShapeParameterMap>("ARKit_FT_ShapeParamMap.asset");

            _missingArkitShapes.Clear();
            _emptyArkitShapes.Clear();
            _ueFallbackResolvedShapes.Clear();
            _arkitCheckSmrPath = "";
            if (_smrIndex >= _smrs.Length) return;
            var smr = _smrs[_smrIndex];
            if (smr == null || smr.sharedMesh == null) return;
            var mesh = smr.sharedMesh;
            var nameLookup = BuildTrimmedShapeNameLookup(mesh);
            foreach (var name in ARKIT_SHAPE_NAMES)
            {
                if (nameLookup.ContainsKey(ResolveArkitShapeName(name, nameLookup).Trim())) continue;

                // ARKit名では見つからなかった。UEフォールバックが有効なら代替名を探す。
                if (_ueFallbackEnabled && ARKIT_TO_UE_FALLBACK.TryGetValue(name, out var candidateGroups))
                {
                    var chosenGroup = candidateGroups.FirstOrDefault(g => g.All(n => nameLookup.ContainsKey(n.Trim())));
                    if (chosenGroup != null)
                    {
                        _ueFallbackResolvedShapes[name] = chosenGroup;
                        continue; // 不足扱いにしない
                    }

                    // UEフォールバックも失敗した場合、原因調査用にどの候補名まで
                    // 見つからなかったかをログに残す(「片方の目だけ別メッシュにある」
                    // といった非対称な配置の切り分けに役立つ)。
                    var candidateDiag = string.Join(" / ", candidateGroups.Select(g =>
                        "[" + string.Join(", ", g.Select(n => n + (nameLookup.ContainsKey(n.Trim()) ? "○" : "✗"))) + "]"));
                    Debug.Log($"[hinzka ARKit FT][DIAG] '{name}' はUE代替名でも解決できませんでした。" +
                              $"候補: {candidateDiag} " +
                              "(✗は現在のFace Mesh上に見つからなかった名前。別メッシュに存在する可能性があります)");
                }

                _missingArkitShapes.Add(name);
            }
            _arkitCheckSmrPath = _smrPaths[_smrIndex];

            _emptyArkitShapes = DetectEmptyArkitShapeNames(smr, _arkitShapePrefix)
                .OrderBy(s => s, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// 標準ARKitシェイプ名に、指定された接頭辞(独自命名のアバター対応。カンマ区切りで
        /// 複数候補を指定できる)を付けて返す。nameLookupを渡した場合、候補接頭辞のうち
        /// メッシュ上に実在する組み合わせを優先して返す。どれも見つからない/nameLookup未指定の
        /// 場合は、先頭の候補接頭辞を使ったフォールバック名を返す。
        /// </summary>
        private string ResolveArkitShapeName(string standardName, Dictionary<string, string> nameLookup = null)
        {
            var prefixes = ParsePrefixList(_arkitShapePrefix);
            if (nameLookup != null)
            {
                var resolved = ResolveNameAcrossPrefixes(nameLookup, standardName, prefixes);
                if (resolved != null) return resolved;
            }
            string first = prefixes[0];
            return string.IsNullOrEmpty(first) ? standardName : first + standardName;
        }

        /// <summary>
        /// ARKit標準シェイプキー(52種)のうち「無効果」なものを検出し、標準名(接頭辞なし)のSetで返す。
        /// 検出は2段階:
        ///   段階1(常時実施): シェイプキー自体がメッシュに存在しないもの。
        ///     GetBlendShapeIndexだけで判定できるため、メッシュのRead/Write設定に依存しない。
        ///   段階2(Read/Write有効時のみ): 存在はするが頂点差分が実質ゼロ
        ///     (=形状として何も彫られていない)もの。
        /// Read/Writeが無効な場合は段階2のみスキップし、段階1の結果は返す。
        /// </summary>
        private static HashSet<string> DetectEmptyArkitShapeNames(SkinnedMeshRenderer smr, string arkitPrefix)
        {
            var empty = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (smr == null || smr.sharedMesh == null) return empty;
            var mesh = smr.sharedMesh;
            var nameLookup = BuildTrimmedShapeNameLookup(mesh);
            var prefixes = ParsePrefixList(arkitPrefix);

            // --- 段階1: 存在チェック(Read/Write不要) ---
            // 存在しないシェイプは、中身が空である以上に確実に何の効果もないため、
            // あえてINSTALLを続行した場合は対応する同期パラメータもオフにしてよい。
            var existingShapes = new List<KeyValuePair<string, int>>(); // (標準名, blendShapeIndex)
            foreach (var standardName in ARKIT_SHAPE_NAMES)
            {
                // 候補接頭辞(カンマ区切り指定に対応)のうち、メッシュ上に実在するものを探す。
                string actualName = ResolveNameAcrossPrefixes(nameLookup, standardName, prefixes);
                int idx = actualName != null ? mesh.GetBlendShapeIndex(actualName) : -1;
                if (idx < 0)
                    empty.Add(standardName);
                else
                    existingShapes.Add(new KeyValuePair<string, int>(standardName, idx));
            }

            // --- 段階2: 頂点差分チェック(Read/Write必須) ---
            if (!mesh.isReadable)
            {
                Debug.LogWarning("[hinzka ARKit FT] 対象メッシュのRead/Writeが無効なため、" +
                                  "「中身が空」のARKitシェイプキー検出はスキップしました" +
                                  "(「存在しない」シェイプキーの検出は実施済みです。" +
                                  "Import SettingsでRead/Writeを有効にすると空シェイプも検出できます)。");
                return empty;
            }

            int vCount = mesh.vertexCount;

            // 書き出し時の微小な数値ノイズを「空」判定に含めるため、閾値は緩めに取る
            // (完全な0だけでなく、視覚上意味のない極小デルタも「空」とみなす)。
            const float emptyThresholdSqr = 1e-6f;

            var dv = new Vector3[vCount];
            var dn = new Vector3[vCount];
            var dt = new Vector3[vCount];
            foreach (var pair in existingShapes)
            {
                int idx = pair.Value;
                int frameCount = mesh.GetBlendShapeFrameCount(idx);
                bool anyDelta = false;
                for (int f = 0; f < frameCount && !anyDelta; f++)
                {
                    mesh.GetBlendShapeFrameVertices(idx, f, dv, dn, dt);
                    for (int v = 0; v < vCount; v++)
                    {
                        if (dv[v].sqrMagnitude > emptyThresholdSqr) { anyDelta = true; break; }
                    }
                }
                if (!anyDelta) empty.Add(pair.Key);
            }
            return empty;
        }

        private void RefreshShapeList()
        {
            _shapeNames = Array.Empty<string>();
            _squintShapeIndices = new List<int>();
            if (_smrIndex >= _smrs.Length)
            {
                RefreshEyeLookBakeShapeNamesCache();
                return;
            }

            var smr = _smrs[_smrIndex];
            if (smr == null || smr.sharedMesh == null)
            {
                RefreshEyeLookBakeShapeNamesCache();
                return;
            }

            var mesh = smr.sharedMesh;
            var names = new string[mesh.blendShapeCount];
            for (int i = 0; i < mesh.blendShapeCount; i++)
                names[i] = mesh.GetBlendShapeName(i);
            _shapeNames = names;
            _squintSearchQuery = ""; // SMR変更時は検索をリセット
            // インデックスを有効範囲にクランプ
            for (int i = 0; i < _squintShapeIndices.Count; i++)
                _squintShapeIndices[i] = Mathf.Clamp(_squintShapeIndices[i], 0, names.Length - 1);
            RefreshArkitCheck();

            // 目線シェイプキー生成のベイク対象シェイプキー一覧も、Face Mesh選択に連動して
            // 更新する(Eye SMRが別に指定されている場合はそちらを参照するため、
            // この関数自身の対象がFace SMRであっても呼び出しておく必要がある)。
            RefreshEyeLookBakeShapeNamesCache();
        }

        // ── インストール本体 ──────────────────────────────

        private void Install()
        {
            if (_avatarPrefab == null) return;
            _lastEyeLookEmptyDeltaShapes = new List<string>();
            _installGeneratedMeshPaths = new List<string>();

            if (!IsValidAssetsFolder(_outputFolder))
            {
                EditorUtility.DisplayDialog("Error",
                    ArkitFTLoc.T("Output FolderはUnityプロジェクトのAssetsフォルダ内を指定してください。"), "OK");
                return;
            }

            // ARKitシェイプキーの最終確認
            RefreshArkitCheck();
            string missingShapesSummary = "";
            if (_missingArkitShapes.Count > 0)
            {
                var missing = string.Join("\n",
                    _missingArkitShapes.Count <= 20
                        ? _missingArkitShapes
                        : _missingArkitShapes.Take(20).Append(
                            string.Format(ArkitFTLoc.T("... 他{0}個"), _missingArkitShapes.Count - 20)));
                Debug.LogWarning(string.Format(ArkitFTLoc.T("警告: ARKitシェイプキーが {0} 個不足しています"), _missingArkitShapes.Count) +
                    "\n" + string.Format(ArkitFTLoc.T("以下のシェイプキーが見つかりません:\n{0}\n\n"), missing) +
                    ArkitFTLoc.T("不足しているシェイプキーに対応するFXレイヤーは正しく動作しません。"));
                // 目線シェイプキーなどわずかな動きはあえて作っていないアバターも多いため、
                // これ自体はダイアログで作業を止めるほどの不都合ではない。ログに残しつつ
                // インストールは続行し、結果はインストール結果画面で確認できるようにする。
                missingShapesSummary = string.Format(ArkitFTLoc.T("{0}件: {1}"), _missingArkitShapes.Count, missing);
            }

            // テンプレートは複製前に確認する。ここで失敗してもシーンに半端な複製を残さない。
            var templateFx    = FindTemplate<AnimatorController>("ARKit_FT_Template.controller");
            var templateMenu  = FindTemplate<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>("ARKit_FT_Menu.asset");
            var templateParam = FindTemplate<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>("ARKit_FT_Parameters.asset");
            if (templateFx == null || templateMenu == null || templateParam == null)
            {
                EditorUtility.DisplayDialog("Error",
                    ArkitFTLoc.T("テンプレートアセットが見つかりません。\n") +
                    ArkitFTLoc.T("パッケージの Templates/ フォルダに以下のファイルが必要です:\n") +
                    "  ARKit_FT_Template.controller\n" +
                    "  ARKit_FT_Menu.asset\n" +
                    "  ARKit_FT_Parameters.asset", "OK");
                return;
            }

            EnsureAssetFolder(_outputFolder);

            GameObject workingAvatar = null;
            string installOutputFolder = null;
            bool installSucceeded = false;
            bool sourceWasSceneInstance = false;

            try
            {
                workingAvatar = DuplicateAvatarForInstall(_avatarPrefab, out sourceWasSceneInstance);
                if (workingAvatar == null)
                    throw new InvalidOperationException(ArkitFTLoc.T("アバターの複製に失敗しました。"));

                var workingDesc = workingAvatar.GetComponentInChildren<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
                if (workingDesc == null)
                    throw new InvalidOperationException(ArkitFTLoc.T("複製したアバターにVRCAvatarDescriptorが見つかりませんでした。"));

                var workingFaceSmrTransform = workingAvatar.transform.Find(_smrPaths[_smrIndex]);
                var workingFaceSmr = workingFaceSmrTransform != null
                    ? workingFaceSmrTransform.GetComponent<SkinnedMeshRenderer>() : null;
                if (workingFaceSmr == null)
                    throw new InvalidOperationException(ArkitFTLoc.T("複製したアバター上でFace SMRが見つかりませんでした。"));

                SkinnedMeshRenderer workingEyeSmr = null;
                if (_eyeSmrSeparate)
                {
                    var workingEyeSmrTransform = workingAvatar.transform.Find(_smrPaths[_eyeSmrIndex]);
                    workingEyeSmr = workingEyeSmrTransform != null
                        ? workingEyeSmrTransform.GetComponent<SkinnedMeshRenderer>() : null;
                    if (workingEyeSmr == null)
                        throw new InvalidOperationException(ArkitFTLoc.T("複製したアバター上でEye SMRが見つかりませんでした。"));
                }

                // コンストレイント経由の目玉: 元アバター上で選んだTransformを、複製アバター上の
                // 対応するTransformに解決し直す(参照そのままだと元アバター側を書き換えてしまうため)。
                Transform workingLeftEyeConstraintTarget = null;
                Transform workingRightEyeConstraintTarget = null;
                if (_generateEyeLookShapes && _eyeUsesConstraint)
                {
                    if (_leftEyeConstraintTarget != null)
                    {
                        var relPath = GetRelativePath(_avatarPrefab.transform, _leftEyeConstraintTarget);
                        workingLeftEyeConstraintTarget = workingAvatar.transform.Find(relPath);
                        if (workingLeftEyeConstraintTarget == null)
                            Debug.LogWarning("[hinzka ARKit FT] 複製アバター上でLeft Eyeのコンストレイント先が見つかりませんでした。");
                    }
                    if (_rightEyeConstraintTarget != null)
                    {
                        var relPath = GetRelativePath(_avatarPrefab.transform, _rightEyeConstraintTarget);
                        workingRightEyeConstraintTarget = workingAvatar.transform.Find(relPath);
                        if (workingRightEyeConstraintTarget == null)
                            Debug.LogWarning("[hinzka ARKit FT] 複製アバター上でRight Eyeのコンストレイント先が見つかりませんでした。");
                    }
                }

                // INSTALLごとに専用フォルダを作る。既存生成物を誤って再利用・上書きしない。
                installOutputFolder = CreateUniqueInstallOutputFolder(_outputFolder, workingAvatar.name);

                // FXをコピーして書き換え
                var fxDst = installOutputFolder + "/ARKit_FT_FX.controller";
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(templateFx), fxDst))
                    throw new InvalidOperationException(ArkitFTLoc.T("FXテンプレートのコピーに失敗しました。"));
                AssetDatabase.SaveAssets();

                var fx = AssetDatabase.LoadAssetAtPath<AnimatorController>(fxDst);
                if (fx == null)
                    throw new InvalidOperationException(ArkitFTLoc.T("コピーしたFXを読み込めませんでした。"));

                // Stable Eye Mode (AvatarDescriptor Eye Look無効化)では、テンプレートの
                // UE_FT_AutoStop_Eyes が持つ VRCAnimatorTrackingControl(Eyes=Animation/Tracking)を
                // 発火させない。AutoStopレイヤー自体を削除すると UEFx/FT_EnableEyes の計算や
                // Eyeパラメータのリセットまで失われるため、TrackingControlのtrackingEyesだけを
                // NoChangeへ置き換え、Parameter Driver等のウォッチドッグ機能はそのまま残す。
                //
                // 実機で、Eye LookをDisableにしていてもこのレイヤーのIdle_A/Idle_B間の再入場により
                // trackingEyes=Animationが繰り返し発火すると目ボーンが振動し、レイヤーを除去すると
                // 振動が止まることを確認したための対策。
                if (_disableNativeEyeLook)
                {
                    NeutralizeTrackingControlFieldInLayer(
                        fx, "UE_FT_AutoStop_Eyes", "trackingEyes");
                }

                // UE_FT_AutoStop_Eyes の Idle_A / Idle_B 反復は、実機検証で
                // AvatarDescriptor Eye Lookの有効/無効どちらでも削除して問題ないことを確認済み。
                // Idle_A側のParameter Driver / TrackingControl等は残したまま単一Idle化する。
                CollapseEyeAutoStopIdleLoop(fx, "UE_FT_AutoStop_Eyes");

                string realSmrPath = _smrPaths[_smrIndex];
                string eyeSmrPath = _eyeSmrSeparate ? _smrPaths[_eyeSmrIndex] : null;

                if (_eyeSmrSeparate && eyeSmrPath != null)
                {
                    // FT_EyeBone_* のカーブだけEye SMR側へ、それ以外はFace SMR側へ振り分ける。
                    // (メッシュを物理統合せず、FXのカーブが向き先を出し分けるだけで両方を動かす)
                    var eyeLookShapeNames = new HashSet<string>(
                        EyeLookBoneToBlendShapeBaker.Specs.Select(s => EYELOOK_BONE_PREFIX + s.baseName));
                    RewriteSmrPathsSelective(fx, TEMPLATE_SMR_PATH, realSmrPath, eyeLookShapeNames, eyeSmrPath);
                }
                else if (realSmrPath != TEMPLATE_SMR_PATH)
                {
                    RewriteSmrPaths(fx, TEMPLATE_SMR_PATH, realSmrPath);
                }

                // ARKitシェイプキーに独自の接頭辞が付いているアバター向け:
                // Driver等のカーブが実在するシェイプキー名へ値を書き込むよう、標準ARKit名に
                // 接頭辞を付与する。SMRパスの書き換えより後に行う(パス確定後の方が安全なため)。
                // カンマ区切りで複数の候補接頭辞を指定できる(混在するアバター向け)。
                //
                // 接頭辞が1つも指定されていない(空文字のみ)場合でも、必ず呼び出す。
                // 大文字小文字だけが標準ARKit名と異なるシェイプキー(接頭辞は無い)を持つ
                // アバターでも、実際に存在する綴りへカーブを向け直す必要があるため
                // (接頭辞が無いことを理由に呼び出し自体をスキップすると、このケースを
                // 取りこぼしてしまう)。
                var arkitPrefixList = ParsePrefixList(_arkitShapePrefix);
                RewriteArkitBlendShapeNames(fx, arkitPrefixList, workingFaceSmr.sharedMesh);

                // ARKit標準名(接頭辞込み)でも見つからないシェイプについて、UEフォールバックが
                // 有効ならUE代替名へカーブを複製設定する。プレフィックス書き換えの後に行う
                // (両方を組み合わせて使うことは通常ないが、順序としてはこちらが自然)。
                if (_ueFallbackEnabled)
                    RewriteMissingArkitShapesToUeFallback(fx, workingFaceSmr.sharedMesh, _arkitShapePrefix);

                // にっこり目
                var squintShapeNames = _squintShapeIndices
                    .Where(i => i >= 0 && i < _shapeNames.Length)
                    .Select(i => _shapeNames[i])
                    .Distinct()
                    .ToList();
                if (squintShapeNames.Count > 0)
                    InjectSquintShapes(fx, realSmrPath, squintShapeNames);
                else
                    Debug.Log("[hinzka ARKit FT] にっこり目は未指定です。追加のにっこり目表情を注入しません。");

                // ジェスチャー抑制
                var distinctGestureLayers = _gestureLayerIndices
                    .Where(i => i >= 0 && i < _fxLayerNames.Length)
                    .Distinct()
                    .ToList();
                if (distinctGestureLayers.Count > 0)
                {
                    // Eyeトリガーは廃止した。TrackingControl(ボーン)競合はStable Eye Modeで
                    // 根本的に解決するものであり、weight抑制の発動条件として「目」を選んでも
                    // 解決にならないことが実地で判明したため。全レイヤーをMouthトリガー扱いにする。
                    var mouthTriggerLayers = new HashSet<int>(distinctGestureLayers);
                    var eyesTriggerLayers = new HashSet<int>();
                    ApplyGestureSuppressionDirectly(workingDesc, distinctGestureLayers, realSmrPath, installOutputFolder, mouthTriggerLayers, eyesTriggerLayers, fx, workingFaceSmr);
                }
                else
                {
                    Debug.Log("[hinzka ARKit FT] ジェスチャーレイヤーは未指定です。ジェスチャー抑制を生成しません。");
                }

                // アバター本来のFX(ジェスチャー抑制適用後のもの)から、ジェスチャーが
                // Mouth/EyesをAnimationへ切り替える条件を抽出しておく。逆Viseme・EyeLookは
                // NK Installer自身のFXレイヤーでありTrackingControlの管轄外のため、
                // 同じ条件が成立している間はこちら側のシェイプキーも0に固定して衝突を防ぐ
                // (詳細は AddTrackingAnimationGuardLayer のコメントを参照)。
                AnimatorController avatarOwnFx = null;
                {
                    int avatarFxIdx = ResolveFxLayerIndex(workingDesc.baseAnimationLayers);
                    if (avatarFxIdx >= 0)
                        avatarOwnFx = workingDesc.baseAnimationLayers[avatarFxIdx].animatorController as AnimatorController;
                }
                var mouthAnimationTriggers = avatarOwnFx != null
                    ? ScanTrackingAnimationTriggers(avatarOwnFx, "trackingMouth")
                    : new List<List<AnimatorCondition>>();

                // Viseme補償。Face SMRとViseme SMRが別でも、それぞれ正しいパスを使用する。
                string visemeResult = ArkitFTLoc.T("無効");
                if (_generateVisemeCompensation)
                {
                    if (workingDesc.lipSync != VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
                    {
                        visemeResult = ArkitFTLoc.T("スキップ(LipSyncがVisemeBlendShapeではありません)");
                        Debug.LogWarning("[hinzka ARKit FT] AvatarDescriptorのLipSyncがVisemeBlendShape以外のためViseme補償をスキップしました。");
                    }
                    else if (workingDesc.VisemeSkinnedMesh == null)
                    {
                        visemeResult = ArkitFTLoc.T("スキップ(Viseme SMR未設定)");
                        Debug.LogWarning("[hinzka ARKit FT] VisemeSkinnedMeshが未設定のためViseme補償をスキップしました。");
                    }
                    else
                    {
                        // Visemeの縮小は、逆Viseme生成より必ず先に行う。
                        // 逆シェイプはこの時点のVisemeBlendShapes参照先を読んで打消し量を計算するため、
                        // 縮小後のシェイプキーに対して正しく計算されるようにする。
                        if (_visemeScale < 0.999f)
                            ScaleVisemeShapesIfNeeded(workingDesc, _visemeScale, installOutputFolder);

                        string visemeSmrPath = GetRelativePath(workingAvatar.transform, workingDesc.VisemeSkinnedMesh.transform);
                        var inverseNames = GenerateInverseVisemeShapes(workingDesc, installOutputFolder);
                        if (inverseNames != null)
                        {
                            if (visemeSmrPath != realSmrPath)
                                RewriteBlendShapePathsInLayer(fx, "VisemeCompensate", realSmrPath, visemeSmrPath, inverseNames);
                            ValidateVisemeCompensateClips(fx, visemeSmrPath, inverseNames);
                            visemeResult = ArkitFTLoc.T("生成済み");

                            // ジェスチャーがMouth=Animationへ切り替える条件と同じ条件下では、
                            // 逆Visemeシェイプキーを0に固定する(打消し相手がいないのに
                            // 打ち消し続けてしまう事故を防ぐ)。
                            AddTrackingAnimationGuardLayer(
                                fx, mouthAnimationTriggers, visemeSmrPath, inverseNames.ToList(),
                                "hinzkaFT_MouthAnimGuard");
                        }
                        else
                        {
                            visemeResult = ArkitFTLoc.T("スキップ(生成条件を満たしません)");
                        }
                    }
                }

                // EyeLook (表情メッシュと目メッシュが別々の場合はEye SMR側へ生成する)
                if (_generateEyeLookShapes)
                {
                    var eyeLookTargetSmr = _eyeSmrSeparate ? workingEyeSmr : workingFaceSmr;
                    List<string> eyeLookBakeShapeNames = null;
                    if (_useEyeLookBakeShapes)
                    {
                        RefreshEyeLookBakeShapeNamesCache(); // 呼び出し順序に依存せず必ず最新の状態を参照する
                        var eyeLookBakeTargetNames = _eyeLookBakeShapeNamesCache;
                        eyeLookBakeShapeNames = _eyeLookBakeShapeIndices
                            .Where(i => i >= 0 && i < eyeLookBakeTargetNames.Length)
                            .Select(i => eyeLookBakeTargetNames[i])
                            .Distinct()
                            .ToList();
                    }
                    GenerateEyeLookShapesIfNeeded(
                        workingDesc, eyeLookTargetSmr, installOutputFolder,
                        workingLeftEyeConstraintTarget, workingRightEyeConstraintTarget, _eyeLookIntensity,
                        eyeLookBakeShapeNames);
                }

                // ネイティブアイルック無効化(EyeLookベイクで角度情報を使い終えた後に行う)。
                // ジェスチャー表情の抑制だけでは競合を解消しきれない場合の
                // 最終手段として、フォールバック先自体を無くすことで確実に防ぐ。
                if (_disableNativeEyeLook)
                {
                    workingDesc.enableEyeLook = false;
                    EditorUtility.SetDirty(workingDesc);
                    Debug.Log("[hinzka ARKit FT] AvatarDescriptorのEye Lookを無効化しました。");
                }

                // まばたき制御方式(Blink2D / Blink Simple 1D)。テンプレートFXに両方式が
                // 同梱されている場合、選択されなかった方をここで無効化する。
                ApplyBlinkControlModeSelection(fx, _blinkControlMode);

                // 眉アシスト
                if (_generateBrowAssistShapes)
                {
                    GenerateBrowAssistShapesIfNeeded(workingFaceSmr, installOutputFolder, _arkitShapePrefix, _ueFallbackEnabled);
                    InjectBrowAssistBinding(fx, realSmrPath, _browAssistIntensity);
                }

                // 舌アシスト(検出頂点の持ち上げ + tongueOut本体とのミックス)。
                // tongueOutの0%→100%遷移の50%地点(唇を越えるタイミング)で持ち上げが
                // 最大になるピーク形状を生成し、標準の舌駆動BlendTree
                // (hinzkaUE_Gain_v2_TongueOut)へ組み込む。
                if (_generateTongueAssistShapes)
                {
                    // mm(ワールド実寸)で指定された閾値を、このSMRのメッシュ空間の値へ変換する
                    // (AutoDetectモードのときのみ使用される)。
                    float tongueUnit = ResolveTongueMeshUnit(workingFaceSmr);
                    float tongueDetectThresholdMesh = (_tongueDetectThresholdMm / 1000f) / tongueUnit;
                    float tongueLipExcludeThresholdMesh = (_tongueLipExcludeThresholdMm / 1000f) / tongueUnit;

                    var tongueResult = GenerateTongueAssistShapesIfNeeded(
                        workingFaceSmr, installOutputFolder, _arkitShapePrefix,
                        _tongueLiftSource, _tongueExistingLiftShapeName, _tongueExistingLiftShapeWeight,
                        _tongueMoveAmount, tongueDetectThresholdMesh, tongueLipExcludeThresholdMesh,
                        _tongueLiftAxis, _tongueExcludeTeethFromPrimary);
                    if (tongueResult.anyMixShapeCreated && !string.IsNullOrEmpty(tongueResult.peakShapeName))
                    {
                        ApplyTongueLiftEnvelope(fx, realSmrPath, tongueResult.rawTongueOutPropName, tongueResult.peakShapeName);
                    }
                }

                // まばたきエフェクト(おまけ機能)。既定の演出は同梱していない
                // (アバターごとにシェイプキー構成が異なるため)。OFFならレイヤーごと除去、
                // クリップ指定があれば差し替える。
                if (!_addBlinkEffect)
                    RemoveLayerByName(fx, BLINK_EFFECT_LAYER_NAME);
                else if (_blinkEffectClip != null)
                    ApplyBlinkEffectClip(fx, _blinkEffectClip);

                EditorUtility.SetDirty(fx);
                AssetDatabase.SaveAssets();

                // Menu / Parametersを専用フォルダへコピー
                var menuDst  = installOutputFolder + "/ARKit_FT_Menu.asset";
                var paramDst = installOutputFolder + "/ARKit_FT_Parameters.asset";
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(templateMenu), menuDst))
                    throw new InvalidOperationException(ArkitFTLoc.T("Menuテンプレートのコピーに失敗しました。"));
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(templateParam), paramDst))
                    throw new InvalidOperationException(ArkitFTLoc.T("Parametersテンプレートのコピーに失敗しました。"));
                AssetDatabase.SaveAssets();

                var menuAsset  = AssetDatabase.LoadAssetAtPath<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu>(menuDst);
                var paramAsset = AssetDatabase.LoadAssetAtPath<VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters>(paramDst);
                if (menuAsset == null || paramAsset == null)
                    throw new InvalidOperationException(ArkitFTLoc.T("生成したMenuまたはParametersを読み込めませんでした。"));

                var inst = new GameObject("hinzka_ARKit_FT");
                inst.transform.SetParent(workingAvatar.transform, false);
                inst.transform.localPosition = Vector3.zero;

                var ma = inst.AddComponent<ModularAvatarMergeAnimator>();
                ma.animator = fx;
                ma.layerType = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor.AnimLayerType.FX;
                ma.pathMode = MergeAnimatorPathMode.Absolute;
                ma.matchAvatarWriteDefaults = false;

                var mi = inst.AddComponent<ModularAvatarMenuInstaller>();
                mi.menuToAppend = menuAsset;

                // 中身が空のARKitシェイプキーを検出する
                var emptyShapeNames = _disableSyncForEmptyShapes
                    ? DetectEmptyArkitShapeNames(workingFaceSmr, _arkitShapePrefix)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 対応表アセット(ARKit_FT_ShapeParamMap.asset)が見つかれば、1対1対応が確認できる
                // シェイプ名だけに絞る(BrowOuterUp等、左右/正負を1パラメータにまとめているものは
                // 対象から除外される)。見つからない場合は空シェイプ名をそのまま使う
                // (従来通りの部分一致、やや不正確)。
                HashSet<string> matchTargetNames;
                if (_disableSyncForEmptyShapes && _shapeParameterMap != null)
                {
                    var oneToOneMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var e in _shapeParameterMap.entries)
                        if (e != null && !string.IsNullOrEmpty(e.arkitShapeName) && !string.IsNullOrEmpty(e.oscmoothParamName))
                            oneToOneMap[e.arkitShapeName] = e.oscmoothParamName;

                    matchTargetNames = new HashSet<string>(
                        emptyShapeNames.Where(s => oneToOneMap.ContainsKey(s))
                                       .Select(s => oneToOneMap[s]),
                        StringComparer.OrdinalIgnoreCase);
                    var skipped = emptyShapeNames.Where(s => !oneToOneMap.ContainsKey(s)).ToList();
                    if (skipped.Count > 0)
                        Debug.Log($"[hinzka ARKit FT] 空シェイプのうち{skipped.Count}個は、対応表上で" +
                                  $"1対1対応が確認できなかったため対象から除外しました: {string.Join(", ", skipped)}");
                }
                else
                {
                    matchTargetNames = emptyShapeNames;
                }

                var mp = inst.AddComponent<ModularAvatarParameters>();
                int emptyShapeParamsDisabledCount = 0;
                int emptyShapeParamsDisabledBits = 0; // 同期オフ(localOnly化)で削減されたbit数
                var emptyShapeParamsDisabledNames = new List<string>();
                if (paramAsset.parameters != null)
                {
                    foreach (var p in paramAsset.parameters)
                    {
                        if (p == null || string.IsNullOrEmpty(p.name)) continue;

                        ParameterSyncType syncType;
                        switch (p.valueType)
                        {
                            case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Int:
                                syncType = ParameterSyncType.Int; break;
                            case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float:
                                syncType = ParameterSyncType.Float; break;
                            case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool:
                                syncType = ParameterSyncType.Bool; break;
                            default:
                                syncType = ParameterSyncType.Float; break;
                        }

                        // パラメータ名に対象名(OSCmooth Configで1対1確認済み、またはARKit標準名)が
                        // 含まれていれば、同期を強制的にオフにする。
                        bool matchesEmptyShape = matchTargetNames.Any(name =>
                            p.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);

                        // 元々同期がONだったものだけを「今回オフにした」として数える
                        // (元々OFFだったものを含めると実態より多い数を報告してしまうため)
                        if (matchesEmptyShape && p.networkSynced)
                        {
                            emptyShapeParamsDisabledCount++;
                            emptyShapeParamsDisabledBits += (syncType == ParameterSyncType.Bool) ? 1 : 8;
                            emptyShapeParamsDisabledNames.Add(p.name);
                        }

                        mp.parameters.Add(new ParameterConfig
                        {
                            nameOrPrefix = p.name,
                            isPrefix = false,
                            syncType = syncType,
                            localOnly = !p.networkSynced || matchesEmptyShape,
                            saved = p.saved,
                            hasExplicitDefaultValue = true,
                            defaultValue = p.defaultValue,
                        });
                    }
                }
                if (_disableSyncForEmptyShapes)
                {
                    Debug.Log($"[hinzka ARKit FT] 中身が空のARKitシェイプキー{emptyShapeNames.Count}個を検出しました" +
                              (emptyShapeNames.Count > 0 ? $": {string.Join(", ", emptyShapeNames)}" : "") + "\n" +
                              $"実際に同期をオフにしたパラメータ: {emptyShapeParamsDisabledCount}個" +
                              (emptyShapeParamsDisabledCount > 0
                                  ? $" (同期bit -{emptyShapeParamsDisabledBits}bit): {string.Join(", ", emptyShapeParamsDisabledNames)}"
                                  : ""));
                }

                // Expression Parametersの合計ビット数チェック(ARKit分のみ先に確定させておく。
                // UE分を含めた最終チェックはUE用FXインストール後にまとめて行う)。
                int ftParamBits = ComputeVrcParameterBits(paramAsset) - emptyShapeParamsDisabledBits;
                int existingParamBits = ComputeVrcParameterBits(workingDesc.expressionParameters);

                // MA部分をPrefab化して配置
                var prefabDst = installOutputFolder + "/ARKit_FT_Installed.prefab";
                var saved = PrefabUtility.SaveAsPrefabAsset(inst, prefabDst);
                DestroyImmediate(inst);
                if (saved == null)
                    throw new InvalidOperationException(ArkitFTLoc.T("Modular Avatar用Prefabの保存に失敗しました。"));

                var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabDst);
                if (savedPrefab == null)
                    throw new InvalidOperationException(ArkitFTLoc.T("保存したModular Avatar用Prefabを読み込めませんでした。"));

                var placed = (GameObject)PrefabUtility.InstantiatePrefab(savedPrefab, workingAvatar.transform);
                if (placed == null)
                    throw new InvalidOperationException(ArkitFTLoc.T("Modular Avatar用Prefabの配置に失敗しました。"));
                placed.transform.localPosition = Vector3.zero;

                // Expression Parametersの合計ビット数チェック(VRChatの上限は256bit)。
                int totalParamBits = ftParamBits + existingParamBits;
                bool paramBitsOverBudget = totalParamBits > VRC_PARAM_BIT_BUDGET;
                if (paramBitsOverBudget)
                {
                    Debug.LogWarning($"[hinzka ARKit FT] Expression Parametersの合計が{totalParamBits}bit " +
                                      $"(既存{existingParamBits}bit + FT追加{ftParamBits}bit" +
                                      $") / 上限{VRC_PARAM_BIT_BUDGET}bit を" +
                                      "超えています。VRChatへのアップロード時にエラーになる可能性があります。" +
                                      "不要なパラメータの同期をオフにするなどして削減してください。");
                }

                AssetDatabase.Refresh();

                if (sourceWasSceneInstance && _avatarPrefab != null)
                {
                    Undo.RecordObject(_avatarPrefab, "ARKit FT Install (Deactivate Source)");
                    _avatarPrefab.SetActive(false);
                }

                installSucceeded = true;

                string eyeLookStatus = _generateEyeLookShapes ? string.Format(ArkitFTLoc.T("有効(強度{0:0.0}x)"), _eyeLookIntensity) : ArkitFTLoc.T("無効");
                if (_generateEyeLookShapes && _lastEyeLookEmptyDeltaShapes.Count > 0)
                {
                    eyeLookStatus += $"\n  ⚠ 以下のシェイプキーは生成されましたが、頂点差分がほぼゼロでした" +
                                      $"(目ボーンを回転させてもFace SMR側が追従して変形していません。" +
                                      $"Face SMRと眼球メッシュが別々になっている場合によく起こります):\n" +
                                      $"  {string.Join(", ", _lastEyeLookEmptyDeltaShapes)}";
                }

                var msg = $"インストール完了(複製アバター: {workingAvatar.name})\n" +
                          $"SMR: {realSmrPath}\n" +
                          $"にっこり目: {(squintShapeNames.Count > 0 ? string.Join(", ", squintShapeNames) : "未指定")}\n" +
                          $"ジェスチャーレイヤー: {(distinctGestureLayers.Count > 0 ? string.Join(", ", distinctGestureLayers.Select(i => $"{i}:{_fxLayerNames[i]}")) : "未指定")}\n" +
                          $"Viseme補償: {visemeResult}{(_visemeScale < 0.999f ? $" (強さ{_visemeScale:0.00}倍)" : "")}\n" +
                          $"EyeLook自動生成: {eyeLookStatus}\n" +
                          $"眉アシスト: {(_generateBrowAssistShapes ? $"有効(強度{_browAssistIntensity:P0})" : "無効")}\n" +
                          $"ネイティブEye Look: {(_disableNativeEyeLook ? "無効化(FTオフ時は目が動きません)" : "有効のまま")}\n" +
                          $"メッシュ統合ヘルパー: {(_eyeSmrSeparate ? $"未使用(FXがFace SMR/{_smrPaths[_eyeSmrIndex]}へ直接カーブを振り分けます)" : "未使用")}\n" +
                          $"Expression Parameters: {totalParamBits}bit / {VRC_PARAM_BIT_BUDGET}bit" +
                          (paramBitsOverBudget ? $" ⚠ 上限を超えています(既存{existingParamBits}bit + FT追加{ftParamBits}bit)" : "") +
                          (_disableSyncForEmptyShapes
                              ? $"\n  空シェイプ検出: {emptyShapeNames.Count}個 / 実際に同期オフにしたパラメータ: {emptyShapeParamsDisabledCount}個" +
                                (emptyShapeParamsDisabledBits > 0 ? $" (同期bit -{emptyShapeParamsDisabledBits}bit)" : "") +
                                $"{(_shapeParameterMap != null ? " (対応表参照・1対1対応のみ)" : " (部分一致・要確認)")}"
                              : "") + "\n" +
                          $"出力先: {installOutputFolder}";
                Debug.Log("[hinzka ARKit FT] " + msg);

                _lastInstallResult = new InstallResultSummary
                {
                    avatar = workingAvatar,
                    avatarName = workingAvatar.name,
                    faceSmr = realSmrPath,
                    eyeSmr = _eyeSmrSeparate && _eyeSmrIndex >= 0 && _eyeSmrIndex < _smrPaths.Length
                        ? _smrPaths[_eyeSmrIndex] : "",
                    squint = squintShapeNames.Count > 0 ? string.Join(", ", squintShapeNames) : ArkitFTLoc.T("未指定"),
                    gestures = distinctGestureLayers.Count > 0
                        ? string.Join(", ", distinctGestureLayers.Select(i => $"{_fxLayerNames[i]} [{i}]"))
                        : ArkitFTLoc.T("未指定"),
                    viseme = visemeResult + (_visemeScale < 0.999f ? string.Format(ArkitFTLoc.T(" / 強さ {0:0.00}"), _visemeScale) : ""),
                    eyeLook = eyeLookStatus,
                    brow = _generateBrowAssistShapes ? string.Format(ArkitFTLoc.T("有効 / 強度 {0:P0}"), _browAssistIntensity) : ArkitFTLoc.T("無効"),
                    blinkEffect = !_addBlinkEffect
                        ? ArkitFTLoc.T("なし")
                        : (_blinkEffectClip != null
                            ? string.Format(ArkitFTLoc.T("有効 / クリップ設定済み ({0})"), _blinkEffectClip.name)
                            : ArkitFTLoc.T("有効 / クリップ未設定(演出は再生されません)")),
                    nativeEyeLook = _disableNativeEyeLook
                        ? ArkitFTLoc.T("無効化 / FT OFF時は自動目線なし・自動まばたきなし")
                        : ArkitFTLoc.T("維持 / Compatibility"),
                    parameters = $"{totalParamBits} / {VRC_PARAM_BIT_BUDGET} bit" +
                                 (paramBitsOverBudget ? string.Format(ArkitFTLoc.T("  ⚠ 超過（既存 {0} + FT {1}）"), existingParamBits, ftParamBits) : ""),
                    parametersOverBudget = paramBitsOverBudget,
                    emptySync = _disableSyncForEmptyShapes
                        ? string.Format(ArkitFTLoc.T("空/未検出 {0}件 → 同期OFF {1}件"), emptyShapeNames.Count, emptyShapeParamsDisabledCount) +
                          (emptyShapeParamsDisabledBits > 0 ? $" / -{emptyShapeParamsDisabledBits}bit" : "")
                        : "",
                    ueFallback = (_ueFallbackEnabled && _ueFallbackResolvedShapes.Count > 0)
                        ? string.Format(ArkitFTLoc.T("UE代替名で解決: {0}件"), _ueFallbackResolvedShapes.Count)
                        : "",
                    missingShapes = missingShapesSummary,
                    outputFolder = installOutputFolder,
                };

                // Viseme縮小・逆Viseme生成・EyeLook・眉アシスト・舌アシストの各ステップは、
                // 直前のメッシュを元に新しいメッシュアセットを生成し、Face SMRへ順に差し替えて
                // いく方式になっている。そのため、最終的にFace SMRへ実際に割り当てられている
                // メッシュ以外は、途中経過として生成されただけの不要な中間生成物である。
                // ここでその他のメッシュアセットを削除し、出力フォルダに最終メッシュだけが
                // 残るようにする。
                // ただし、目メッシュがFace SMRとは別々(_eyeSmrSeparate)の場合、EyeLook生成は
                // Face SMRとは完全に別のメッシュチェーン(workingEyeSmr側)に対して行われる。
                // これを「最終メッシュ」の判定に含めずFace SMR側のパスとだけ比較すると、
                // Eye SMR側の最終メッシュも常に不一致とみなされて誤って削除されてしまい、
                // 目メッシュが消える・目線シェイプキーも道連れで失われる不具合の原因になる。
                // そのため、Eye SMRが分離している場合はそちらの最終メッシュも保護対象に含める。
                if (workingFaceSmr != null && workingFaceSmr.sharedMesh != null && _installGeneratedMeshPaths.Count > 0)
                {
                    var finalMeshPaths = new HashSet<string> { AssetDatabase.GetAssetPath(workingFaceSmr.sharedMesh) };
                    if (_eyeSmrSeparate && workingEyeSmr != null && workingEyeSmr.sharedMesh != null)
                        finalMeshPaths.Add(AssetDatabase.GetAssetPath(workingEyeSmr.sharedMesh));

                    int deletedIntermediateMeshCount = 0;
                    foreach (var meshPath in _installGeneratedMeshPaths)
                    {
                        if (string.IsNullOrEmpty(meshPath) || finalMeshPaths.Contains(meshPath)) continue;
                        if (AssetDatabase.DeleteAsset(meshPath)) deletedIntermediateMeshCount++;
                    }
                    if (deletedIntermediateMeshCount > 0)
                    {
                        AssetDatabase.Refresh();
                        Debug.Log($"[hinzka ARKit FT] 最終的に採用されなかった中間生成メッシュ{deletedIntermediateMeshCount}個を削除しました。");
                    }
                }

                // Install完了後、複製元アバターが非表示になる等でScene View上のプレビュー球が
                // 対象を失ったまま残ってしまうことがあるため、完了時にプレビュー表示を自動でOFFにする。
                if (_showTonguePreview)
                {
                    _showTonguePreview = false;
                    _tonguePreviewVertexIndices = null;
                    _tonguePreviewBaseVertices = null;
                    SceneView.RepaintAll();
                }

                RefreshToolkitUI();
                SelectToolkitTab(2);
                ShowNotification(new GUIContent(ArkitFTLoc.T("✓ インストール完了")));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("NK Installer Error",
                    ArkitFTLoc.T("インストールに失敗しました。途中生成物は可能な範囲で削除します。\n\n") + ex.Message, "OK");
            }
            finally
            {
                if (!installSucceeded)
                {
                    if (workingAvatar != null)
                        DestroyImmediate(workingAvatar);

                    if (!string.IsNullOrEmpty(installOutputFolder) && AssetDatabase.IsValidFolder(installOutputFolder))
                    {
                        AssetDatabase.DeleteAsset(installOutputFolder);
                        AssetDatabase.Refresh();
                    }
                }
            }
        }

        // ── SMRパス一括置換 ──────────────────────────────

        /// <summary>
        /// FX内の全アニメーションクリップのEditorCurveBindingのpathを
        /// oldPath → newPath に一括置換する。
        /// </summary>
        /// <summary>
        /// oldPathを持つカーブのうち、blendShape名がspecialShapeNamesに含まれるものだけ
        /// specialNewPathへ、それ以外はdefaultNewPathへ振り分けて書き換える。
        /// 表情メッシュと目メッシュが別々のSkinnedMeshRendererな場合に、メッシュを物理統合せず
        /// FXのカーブの向き先だけを出し分けて両方を動かすために使う。
        /// </summary>
        private static void RewriteSmrPathsSelective(
            AnimatorController fx, string oldPath, string defaultNewPath,
            HashSet<string> specialShapeNames, string specialNewPath)
        {
            var clips = new HashSet<AnimationClip>();
            foreach (var layer in fx.layers)
                CollectClips(layer.stateMachine, clips);

            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                bool dirty = false;
                foreach (var b in bindings)
                {
                    if (b.path != oldPath) continue;

                    string targetPath = defaultNewPath;
                    if (b.propertyName.StartsWith("blendShape."))
                    {
                        var shapeName = b.propertyName.Substring("blendShape.".Length);
                        if (specialShapeNames.Contains(shapeName))
                            targetPath = specialNewPath;
                    }
                    if (targetPath == oldPath) continue; // 変更不要

                    var curve = AnimationUtility.GetEditorCurve(clip, b);
                    AnimationUtility.SetEditorCurve(clip, b, null);
                    var nb = b;
                    nb.path = targetPath; // EditorCurveBindingはstruct
                    AnimationUtility.SetEditorCurve(clip, nb, curve);
                    dirty = true;
                }
                if (dirty) EditorUtility.SetDirty(clip);
            }
        }

        /// <summary>
        /// FX内の全blendShapeカーブのうち、プロパティ名(shape名)が標準ARKit名(ARKIT_SHAPE_NAMES)に
        /// 一致するものだけ、接頭辞を付けて書き換える。アバターのARKitシェイプキーに独自の接頭辞が
        /// 付いている場合、Driver自体が実在するシェイプキー名へ値を書き込むようにするために必要
        /// (接頭辞なしでは、存在しない標準名へ値を書き込み続けてしまい、トラッキングが根本的に
        /// 効かない)。
        ///
        /// prefixesは候補接頭辞のリスト(カンマ区切り指定に対応。アバターによっては、標準ARKit
        /// シェイプキーに複数の異なる接頭辞が混在している場合があるため)。meshを渡した場合、
        /// シェイプごとに実際にメッシュ上に存在する接頭辞を優先して選ぶ。meshが無い、またはどの
        /// 候補でも見つからない場合は、先頭の候補接頭辞をフォールバックとして使う。
        /// </summary>
        private static void RewriteArkitBlendShapeNames(AnimatorController fx, string[] prefixes, Mesh mesh)
        {
            if (fx == null || prefixes == null || prefixes.Length == 0) return;
            // 接頭辞が全て空("")でも、meshが渡されていれば大文字小文字違いだけの補正を
            // 行う必要があるため、ここでは終了しない。接頭辞が全て空、かつmeshも無い
            // (=接頭辞の付与も大文字小文字の補正もできない)場合のみスキップする。
            if (prefixes.All(string.IsNullOrEmpty) && mesh == null) return;

            var arkitSet = new HashSet<string>(ARKIT_SHAPE_NAMES);
            const string bsPrefix = "blendShape.";
            var nameLookup = mesh != null ? BuildTrimmedShapeNameLookup(mesh) : null;

            var clips = new HashSet<AnimationClip>();
            foreach (var layer in fx.layers)
                CollectClips(layer.stateMachine, clips);

            int rewrittenCount = 0;
            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                bool dirty = false;
                foreach (var b in bindings)
                {
                    if (b.type != typeof(SkinnedMeshRenderer)) continue;
                    if (!b.propertyName.StartsWith(bsPrefix)) continue;

                    var shapeName = b.propertyName.Substring(bsPrefix.Length);
                    if (!arkitSet.Contains(shapeName)) continue; // 標準ARKit名以外は触らない
                    if (prefixes.Any(p => !string.IsNullOrEmpty(p) && shapeName.StartsWith(p, StringComparison.Ordinal)))
                        continue; // 既に接頭辞付きなら何もしない(念のため)

                    // 候補接頭辞のうち、実際にメッシュ上に存在するものを優先して選ぶ。
                    // 大文字小文字違い(例: "EyeBlinkLeft"等、頭文字が大文字になっている
                    // アバター)にも対応するため、判定だけでなく実際の書き込み先の名前も
                    // メッシュ上の実名(nameLookupが返す実際の綴り)をそのまま使う。
                    // どれも見つからない場合は先頭の候補をそのまま使う(フォールバック)。
                    string finalName = null;
                    if (nameLookup != null)
                    {
                        foreach (var p in prefixes)
                        {
                            string candidate = (string.IsNullOrEmpty(p) ? shapeName : p + shapeName).Trim();
                            if (nameLookup.TryGetValue(candidate, out var actual))
                            {
                                finalName = actual; // メッシュ上の実際の綴り(大文字小文字含む)をそのまま使う
                                break;
                            }
                        }
                    }
                    if (finalName == null)
                    {
                        string chosenPrefix = prefixes[0];
                        if (string.IsNullOrEmpty(chosenPrefix)) continue; // 空接頭辞が選ばれた=接頭辞不要
                        finalName = chosenPrefix + shapeName;
                    }
                    else if (finalName == shapeName)
                    {
                        continue; // 実質「接頭辞無し」で解決した(大文字小文字違いのみ等)ため書き換え不要
                    }

                    var curve = AnimationUtility.GetEditorCurve(clip, b);
                    AnimationUtility.SetEditorCurve(clip, b, null);
                    var nb = b;
                    nb.propertyName = bsPrefix + finalName; // EditorCurveBindingはstruct
                    AnimationUtility.SetEditorCurve(clip, nb, curve);
                    dirty = true;
                    rewrittenCount++;
                }
                if (dirty) EditorUtility.SetDirty(clip);
            }

            if (rewrittenCount > 0)
                Debug.Log($"[hinzka ARKit FT] FX内の標準ARKitシェイプキーカーブ{rewrittenCount}個に接頭辞を付与しました" +
                          $"(候補: {string.Join(", ", prefixes)})。");
        }

        /// <summary>
        /// ARKit標準名(接頭辞込み)がメッシュ上に見つからないシェイプについて、UE(Unified
        /// Expressions)側の代替名でカーブを差し替える。ARKIT_TO_UE_FALLBACKの候補グループを
        /// 優先順に確認し、グループ内の全シェイプがメッシュ上に存在する最初のグループを採用する。
        ///
        /// 採用したグループが複数名からなる場合(例: CheekPuffLeft + CheekPuffRight)は、
        /// 元のARKitカーブと同じ内容を複数のBlendShapeプロパティへ複製設定する。これにより
        /// 1つのOSCパラメータで複数のBlendShapeが同時に駆動される(Unityの通常の仕組みで、
        /// 特別な対応は不要)。
        /// </summary>
        private static void RewriteMissingArkitShapesToUeFallback(
            AnimatorController fx, Mesh mesh, string arkitPrefix)
        {
            if (fx == null || mesh == null) return;

            var nameLookup = BuildTrimmedShapeNameLookup(mesh);
            var prefixes = ParsePrefixList(arkitPrefix);

            const string bsPrefix = "blendShape.";
            var clips = new HashSet<AnimationClip>();
            foreach (var layer in fx.layers)
                CollectClips(layer.stateMachine, clips);

            int resolvedShapeCount = 0;
            int rewrittenCurveCount = 0;

            foreach (var arkitName in ARKIT_SHAPE_NAMES)
            {
                // 候補接頭辞(カンマ区切り指定に対応)のいずれでも標準名が見つからない場合のみ、
                // UE代替名を試す。
                if (ResolveNameAcrossPrefixes(nameLookup, arkitName, prefixes) != null) continue;

                if (!ARKIT_TO_UE_FALLBACK.TryGetValue(arkitName, out var candidateGroups)) continue;
                var chosenGroup = candidateGroups.FirstOrDefault(g => g.All(n => nameLookup.ContainsKey(n.Trim())));
                if (chosenGroup == null) continue; // UE代替名も見つからない(引き続き不足のまま)

                // 実際にメッシュ上にある名前(前後の空白を含む可能性がある)に変換する。
                // ここを取り違えると、Unityが実在しない名前のプロパティとして扱い、
                // 見た目上は解決できたはずのシェイプが実際には全く動かなくなってしまう。
                var actualTargetNames = chosenGroup.Select(n => nameLookup[n.Trim()]).ToArray();

                // RewriteArkitBlendShapeNamesは、候補接頭辞のどれでもメッシュ上に見つからない場合、
                // 先頭の候補接頭辞をフォールバックとして使ってFXカーブへ書き込んでいる。
                // ここでも同じ規則でFX内の検索対象プロパティ名を組み立てる。
                string fallbackPrefix = prefixes[0];
                string resolvedName = string.IsNullOrEmpty(fallbackPrefix) ? arkitName : fallbackPrefix + arkitName;
                string oldProp = bsPrefix + resolvedName;
                bool anyRewritten = false;

                foreach (var clip in clips)
                {
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    bool dirty = false;
                    foreach (var b in bindings)
                    {
                        if (b.type != typeof(SkinnedMeshRenderer)) continue;
                        if (b.propertyName != oldProp) continue;

                        var curve = AnimationUtility.GetEditorCurve(clip, b);
                        AnimationUtility.SetEditorCurve(clip, b, null);
                        foreach (var actualTargetName in actualTargetNames)
                        {
                            var nb = b;
                            nb.propertyName = bsPrefix + actualTargetName;
                            AnimationUtility.SetEditorCurve(clip, nb, curve);
                        }
                        dirty = true;
                        anyRewritten = true;
                        rewrittenCurveCount++;
                    }
                    if (dirty) EditorUtility.SetDirty(clip);
                }

                if (anyRewritten)
                {
                    resolvedShapeCount++;
                    Debug.Log($"[hinzka ARKit FT] '{arkitName}' が見つからないため、" +
                              $"UE代替名 [{string.Join(", ", chosenGroup)}] へ差し替えました。");
                }
            }

            if (resolvedShapeCount > 0)
                Debug.Log($"[hinzka ARKit FT] UE代替名での解決: {resolvedShapeCount}シェイプ" +
                          $"(カーブ{rewrittenCurveCount}個を複製設定)");
        }

        private static void RewriteSmrPaths(AnimatorController fx, string oldPath, string newPath)
        {
            var clips = new HashSet<AnimationClip>();
            foreach (var layer in fx.layers)
                CollectClips(layer.stateMachine, clips);

            foreach (var clip in clips)
            {
                var bindings = AnimationUtility.GetCurveBindings(clip);
                bool dirty = false;
                foreach (var b in bindings)
                {
                    if (b.path != oldPath) continue;
                    var curve = AnimationUtility.GetEditorCurve(clip, b);
                    AnimationUtility.SetEditorCurve(clip, b, null);
                    var nb = b;
                    nb.path = newPath; // EditorCurveBindingはstruct
                    AnimationUtility.SetEditorCurve(clip, nb, curve);
                    dirty = true;
                }
                if (dirty) EditorUtility.SetDirty(clip);
            }
        }


        /// <summary>
        /// メッシュ上の全BlendShape名を、前後の空白を取り除いた文字列をキーとして引ける
        /// 辞書にして返す(値は実際のメッシュ上の名前そのもの、空白を含む)。
        ///
        /// Blenderなどで作成されたシェイプキーは、名前の末尾に半角スペースが紛れ込んでいても
        /// Inspector上の表示では気付きにくい。Unity標準のGetBlendShapeIndex/名前比較は
        /// 完全一致でしかヒットしないため、このような「見た目は同じだが実際には違う文字列」の
        /// シェイプキーが、ARKit標準名・UE代替名のどちらでも見つからず「不足」と誤判定される
        /// 事故が実際に発生した。判定(検索)は前後の空白を無視して行い、実際にカーブを
        /// 書き込む際は辞書の値(実際の名前、空白込み)を使うことで、この問題を回避する。
        /// </summary>
        /// <summary>
        /// メッシュ空間の1単位が、ワールド空間で何メートルになるかを返す。
        /// アーマチュアが100倍スケール等になっているアバターでは、メッシュ空間の生の頂点移動量
        /// (Unity単位)がそのまま実寸(m)を表さない。舌アシストの検出閾値をmm単位で
        /// 直感的に指定できるようにするため、メッシュ空間(ローカル)のバウンディングボックスと、
        /// 現在のポーズでのワールド空間バウンディングボックスの比率からスケール係数を求める。
        /// (特定の1本のボーンのbindposeに依存する方式だと、そのボーンがたまたまこのメッシュの
        /// スキニングに使われていない/特殊なスケールを持つ場合に誤った係数を返してしまうため、
        /// メッシュ全体の見かけの大きさを比較するこちらの方式の方が頑健)。
        /// </summary>
        private static float MeshUnitToWorld(SkinnedMeshRenderer smr)
        {
            if (smr == null) return 1f;
            var mesh = smr.sharedMesh;
            if (mesh == null) return 1f;

            float localSize = mesh.bounds.size.magnitude;
            if (localSize < 1e-6f) return 1f;

            float worldSize = smr.bounds.size.magnitude; // 現在のポーズでのワールド境界
            if (worldSize < 1e-6f)
            {
                // SMRのboundsが未計算(0)の場合のフォールバック: Transformのスケールのみで代替する
                return Mathf.Max(1e-6f, smr.transform.lossyScale.magnitude / Mathf.Sqrt(3f));
            }

            float s = worldSize / localSize;
            return s > 1e-6f ? s : 1f;
        }

        /// <summary>
        /// 舌アシストの単位変換係数を解決する。_tongueUnitOverrideが0より大きければそれを
        /// そのまま使う(自動推定がアバターによって外れることがあるため、手動上書きできるようにしている)。
        /// そうでなければMeshUnitToWorldによる自動推定を使う。
        /// </summary>
        private float ResolveTongueMeshUnit(SkinnedMeshRenderer smr)
        {
            if (_tongueUnitOverride > 0f) return _tongueUnitOverride;
            return MeshUnitToWorld(smr);
        }

        /// <summary>
        /// カンマ区切りの接頭辞文字列を候補リストへ分解する。前後の空白は除去し、空の要素・
        /// 重複は取り除く。何も指定が無い場合は「接頭辞なし」を表す単一要素の配列を返す
        /// (アバターによっては、標準ARKitシェイプキー名に複数の異なる接頭辞が混在している
        /// 場合があるため、単一文字列ではなく候補リストとして扱えるようにしている)。
        /// </summary>
        private static string[] ParsePrefixList(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new[] { "" };
            var parts = raw.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Distinct()
                .ToArray();
            return parts.Length > 0 ? parts : new[] { "" };
        }

        /// <summary>
        /// 標準シェイプ名に対して、候補接頭辞を順に試し、nameLookup(メッシュ上の実在名一覧)に
        /// 実在する最初の解決名(メッシュ上の実際の名前、空白込み)を返す。どの接頭辞でも
        /// 見つからない場合はnullを返す。
        /// </summary>
        private static string ResolveNameAcrossPrefixes(
            Dictionary<string, string> nameLookup, string standardName, string[] prefixes)
        {
            if (nameLookup == null || prefixes == null) return null;
            foreach (var p in prefixes)
            {
                string candidate = string.IsNullOrEmpty(p) ? standardName : p + standardName;
                if (nameLookup.TryGetValue(candidate.Trim(), out var actual)) return actual;
            }
            return null;
        }

        /// <summary>
        /// trimmed(前後空白除去済みのシェイプ名)の先頭が、候補接頭辞のいずれかと一致する場合、
        /// その接頭辞を取り除いた残り部分を返す。どれとも一致しなければtrimmedをそのまま返す。
        /// </summary>
        private static string StripAnyPrefix(string trimmed, string[] prefixes)
        {
            if (prefixes == null) return trimmed;
            foreach (var p in prefixes)
            {
                if (!string.IsNullOrEmpty(p) && trimmed.StartsWith(p, StringComparison.Ordinal))
                    return trimmed.Substring(p.Length);
            }
            return trimmed;
        }

        /// <summary>
        /// メッシュ上のシェイプキー名を、前後の空白を無視して検索できるルックアップ辞書として返す
        /// (キー: トリムした名前 → 値: メッシュ上の実際の名前)。
        ///
        /// 大文字・小文字の違いも同一のキーとして扱う(例: "eyeBlinkLeft"と"EyeBlinkLeft"は
        /// 同じキーにマッチする)。アバターによっては、ARKit標準では頭文字が小文字であるべき
        /// シェイプキー名が、頭文字だけ大文字になっているケースがあるため。
        /// 同じキーに対応する名前が複数存在する(大文字/小文字違いが両方ある)場合は、
        /// ARKit標準の命名規則(頭文字が小文字)に一致する方を優先して採用する。
        /// </summary>
        private static Dictionary<string, string> BuildTrimmedShapeNameLookup(Mesh mesh)
        {

            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (mesh == null) return map;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                var raw = mesh.GetBlendShapeName(i);
                var trimmed = raw.Trim();
                if (!map.TryGetValue(trimmed, out var existingRaw))
                {
                    map[trimmed] = raw; // このキーで初めて見つかった名前をひとまず採用
                }
                else
                {
                    // 大文字小文字違いで既に別の名前が登録されている場合、頭文字が小文字
                    // (ARKit標準の命名規則)の方を優先する。
                    bool existingStartsLower = existingRaw.Length > 0 && char.IsLower(existingRaw[0]);
                    bool newStartsLower = trimmed.Length > 0 && char.IsLower(trimmed[0]);
                    if (!existingStartsLower && newStartsLower)
                        map[trimmed] = raw;
                }
            }
            return map;
        }

        private static void CollectClips(AnimatorStateMachine sm, HashSet<AnimationClip> clips)
        {
            if (sm == null) return;
            foreach (var s in sm.states)
            {
                if (s.state.motion is AnimationClip c) clips.Add(c);
                else if (s.state.motion is BlendTree bt) CollectClipsFromBT(bt, clips);
            }
            foreach (var sub in sm.stateMachines)
                CollectClips(sub.stateMachine, clips);
        }

        private static void CollectClipsFromBT(BlendTree bt, HashSet<AnimationClip> clips)
        {
            if (bt == null) return;
            foreach (var child in bt.children)
            {
                if (child.motion is AnimationClip c) clips.Add(c);
                else if (child.motion is BlendTree sub) CollectClipsFromBT(sub, clips);
            }
        }

        // ── にっこり目クリップ注入 ────────────────────────

        /// <summary>
        /// テンプレートFXの "hinzkaFT_FX_ComboLayer__FTextra_EyeSquint" レイヤーを探し、
        /// にっこり目シェイプキーを動かすクリップを書き換える。
        /// </summary>
        private static void InjectSquintShapes(
            AnimatorController fx, string smrPath, List<string> shapeNames)
        {
            const string LAYER_NAME_CONTAINS = "EyeSquint";
            const string SQUINT_CLIP_MARKER  = "EyeSquint";

            foreach (var layer in fx.layers)
            {
                if (!layer.name.Contains(LAYER_NAME_CONTAINS)) continue;

                var clips = new HashSet<AnimationClip>();
                CollectClips(layer.stateMachine, clips);

                foreach (var clip in clips)
                {
                    if (!clip.name.Contains(SQUINT_CLIP_MARKER)) continue;

                    // 既存のblendShapeカーブを全クリアして書き直す
                    var bindings = AnimationUtility.GetCurveBindings(clip)
                        .Where(b => b.propertyName.StartsWith("blendShape.")).ToArray();
                    foreach (var b in bindings)
                        AnimationUtility.SetEditorCurve(clip, b, null);

                    // 指定した全シェイプキーを100にセット
                    foreach (var shapeName in shapeNames)
                    {
                        if (string.IsNullOrEmpty(shapeName)) continue;
                        AnimationUtility.SetEditorCurve(clip,
                            new EditorCurveBinding
                            {
                                type = typeof(SkinnedMeshRenderer),
                                path = smrPath,
                                propertyName = "blendShape." + shapeName,
                            },
                            AnimationCurve.Constant(0f, 0f, 100f));
                    }

                    EditorUtility.SetDirty(clip);
                }
            }
        }

        // ── 逆Visemeシェイプキー生成 ─────────────────────

        // UEFxGeneratorWindow の VisemeCompensateTokens と完全一致させる (全て小文字)
        private static readonly string[] VISEME_SUFFIX =
            { "sil", "pp", "ff", "th", "dd", "kk", "ch", "ss", "nn", "rr", "aa", "e", "i", "o", "u" };

        // モデラー製のARKit標準シェイプキー(eyeLookUpLeft等)と衝突しないための接頭辞。
        // EyeLookBoneToBlendShapeBaker.DefaultBonePrefix と一致させること。
        private const string EYELOOK_BONE_PREFIX = hinzka.FaceTracking.DevTools.EyeLookBoneToBlendShapeBaker.DefaultBonePrefix;

        /// <summary>
        /// 「新規Profile」ボタンで保存ダイアログを開いたとき、既定で開くフォルダ。
        /// あらかじめこのフォルダにアバターごとのProfileをまとめておく運用を想定している。
        /// フォルダが存在しない場合は保存時に自動作成する。
        /// </summary>
        private const string PROFILE_DEFAULT_FOLDER = "Assets/NK Installer/hinzka/ARKitInstaller/Profiles";
        private const int VRC_PARAM_BIT_BUDGET = 256;

        /// <summary>
        /// AvatarDescriptorに登録された各Visemeシェイプキーのデルタ(頂点/法線)をscale倍した
        /// 新規シェイプキーをメッシュに追加し、VisemeBlendShapesの参照先をそちらへ差し替える。
        /// 元のシェイプキー自体は削除・変更せず残す(縮小方向のみサポートし、拡大は行わない前提)。
        /// この処理は GenerateInverseVisemeShapes より必ず先に呼ぶこと
        /// (逆Visemeはこの時点のVisemeBlendShapes参照先を基準に打消し量を計算するため)。
        /// </summary>
        private static void ScaleVisemeShapesIfNeeded(
            VRC.SDK3.Avatars.Components.VRCAvatarDescriptor desc, float scale, string outputFolder)
        {
            var visemeSmr = desc.VisemeSkinnedMesh;
            if (visemeSmr == null) return;
            var srcMesh = visemeSmr.sharedMesh;
            if (srcMesh == null) return;
            if (!srcMesh.isReadable)
            {
                Debug.LogWarning("[hinzka ARKit FT] Viseme SMRのMeshのRead/Writeが無効なため、Visemeの縮小をスキップしました。");
                return;
            }

            var names = desc.VisemeBlendShapes;
            if (names == null || names.Length == 0) return;

            Mesh newMesh = null;
            int scaledCount = 0;

            for (int i = 0; i < names.Length; i++)
            {
                var shapeName = names[i];
                if (string.IsNullOrEmpty(shapeName)) continue;

                int srcIdx = srcMesh.GetBlendShapeIndex(shapeName);
                if (srcIdx < 0)
                {
                    Debug.LogWarning($"[hinzka ARKit FT] Visemeシェイプキー '{shapeName}' がメッシュに見つからないため、縮小をスキップしました。");
                    continue;
                }

                if (newMesh == null)
                {
                    newMesh = UnityEngine.Object.Instantiate(srcMesh);
                    newMesh.name = srcMesh.name + "_VisemeScaled";
                }

                string newName = shapeName + "_FTScale";
                if (newMesh.GetBlendShapeIndex(newName) < 0)
                {
                    int frameCount = newMesh.GetBlendShapeFrameCount(srcIdx);
                    int vCount = newMesh.vertexCount;
                    for (int f = 0; f < frameCount; f++)
                    {
                        float weight = newMesh.GetBlendShapeFrameWeight(srcIdx, f);
                        var dv = new Vector3[vCount];
                        var dn = new Vector3[vCount];
                        var dt = new Vector3[vCount];
                        newMesh.GetBlendShapeFrameVertices(srcIdx, f, dv, dn, dt);
                        for (int v = 0; v < vCount; v++)
                        {
                            dv[v] *= scale;
                            dn[v] *= scale;
                            dt[v] *= scale;
                        }
                        newMesh.AddBlendShapeFrame(newName, weight, dv, dn, dt);
                    }
                }

                names[i] = newName;
                scaledCount++;
            }

            if (newMesh == null || scaledCount == 0)
            {
                Debug.Log("[hinzka ARKit FT] Viseme縮小: 対象のシェイプキーがありませんでした。");
                return;
            }

            var meshPath = AssetDatabase.GenerateUniqueAssetPath(outputFolder + "/" + newMesh.name + ".asset");
            AssetDatabase.CreateAsset(newMesh, meshPath);
            _installGeneratedMeshPaths.Add(meshPath);
            AssetDatabase.SaveAssets();

            var smrSo = new SerializedObject(visemeSmr);
            smrSo.FindProperty("m_Mesh").objectReferenceValue = newMesh;
            smrSo.ApplyModifiedProperties();

            var descSo = new SerializedObject(desc);
            var visemeProp = descSo.FindProperty("VisemeBlendShapes");
            if (visemeProp != null)
            {
                for (int i = 0; i < names.Length && i < visemeProp.arraySize; i++)
                    visemeProp.GetArrayElementAtIndex(i).stringValue = names[i];
                descSo.ApplyModifiedProperties();
            }
            else
            {
                // フォールバック: SerializedPropertyで見つからない場合は直接代入
                desc.VisemeBlendShapes = names;
                EditorUtility.SetDirty(desc);
            }

            Debug.Log($"[hinzka ARKit FT] Visemeシェイプキーを{scaledCount}個、{scale:0.00}倍に縮小しました: {meshPath}");
        }

        /// <summary>
        /// AvatarDescriptorのVisemeBlendShapesから各Visemeの逆形状を生成してメッシュに追加する。
        /// 生成したシェイプキー名の配列(15個)を返す。失敗時はnullを返す。
        /// </summary>
        private static string[] GenerateInverseVisemeShapes(
            VRC.SDK3.Avatars.Components.VRCAvatarDescriptor desc, string outputFolder)
        {
            var visemeSmr = desc.VisemeSkinnedMesh;
            if (visemeSmr == null || visemeSmr.sharedMesh == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] VisemeSkinnedMeshが未設定です。Viseme補償をスキップしました。");
                return null;
            }

            var srcMesh = visemeSmr.sharedMesh;
            if (!srcMesh.isReadable)
            {
                Debug.LogWarning($"[hinzka ARKit FT] メッシュ '{srcMesh.name}' のRead/WriteがOFFです。Viseme補償をスキップしました。");
                return null;
            }

            // メッシュをコピーして作業
            var newMesh = UnityEngine.Object.Instantiate(srcMesh);
            newMesh.name = srcMesh.name + "_FT";

            int vCount = newMesh.vertexCount;
            var dV = new Vector3[vCount];
            var dN = new Vector3[vCount];
            var dT = new Vector3[vCount];
            var invV = new Vector3[vCount];
            var invN = new Vector3[vCount];
            var invT = new Vector3[vCount];

            var visemeShapes = desc.VisemeBlendShapes; // string[15]
            var inverseNames = new string[15];

            for (int vi = 0; vi < 15; vi++)
            {
                var vsName = (visemeShapes != null && vi < visemeShapes.Length) ? visemeShapes[vi] : "";
                var invName = "inverse.FT_v_" + VISEME_SUFFIX[vi];
                inverseNames[vi] = invName;

                // 既存の逆シェイプキーがあれば削除して再生成
                int existingIdx = newMesh.GetBlendShapeIndex(invName);
                if (existingIdx >= 0)
                {
                    // Unityは個別削除不可のため全クリア→再追加は重いので既存のものをそのまま使う
                    Debug.Log($"[hinzka ARKit FT] '{invName}' は既に存在するためスキップ。");
                    continue;
                }

                // Visemeシェイプキーのデルタを取得
                if (!string.IsNullOrEmpty(vsName))
                {
                    int shapeIdx = newMesh.GetBlendShapeIndex(vsName);
                    if (shapeIdx >= 0)
                    {
                        newMesh.GetBlendShapeFrameVertices(shapeIdx, 0, dV, dN, dT);
                        for (int i = 0; i < vCount; i++)
                        {
                            invV[i] = -dV[i];
                            invN[i] = -dN[i];
                            invT[i] = -dT[i];
                        }
                        newMesh.AddBlendShapeFrame(invName, 100f, invV, invN, invT);
                        continue;
                    }
                }

                // Visemeシェイプキーが存在しない場合はゼロデルタで追加
                Array.Clear(invV, 0, vCount);
                Array.Clear(invN, 0, vCount);
                Array.Clear(invT, 0, vCount);
                newMesh.AddBlendShapeFrame(invName, 100f, invV, invN, invT);
            }

            // メッシュアセットを保存
            var meshPath = outputFolder + "/" + newMesh.name + ".asset";
            AssetDatabase.CreateAsset(newMesh, meshPath);
            _installGeneratedMeshPaths.Add(meshPath);
            AssetDatabase.SaveAssets();

            // VisemeSMRのメッシュを差し替え
            // (Prefabのコンテキスト外で直接変更する場合はSerializedObjectを使う)
            var so = new SerializedObject(visemeSmr);
            so.FindProperty("m_Mesh").objectReferenceValue = newMesh;
            so.ApplyModifiedProperties();

            Debug.Log($"[hinzka ARKit FT] 逆Visemeシェイプキー15個を生成しました: {meshPath}");
            return inverseNames;
        }

        // ── EyeLookシェイプキー自動生成 ───────────────────

        /// <summary>
        /// AvatarDescriptorのEye Look角度(目ボーンの回転)から、目ボーンの動きを再現する
        /// ボーン由来の視線シェイプキー(FT_EyeBone_接頭辞、8個)のうち未生成のものだけを追加する。
        /// モデラー製のARKit標準シェイプキー(eyeLookUpLeft等)とは別名前空間なので、それらの
        /// 有無・内容には一切関与しない(既存の同名FT_EyeBone_*シェイプのみ上書きしない)。
        /// GenerateInverseVisemeShapesと同じ分担: メッシュの複製・保存・SMR再割当はこのメソッドが行う。
        /// </summary>
        private void GenerateEyeLookShapesIfNeeded(
            VRC.SDK3.Avatars.Components.VRCAvatarDescriptor desc,
            SkinnedMeshRenderer faceSmr,
            string outputFolder,
            Transform leftConstraintTarget = null,
            Transform rightConstraintTarget = null,
            float intensity = 1f,
            List<string> bakeEnableShapeNames = null)
        {
            if (desc == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] AvatarDescriptorが見つからないためEyeLook自動生成をスキップしました。");
                return;
            }
            if (faceSmr == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] Face SMRが取得できないためEyeLook自動生成をスキップしました。");
                return;
            }

            var srcMesh = faceSmr.sharedMesh; // Viseme補償が同一SMRを既に差し替えていればその結果を反映した最新メッシュ
            if (srcMesh == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] Face SMRにMeshが割り当てられていないためEyeLook自動生成をスキップしました。");
                return;
            }

            if (EyeLookBoneToBlendShapeBaker.AllShapesExist(srcMesh, EYELOOK_BONE_PREFIX))
            {
                Debug.Log($"[hinzka ARKit FT] {EYELOOK_BONE_PREFIX}eyeLook系シェイプキーは既に8個すべて揃っているため生成をスキップしました。");
                return;
            }

            var validateMsg = EyeLookBoneToBlendShapeBaker.Validate(desc, faceSmr);
            if (validateMsg != null)
            {
                Debug.LogWarning($"[hinzka ARKit FT] {validateMsg} EyeLook自動生成をスキップしました。");
                return;
            }

            // 強度が高いほど「満杯(=デルタ100%)になるウェイト値」を下げることで、
            // 実際のトラッキングがAvatarDescriptorの角度まで届かない場合でも強く動くようにする。
            float frameWeight = 100f / Mathf.Max(0.01f, intensity);

            var newMesh = UnityEngine.Object.Instantiate(srcMesh);
            newMesh.name = srcMesh.name + "_EyeLook";

            // 指定された追加シェイプキー(目のハイライト・瞳孔等のサブメッシュを手前に移動させる
            // シェイプキー等)を、ベイクの間だけ重み100にしておく。BakeMeshはその時点のSMRの
            // 実際の状態(他のシェイプキーの重みも含む)を読み取るため、サブメッシュが手前に
            // 出た状態を基準にボーン回転の移動量を計算させることができる
            // (詳細: 回転による頂点移動量は回転軸からの距離に比例するため、サブメッシュが
            // 奥にある状態のままベイクすると移動量が過小評価され、実際に手前へ出した状態で
            // 目線を動かすと眼球メッシュを貫通してしまうことがある)。
            var savedBakeShapeWeights = new List<(int index, float weight)>();
            if (bakeEnableShapeNames != null)
            {
                foreach (var shapeName in bakeEnableShapeNames)
                {
                    int idx = srcMesh.GetBlendShapeIndex(shapeName);
                    if (idx < 0)
                    {
                        Debug.LogWarning($"[hinzka ARKit FT] 目線シェイプキー生成用の追加シェイプキー '{shapeName}' が見つからないためスキップしました。");
                        continue;
                    }
                    savedBakeShapeWeights.Add((idx, faceSmr.GetBlendShapeWeight(idx)));
                    faceSmr.SetBlendShapeWeight(idx, 100f);
                }
            }

            List<string> added;
            try
            {
                added = EyeLookBoneToBlendShapeBaker.GenerateMissingShapesAdditive(
                    desc, faceSmr, newMesh, frameWeight, EYELOOK_BONE_PREFIX,
                    leftConstraintTarget, rightConstraintTarget, out var emptyDeltaNames);
                _lastEyeLookEmptyDeltaShapes = emptyDeltaNames;
            }
            finally
            {
                // ベイクのためだけの一時的な変更なので、恒久的な状態変更にならないよう必ず元へ戻す。
                foreach (var (idx, weight) in savedBakeShapeWeights)
                    faceSmr.SetBlendShapeWeight(idx, weight);
            }

            if (added.Count == 0)
            {
                Debug.Log("[hinzka ARKit FT] EyeLook: 追加対象のシェイプキーがありませんでした。");
                UnityEngine.Object.DestroyImmediate(newMesh);
                return;
            }

            var meshPath = AssetDatabase.GenerateUniqueAssetPath(outputFolder + "/" + newMesh.name + ".asset");
            AssetDatabase.CreateAsset(newMesh, meshPath);
            _installGeneratedMeshPaths.Add(meshPath);
            AssetDatabase.SaveAssets();

            // FaceSMRのメッシュを差し替え(Prefabのコンテキスト外でも安全なようSerializedObjectを使う)
            var so = new SerializedObject(faceSmr);
            so.FindProperty("m_Mesh").objectReferenceValue = newMesh;
            so.ApplyModifiedProperties();

            Debug.Log($"[hinzka ARKit FT] eyeLook系シェイプキーを{added.Count}個生成しました: " +
                      $"{string.Join(", ", added)}\n{meshPath}");
        }

        // ── 眉アシスト用シェイプキー生成 ───────────────────

        /// <summary>
        /// 標準ARKit眉シェイプキー(browInnerUp等)のデルタ(頂点/法線)をそのまま複製し、
        /// sub_brow*という別名のシェイプキーとして追加する。ボーン回転は関与せず、既存シェイプの
        /// コピーのみ。既存のsub_brow*がある場合はそのシェイプだけスキップする(上書きしない)。
        /// まばたき(v2/EyeLidLeft・v2/EyeLidRight)との連動は InjectBrowAssistBinding が別途行う
        /// (このメソッドはシェイプキー生成のみ担当)。
        ///
        /// ueFallbackEnabledがtrueの場合、標準ARKit名(接頭辞込み)で複製元が見つからないときは
        /// ARKIT_TO_UE_FALLBACKの候補グループも試す。候補グループが複数名からなる場合
        /// (例: browDownLeft → BrowLowererLeft + BrowPinchLeft)は、それぞれの頂点差分を
        /// 合算したものを複製元として使う。
        /// </summary>
        private static void GenerateBrowAssistShapesIfNeeded(SkinnedMeshRenderer faceSmr, string outputFolder, string arkitPrefix = "", bool ueFallbackEnabled = false)
        {
            if (faceSmr == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] Face SMRが取得できないため眉アシスト生成をスキップしました。");
                return;
            }
            var srcMesh = faceSmr.sharedMesh;
            if (srcMesh == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] Face SMRにMeshが割り当てられていないため眉アシスト生成をスキップしました。");
                return;
            }
            if (!srcMesh.isReadable)
            {
                Debug.LogWarning("[hinzka ARKit FT] 対象メッシュのRead/Writeが無効なため眉アシスト生成をスキップしました。");
                return;
            }

            // 複製元(ARKit標準名・UE代替名)の検索は前後の空白を無視して行う。
            var srcNameLookup = BuildTrimmedShapeNameLookup(srcMesh);
            var arkitPrefixes = ParsePrefixList(arkitPrefix);

            // (標準ARKitシェイプ名, 複製先のsub_名) ※srcは実際の検索時に候補接頭辞を順に試す
            var pairs = new (string src, string dst)[]
            {
                ("browInnerUp",    "sub_browInnerUp"),
                ("browDownLeft",   "sub_browDownLeft"),
                ("browDownRight",  "sub_browDownRight"),
                ("browOuterUpLeft",  "sub_browOuterUpLeft"),
                ("browOuterUpRight", "sub_browOuterUpRight"),
            };

            if (pairs.All(p => srcMesh.GetBlendShapeIndex(p.dst) >= 0))
            {
                Debug.Log("[hinzka ARKit FT] sub_brow*シェイプキーは既に5個すべて揃っているため生成をスキップしました。");
                return;
            }

            Mesh newMesh = null;
            int addedCount = 0;

            foreach (var p in pairs)
            {
                if (srcMesh.GetBlendShapeIndex(p.dst) >= 0) continue; // 既存はスキップ

                string directActualName = ResolveNameAcrossPrefixes(srcNameLookup, p.src, arkitPrefixes);
                int directIdx = directActualName != null ? srcMesh.GetBlendShapeIndex(directActualName) : -1;

                var sourceIndices = new List<int>();
                if (directIdx >= 0)
                {
                    sourceIndices.Add(directIdx);
                }
                else if (ueFallbackEnabled && ARKIT_TO_UE_FALLBACK.TryGetValue(p.src, out var candidateGroups))
                {
                    var chosenGroup = candidateGroups.FirstOrDefault(
                        g => g.All(n => srcNameLookup.ContainsKey(n.Trim())));
                    if (chosenGroup != null)
                    {
                        sourceIndices.AddRange(chosenGroup.Select(n => srcMesh.GetBlendShapeIndex(srcNameLookup[n.Trim()])));
                        Debug.Log($"[hinzka ARKit FT] 眉アシスト: 複製元 '{p.src}' の代わりに" +
                                  $"UE代替名 [{string.Join(", ", chosenGroup)}] を合算して使用します。");
                    }
                }

                if (sourceIndices.Count == 0)
                {
                    Debug.LogWarning($"[hinzka ARKit FT] 複製元のシェイプキー '{p.src}' が見つからないため '{p.dst}' の生成をスキップしました。");
                    continue;
                }

                if (newMesh == null)
                {
                    newMesh = UnityEngine.Object.Instantiate(srcMesh);
                    newMesh.name = srcMesh.name + "_BrowAssist";
                }

                int primaryIdx = sourceIndices[0];
                int frameCount = newMesh.GetBlendShapeFrameCount(primaryIdx);
                int vCount = newMesh.vertexCount;
                for (int f = 0; f < frameCount; f++)
                {
                    float weight = newMesh.GetBlendShapeFrameWeight(primaryIdx, f);
                    var dv = new Vector3[vCount];
                    var dn = new Vector3[vCount];
                    var dt = new Vector3[vCount];
                    newMesh.GetBlendShapeFrameVertices(primaryIdx, f, dv, dn, dt);

                    // 候補グループが複数名からなる場合、残りのソースの頂点差分を合算する
                    // (例: browDownLeft相当としてBrowLowererLeft+BrowPinchLeftを両方反映)。
                    for (int s = 1; s < sourceIndices.Count; s++)
                    {
                        int otherIdx = sourceIndices[s];
                        int otherFrameCount = newMesh.GetBlendShapeFrameCount(otherIdx);
                        if (otherFrameCount <= 0) continue;
                        int otherF = Mathf.Min(f, otherFrameCount - 1);

                        var odv = new Vector3[vCount];
                        var odn = new Vector3[vCount];
                        var odt = new Vector3[vCount];
                        newMesh.GetBlendShapeFrameVertices(otherIdx, otherF, odv, odn, odt);
                        for (int vi = 0; vi < vCount; vi++)
                        {
                            dv[vi] += odv[vi];
                            dn[vi] += odn[vi];
                            dt[vi] += odt[vi];
                        }
                    }

                    newMesh.AddBlendShapeFrame(p.dst, weight, dv, dn, dt);
                }
                addedCount++;
            }

            if (newMesh == null)
            {
                Debug.Log("[hinzka ARKit FT] 眉アシスト: 追加対象のシェイプキーがありませんでした。");
                return;
            }

            var meshPath = AssetDatabase.GenerateUniqueAssetPath(outputFolder + "/" + newMesh.name + ".asset");
            AssetDatabase.CreateAsset(newMesh, meshPath);
            _installGeneratedMeshPaths.Add(meshPath);
            AssetDatabase.SaveAssets();

            var so = new SerializedObject(faceSmr);
            so.FindProperty("m_Mesh").objectReferenceValue = newMesh;
            so.ApplyModifiedProperties();

            Debug.Log($"[hinzka ARKit FT] sub_brow*シェイプキーを{addedCount}個生成しました: {meshPath}");
        }

        // ── 眉アシストのFX注入 ───────────────────────────

        /// <summary>
        /// 生成済みFXのDriverブレンドツリー内、Eyesゲート配下(FT_EnableEyesでMuxされている
        /// eyesChildren)に、まばたき(v2/EyeLidLeft・v2/EyeLidRight)でsub_brow*シェイプキーを
        /// 動かすサブツリーを直接追加する。左目・右目それぞれ独立した3点(Blink/Neutral/Wide)の
        /// Simple1D BlendTreeを作り、Direct BTで合算する(sub_browInnerUpは左右の寄与が加算される)。
        /// 再インストール時は既存の同名サブツリーを見つけて中身だけ更新する(重複追加しない)。
        /// </summary>
        private static void InjectBrowAssistBinding(AnimatorController fx, string smrPath, float intensity)
        {
            const string PARAM_LID_LEFT  = "v2/EyeLidLeft";
            const string PARAM_LID_RIGHT = "v2/EyeLidRight";
            const float THRESHOLD_BLINK   = 0f;
            const float THRESHOLD_NEUTRAL = 0.75f; // FTAutoStop等のリセットデフォルト値に合わせる
            const float THRESHOLD_WIDE    = 1f;

            const string WRAPPER_NAME = "hinzka_BrowAssist_Blink_Direct";
            const string LEFT_BT_NAME = "hinzka_BrowAssist_Blink_Left_1D";
            const string RIGHT_BT_NAME = "hinzka_BrowAssist_Blink_Right_1D";

            if (fx == null) return;

            var driverState = FindStateByNameContains(fx, "Driver");
            if (driverState == null || !(driverState.motion is BlendTree topBt))
            {
                Debug.LogWarning("[hinzka ARKit FT] Driverステートが見つからないため、眉アシストのFX注入をスキップしました。");
                return;
            }

            BlendTree eyesMux = null;
            foreach (var c in topBt.children)
                if (c.motion is BlendTree bt && bt.name.Contains("FTEnableEyes")) { eyesMux = bt; break; }
            if (eyesMux == null || eyesMux.children.Length < 2)
            {
                Debug.LogWarning("[hinzka ARKit FT] EyesゲートのBlendTreeが見つからないため、眉アシストのFX注入をスキップしました。" +
                                  "(splitEyesMouthEnable無効なFXでは非対応です)");
                return;
            }

            var eyesChildrenChild = eyesMux.children.OrderByDescending(c => c.threshold).First();
            if (!(eyesChildrenChild.motion is BlendTree eyesChildrenBt))
            {
                Debug.LogWarning("[hinzka ARKit FT] eyesChildren Direct BTが見つからないため、眉アシストのFX注入をスキップしました。");
                return;
            }

            string constOneParam = eyesChildrenBt.children.Length > 0
                ? eyesChildrenBt.children[0].directBlendParameter
                : null;
            if (string.IsNullOrWhiteSpace(constOneParam))
            {
                Debug.LogWarning("[hinzka ARKit FT] 常時1定数パラメータが特定できないため、眉アシストのFX注入をスキップしました。");
                return;
            }

            EnsureFloatParam(fx, PARAM_LID_LEFT, 0.75f);
            EnsureFloatParam(fx, PARAM_LID_RIGHT, 0.75f);

            float w = Mathf.Clamp01(intensity) * 100f;

            // ── 左目 ──────────────────────────────
            var blinkClipL = GetOrCreateNamedClip(fx, WRAPPER_NAME + "_L_Blink");
            SetCurve(blinkClipL, smrPath, "sub_browDownLeft", w);
            SetCurve(blinkClipL, smrPath, "sub_browOuterUpLeft", 0f);
            SetCurve(blinkClipL, smrPath, "sub_browInnerUp", 0f);

            var neutralClipL = GetOrCreateNamedClip(fx, WRAPPER_NAME + "_L_Neutral");
            SetCurve(neutralClipL, smrPath, "sub_browDownLeft", 0f);
            SetCurve(neutralClipL, smrPath, "sub_browOuterUpLeft", 0f);
            SetCurve(neutralClipL, smrPath, "sub_browInnerUp", 0f);

            var wideClipL = GetOrCreateNamedClip(fx, WRAPPER_NAME + "_L_Wide");
            SetCurve(wideClipL, smrPath, "sub_browDownLeft", 0f);
            SetCurve(wideClipL, smrPath, "sub_browOuterUpLeft", w);
            SetCurve(wideClipL, smrPath, "sub_browInnerUp", w * 0.5f);

            // ── 右目 ──────────────────────────────
            var blinkClipR = GetOrCreateNamedClip(fx, WRAPPER_NAME + "_R_Blink");
            SetCurve(blinkClipR, smrPath, "sub_browDownRight", w);
            SetCurve(blinkClipR, smrPath, "sub_browOuterUpRight", 0f);
            SetCurve(blinkClipR, smrPath, "sub_browInnerUp", 0f);

            var neutralClipR = GetOrCreateNamedClip(fx, WRAPPER_NAME + "_R_Neutral");
            SetCurve(neutralClipR, smrPath, "sub_browDownRight", 0f);
            SetCurve(neutralClipR, smrPath, "sub_browOuterUpRight", 0f);
            SetCurve(neutralClipR, smrPath, "sub_browInnerUp", 0f);

            var wideClipR = GetOrCreateNamedClip(fx, WRAPPER_NAME + "_R_Wide");
            SetCurve(wideClipR, smrPath, "sub_browDownRight", 0f);
            SetCurve(wideClipR, smrPath, "sub_browOuterUpRight", w);
            SetCurve(wideClipR, smrPath, "sub_browInnerUp", w * 0.5f);

            // ── 既存のラッパー(Direct BT)があれば使い回し ──────────────
            BlendTree wrapperBt = null;
            foreach (var c in eyesChildrenBt.children)
                if (c.motion is BlendTree bt && bt.name == WRAPPER_NAME) { wrapperBt = bt; break; }

            BlendTree leftBt = null, rightBt = null;
            if (wrapperBt != null)
            {
                foreach (var c in wrapperBt.children)
                {
                    if (c.motion is BlendTree bt && bt.name == LEFT_BT_NAME) leftBt = bt;
                    else if (c.motion is BlendTree bt2 && bt2.name == RIGHT_BT_NAME) rightBt = bt2;
                }
            }

            if (wrapperBt == null)
            {
                wrapperBt = new BlendTree { name = WRAPPER_NAME, blendType = BlendTreeType.Direct, useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(wrapperBt, fx);
                HideGeneratedSubAsset(wrapperBt);

                var newChildren = eyesChildrenBt.children.ToList();
                newChildren.Add(new ChildMotion { motion = wrapperBt, directBlendParameter = constOneParam, timeScale = 1f });
                eyesChildrenBt.children = newChildren.ToArray();
                EditorUtility.SetDirty(eyesChildrenBt);
            }

            if (leftBt == null)
            {
                leftBt = new BlendTree { name = LEFT_BT_NAME, blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(leftBt, fx);
                HideGeneratedSubAsset(leftBt);
            }
            leftBt.blendParameter = PARAM_LID_LEFT;
            leftBt.children = new[]
            {
                new ChildMotion { motion = blinkClipL,   threshold = THRESHOLD_BLINK,   timeScale = 1f },
                new ChildMotion { motion = neutralClipL, threshold = THRESHOLD_NEUTRAL, timeScale = 1f },
                new ChildMotion { motion = wideClipL,    threshold = THRESHOLD_WIDE,    timeScale = 1f },
            };
            EditorUtility.SetDirty(leftBt);

            if (rightBt == null)
            {
                rightBt = new BlendTree { name = RIGHT_BT_NAME, blendType = BlendTreeType.Simple1D, useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(rightBt, fx);
                HideGeneratedSubAsset(rightBt);
            }
            rightBt.blendParameter = PARAM_LID_RIGHT;
            rightBt.children = new[]
            {
                new ChildMotion { motion = blinkClipR,   threshold = THRESHOLD_BLINK,   timeScale = 1f },
                new ChildMotion { motion = neutralClipR, threshold = THRESHOLD_NEUTRAL, timeScale = 1f },
                new ChildMotion { motion = wideClipR,    threshold = THRESHOLD_WIDE,    timeScale = 1f },
            };
            EditorUtility.SetDirty(rightBt);

            wrapperBt.children = new[]
            {
                new ChildMotion { motion = leftBt,  directBlendParameter = constOneParam, timeScale = 1f },
                new ChildMotion { motion = rightBt, directBlendParameter = constOneParam, timeScale = 1f },
            };
            EditorUtility.SetDirty(wrapperBt);
            EditorUtility.SetDirty(fx);

            Debug.Log($"[hinzka ARKit FT] 眉アシスト(まばたき連動, 強度{Mathf.Clamp01(intensity):P0})をFXへ注入しました。");
        }

        // ── 舌アシスト ────────────────────────────────────

        // 歯を含むシェイプキー名のキーワード(舌系シェイプキーが歯も一緒に動かすことがあるため、
        // そのようなキーで動く頂点は舌候補から除外する)。
        private static readonly string[] TONGUE_TOOTH_EXCLUDE_KEYWORDS = { "tooth", "teeth", "歯" };

        /// <summary>
        /// メッシュ上の"tongueOut"シェイプキーから動く頂点を集め、唇・頬系シェイプキー
        /// (mouthPucker・cheekPuff等)・歯を含む名前のシェイプキーで動く頂点を除いたものを
        /// 「舌の頂点」とする。
        ///
        /// このツールはARKit標準のFace Trackingシェイプキーを前提としているため、ARKit標準名
        /// である"tongueOut"のみを検出対象とする(tongueOutStep1・tongueOutStep2等の補助的な
        /// 段階シェイプキーを持つアバターはごく少数であるため、対応を廃止した)。
        ///
        /// 検出閾値・除外閾値はメッシュ空間の生の値(呼び出し側でmm→メッシュ空間へ変換済みのもの)を渡す。
        /// </summary>
        private static HashSet<int> DetectTongueVertices(
            Mesh mesh, string arkitPrefix, float detectThreshold, float lipExcludeThreshold,
            bool excludeTeethFromPrimary = true)
        {
            var result = new HashSet<int>();
            if (mesh == null || !mesh.isReadable) return result;

            int vertexCount = mesh.vertexCount;
            var nameLookup = BuildTrimmedShapeNameLookup(mesh);
            var arkitPrefixes = ParsePrefixList(arkitPrefix);

            // 唇・頬系シェイプキー(ARKit標準名)で動く頂点。これらは舌も一緒に動かしてしまう
            // ことがあるため(mouthPucker等)、tongueOut(主判定)には適用しない
            // (下の主判定ループ後にだけ適用する)。
            var lipCheekExcludedVertices = new HashSet<int>();
            foreach (var lipName in TONGUE_LIP_EXCLUDE_SHAPES)
            {
                string actualName = ResolveNameAcrossPrefixes(nameLookup, lipName, arkitPrefixes);
                if (actualName == null) continue;
                int idx = mesh.GetBlendShapeIndex(actualName);
                if (idx < 0) continue;

                int frame = mesh.GetBlendShapeFrameCount(idx) - 1;
                if (frame < 0) continue;
                var delta = new Vector3[vertexCount];
                mesh.GetBlendShapeFrameVertices(idx, frame, delta, null, null);
                for (int v = 0; v < vertexCount; v++)
                    if (delta[v].magnitude > lipExcludeThreshold)
                        lipCheekExcludedVertices.Add(v);
            }

            // 歯を含む名前のシェイプキー(キーワード一致)で動く頂点。tongueOutが歯を動かす
            // 正当な理由は無いはずなので、こちらはtongueOut(主判定)にも適用する
            // (先に計算しておき、主判定のアンカー選定・候補判定の時点で除外できるようにする)。
            var toothExcludedVertices = new HashSet<int>();
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string raw = mesh.GetBlendShapeName(i);
                string trimmed = raw.Trim();
                string bare = StripAnyPrefix(trimmed, arkitPrefixes);
                bool isTooth = false;
                foreach (var kw in TONGUE_TOOTH_EXCLUDE_KEYWORDS)
                    if (bare.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) { isTooth = true; break; }
                if (!isTooth) continue;

                int frame = mesh.GetBlendShapeFrameCount(i) - 1;
                if (frame < 0) continue;
                var delta = new Vector3[vertexCount];
                mesh.GetBlendShapeFrameVertices(i, frame, delta, null, null);
                for (int v = 0; v < vertexCount; v++)
                    if (delta[v].magnitude > lipExcludeThreshold)
                        toothExcludedVertices.Add(v);
            }

            float maxMagnitudeSeen = 0f;
            var primaryVertices = new HashSet<int>();
            // tongueOutで最も大きく動いた頂点(ただし歯除外に該当する頂点は除く)。
            // クラスタリング時に「頂点数が多い塊」ではなく「本当にtongueOutで大きく動いた塊」を
            // 選ぶための手がかりとして使う(Detect Thresholdを下げすぎた際に、まつげ・歯等の
            // 微小ノイズの塊の方が頂点数で上回って誤選択されるのを防ぐ)。
            int primaryAnchorVertex = -1;
            float primaryAnchorMag = 0f;

            string actualTongueOutName =
                ResolveNameAcrossPrefixes(nameLookup, "tongueOut", arkitPrefixes) ??
                (nameLookup.TryGetValue("TongueOut", out var ueDirect) ? ueDirect : null); // UE代替名フォールバック

            if (actualTongueOutName == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] 舌アシスト: メッシュ上に'tongueOut'シェイプキーが見つかりませんでした。" +
                    "接頭辞(Blendshapeに接頭辞がある設定)や、アバターにtongueOutシェイプキーが" +
                    "存在するかどうかをご確認ください。");
                return result;
            }

            int tongueOutIdx = mesh.GetBlendShapeIndex(actualTongueOutName);
            int tongueOutFrame = tongueOutIdx >= 0 ? mesh.GetBlendShapeFrameCount(tongueOutIdx) - 1 : -1;
            if (tongueOutIdx >= 0 && tongueOutFrame >= 0)
            {
                var delta = new Vector3[vertexCount];
                mesh.GetBlendShapeFrameVertices(tongueOutIdx, tongueOutFrame, delta, null, null);
                for (int v = 0; v < vertexCount; v++)
                {
                    float mag = delta[v].magnitude;
                    if (mag > maxMagnitudeSeen) maxMagnitudeSeen = mag;
                    bool toothExcludedHere = excludeTeethFromPrimary && toothExcludedVertices.Contains(v);
                    if (!toothExcludedHere && mag > primaryAnchorMag)
                    {
                        primaryAnchorMag = mag; primaryAnchorVertex = v;
                    }
                    if (mag > detectThreshold && !toothExcludedHere)
                        primaryVertices.Add(v);
                }
            }

            result.UnionWith(primaryVertices); // tongueOut(主判定、歯除外済み)は無条件で採用する

            if (result.Count == 0)
            {
                // 診断用: 閾値が高すぎて何も拾えなかった場合、実際に観測された最大移動量を
                // ログへ出す。この値より少し小さい閾値に調整すれば検出できるようになるはず。
                // また、歯除外(toothExcludedVertices)によって「閾値は超えたが除外された」頂点が
                // 実際にあったかどうかも切り分けられるよう、通過数と除外数を分けて出す
                // (歯シェイプキーが舌の頂点と重なっている場合、閾値を超えていても全滅することがある)。
                Debug.LogWarning($"[hinzka ARKit FT] 舌アシスト: tongueOutシェイプキーは見つかりましたが、" +
                    $"現在の検出閾値({detectThreshold:0.######})の条件を満たす頂点移動がありませんでした。" +
                    $"[診断] 生の最大移動量(歯除外を考慮しない): {maxMagnitudeSeen:0.######} / " +
                    $"歯除外を除いた上での最大移動量(primaryAnchorMag): {primaryAnchorMag:0.######} / " +
                    $"歯除外に該当する頂点数: {toothExcludedVertices.Count}。" +
                    "Detect Thresholdを下げてみてください。" +
                    (maxMagnitudeSeen > detectThreshold * 2f && primaryAnchorMag <= detectThreshold
                        ? " (生の最大移動量が閾値を大きく上回っているのにprimaryAnchorMagが閾値以下のため、" +
                          "歯除外が原因で本来検出されるべき頂点が除外されている可能性が高いです。" +
                          "Lip Exclude Thresholdを上げてみてください)"
                        : ""));
                return result;
            }

            int candidateCountBeforeExclude = result.Count;

            // auxiliary側(補助シェイプキー由来)には唇・頬・歯すべての除外を適用する。
            // tongueOut(主判定)側は歯除外のみ既に適用済みで、唇・頬除外は対象外
            // (mouthPucker等は舌自体も動かしてしまうため、tongueOutの信頼できる結果を
            // 上書きさせないようにしている)。
            var excludedVertices = new HashSet<int>(lipCheekExcludedVertices);
            excludedVertices.UnionWith(toothExcludedVertices);

            result.ExceptWith(excludedVertices);
            result.UnionWith(primaryVertices); // tongueOut(歯除外済み)を再度戻す

            // 診断用: 除外でほとんど/全て消えてしまった場合に気づけるようにログを出す。
            // Lip Exclude Thresholdは値を上げるほど除外が「弱まる」(残る頂点が増える)ため、
            // Detect Thresholdとは逆方向に効くことに注意。
            if (candidateCountBeforeExclude > 0 && result.Count < candidateCountBeforeExclude)
            {
                Debug.Log($"[hinzka ARKit FT] 舌アシスト: 除外前の候補頂点数 {candidateCountBeforeExclude} → " +
                          $"除外後 {result.Count}(唇・頬・歯系シェイプキーで除外された頂点数: {excludedVertices.Count})。" +
                          $"除外され過ぎている場合はLip Exclude Threshold({lipExcludeThreshold:0.######})を" +
                          "上げると残る頂点が増えます。");
            }

            // 舌シェイプキー自身に紛れ込んだ、目周辺などの離れた場所にある孤立頂点(ノイズ的な
            // 極小デルタ)を除去する。舌の頂点は密集した一塊のはずなので、候補を距離ベースで
            // クラスタリングする。tongueOut(主判定)で最も大きく動いた頂点(アンカー)が
            // 分かっていれば、そのアンカーを含む塊を優先して残す(Detect Thresholdを下げすぎた
            // 際に、まつげ等の微小ノイズの塊の方が頂点数で上回って誤選択されるのを防ぐため)。
            // アンカーが無い(tongueOutが見つからない)場合のみ、従来通り最大の塊を残す。
            int anchorForClustering = primaryAnchorVertex >= 0 && result.Contains(primaryAnchorVertex)
                ? primaryAnchorVertex : -1;
            int countBeforeClustering = result.Count;
            result = KeepLargestSpatialCluster(result, mesh.vertices, anchorForClustering);
            if (result.Count < countBeforeClustering)
            {
                Debug.Log($"[hinzka ARKit FT] 舌アシスト: 本体から離れた孤立頂点を{countBeforeClustering - result.Count}個除去しました" +
                          $"(クラスタリング後の頂点数: {result.Count})。" +
                          (anchorForClustering >= 0 ? "tongueOutの最大移動頂点を含む塊を採用しました。" : ""));
            }

            return result;
        }

        /// <summary>
        /// 候補頂点を距離ベースでクラスタリングし、1つの塊だけを残す。
        /// 舌のシェイプキーに稀に紛れ込む、目周辺など本体から離れた孤立頂点(ノイズ)を
        /// 自動的に除去するための後処理。連結とみなす距離は、候補同士の最近傍距離の
        /// 中央値を基準に自動計算するため、メッシュのスケールに依存しない。
        ///
        /// anchorVertex(candidatesに含まれる場合のみ有効): 指定されていれば、その頂点を含む塊を
        /// 無条件で採用する(頂点数の多寡は問わない)。tongueOut(主判定)で最も大きく動いた頂点を
        /// アンカーとして渡すことで、Detect Thresholdを下げすぎた際に、まつげ等の微小ノイズの塊の
        /// 方が頂点数で上回って誤選択されるのを防げる。anchorVertexが-1(未指定)の場合のみ、
        /// 従来通り最も頂点数の多い塊を採用する。
        /// </summary>
        private static HashSet<int> KeepLargestSpatialCluster(HashSet<int> candidates, Vector3[] baseVertices, int anchorVertex = -1)
        {
            if (candidates.Count <= 2) return candidates; // 数点しかない場合はクラスタリングしても意味が薄い

            var list = candidates.ToList();
            int n = list.Count;

            var nearestDist = new float[n];
            for (int i = 0; i < n; i++)
            {
                float best = float.MaxValue;
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    float d = Vector3.Distance(baseVertices[list[i]], baseVertices[list[j]]);
                    if (d < best) best = d;
                }
                nearestDist[i] = best;
            }

            var sorted = (float[])nearestDist.Clone();
            Array.Sort(sorted);
            float median = sorted[sorted.Length / 2];
            // 中央値の何倍までを「同じ塊」とみなすか。メッシュの局所的な粗密差を吸収しつつ、
            // 本体から明確に離れた孤立点は弾けるよう、経験的に4倍を採用している。
            float connectRadius = Mathf.Max(median * 4f, 0.0001f);

            var parent = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;

            int Find(int x)
            {
                while (parent[x] != x) { parent[x] = parent[parent[x]]; x = parent[x]; }
                return x;
            }
            void Union(int a, int b)
            {
                a = Find(a); b = Find(b);
                if (a != b) parent[a] = b;
            }

            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    if (Vector3.Distance(baseVertices[list[i]], baseVertices[list[j]]) <= connectRadius)
                        Union(i, j);

            int chosenRoot;
            int anchorLocalIdx = anchorVertex >= 0 ? list.IndexOf(anchorVertex) : -1;
            if (anchorLocalIdx >= 0)
            {
                // アンカー指定あり: そのアンカーを含む塊を無条件で採用する。
                chosenRoot = Find(anchorLocalIdx);
            }
            else
            {
                // アンカー未指定: 従来通り最も頂点数の多い塊を採用する。
                var clusterSizes = new Dictionary<int, int>();
                for (int i = 0; i < n; i++)
                {
                    int root = Find(i);
                    clusterSizes[root] = clusterSizes.TryGetValue(root, out var c) ? c + 1 : 1;
                }
                chosenRoot = clusterSizes.OrderByDescending(kv => kv.Value).First().Key;
            }

            var result = new HashSet<int>();
            for (int i = 0; i < n; i++)
                if (Find(i) == chosenRoot)
                    result.Add(list[i]);

            return result;
        }

        private sealed class TongueAssistResult
        {
            public bool anyMixShapeCreated;
            // 標準ARKit名"tongueOut"の実際の解決名(接頭辞込み、またはUE代替名)。
            public string rawTongueOutPropName;
            // 生成した「持ち上げ×tongueOut50%」のピーク形状の名前(NULL=未生成)。
            // v2/TongueOutの0→100%遷移中、50%地点で最大になるようFX側のBlendTreeへ組み込む。
            public string peakShapeName;
            public int detectedVertexCount;
        }

        /// <summary>
        /// 舌アシストで検出頂点を持ち上げる方向を、選択された軸からローカル空間ベクトルへ解決する。
        /// メッシュによっては「上」がY+軸ではなくZ軸(奥行方向)等になっている場合があるため、
        /// 選択式にしている。
        /// </summary>
        private static Vector3 ResolveTongueLiftDirection(TongueLiftAxis axis)
        {
            switch (axis)
            {
                case TongueLiftAxis.PlusY: return Vector3.up;
                case TongueLiftAxis.MinusY: return Vector3.down;
                case TongueLiftAxis.PlusZ: return Vector3.forward;
                case TongueLiftAxis.MinusZ: return Vector3.back;
                case TongueLiftAxis.PlusX: return Vector3.right;
                case TongueLiftAxis.MinusX: return Vector3.left;
                default: return Vector3.up;
            }
        }

        /// <summary>
        /// Face SMR上に、v2/TongueOutの0→100%遷移中、50%地点(唇を越えるタイミング)で
        /// 最大になるよう舌を持ち上げる「ピーク形状」を1つだけ生成する。
        /// ピーク形状の頂点データには、①持ち上げ量の100%と、②tongueOut本体の伸び50%ぶんが
        /// あらかじめ合成されている。実際の遷移カーブ(0%→50%→100%)はFX側の
        /// ApplyTongueLiftEnvelopeで、既存の標準舌駆動BlendTree(hinzkaUE_Gain_v2_TongueOut)を
        /// 組み替えることで実現する(離散的な2ポーズ切り替えではなく、連続的な山型エンベロープ)。
        ///
        /// ①「持ち上げ」の作り方はliftSourceで選べる:
        /// - ExistingShapeKey: アバターが既に持っている舌持ち上げ用シェイプキー(existingLiftShapeName)
        ///   を、existingLiftShapeWeightPercent(%)の強さで流用する。頂点検出を一切行わないため、
        ///   検出漏れ・誤検出(歯やまつげの混入等)の心配が無く、アバター側で作り込まれた自然な
        ///   形状をそのまま活かせる。
        /// - AutoDetect: tongueOut等の動きから舌頂点を自動検出し、指定軸方向へ一律に持ち上げる
        ///   (従来方式。既存の持ち上げシェイプキーを持たないアバター向け)。
        ///
        /// 舌は口内にあり通常は隠れて見えないため、AutoDetect時はScene View側のプレビュー
        /// (UpdateTonguePreview / OnTongueSceneGUI)で事前に検出範囲を確認できるようにしてある。
        /// ピーク形状が既に存在する場合は再生成しない(複数回Installしても増殖しない)。
        /// </summary>
        private static TongueAssistResult GenerateTongueAssistShapesIfNeeded(
            SkinnedMeshRenderer faceSmr, string outputFolder, string arkitPrefix,
            TongueLiftSource liftSource, string existingLiftShapeName, float existingLiftShapeWeightPercent,
            float moveAmount, float detectThreshold, float lipExcludeThreshold, TongueLiftAxis liftAxis,
            bool excludeTeethFromPrimary = true)
        {
            var result = new TongueAssistResult();
            if (faceSmr == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] Face SMRが取得できないため舌アシスト生成をスキップしました。");
                return result;
            }
            var srcMesh = faceSmr.sharedMesh;
            if (srcMesh == null || !srcMesh.isReadable)
            {
                Debug.LogWarning("[hinzka ARKit FT] Face SMRのMeshが無効(未設定またはRead/Write無効)なため舌アシスト生成をスキップしました。");
                return result;
            }

            var nameLookup = BuildTrimmedShapeNameLookup(srcMesh);
            var arkitPrefixes = ParsePrefixList(arkitPrefix);

            // tongueOut自体を解決する(候補接頭辞を順に試す。無ければUE代替名)。
            string actualTongueOutName =
                ResolveNameAcrossPrefixes(nameLookup, "tongueOut", arkitPrefixes) ??
                (nameLookup.TryGetValue("TongueOut", out var ueDirect) ? ueDirect : null); // UE代替名フォールバック
            result.rawTongueOutPropName = actualTongueOutName;

            if (actualTongueOutName == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] tongueOut(またはUE代替名TongueOut)シェイプキーが見つからないため舌アシスト生成をスキップしました。");
                return result;
            }

            string peakShapeName = actualTongueOutName + "_Peak50_Generated";

            if (srcMesh.GetBlendShapeIndex(peakShapeName) >= 0)
            {
                Debug.Log("[hinzka ARKit FT] 舌アシストのピーク形状は既に生成済みのためスキップしました。");
                result.anyMixShapeCreated = true;
                result.peakShapeName = peakShapeName;
                return result;
            }

            int vertexCount = srcMesh.vertexCount;
            Vector3[] deltaUp;

            if (liftSource == TongueLiftSource.ExistingShapeKey)
            {
                // ①: アバターが既に持っている舌持ち上げ用シェイプキーを、指定強度で流用する。
                if (string.IsNullOrWhiteSpace(existingLiftShapeName))
                {
                    Debug.LogWarning("[hinzka ARKit FT] 舌アシスト: 既存シェイプキー方式が選択されていますが、" +
                        "シェイプキー名が未指定のため舌アシスト生成をスキップしました。");
                    return result;
                }
                if (!nameLookup.TryGetValue(existingLiftShapeName.Trim(), out var actualLiftName))
                {
                    Debug.LogWarning($"[hinzka ARKit FT] 舌アシスト: 指定されたシェイプキー'{existingLiftShapeName}'が" +
                        "メッシュ上に見つからないため舌アシスト生成をスキップしました。");
                    return result;
                }
                int liftIdx = srcMesh.GetBlendShapeIndex(actualLiftName);
                int liftFrame = srcMesh.GetBlendShapeFrameCount(liftIdx) - 1;
                if (liftFrame < 0)
                {
                    Debug.LogWarning($"[hinzka ARKit FT] 舌アシスト: シェイプキー'{actualLiftName}'にフレームが無いため" +
                        "舌アシスト生成をスキップしました。");
                    return result;
                }
                var rawLiftDelta = new Vector3[vertexCount];
                srcMesh.GetBlendShapeFrameVertices(liftIdx, liftFrame, rawLiftDelta, null, null);

                float scale = Mathf.Clamp01(existingLiftShapeWeightPercent / 100f);
                deltaUp = new Vector3[vertexCount];
                int movedVertexCount = 0;
                for (int i = 0; i < vertexCount; i++)
                {
                    deltaUp[i] = rawLiftDelta[i] * scale;
                    if (rawLiftDelta[i].sqrMagnitude > 1e-12f) movedVertexCount++;
                }
                result.detectedVertexCount = movedVertexCount;

                if (movedVertexCount == 0)
                {
                    Debug.LogWarning($"[hinzka ARKit FT] 舌アシスト: シェイプキー'{actualLiftName}'の頂点差分がほぼゼロのため" +
                        "舌アシスト生成をスキップしました。");
                    return result;
                }

                Debug.Log($"[hinzka ARKit FT] 舌アシスト: 既存シェイプキー'{actualLiftName}'を{existingLiftShapeWeightPercent:0.#}%の" +
                          $"強度で流用します(移動頂点数: {movedVertexCount})。");
            }
            else
            {
                // ①: tongueOut等の動きから舌頂点を自動検出し、指定軸方向へ一律に持ち上げる(従来方式)。
                var tongueVertices = DetectTongueVertices(srcMesh, arkitPrefix, detectThreshold, lipExcludeThreshold, excludeTeethFromPrimary);
                result.detectedVertexCount = tongueVertices.Count;
                if (tongueVertices.Count == 0)
                {
                    Debug.LogWarning("[hinzka ARKit FT] 舌の頂点を検出できなかったため舌アシスト生成をスキップしました。" +
                        "(アバターの舌シェイプキーの動きが小さすぎる可能性があります)");
                    return result;
                }

                var liftDir = ResolveTongueLiftDirection(liftAxis);
                deltaUp = new Vector3[vertexCount];
                foreach (int idx in tongueVertices)
                    deltaUp[idx] = liftDir * moveAmount;
            }

            // ②: tongueOut本体の生delta。ピーク形状(①×100% + tongueOut本体×50%)の材料として使う。
            int toIdx = srcMesh.GetBlendShapeIndex(actualTongueOutName);
            int toFrame = srcMesh.GetBlendShapeFrameCount(toIdx) - 1;
            if (toFrame < 0)
            {
                Debug.LogWarning($"[hinzka ARKit FT] 舌アシスト: '{actualTongueOutName}'にフレームが無いため" +
                    "舌アシスト生成をスキップしました。");
                return result;
            }
            var tongueOutOwnDelta = new Vector3[vertexCount];
            srcMesh.GetBlendShapeFrameVertices(toIdx, toFrame, tongueOutOwnDelta, null, null);

            var deltaPeak = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                deltaPeak[i] = deltaUp[i] * 1.0f + tongueOutOwnDelta[i] * 0.5f;

            var newMesh = UnityEngine.Object.Instantiate(srcMesh);
            newMesh.name = srcMesh.name + "_TongueAssist";
            newMesh.AddBlendShapeFrame(peakShapeName, 100f, deltaPeak, null, null);

            var meshPath = AssetDatabase.GenerateUniqueAssetPath(outputFolder + "/" + newMesh.name + ".asset");
            AssetDatabase.CreateAsset(newMesh, meshPath);
            _installGeneratedMeshPaths.Add(meshPath);
            AssetDatabase.SaveAssets();

            var so = new SerializedObject(faceSmr);
            so.FindProperty("m_Mesh").objectReferenceValue = newMesh;
            so.ApplyModifiedProperties();

            Debug.Log($"[hinzka ARKit FT] 舌アシストのピーク形状'{peakShapeName}'を生成しました" +
                      $"(移動頂点数: {result.detectedVertexCount}): {meshPath}");

            result.anyMixShapeCreated = true;
            result.peakShapeName = peakShapeName;
            return result;
        }

        /// <summary>
        /// fx内のBlendTreeサブアセットから、指定名のものを探して返す(見つからなければnull)。
        /// </summary>
        private static BlendTree FindBlendTreeByName(AnimatorController fx, string name)
        {
            if (fx == null || string.IsNullOrEmpty(name)) return null;
            var path = AssetDatabase.GetAssetPath(fx);
            if (string.IsNullOrEmpty(path)) return null;

            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
                if (obj is BlendTree bt && bt.name == name)
                    return bt;
            return null;
        }

        /// <summary>
        /// 指定クリップに、指定プロパティ(blendShape.xxx等)の定数カーブ(常にvalueを返す)を設定する。
        /// 既存の同名バインディングがあれば上書きする。
        /// </summary>
        private static void SetConstantCurve(AnimationClip clip, string smrPath, string propertyName, float value)
        {
            var binding = EditorCurveBinding.FloatCurve(smrPath, typeof(SkinnedMeshRenderer), propertyName);
            AnimationUtility.SetEditorCurve(clip, binding, new AnimationCurve(new Keyframe(0f, value)));
        }

        /// <summary>
        /// v2/TongueOutの0→100%遷移の間、50%地点(唇を越えるタイミング)で舌の持ち上げ
        /// (peakShapeName)が最大になるよう、標準の舌駆動BlendTree(hinzkaUE_Gain_v2_TongueOut)を
        /// 3点構成(0%=無反応, 50%=持ち上げピーク, 100%=tongueOut本体そのまま・持ち上げ無し)へ
        /// 組み替える。既存の0%・100%用クリップはそのまま再利用し、50%用のクリップだけ新規作成する。
        /// これにより、離散的な2ポーズの切り替えではなく、遷移の後半にかけて持ち上げが自然に
        /// 減衰する連続的なアニメーションになる。
        ///
        /// 標準駆動ツリーが見つからない(テンプレートの構成が想定と異なる)場合は何もしない。
        /// 別途TongueOutSteps_BT系(段階シェイプ用の専用クリップ)が存在する場合、そちらも同じ
        /// v2/TongueOutを駆動源にして独自にtongueOut/tongueOutStepNを動かしてしまうため、
        /// 二重駆動を避けるためすべて無効化(0固定)する。
        /// </summary>
        private static void ApplyTongueLiftEnvelope(AnimatorController fx, string smrPath, string tongueOutPropName, string peakShapeName)
        {
            if (fx == null || string.IsNullOrEmpty(tongueOutPropName) || string.IsNullOrEmpty(peakShapeName)) return;

            var gainTree = FindBlendTreeByName(fx, TONGUE_GAIN_TREE_NAME);
            if (gainTree == null)
            {
                Debug.LogWarning($"[hinzka ARKit FT] 舌アシスト: FX内に'{TONGUE_GAIN_TREE_NAME}'が見つからないため、" +
                    "持ち上げエンベロープの組み込みをスキップしました(テンプレートの構成が想定と異なる可能性があります)。");
                return;
            }

            var children = gainTree.children;
            if (children == null || children.Length == 0)
            {
                Debug.LogWarning($"[hinzka ARKit FT] 舌アシスト: '{TONGUE_GAIN_TREE_NAME}'に子が無いため" +
                    "持ち上げエンベロープの組み込みをスキップしました。");
                return;
            }

            var sorted = children.OrderBy(c => c.threshold).ToArray();
            var emptyChild = sorted[0];
            var fullChild = sorted[sorted.Length - 1];

            if (!(emptyChild.motion is AnimationClip emptyClip) || !(fullChild.motion is AnimationClip fullClip))
            {
                Debug.LogWarning($"[hinzka ARKit FT] 舌アシスト: '{TONGUE_GAIN_TREE_NAME}'の子がAnimationClipではないため" +
                    "持ち上げエンベロープの組み込みをスキップしました(想定外の構成です)。");
                return;
            }

            const string bsPrefix = "blendShape.";

            // emptyClip・fullClipは「アバター本来のFXから既存のクリップをそのまま再利用」した
            // ものであり、Gainツリー以外の場所(例: 生のtongueOutを直接反映させるBindingレイヤー等)
            // でも共有して使われている可能性がある。以前の実装ではこの共有クリップ自体に
            // 直接peakShapeNameのカーブを追加していたが、これだと「他の場所でこのクリップが
            // 評価されるたびに、そちらの評価結果としてもpeakShapeName=0が書き込まれてしまう」
            // ことになり、そちらのレイヤーがGainツリーより後に評価される場合、毎フレーム
            // persist上げ量を0へ強制的にリセットしてしまう(「一切動かなくなった」不具合の
            // 直接的な原因と考えられる)。この副作用を避けるため、共有クリップは一切変更せず、
            // 0%地点・100%地点にも専用の新規クリップを作成する(ピーク・中間点と同じ方式)。
            var startClip = new AnimationClip { name = "hinzkaUE_TongueEnvelope_Start" };
            AssetDatabase.AddObjectToAsset(startClip, fx);
            startClip.hideFlags = GENERATED_SUBASSET_HIDE_FLAGS;
            SetConstantCurve(startClip, smrPath, bsPrefix + tongueOutPropName, 0f);
            SetConstantCurve(startClip, smrPath, bsPrefix + peakShapeName, 0f);

            var endClip = new AnimationClip { name = "hinzkaUE_TongueEnvelope_End" };
            AssetDatabase.AddObjectToAsset(endClip, fx);
            endClip.hideFlags = GENERATED_SUBASSET_HIDE_FLAGS;
            SetConstantCurve(endClip, smrPath, bsPrefix + tongueOutPropName, 100f);
            SetConstantCurve(endClip, smrPath, bsPrefix + peakShapeName, 0f);

            // ピーク位置は50%に固定する。以前はユーザーが調整できるようにしていたが、
            // リモート側でv2/TongueOutが4bit(16段階、約6.25%刻み)に量子化される運用の場合、
            // ピークを端寄りに設定すると変化の幅が量子化の1段分より狭くなり、リモートで見ると
            // なめらかさが失われてカクついて見えてしまう。50%固定であれば、上り坂・下り坂
            // 双方に常に50ポイントの幅を確保できるため、量子化に対して安定する。
            const float peakT = 0.5f;

            // 山の頂点用の新規クリップ。tongueOut本体は0にする
            // (peakShapeName自体の頂点データにtongueOut本体の指定%ぶんが既に焼き込まれているため、
            // 生カーブも同時に駆動すると二重に伸びてしまう)。
            var peakClip = new AnimationClip { name = "hinzkaUE_TonguePeak" };
            AssetDatabase.AddObjectToAsset(peakClip, fx);
            peakClip.hideFlags = GENERATED_SUBASSET_HIDE_FLAGS;
            SetConstantCurve(peakClip, smrPath, bsPrefix + tongueOutPropName, 0f);
            SetConstantCurve(peakClip, smrPath, bsPrefix + peakShapeName, 100f);

            // Simple1Dブレンドツリーは隣接する点の間を直線補間することしかできないため、
            // 滑らかな曲線を再現するには、点の数を増やして直線の集まりで近似するしかない。
            const int CURVE_POINTS_PER_SIDE = 5;
            float SmoothStep(float t) => t * t * (3f - 2f * t);

            AnimationClip MakeEnvelopePointClip(string suffix, float valuePercent)
            {
                var clip = new AnimationClip { name = $"hinzkaUE_TongueEnvelope_{suffix}" };
                AssetDatabase.AddObjectToAsset(clip, fx);
                clip.hideFlags = GENERATED_SUBASSET_HIDE_FLAGS;
                SetConstantCurve(clip, smrPath, bsPrefix + tongueOutPropName, 0f);
                SetConstantCurve(clip, smrPath, bsPrefix + peakShapeName, valuePercent);
                return clip;
            }

            // 立ち上がり(0%→ピーク): スムーズステップ(3t²-2t³、両端で傾きが0になる
            // 滑らかなS字カーブ)を、ピークまでの区間全体に使う。
            var ascendingChildren = new List<ChildMotion>();
            for (int i = 1; i <= CURVE_POINTS_PER_SIDE; i++)
            {
                float t = (float)i / (CURVE_POINTS_PER_SIDE + 1); // 0〜ピーク区間内での相対位置(0,1は除く)
                float valuePercent = SmoothStep(t) * 100f;
                float threshold = peakT * t;
                var clip = MakeEnvelopePointClip($"Rise{Mathf.RoundToInt(t * 100f)}", valuePercent);
                ascendingChildren.Add(new ChildMotion { motion = clip, threshold = threshold, timeScale = 1f });
            }

            // 立ち下がり(ピーク→100%): 下り坂全体ではなく、ピーク〜DESCENT_END(ピークから
            // 100%までの区間の85%地点)という区間の中にスムーズステップを収める。
            // 以前は60%(下り坂30ポイントぶん・上り坂50ポイントぶんという非対称)にしていたが、
            // 上り坂に対して下り坂だけが急すぎると、舌を戻す速度が伸ばす速度と異なる場合に
            // 「出すときと戻すときで持ち上がり方が違って見える」という新たな問題が生じたため、
            // 上り坂の幅(50ポイント)に近づけて非対称性を緩和する。100%付近にはごくわずかな
            // 平坦な帯(ピーク〜100%の残り15%ぶん)だけを残し、量子化への耐性も最低限確保する。
            const float DESCENT_FRACTION = 0.85f; // ピーク〜100%の区間のうち、実際に下る割合
            float descentEndT = peakT + (1f - peakT) * DESCENT_FRACTION;
            var descendingChildren = new List<ChildMotion>();
            for (int i = 1; i <= CURVE_POINTS_PER_SIDE; i++)
            {
                float t = (float)i / (CURVE_POINTS_PER_SIDE + 1); // ピーク〜descentEndT区間内での相対位置
                float valuePercent = (1f - SmoothStep(t)) * 100f;
                float threshold = peakT + (descentEndT - peakT) * t;
                var clip = MakeEnvelopePointClip($"Fall{Mathf.RoundToInt(t * 100f)}", valuePercent);
                descendingChildren.Add(new ChildMotion { motion = clip, threshold = threshold, timeScale = 1f });
            }

            var newChildren = new List<ChildMotion>
            {
                new ChildMotion { motion = startClip, threshold = 0f,    timeScale = 1f },
            };
            newChildren.AddRange(ascendingChildren);
            newChildren.Add(new ChildMotion { motion = peakClip, threshold = peakT, timeScale = 1f });
            newChildren.AddRange(descendingChildren);
            newChildren.Add(new ChildMotion { motion = endClip, threshold = 1f, timeScale = 1f });

            gainTree.children = newChildren.ToArray();
            EditorUtility.SetDirty(gainTree);

            Debug.Log($"[hinzka ARKit FT] 舌アシスト: '{TONGUE_GAIN_TREE_NAME}'を持ち上げエンベロープ" +
                      $"(0%→50%で持ち上げ最大→{descentEndT * 100f:0}%でほぼ0→100%まで平坦、" +
                      $"スムーズステップ曲線近似・片側{CURVE_POINTS_PER_SIDE}点)に組み替えました。");

            DisableAllTongueStepClips(fx);
        }

        /// <summary>
        /// TongueOutSteps_BT配下の専用クリップ(hinzkaUE_TongueStep_*)が存在する場合、
        /// そのBlendShapeカーブをすべて0に固定して無効化する。ApplyTongueLiftEnvelopeによる
        /// 持ち上げエンベロープと、同じv2/TongueOutを駆動源とする段階シェイプ系が
        /// 二重に舌を動かしてしまうのを防ぐ。該当クリップが無ければ何もしない。
        /// </summary>
        private static void DisableAllTongueStepClips(AnimatorController fx)
        {
            if (fx == null) return;
            var clips = new HashSet<AnimationClip>();
            foreach (var layer in fx.layers)
                CollectClips(layer.stateMachine, clips);

            int disabledCount = 0;
            foreach (var clip in clips)
            {
                if (!clip.name.StartsWith(TONGUE_STEP_CLIP_PREFIX, StringComparison.Ordinal)) continue;

                var bindings = AnimationUtility.GetCurveBindings(clip);
                bool dirty = false;
                foreach (var b in bindings)
                {
                    if (b.type != typeof(SkinnedMeshRenderer)) continue;
                    if (!b.propertyName.StartsWith("blendShape.", StringComparison.Ordinal)) continue;

                    AnimationUtility.SetEditorCurve(clip, b, new AnimationCurve(new Keyframe(0f, 0f)));
                    dirty = true;
                    disabledCount++;
                }
                if (dirty) EditorUtility.SetDirty(clip);
            }

            if (disabledCount > 0)
                Debug.Log($"[hinzka ARKit FT] 舌アシスト: TongueOutSteps系クリップのカーブ{disabledCount}個を0に固定しました" +
                          "(持ち上げエンベロープとの重複駆動を防止するためです)。");
        }


        // ── まばたき制御方式(Blink2D / Blink Simple 1D) ──────────────────

        /// <summary>
        /// テンプレートFXにUEFxGeneratorで生成した両方式(Blink2D / Blink Simple 1D)が
        /// 同梱されている場合、選択されなかった方をInstall時に無効化する。
        /// 対応するレイヤー/ノードが見つからない(テンプレートが片方しか持たない)場合は
        /// 何もしない(安全にスキップする)。
        /// </summary>
        private static void ApplyBlinkControlModeSelection(AnimatorController fx, BlinkControlMode mode)
        {
            if (fx == null) return;

            bool disable2D = mode == BlinkControlMode.OneD;
            bool disable1D = mode == BlinkControlMode.TwoD;

            // ケースA: LegacySeparateLayer配置(既定)。専用レイヤーが見つかればweight=0にする。
            if (disable2D) DisableBlinkLayerIfExists(fx, BLINK_2D_LAYER_NAME);
            if (disable1D) DisableBlinkLayerIfExists(fx, BLINK_SIMPLE1D_LAYER_NAME);

            // ケースB: InMainDriverDirect配置(Direct BlendTreeへの直接注入)。
            // EyeSquint抑制/BrowModeSwitch/Modulationでラップされていても、子孫を再帰的に
            // 辿って元のBlendTree名を探すため確実に検出できる(BlendTreeContainsNamedDescendant参照)。
            if (disable2D) RemoveDirectBlendTreeChildByNameSuffix(fx, BLINK_2D_MOTION_NAME_SUFFIX);
            if (disable1D) RemoveDirectBlendTreeChildByNameSuffix(fx, BLINK_SIMPLE1D_MOTION_NAME_SUFFIX);
        }

        /// <summary>
        /// 指定名のAnimatorControllerLayerが存在すれば、defaultWeightを0にして無効化する。
        /// レイヤー自体は削除しない(他レイヤーのインデックス参照を壊さないため、非破壊的に
        /// weight=0で無効化するだけに留めている)。
        /// </summary>
        private static void DisableBlinkLayerIfExists(AnimatorController fx, string layerName)
        {
            var layers = fx.layers;
            int idx = -1;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == layerName) { idx = i; break; }
            }
            if (idx < 0) return;

            layers[idx].defaultWeight = 0f;
            fx.layers = layers;
            EditorUtility.SetDirty(fx);
            Debug.Log($"[hinzka ARKit FT] まばたき制御: レイヤー'{layerName}'を無効化しました(weight=0)。");
        }

        /// <summary>
        /// motion(またはその子孫のBlendTree)のいずれかの名前に nameSuffix が含まれるかを再帰的に判定する。
        /// EyeSquint抑制・BrowModeSwitch・Tracking Modulation等でラップされ、Driverに実際に
        /// 差し込まれる最上位ノードの名前が素の"Blink2D_BT"等から変わってしまっていても、
        /// 内部のどこかに元のBlendTree(名前は変わらない)が必ず子孫として残っているため、
        /// これを辿ることで確実に検出できる。
        /// </summary>
        // BlendTreeContainsNamedDescendantが「細い1本道のラッパー」を通り抜けて奥まで探索する際、
        // 子の数がこれを超えるノード(EyeSquint抑制等の単機能ラッパーではなく、眉・視線・まばたき等
        // 複数の無関係な機能が同居する広いグループノード)は不透明な境界として扱い、その先へは
        // 探索しない。これが無いと、例えば「DirectDriver_Eyes」のような広いグループが、その中に
        // 偶然Blink2D_BTを含んでいるというだけの理由で、上位のDriverツリーから丸ごと除去されて
        // しまう(まばたき以外の眉・視線トラッキングまで巻き込んで消える重大なバグになる)。
        private const int BLENDTREE_WRAPPER_MAX_WIDTH = 3;

        private static bool BlendTreeContainsNamedDescendant(Motion motion, string nameSuffix, HashSet<Motion> visited = null)
        {
            if (motion == null || string.IsNullOrEmpty(nameSuffix)) return false;
            visited ??= new HashSet<Motion>();
            if (!visited.Add(motion)) return false; // 既に調べた(共有サブツリー・循環参照対策)

            if (!string.IsNullOrEmpty(motion.name) && motion.name.Contains(nameSuffix)) return true;

            if (motion is BlendTree bt)
            {
                // 子の数が多い(=単機能のラッパーではなく、複数の無関係な機能が同居する広い
                // グループノードである可能性が高い)場合は、その先を探索しない。
                if (bt.children.Length > BLENDTREE_WRAPPER_MAX_WIDTH) return false;

                foreach (var child in bt.children)
                {
                    if (BlendTreeContainsNamedDescendant(child.motion, nameSuffix, visited))
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// fx内の全BlendTreeサブアセットを走査し、Direct型のBlendTreeの子Motionのうち、
        /// 自身または子孫のいずれかの名前に指定文字列を含むものを子リストから除去する
        /// (その方式のまばたきをDriverから完全に取り除き、二重駆動を防ぐ)。
        /// EyeSquint抑制・BrowModeSwitch・Tracking Modulation等でラップされていても、
        /// 子孫を再帰的に辿って検出するため確実に除去できる。該当が無ければ何もしない。
        /// </summary>
        private static void RemoveDirectBlendTreeChildByNameSuffix(AnimatorController fx, string nameSuffix)
        {
            if (fx == null || string.IsNullOrEmpty(nameSuffix)) return;
            var fxPath = AssetDatabase.GetAssetPath(fx);
            if (string.IsNullOrEmpty(fxPath)) return;

            int removedCount = 0;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fxPath))
            {
                if (!(obj is BlendTree bt) || bt == null) continue;
                if (bt.blendType != BlendTreeType.Direct) continue;

                var children = bt.children;
                var kept = children.Where(c => !BlendTreeContainsNamedDescendant(c.motion, nameSuffix)).ToArray();
                if (kept.Length == children.Length) continue;

                bt.children = kept;
                EditorUtility.SetDirty(bt);
                removedCount += children.Length - kept.Length;
            }

            if (removedCount > 0)
                Debug.Log($"[hinzka ARKit FT] まばたき制御: Direct BlendTreeから'{nameSuffix}'を含む子を{removedCount}個除去しました。");
        }

        /// <summary>
        /// TrackingタブのFace SMR選択・各種スライダー変更のたびに呼ばれ、検出結果を
        /// キャッシュしてScene Viewを再描画させる。舌アシストがOFF、またはプレビュー表示が
        /// OFFの場合はキャッシュを空にする(OnTongueSceneGUI側で何も描画しないようにするため)。
        /// </summary>
        private void UpdateTonguePreview()
        {
            // Scene Viewプレビューは、アバターが実際にScene上に配置されている場合のみ機能する。
            // Project内のPrefabアセットを直接指定している場合、Scene上に描画対象自体が
            // 存在しないため、プレビューを有効にしても何も表示されない。原因が分からず
            // 混乱しないよう、該当する場合はUI上に理由を明示する。
            bool avatarIsPrefabAsset = _avatarPrefab != null && !_avatarPrefab.scene.IsValid();
            if (_uiTonguePreviewAssetWarningHint != null)
            {
                bool showAssetWarning = _generateTongueAssistShapes && _showTonguePreview && avatarIsPrefabAsset;
                _uiTonguePreviewAssetWarningHint.style.display = showAssetWarning ? DisplayStyle.Flex : DisplayStyle.None;
                if (showAssetWarning && _uiTonguePreviewAssetWarningHint.childCount > 0 &&
                    _uiTonguePreviewAssetWarningHint[0] is Label assetWarningLabel)
                    assetWarningLabel.text = ArkitFTLoc.T(
                        "選択中のAvatarはProject内のPrefabアセットのため、Scene Viewへのプレビュー表示は機能しません。\n" +
                        "Sceneにモデルを配置してから選択し直してください。");
            }

            // 検出頂点数・単位変換係数の表示は、Scene Viewへの3D描画(_showTonguePreview)とは
            // 独立して常に更新する(描画自体のON/OFFはOnTongueSceneGUI側で別途判定している)。
            // こうしないと、プレビュー表示をOFFにしている間は検出頂点数の欄が常に空欄になってしまう。
            if (!_generateTongueAssistShapes || _tongueLiftSource == TongueLiftSource.ExistingShapeKey ||
                _smrs == null || _smrIndex < 0 || _smrIndex >= _smrs.Length || _smrs[_smrIndex] == null)
            {
                _tonguePreviewVertexIndices = null;
                _tonguePreviewBaseVertices = null;
                if (_uiTongueDetectedCountLabel != null) _uiTongueDetectedCountLabel.text = "";
                if (_uiTongueUnitInfoLabel != null) _uiTongueUnitInfoLabel.text = "";
                SceneView.RepaintAll();
                return;
            }

            var mesh = _smrs[_smrIndex].sharedMesh;
            if (mesh == null || !mesh.isReadable)
            {
                _tonguePreviewVertexIndices = null;
                _tonguePreviewBaseVertices = null;
                if (_uiTongueDetectedCountLabel != null)
                    _uiTongueDetectedCountLabel.text = ArkitFTLoc.T("(メッシュのRead/Writeが無効なためプレビューできません)");
                if (_uiTongueUnitInfoLabel != null) _uiTongueUnitInfoLabel.text = "";
                SceneView.RepaintAll();
                return;
            }

            _tonguePreviewBaseVertices = mesh.vertices;
            float tongueUnitAuto = MeshUnitToWorld(_smrs[_smrIndex]);
            float tongueUnit = ResolveTongueMeshUnit(_smrs[_smrIndex]);
            float tongueDetectThresholdMesh = (_tongueDetectThresholdMm / 1000f) / tongueUnit;
            float tongueLipExcludeThresholdMesh = (_tongueLipExcludeThresholdMm / 1000f) / tongueUnit;
            Debug.Log($"[hinzka ARKit FT] 舌アシスト: MeshUnitToWorld(自動)={tongueUnitAuto:0.######} " +
                      $"実際に使用={tongueUnit:0.######}{(_tongueUnitOverride > 0f ? "(手動指定)" : "")} " +
                      $"(Detect {_tongueDetectThresholdMm}mm → メッシュ空間{tongueDetectThresholdMesh:0.######} / " +
                      $"Lip Exclude {_tongueLipExcludeThresholdMm}mm → メッシュ空間{tongueLipExcludeThresholdMesh:0.######})");
            if (_uiTongueUnitInfoLabel != null)
                _uiTongueUnitInfoLabel.text = string.Format(
                    ArkitFTLoc.T("実際に使われている変換係数: {0:0.######}{1}(自動推定値: {2:0.######})"),
                    tongueUnit, _tongueUnitOverride > 0f ? ArkitFTLoc.T("[手動指定] ") : "", tongueUnitAuto);
            _tonguePreviewVertexIndices = DetectTongueVertices(
                mesh, _arkitShapePrefix, tongueDetectThresholdMesh, tongueLipExcludeThresholdMesh,
                _tongueExcludeTeethFromPrimary);
            if (_uiTongueDetectedCountLabel != null)
                _uiTongueDetectedCountLabel.text = string.Format(ArkitFTLoc.T("検出頂点数: {0}"), _tonguePreviewVertexIndices.Count);

            SceneView.RepaintAll();
        }

        /// <summary>
        /// 舌は口内にあり通常は隠れて見えないため、Handles.zTestを無効化して顔メッシュ越しに
        /// 透過表示する(いわゆるレントゲン表示)。白い球=移動前、赤い球=移動後の位置。
        /// </summary>
        // OnTongueSceneGUIでBakeMeshの出力先として使い回すスクラッチメッシュ。
        // 毎フレーム新規Meshを作ってDestroyするとGCが増えるため、インスタンスを再利用する。
        private Mesh _tongueBakeScratchMesh;

        private void OnTongueSceneGUI(SceneView sceneView)
        {
            if (!_generateTongueAssistShapes || !_showTonguePreview) return;
            if (_tonguePreviewVertexIndices == null) return;
            if (_smrs == null || _smrIndex < 0 || _smrIndex >= _smrs.Length || _smrs[_smrIndex] == null) return;

            var smr = _smrs[_smrIndex];

            // 対象アバターがScene上で非表示(GameObject自体が非アクティブ、または
            // Renderer自体が無効)になっている場合は、プレビューも表示しない。
            // アバターの表示状態と食い違ったまま浮いて見え続けるのを防ぐため。
            if (!smr.gameObject.activeInHierarchy || !smr.enabled) return;

            // 現在のBlendShapeウェイト(シェイプキーで舌を動かした結果)を反映した頂点位置を
            // 毎フレーム焼き込む。mesh.vertices(バインドポーズ、静止状態)ではシェイプキーで
            // 動かした現在位置が反映されないため、BakeMeshでリアルタイムに追従させる。
            if (_tongueBakeScratchMesh == null)
                _tongueBakeScratchMesh = new Mesh { name = "TonguePreviewBakeScratch" };
            smr.BakeMesh(_tongueBakeScratchMesh, true);
            Vector3[] currentVertices = _tongueBakeScratchMesh.vertices;

            Transform t = smr.transform;
            var prevZTest = Handles.zTest;
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            var liftDir = ResolveTongueLiftDirection(_tongueLiftAxis);

            foreach (int idx in _tonguePreviewVertexIndices)
            {
                if (idx >= currentVertices.Length) continue;

                Vector3 basePos = t.TransformPoint(currentVertices[idx]);
                Vector3 movedPos = t.TransformPoint(currentVertices[idx] + liftDir * _tongueMoveAmount);

                float baseSize = HandleUtility.GetHandleSize(basePos) * _tonguePreviewPointScale;
                float movedSize = HandleUtility.GetHandleSize(movedPos) * _tonguePreviewPointScale;

                // 陰影付きの立体球(SphereHandleCap)はアウトラインが強調され、頂点が
                // 密集するとツブツブした見た目になり不快感が出るため、平面的な点にしたい。
                // DotHandleCapは環境によって矩形に見えてしまう場合があるため、常にカメラ方向を
                // 向く塗りつぶし円(DrawSolidDisc)を直接描画する(形状の解釈違いが起きず確実に丸くなる)。
                var camForward = sceneView.camera != null ? sceneView.camera.transform.forward : Vector3.forward;
                Handles.color = Color.white;
                Handles.DrawSolidDisc(basePos, camForward, baseSize);

                Handles.color = Color.red;
                Handles.DrawSolidDisc(movedPos, camForward, movedSize);

                // 移動前後を結ぶ線。原色の黄色だと視覚的にうるさいため、半透明のシアン系に
                // して、白(移動前)・赤(移動後)の球を邪魔しない控えめな案内線にする。
                Handles.color = new Color(0.4f, 0.85f, 0.9f, 0.6f);
                Handles.DrawLine(basePos, movedPos);
            }

            Handles.zTest = prevZTest;

            // シェイプキーをドラッグ操作中は継続的に再描画させ、追従をリアルタイムに見せる。
            sceneView.Repaint();
        }

        // NK Installerが生成・注入するBlendTree/StateMachine/AnimationClip等のサブアセットに
        // 一律で適用するHideFlags。FXジェネレータ側(UEFxGeneratorWindow)の既定値と揃えてある。
        // FX本体・Menu・Parametersなどのトップレベル出力物には適用しない(Project上で
        // 見えなくなってしまうため、サブアセットに対してのみ使う)。
        private static readonly HideFlags GENERATED_SUBASSET_HIDE_FLAGS =
            HideFlags.HideInHierarchy | HideFlags.HideInInspector;

        private static void HideGeneratedSubAsset(UnityEngine.Object obj)
        {
            if (obj == null) return;
            obj.hideFlags = GENERATED_SUBASSET_HIDE_FLAGS;
            EditorUtility.SetDirty(obj);
        }

        private static AnimatorState FindStateByNameContains(AnimatorController fx, string nameContains)
        {
            foreach (var layer in fx.layers)
            {
                foreach (var s in layer.stateMachine.states)
                    if (s.state != null && s.state.name.Contains(nameContains))
                        return s.state;
            }
            return null;
        }

        // テンプレートFX側でこの名前で用意されている前提(UEFxGeneratorWindowのComboRuleで生成)。
        // レイヤー名: ComboLayer__<ComboRule名>、State名: <ComboRule名> そのもの。
        private const string BLINK_EFFECT_LAYER_NAME = "ComboLayer__FTextra_EyeBlinkEffect";
        private const string BLINK_EFFECT_STATE_NAME = "FTextra_EyeBlinkEffect";
        // FXジェネレータ側のComboRuleが自動生成するクリップの命名規則(UEFxGeneratorWindow準拠)。
        // このプレフィックスに一致するクリップだけを「安全に破棄してよい生成物」とみなす。
        private const string BLINK_EFFECT_CLIP_PREFIX = "hinzkaUE_Combo_FTextra_EyeBlinkEffect";

        /// <summary>
        /// まばたき検出時に1回だけ再生される「おまけ」のState(FTextra_EyeBlinkEffect)が
        /// 参照するMotionを、ユーザー指定のAnimationClipに差し替える。テンプレート側の
        /// トランジション(まぶたが閉じたら再生→開いたら元に戻る)はそのまま活用し、
        /// 再生されるクリップの中身だけを入れ替えるだけなので、既存の仕組みを壊さない。
        /// </summary>
        private static void ApplyBlinkEffectClip(AnimatorController fx, AnimationClip userClip)
        {
            var state = FindStateByNameContains(fx, BLINK_EFFECT_STATE_NAME);
            if (state == null)
            {
                Debug.LogWarning($"[hinzka ARKit FT] まばたきエフェクトのState('{BLINK_EFFECT_STATE_NAME}')が" +
                                  "テンプレートFX内に見つからないため、クリップの差し替えをスキップしました。");
                return;
            }

            var oldMotion = state.motion;

            // 外部ファイル参照のままにせず、コピーをFXアセット内に埋め込む
            // (他のcombo/生成クリップと同様、FXを配布・共有しても参照が切れないようにするため)。
            var copy = UnityEngine.Object.Instantiate(userClip);
            copy.name = "hinzkaUE_Combo_FTextra_EyeBlinkEffect_User";
            AssetDatabase.AddObjectToAsset(copy, fx);
            HideGeneratedSubAsset(copy);

            state.motion = copy;
            EditorUtility.SetDirty(fx);

            // テンプレートが元々同梱していた自動生成クリップは、差し替え後は誰からも
            // 参照されない不要物として残ってしまう。配布物を肥大化・混乱させないため破棄する。
            RemoveGeneratedClipIfOrphaned(fx, oldMotion);

            Debug.Log($"[hinzka ARKit FT] まばたきエフェクトのクリップをユーザー指定のもの" +
                      $"('{userClip.name}')に差し替えました。");
        }

        /// <summary>
        /// 指定した名前のレイヤーをFXから完全に除去する(まばたきエフェクトをOFFにする場合など)。
        /// weight=0にするだけでなくレイヤー自体を削除するのは、不要なState/Transitionを
        /// 生成物に残さずクリーンに保つため。レイヤーが参照していた自動生成クリップも
        /// 合わせて破棄し、孤立したサブアセットを残さない。
        ///
        /// 【重要】クリップの破棄は必ずfx.RemoveLayer()より前に行うこと。RemoveLayer()は
        /// Unity内部でそのレイヤー専有のState/Motionを巻き込んで破棄することがあり、
        /// 除去後に古い参照(Motion)へアクセスするとMissingReferenceExceptionになる
        /// (実際にこの順序ミスでInstall全体が失敗する不具合が発生したことがある)。
        /// </summary>
        private static void RemoveLayerByName(AnimatorController fx, string layerName)
        {
            for (int i = fx.layers.Length - 1; i >= 0; i--)
            {
                if (fx.layers[i].name == layerName)
                {
                    var state = FindStateByNameContains(fx, BLINK_EFFECT_STATE_NAME);
                    var motionToClean = state?.motion;

                    // 先にクリップを掃除してから、レイヤー自体を除去する。
                    RemoveGeneratedClipIfOrphaned(fx, motionToClean);
                    fx.RemoveLayer(i);

                    Debug.Log($"[hinzka ARKit FT] レイヤー '{layerName}' を除去しました。");
                    return;
                }
            }
        }

        /// <summary>
        /// FXジェネレータが自動生成したクリップ(BLINK_EFFECT_CLIP_PREFIXで始まる名前)が、
        /// このfxアセット内のサブアセットとして存在し、かつもう他のどのStateからも
        /// 参照されていない場合に破棄する。名前で対象を絞ることで、無関係なアセットを
        /// 誤って破棄しないようにしている。
        ///
        /// Unity側の別処理(RemoveLayer等)が同じオブジェクトを既に破棄している可能性が
        /// あるため、Unity標準のnullチェック(== null)だけに頼らず例外に対しても防御的に
        /// 書く。ここで例外が起きてもInstall全体を失敗させたくない(サブアセットの
        /// 掃除漏れは致命的ではないため)。
        /// </summary>
        private static void RemoveGeneratedClipIfOrphaned(AnimatorController fx, Motion motion)
        {
            try
            {
                if (motion == null) return;
                if (!(motion is AnimationClip clip) || clip == null) return;
                if (!clip.name.StartsWith(BLINK_EFFECT_CLIP_PREFIX, StringComparison.Ordinal)) return;

                var fxPath = AssetDatabase.GetAssetPath(fx);
                if (string.IsNullOrEmpty(fxPath) || AssetDatabase.GetAssetPath(clip) != fxPath) return;

                // まだどこかのStateから参照されていないか最終確認してから破棄する。
                foreach (var layer in fx.layers)
                    foreach (var s in layer.stateMachine.states)
                        if (s.state != null && s.state.motion == clip)
                            return; // 参照が残っているので破棄しない

                var clipName = clip.name;
                AssetDatabase.RemoveObjectFromAsset(clip);
                UnityEngine.Object.DestroyImmediate(clip, true);
                Debug.Log($"[hinzka ARKit FT] 使われなくなった自動生成クリップ '{clipName}' を破棄しました。");
            }
            catch (MissingReferenceException)
            {
                // 既に(Unity側の別処理などで)破棄済みだった場合はここに来る。
                // 掃除自体は不要になっているだけなので、警告に留めてInstallは継続する。
                Debug.LogWarning("[hinzka ARKit FT] まばたきエフェクトの旧クリップは既に破棄済みでした" +
                                  "(掃除は不要なためスキップします)。");
            }
        }

        private static void EnsureFloatParam(AnimatorController fx, string name, float defaultValue)
        {
            if (fx.parameters.Any(p => p.name == name)) return;
            fx.AddParameter(new AnimatorControllerParameter
            { name = name, type = AnimatorControllerParameterType.Float, defaultFloat = defaultValue });
        }

        private static AnimationClip GetOrCreateNamedClip(AnimatorController fx, string name)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(fx)))
                if (obj is AnimationClip c && c.name == name) return c;

            var clip = new AnimationClip { name = name };
            AssetDatabase.AddObjectToAsset(clip, fx);
            HideGeneratedSubAsset(clip);
            return clip;
        }

        private static void SetCurve(AnimationClip clip, string smrPath, string blendShapeName, float weight)
        {
            var binding = new EditorCurveBinding
            {
                type = typeof(SkinnedMeshRenderer),
                path = smrPath,
                propertyName = "blendShape." + blendShapeName,
            };
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0f, 0f, weight));
        }

        /// <summary>
        /// FX内のVisemeCompensateレイヤーが、生成した逆Visemeシェイプキーと一致した構成になっているか検証する。
        /// テンプレートFX側(UEFxGeneratorWindow.BuildVisemeCompensateLayer)とAvatar側(GenerateInverseVisemeShapes)は
        /// 同じ固定命名規則(inverse.FT_v_&lt;token&gt;)を使う設計のため、通常はテンプレート生成時点で既に正しい状態になっている。
        /// このメソッドはクリップを一切書き換えず、不一致があればログ警告のみを出す。
        /// </summary>
        private static void ValidateVisemeCompensateClips(
            AnimatorController fx, string smrPath, string[] inverseNames)
        {
            const string LAYER_MARKER = "VisemeCompensate";

            var layer = fx.layers.FirstOrDefault(l => l.name.Contains(LAYER_MARKER));
            if (layer == null)
            {
                Debug.LogWarning($"[hinzka ARKit FT] FX内に'{LAYER_MARKER}'を含むレイヤーが見つかりません。" +
                                  "UEFxGeneratorWindowで useVisemeCompensate=true を有効にしてテンプレートFXを再生成してください。");
                return;
            }

            var clips = new HashSet<AnimationClip>();
            CollectClips(layer.stateMachine, clips);
            if (clips.Count == 0)
            {
                Debug.LogWarning($"[hinzka ARKit FT] '{layer.name}'レイヤーにクリップが見つかりませんでした。");
                return;
            }

            // レイヤー内の全クリップが参照している blendShape カーブ(名前・SMRパス)を集める
            var foundNames = new HashSet<string>();
            var mismatchedPaths = new HashSet<string>();
            foreach (var clip in clips)
            {
                foreach (var b in AnimationUtility.GetCurveBindings(clip))
                {
                    if (!b.propertyName.StartsWith("blendShape.")) continue;
                    foundNames.Add(b.propertyName.Substring("blendShape.".Length));
                    if (b.path != smrPath)
                        mismatchedPaths.Add(b.path);
                }
            }

            var missing = inverseNames.Where(n => !string.IsNullOrEmpty(n) && !foundNames.Contains(n)).ToList();
            if (missing.Count > 0)
            {
                Debug.LogWarning($"[hinzka ARKit FT] '{layer.name}'レイヤーに以下の逆Visemeシェイプキーのカーブが" +
                                  $"見つかりませんでした(テンプレートFXとAvatar側の命名規則" +
                                  $"(visemeCompensatePrefix / inverseVisemePrefix)が一致していない可能性があります): " +
                                  $"{string.Join(", ", missing)}");
            }

            if (mismatchedPaths.Count > 0)
            {
                Debug.LogWarning($"[hinzka ARKit FT] '{layer.name}'レイヤーのカーブが想定と異なるSMRパスを参照しています" +
                                  $"(期待値: '{smrPath}'): {string.Join(", ", mismatchedPaths)}\n" +
                                  "RewriteSmrPathsが正しく実行されているか確認してください。");
            }

            if (missing.Count == 0 && mismatchedPaths.Count == 0)
            {
                Debug.Log($"[hinzka ARKit FT] '{layer.name}'レイヤーの構成を検証しました: " +
                          $"{inverseNames.Length}個の逆Visemeシェイプキーが正しく参照されています。");
            }
        }

                // ── ジェスチャーレイヤーweight制御 ───────────────

        /// <summary>
        /// FT有効中(FT_MenuEnableEyes=true かつ FT_MenuEnableMouth=true)は
        /// 指定レイヤーのweightを0に、停止中は1に制御するレイヤーを追加する。
        /// </summary>
        // ── アバターの複製 ──────────────────────────────

        /// <summary>
        /// インストール対象アバターを複製する。シーン上のインスタンス/Prefabアセットどちらの場合も、
        /// 元の参照(_avatarPrefab)自体は一切変更しない。以降の処理はすべて返り値の複製に対して行う。
        /// </summary>
        private static GameObject DuplicateAvatarForInstall(GameObject source, out bool sourceWasSceneInstance)
        {
            sourceWasSceneInstance = source.scene.IsValid();

            GameObject copy;
            if (sourceWasSceneInstance)
            {
                // 【重要】素朴な UnityEngine.Object.Instantiate() はランタイム向けAPIであり、
                // シーン上に配置されたPrefabインスタンスに対して使うと、ネストしたPrefabとの
                // 接続情報(Hierarchy上の青いアイコン)が失われてしまう(コンポーネントや値は
                // 正しくコピーされるが、Prefabインスタンスとしての構造情報だけが失われる)。
                // これはProjectウィンドウから直接Prefabアセットを指定した場合には起きない
                // (その場合はPrefabUtility.InstantiatePrefabを使っており、そちらは正しく
                // ネスト構造を維持する)。
                //
                // そこで、シーン上のインスタンスに対してはEditorの「複製」(Ctrl+D)と全く同じ
                // 内部処理(Unsupported.DuplicateGameObjectsUsingPasteboard)を使う。これは
                // ネストしたPrefab接続・Prefab Overrides・シーン上で追加されたオブジェクトを
                // すべて正しく維持したまま複製できる、Unity Editor自身が使っているのと
                // 同じ経路である。
                var previousSelection = Selection.objects;
                try
                {
                    Selection.activeGameObject = source;
                    Unsupported.DuplicateGameObjectsUsingPasteboard();
                    copy = Selection.activeGameObject;
                }
                finally
                {
                    Selection.objects = previousSelection;
                }

                if (copy == null || copy == source)
                {
                    // 万一この経路で複製できなかった場合のみ、従来の単純複製にフォールバックする
                    // (この場合ネストしたPrefab接続は失われる可能性がある)。
                    Debug.LogWarning("[hinzka ARKit FT] Editor複製と同等の方式でのアバター複製に失敗したため、" +
                                      "単純な複製にフォールバックします。ネストしたPrefabの接続が失われる場合があります。");
                    copy = Instantiate(source, source.transform.parent);
                    copy.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
                    copy.transform.localScale = source.transform.localScale;
                }
                Undo.RegisterCreatedObjectUndo(copy, "ARKit FT Install (Duplicate Avatar)");
            }
            else
            {
                // Prefabアセットの場合はシーンへインスタンス化するだけで、アセット自体には触れない
                copy = (GameObject)PrefabUtility.InstantiatePrefab(source);
                if (copy == null) return null;
                Undo.RegisterCreatedObjectUndo(copy, "ARKit FT Install (Instantiate Avatar)");
            }

            copy.name = source.name + "_ARKitFT";

            // 元アバターが既にVRChatへアップロード済みの場合、PipelineManagerに
            // Blueprint IDが記録されている。複製先にこれをそのまま引き継ぐと、
            // 同じBlueprint IDを持つGameObjectがプロジェクト内に複数存在することになり、
            // VRC SDK Control Panel側でアバター切替時にNullReferenceExceptionを起こす
            // (アバター一覧の内部処理がBlueprint IDの一意性を前提としているため)。
            // 複製物は「まだ一度もアップロードしていない別のアバター」として扱われるべきなので、
            // Blueprint IDをクリアしておく。
            var pipelineManager = copy.GetComponentInChildren<VRC.Core.PipelineManager>(true);
            if (pipelineManager != null && !string.IsNullOrEmpty(pipelineManager.blueprintId))
            {
                Debug.Log("[hinzka ARKit FT] 複製先のBlueprint ID(元アバター由来)をクリアしました。" +
                          "アップロード時は新規アバターとして扱われます。");
                pipelineManager.blueprintId = "";
                EditorUtility.SetDirty(pipelineManager);
            }

            return copy;
        }

        // ── ジェスチャーレイヤー抑制(アバター本来のFXへ直接注入) ─────

        /// <summary>
        /// アバター本来のFXコントローラーを複製し、その複製に対して直接ジェスチャーweight制御レイヤーを
        /// 追加する。Modular AvatarのMergeAnimatorはVRC_AnimatorLayerControl.layerを
        /// 「マージ元コントローラー内でのローカル番号」としてしか扱えず、マージ元の外にある既存レイヤーを
        /// 狙い撃つ用途はサポートされていないため、対象レイヤーが実際に存在するコントローラー自身に
        /// 直接追加する必要がある。元のFXアセットには一切触れず、複製アバターのDescriptorのFX参照だけを
        /// 複製後のコントローラーに差し替える。
        /// </summary>
        private static void ApplyGestureSuppressionDirectly(
            VRC.SDK3.Avatars.Components.VRCAvatarDescriptor desc,
            List<int> gestureLayerIndices,
            string smrPath,
            string outputFolder,
            HashSet<int> mouthTriggerLayerIndices,
            HashSet<int> eyesTriggerLayerIndices,
            AnimatorController ftFx,
            SkinnedMeshRenderer faceSmr)
        {
            if (desc == null || gestureLayerIndices == null || gestureLayerIndices.Count == 0) return;

            var layers = desc.baseAnimationLayers;
            int fxIdx = ResolveFxLayerIndex(layers);
            if (fxIdx < 0)
            {
                Debug.LogWarning("[hinzka ARKit FT] AvatarDescriptorにFXレイヤーが見つからないため、ジェスチャー抑制をスキップしました。");
                return;
            }

            var srcController = layers[fxIdx].animatorController as AnimatorController;
            if (srcController == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] アバター本来のFXコントローラーが未設定のため、ジェスチャー抑制をスキップしました。");
                return;
            }

            var dstPath = AssetDatabase.GenerateUniqueAssetPath(
                outputFolder + "/" + srcController.name + "_GestureSuppress.controller");
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(srcController), dstPath);
            AssetDatabase.SaveAssets();
            var dstController = AssetDatabase.LoadAssetAtPath<AnimatorController>(dstPath);
            if (dstController == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] アバター本来のFXコントローラーの複製に失敗したため、ジェスチャー抑制をスキップしました。");
                return;
            }

            // 抑制対象レイヤーが実際に触っているシェイプキーを自動検出する(FT自身が駆動しているシェイプは除外)。
            // レイヤーweightの抑制だけでは「そのレイヤーが表情以外の何か(手のIK・小物トグル等)も
            // 兼ねていた場合にそれも巻き込む」ため、シェイプ単位の抑制も併用して被害範囲を絞る。
            // レイヤーweight制御とシェイプリセットは同じ条件(FT_MenuEnableMouth等)で切り替わるため、
            // 1つのレイヤー(1つのStateに両方のBehaviour/Motionを持たせる)にまとめている。
            // 【注意】ここは複製元のsrcController・元のレイヤーインデックスに対して行う必要がある
            // (直後でdstController側にダミーレイヤーを挿入してインデックスがずれる可能性があるため、
            // その処理より必ず先に行う)。
            var customShapes = ScanCustomShapeNamesFromLayers(srcController, gestureLayerIndices, smrPath, ftFx);

            // VRCAnimatorLayerControlは、Layer 0(ベースレイヤー)のweightを変更できないという
            // VRChatの仕様上の制約がある(常にweight=1として扱われる)。抑制対象レイヤーに0番目が
            // 含まれていると、weight制御が実行されても実際には何も起こらず、抑制が効かない。
            // これを避けるため、対象レイヤーに0番目が含まれる場合は、ダミーの空レイヤーを
            // 先頭に挿入して全レイヤーのインデックスを1つずつ後ろへずらす
            // (これにより元の0番目のレイヤーが1番目になり、正常にweight制御できるようになる)。
            if (gestureLayerIndices.Contains(0))
            {
                var dummyLayerName = "hinzkaFT_DummyBaseLayer";
                var existingDummyIdx = dstController.layers.ToList().FindIndex(l => l.name == dummyLayerName);
                if (existingDummyIdx < 0)
                {
                    var dummySm = new AnimatorStateMachine { name = dummyLayerName + "_SM" };
                    AssetDatabase.AddObjectToAsset(dummySm, dstController);
                    HideGeneratedSubAsset(dummySm);
                    dummySm.AddState("Idle", new Vector3(200f, 80f, 0f));

                    var originalLayers = dstController.layers;
                    var newLayers = new AnimatorControllerLayer[originalLayers.Length + 1];
                    newLayers[0] = new AnimatorControllerLayer
                    {
                        name          = dummyLayerName,
                        stateMachine  = dummySm,
                        defaultWeight = 1f,
                        blendingMode  = AnimatorLayerBlendingMode.Override,
                    };
                    Array.Copy(originalLayers, 0, newLayers, 1, originalLayers.Length);

                    // Unityの仕様上、Layer 0は defaultWeight フィールドの値に関わらず常にweight=1
                    // として扱われる。そのため、アバター制作側でもこのフィールドの値を気にせず
                    // (0のまま等)放置されているケースが珍しくない。ダミーレイヤー挿入によって
                    // 元のLayer 0が1番目(=通常のレイヤー)へシフトすると、これまで無視されていた
                    // このフィールドの値が急に有効になってしまい、例えば0のまま放置されていた
                    // 場合はレイヤー全体が実質的にweight=0(非表示)になってしまう。
                    // これを避けるため、シフト後の元Layer 0のdefaultWeightを明示的に1へ補正し、
                    // シフト前と同じ「常にweight=1」の実効的な挙動を保つ。
                    newLayers[1].defaultWeight = 1f;

                    dstController.layers = newLayers;
                    EditorUtility.SetDirty(dstController);

                    // 挿入した分、以降(dstController側)で使う全てのレイヤーインデックス参照を
                    // +1する(呼び出し元が保持しているリストの中身を直接書き換える。
                    // ScanCustomShapeNamesFromLayersは既に完了しているため、ここで書き換えても影響しない)。
                    for (int i = 0; i < gestureLayerIndices.Count; i++)
                        gestureLayerIndices[i] += 1;
                    var shiftedMouth = mouthTriggerLayerIndices.Select(i => i + 1).ToList();
                    mouthTriggerLayerIndices.Clear();
                    foreach (var i in shiftedMouth) mouthTriggerLayerIndices.Add(i);
                    var shiftedEyes = eyesTriggerLayerIndices.Select(i => i + 1).ToList();
                    eyesTriggerLayerIndices.Clear();
                    foreach (var i in shiftedEyes) eyesTriggerLayerIndices.Add(i);

                    Debug.Log("[hinzka ARKit FT] 抑制対象レイヤーにLayer 0(weight制御不可)が含まれていたため、" +
                              "先頭にダミーレイヤーを挿入し、全レイヤーのインデックスを1つずつ後ろへずらしました。");
                }
            }

            AddGestureSuppressionLayer(dstController, smrPath, gestureLayerIndices, customShapes, mouthTriggerLayerIndices, eyesTriggerLayerIndices, faceSmr);
            if (customShapes.Count > 0)
            {
                Debug.Log($"[hinzka ARKit FT] ジェスチャーレイヤーから検出したカスタムシェイプキー" +
                          $"{customShapes.Count}個を、FT有効中はアバター本来の現在値(BlendShapeの現在のウェイト)に" +
                          $"固定します: {string.Join(", ", customShapes)}");
            }
            Debug.Log("[hinzka ARKit FT] Mouth TrackingControlは常にTrackingへ上書きします" +
                      "(FT有効中・FT終了後の両方。VRCAnimatorTrackingControlはステート入場時に一度だけ" +
                      "発火しその後は値が残り続けるため、レイヤーweight抑制だけでは不十分)。" +
                      "ジェスチャーを押しっぱなしのままFTだけをオフにした場合、そのジェスチャーが" +
                      "本来持つMouth=Animationはジェスチャーを再度発火させるまで反映されません。");

            layers[fxIdx].animatorController = dstController;
            layers[fxIdx].isDefault = false;
            desc.baseAnimationLayers = layers;
            EditorUtility.SetDirty(desc);

            Debug.Log($"[hinzka ARKit FT] アバター本来のFXを複製し({dstPath})、" +
                      $"ジェスチャー抑制レイヤーを直接追加しました: {string.Join(", ", gestureLayerIndices)}");
        }

        /// <summary>
        /// 指定したFXレイヤー群(元アバターのFX内、インデックス指定)が参照しているblendShapeカーブのうち、
        /// 標準ARKitシェイプ名(52種)と逆Visemeシェイプ(inverse.*)を除いた「そのアバター固有のカスタムシェイプ」
        /// だけを重複なく抽出する。標準ARKitシェイプは実際のトラッキング値と衝突する恐れがあるため対象外にする。
        /// </summary>
        private static List<string> ScanCustomShapeNamesFromLayers(
            AnimatorController avatarFx, List<int> layerIndices, string smrPath, AnimatorController ftFx)
        {
            var found = new HashSet<string>();
            if (avatarFx == null || layerIndices == null) return new List<string>();

            // 除外対象は「標準ARKit名リスト」ではなく「FT自身(ftFx)が実際に駆動しているシェイプ名」にする。
            // 例えば視線をFT_EyeBone_*(独自名)で駆動している場合、標準名のeyeLookUpLeft等はFT側では
            // 誰も触っていないため、除外してしまうとジェスチャー側の値が誰にもリセットされず残ってしまう。
            var ftOwnedShapes = new HashSet<string>();
            if (ftFx != null)
            {
                var ftClips = new HashSet<AnimationClip>();
                foreach (var l in ftFx.layers)
                    if (l.stateMachine != null) CollectClips(l.stateMachine, ftClips);

                foreach (var clip in ftClips)
                {
                    if (clip == null) continue;
                    foreach (var b in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (b.type != typeof(SkinnedMeshRenderer)) continue;
                        if (!b.propertyName.StartsWith("blendShape.")) continue;
                        ftOwnedShapes.Add(b.propertyName.Substring("blendShape.".Length));
                    }
                }
            }

            var fxLayers = avatarFx.layers;

            foreach (var idx in layerIndices)
            {
                if (idx < 0 || idx >= fxLayers.Length) continue;
                var layer = fxLayers[idx];
                if (layer.stateMachine == null) continue;

                var clips = new HashSet<AnimationClip>();
                CollectClips(layer.stateMachine, clips);

                foreach (var clip in clips)
                {
                    if (clip == null) continue;
                    foreach (var b in AnimationUtility.GetCurveBindings(clip))
                    {
                        if (b.type != typeof(SkinnedMeshRenderer)) continue;
                        if (b.path != smrPath) continue;
                        if (!b.propertyName.StartsWith("blendShape.")) continue;

                        var shapeName = b.propertyName.Substring("blendShape.".Length);
                        if (ftOwnedShapes.Contains(shapeName)) continue; // FT自身が実際に駆動しているシェイプは除外
                        if (shapeName.StartsWith("inverse.")) continue;  // 逆Visemeも除外
                        found.Add(shapeName);
                    }
                }
            }

            return found.OrderBy(s => s, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// ジェスチャーレイヤーの抑制を行う。レイヤーごとに選択されたトリガー(口/目)の
        /// 組み合わせが異なりうるため、実際に使われている組み合わせごとに個別のFXレイヤーを
        /// 生成する(1つのAnimatorレイヤー内では同時に1つのステートしかアクティブになれない
        /// ため、条件が同時に成立しうる複数グループを1レイヤー内の複数ステートにまとめると、
        /// 片方のグループの制御が失われてしまう。レイヤーを分ければ、各レイヤーが独立して
        /// 評価されるため、複数グループの条件が同時に成立していても正しく両方とも効く)。
        /// 検出したカスタムシェイプキーのリセットは、使われている全条件のいずれかが成立して
        /// いれば発動する(Union条件)専用レイヤーとして別途生成する。
        /// </summary>
        private static void AddGestureSuppressionLayer(
            AnimatorController fx,
            string smrPath,
            List<int> gestureLayerIndices,
            List<string> shapeNames,
            HashSet<int> mouthTriggerLayerIndices,
            HashSet<int> eyesTriggerLayerIndices,
            SkinnedMeshRenderer faceSmr)
        {
            const string LAYER_PREFIX = "hinzkaFT_GestureSuppression";
            // FT_MenuEnableEyes/Mouthは「メニューのトグル自体」の生値であり、VRCFTの接続断・
            // AFK等でAutoStopが実際のトラッキングを止めていても、メニュートグル自体は
            // ONのまま残ってしまう。UEFx/FT_EnableEyes・UEFx/FT_EnableMouthは、AutoStop
            // ウォッチドッグがそれらの条件も踏まえて計算した「実際に有効かどうか」を示す
            // Float値(既定1、無効時0)なので、ジェスチャー抑制の実際の発動条件としては
            // こちらを監視する必要がある(メニュートグルだけを見ていると、AutoStopで
            // トラッキングが止まっているのに抑制だけがかかったまま戻らなくなる)。
            const string PARAM_EYES  = "UEFx/FT_EnableEyes";
            const string PARAM_MOUTH = "UEFx/FT_EnableMouth";
            const float PARAM_THRESHOLD = 0.5f;

            bool hasLayers = gestureLayerIndices != null && gestureLayerIndices.Count > 0;
            bool hasShapes = shapeNames != null && shapeNames.Count > 0;
            if (!hasLayers && !hasShapes) return;

            // 既存の関連レイヤーをすべて削除して作り直す(グループ数が変わりうるため、
            // 前回生成されたぶんも含めてプレフィックス一致で掃除する)。
            var existingLayers = fx.layers.ToList();
            for (int i = existingLayers.Count - 1; i >= 0; i--)
                if (existingLayers[i].name.StartsWith(LAYER_PREFIX, StringComparison.Ordinal))
                    fx.RemoveLayer(i);

            foreach (var pName in new[] { PARAM_EYES, PARAM_MOUTH })
            {
                if (!fx.parameters.Any(p => p.name == pName))
                    fx.AddParameter(new AnimatorControllerParameter
                    { name = pName, type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            }

            // (mouth, eyes)の組み合わせごとに対象レイヤーをグループ化する。
            // どちらも未選択のレイヤーは安全側でMouthのみ扱いにする。
            var groups = new Dictionary<(bool mouth, bool eyes), List<int>>();
            var usedParams = new HashSet<string>();
            if (hasLayers)
            {
                foreach (var layerIdx in gestureLayerIndices.Distinct())
                {
                    bool useMouth = mouthTriggerLayerIndices.Contains(layerIdx);
                    bool useEyes = eyesTriggerLayerIndices.Contains(layerIdx);
                    if (!useMouth && !useEyes) useMouth = true;
                    var key = (useMouth, useEyes);
                    if (!groups.TryGetValue(key, out var list))
                    {
                        list = new List<int>();
                        groups[key] = list;
                    }
                    list.Add(layerIdx);
                    if (useMouth) usedParams.Add(PARAM_MOUTH);
                    if (useEyes) usedParams.Add(PARAM_EYES);
                }
            }

            int groupIndex = 0;
            foreach (var kvp in groups)
            {
                groupIndex++;
                bool useMouth = kvp.Key.mouth;
                bool useEyes = kvp.Key.eyes;
                var targetLayers = kvp.Value;

                string suffix = (useMouth && useEyes) ? "MouthOrEyes" : (useMouth ? "Mouth" : "Eyes");
                string layerName = $"{LAYER_PREFIX}_{suffix}";

                var sm = new AnimatorStateMachine { name = layerName + "_SM" };
                AssetDatabase.AddObjectToAsset(sm, fx);
                HideGeneratedSubAsset(sm);

                var emptyClip = new AnimationClip { name = layerName + "_Empty" };
                AssetDatabase.AddObjectToAsset(emptyClip, fx);
                HideGeneratedSubAsset(emptyClip);

                var activeState = sm.AddState("FT_Active",  new Vector3(200f, 80f,  0f));
                var stopState   = sm.AddState("FT_Stopped", new Vector3(200f, 200f, 0f));
                activeState.motion = emptyClip;
                stopState.motion   = emptyClip;
                sm.defaultState     = stopState;

                // レイヤーweight制御: FT_Active → 対象レイヤーweight=0、FT_Stopped → weight=1
                foreach (var targetLayerIdx in targetLayers)
                {
                    AttachLayerControl(activeState, fx, targetLayerIdx, 0f);
                    AttachLayerControl(stopState,   fx, targetLayerIdx, 1f);
                }

                // 抑制対象レイヤーがVRCAnimatorTrackingControlでMouth/EyesをAnimation/Tracking間で
                // 切り替えている場合、レイヤーweightを0にしただけではその切り替え自体は
                // 止まらない(Behaviourはweightと無関係に発火する)。このグループの条件が
                // 有効な間はMouth/Eyes=Trackingで強制的に上書きすることで、ジェスチャー側の
                // 切り替えを無効化する。ジェスチャー自体のモーション(表情等)はそのまま
                // 再生されるため、ジェスチャーの見た目を止めずに済む。
                // TrackingControl由来の値の競合は後段からTrackingを明示して解消する。
                //
                // 【重要】上書きする対象は、このグループ自身のトリガー(useMouth/useEyes)に
                // 対応するフィールドだけに限定する。例えばMouthのみをトリガーとするグループが
                // trackingEyesまで無条件に上書きしてしまうと、目と口のトラッキングを個別に
                // ON/OFFできるはずの仕様が崩れ、Eyeトラッキングの状態に関係なく目線が
                // Trackingへ固定されてしまう(=EyeとMouthの制御がまとまって見える不具合になる)。
                //
                // 【重要】VRCAnimatorTrackingControlは「毎フレーム再評価」ではなく「ステートに
                // 入った瞬間に一度だけ発火し、その後は値が残り続ける」仕様であることを実機検証で
                // 確認した。そのため、FT_Active側にだけ上書きを付けてFT_Stopped側を素通り
                // (何もBehaviourを付けない)にすると、FT終了後もTrackingのまま固定されて
                // しまい、「主張を降ろした」ことにならない。FT_Stopped側にも明示的にTracking
                // を書き込むことで、少なくとも常に一貫した状態(FT中もFT外もTracking)を維持する。
                // なお、ジェスチャーを押しっぱなしのままFTだけをオフにした場合、そのジェスチャーが
                // 本来持つMouth/Eyes=Animationは、ジェスチャーを再度発火させるまで反映されません。
                if (useMouth)
                {
                    AttachTrackingOverride(activeState, "trackingMouth");
                    AttachTrackingOverride(stopState, "trackingMouth");
                }
                if (useEyes)
                {
                    AttachTrackingOverride(activeState, "trackingEyes");
                    AttachTrackingOverride(stopState, "trackingEyes");
                }

                var activeParams = new List<string>();
                if (useMouth) activeParams.Add(PARAM_MOUTH);
                if (useEyes) activeParams.Add(PARAM_EYES);

                foreach (var p in activeParams)
                {
                    var t = sm.AddAnyStateTransition(activeState);
                    t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false;
                    t.AddCondition(AnimatorConditionMode.Greater, PARAM_THRESHOLD, p);
                }

                var tOff = sm.AddAnyStateTransition(stopState);
                tOff.hasExitTime = false; tOff.duration = 0f; tOff.canTransitionToSelf = false;
                foreach (var p in activeParams)
                    tOff.AddCondition(AnimatorConditionMode.Less, PARAM_THRESHOLD, p);

                fx.AddLayer(new AnimatorControllerLayer
                {
                    name          = layerName,
                    stateMachine  = sm,
                    defaultWeight = 1f,
                    blendingMode  = AnimatorLayerBlendingMode.Override,
                });
            }

            // カスタムシェイプキーのリセットは、使われている全条件のいずれかが有効なら発動する
            // (Union条件)。専用レイヤーとして別途1つ生成する。
            if (hasShapes)
            {
                string layerName = LAYER_PREFIX + "_Shapes";
                var sm = new AnimatorStateMachine { name = layerName + "_SM" };
                AssetDatabase.AddObjectToAsset(sm, fx);
                HideGeneratedSubAsset(sm);

                var emptyClip = new AnimationClip { name = layerName + "_Empty" };
                AssetDatabase.AddObjectToAsset(emptyClip, fx);
                HideGeneratedSubAsset(emptyClip);

                var resetClip = new AnimationClip { name = layerName + "_Reset" };
                AssetDatabase.AddObjectToAsset(resetClip, fx);
                HideGeneratedSubAsset(resetClip);

                // FT有効中にこのシェイプを固定する値。既定は0だが、アバター本来のBlendShapeが
                // (カスタム表情の作り込み等で)既に0以外の値に設定されている場合は、その現在値を
                // 維持する。そうしないと、FT開始時にこのリセットレイヤーがユーザーの意図した
                // ベースの表情を一律0へ巻き戻してしまう。
                var faceMesh = faceSmr != null ? faceSmr.sharedMesh : null;
                foreach (var shapeName in shapeNames)
                {
                    float weight = 0f;
                    if (faceMesh != null)
                    {
                        int idx = faceMesh.GetBlendShapeIndex(shapeName);
                        if (idx >= 0) weight = faceSmr.GetBlendShapeWeight(idx);
                    }
                    SetCurve(resetClip, smrPath, shapeName, weight);
                }

                var activeState = sm.AddState("FT_Active",  new Vector3(200f, 80f,  0f));
                var stopState   = sm.AddState("FT_Stopped", new Vector3(200f, 200f, 0f));
                activeState.motion = resetClip;
                stopState.motion   = emptyClip;
                sm.defaultState     = stopState;

                var shapeParams = usedParams.Count > 0 ? usedParams : new HashSet<string> { PARAM_MOUTH };
                foreach (var p in shapeParams)
                {
                    var t = sm.AddAnyStateTransition(activeState);
                    t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false;
                    t.AddCondition(AnimatorConditionMode.Greater, PARAM_THRESHOLD, p);
                }

                var tOff = sm.AddAnyStateTransition(stopState);
                tOff.hasExitTime = false; tOff.duration = 0f; tOff.canTransitionToSelf = false;
                foreach (var p in shapeParams)
                    tOff.AddCondition(AnimatorConditionMode.Less, PARAM_THRESHOLD, p);

                fx.AddLayer(new AnimatorControllerLayer
                {
                    name          = layerName,
                    stateMachine  = sm,
                    defaultWeight = 1f,
                    blendingMode  = AnimatorLayerBlendingMode.Override,
                });
            }

            EditorUtility.SetDirty(fx);
        }

        private static void AttachLayerControl(AnimatorState state, AnimatorController fx, int layerIndex, float weight)
        {
            // VRCAnimatorLayerControlをリフレクションで取得
            Type lcType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name?.Contains("VRCSDK") != true) continue;
                lcType = asm.GetType("VRC.SDK3.Avatars.Components.VRCAnimatorLayerControl");
                if (lcType != null) break;
            }
            if (lcType == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] VRCAnimatorLayerControl not found. Skipped.");
                return;
            }

            var lc = state.AddStateMachineBehaviour(lcType);

            // playable(BlendableLayer列挙型)は VRCAvatarDescriptor.AnimLayerType とは別物で、
            // 並び順も異なる(要素0はActionであってFXではない)。数値を決め打ちせず、
            // フィールドの実際の型からEnum名"FX"を解決して設定する。
            var playableField = lcType.GetField("playable");
            if (playableField != null)
            {
                var fxValue = Enum.Parse(playableField.FieldType, "FX");
                playableField.SetValue(lc, fxValue);
            }
            else
            {
                Debug.LogWarning("[hinzka ARKit FT] VRCAnimatorLayerControl.playable フィールドが見つかりませんでした。");
            }

            lcType.GetField("layer")?.SetValue(lc, layerIndex);
            lcType.GetField("goalWeight")?.SetValue(lc, weight);
            lcType.GetField("blendDuration")?.SetValue(lc, 0f);
            lcType.GetField("debugString")?.SetValue(lc, $"GestureWeight={weight}");
        }

        /// <summary>
        /// VRCAnimatorTrackingControlのtrackingMouthを強制的にTrackingへ上書きするBehaviourを
        /// Stateへ追加する。
        ///
        /// 【背景】VRCAnimatorLayerControlでレイヤーweightを0にしても、そのレイヤー自身の
        /// StateMachineBehaviour(VRCAnimatorTrackingControl等)はweightに関係なく発火し続ける
        /// (weightはブレンド結果の寄与率を制御するだけで、Stateの遷移やBehaviourの実行は
        /// 止めない)。そのため、ジェスチャーレイヤーがMouthのTrackingControlを
        /// Animation/Tracking間で切り替えている場合、抑制中でもその切り替えは動き続け、
        /// Visemeが正しく動作しない(または直前のジェスチャー表情が残る)ことがある。
        ///
        /// この抑制レイヤーはアバター本来のFXコントローラーの末尾に追加されるため、
        /// 同一フレーム内で他レイヤーより後に評価され、Mouth=Trackingで確実に上書きできる。
        /// </summary>
        private static void AttachTrackingOverride(AnimatorState state, string fieldName)
        {
            Type trackingControlType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name?.Contains("VRCSDK") != true) continue;
                trackingControlType = asm.GetType("VRC.SDK3.Avatars.Components.VRCAnimatorTrackingControl");
                if (trackingControlType != null) break;
            }
            if (trackingControlType == null)
            {
                Debug.LogWarning($"[hinzka ARKit FT] VRCAnimatorTrackingControl not found. {fieldName} tracking override skipped.");
                return;
            }

            var field = trackingControlType.GetField(fieldName);
            if (field == null)
            {
                Debug.LogWarning($"[hinzka ARKit FT] {fieldName} field not found. Tracking override skipped.");
                return;
            }

            object trackingValue;
            try
            {
                trackingValue = Enum.Parse(field.FieldType, "Tracking");
            }
            catch
            {
                Debug.LogWarning($"[hinzka ARKit FT] TrackingType.Tracking の解決に失敗しました。{fieldName} tracking override をスキップします。");
                return;
            }

            var tc = state.AddStateMachineBehaviour(trackingControlType);
            field.SetValue(tc, trackingValue);
        }

        // ── Profile互換 / 出力 / パス補助 ─────────────────────

        /// <summary>
        /// ARKitFTProfileに新しい gestureLayerNames(List&lt;string&gt;) が存在する場合だけ読み取る。
        /// 旧Profile型ではプロパティが存在しないため空リストを返し、gestureLayerIndicesへフォールバックする。
        /// </summary>
        private static List<string> GetProfileGestureLayerNames(ARKitFTProfile profile)
        {
            var result = new List<string>();
            if (profile == null) return result;

            var so = new SerializedObject(profile);
            var prop = so.FindProperty("gestureLayerNames");
            if (prop == null || !prop.isArray) return result;

            for (int i = 0; i < prop.arraySize; i++)
            {
                var e = prop.GetArrayElementAtIndex(i);
                if (e != null && !string.IsNullOrEmpty(e.stringValue))
                    result.Add(e.stringValue);
            }
            return result.Distinct().ToList();
        }

        /// <summary>
        /// Profile型に gestureLayerNames が追加済みなら名前を保存する。旧Profile型では何もしない。
        /// </summary>
        private static void SetProfileGestureLayerNames(ARKitFTProfile profile, List<string> names)
        {
            if (profile == null) return;

            var so = new SerializedObject(profile);
            var prop = so.FindProperty("gestureLayerNames");
            if (prop == null || !prop.isArray) return;

            names = names ?? new List<string>();
            prop.arraySize = names.Count;
            for (int i = 0; i < names.Count; i++)
                prop.GetArrayElementAtIndex(i).stringValue = names[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// VRChatのExpression Parameters合計ビット数を計算する(networkSynced=trueのもののみ対象)。
        /// Bool=1bit、Int/Float=8bit。VRChatの合計上限は256bit。
        /// </summary>
        private static int ComputeVrcParameterBits(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters paramAsset)
        {
            if (paramAsset?.parameters == null) return 0;
            int bits = 0;
            foreach (var p in paramAsset.parameters)
            {
                if (p == null || !p.networkSynced) continue;
                switch (p.valueType)
                {
                    case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Bool:
                        bits += 1; break;
                    case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Int:
                    case VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.ValueType.Float:
                        bits += 8; break;
                }
            }
            return bits;
        }

        private static bool IsValidAssetsFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            path = path.Replace('\\', '/').TrimEnd('/');
            return path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal);
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            folderPath = folderPath.Replace('\\', '/').TrimEnd('/');
            if (!IsValidAssetsFolder(folderPath))
                throw new ArgumentException("Assets配下ではないフォルダは作成できません。", nameof(folderPath));
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            var parts = folderPath.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    var guid = AssetDatabase.CreateFolder(current, parts[i]);
                    if (string.IsNullOrEmpty(guid))
                        throw new IOException($"出力フォルダを作成できませんでした: {next}");
                }
                current = next;
            }
        }

        private static string CreateUniqueInstallOutputFolder(string baseFolder, string avatarName)
        {
            EnsureAssetFolder(baseFolder);
            string safeName = SanitizeAssetName(string.IsNullOrWhiteSpace(avatarName) ? "Avatar_ARKitFT" : avatarName);
            string desired = baseFolder.TrimEnd('/') + "/" + safeName;
            string unique = AssetDatabase.GenerateUniqueAssetPath(desired);

            string parent = Path.GetDirectoryName(unique)?.Replace('\\', '/');
            string leaf = Path.GetFileName(unique);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
                throw new IOException("INSTALL用出力フォルダ名を生成できませんでした。");

            var guid = AssetDatabase.CreateFolder(parent, leaf);
            if (string.IsNullOrEmpty(guid) || !AssetDatabase.IsValidFolder(unique))
                throw new IOException($"INSTALL用出力フォルダを作成できませんでした: {unique}");
            return unique;
        }

        private static string SanitizeAssetName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.Select(c => invalid.Contains(c) || c == '/' || c == '\\' ? '_' : c).ToArray();
            var result = new string(chars).Trim();
            return string.IsNullOrEmpty(result) ? "Avatar_ARKitFT" : result;
        }

        /// <summary>
        /// 指定レイヤー内の指定blendShapeだけ、参照SMRパスを差し替える。
        /// Viseme SMRがFace SMRとは別メッシュの場合に使用する。
        /// </summary>
        private static void RewriteBlendShapePathsInLayer(
            AnimatorController fx,
            string layerNameContains,
            string oldPath,
            string newPath,
            IEnumerable<string> shapeNames)
        {
            if (fx == null || oldPath == newPath) return;
            var targetNames = new HashSet<string>(shapeNames ?? Enumerable.Empty<string>());
            if (targetNames.Count == 0) return;

            foreach (var layer in fx.layers)
            {
                if (!layer.name.Contains(layerNameContains)) continue;

                var clips = new HashSet<AnimationClip>();
                CollectClips(layer.stateMachine, clips);
                foreach (var clip in clips)
                {
                    var bindings = AnimationUtility.GetCurveBindings(clip);
                    bool dirty = false;
                    foreach (var b in bindings)
                    {
                        if (b.type != typeof(SkinnedMeshRenderer)) continue;
                        if (b.path != oldPath) continue;
                        if (!b.propertyName.StartsWith("blendShape.")) continue;

                        string shapeName = b.propertyName.Substring("blendShape.".Length);
                        if (!targetNames.Contains(shapeName)) continue;

                        var curve = AnimationUtility.GetEditorCurve(clip, b);
                        AnimationUtility.SetEditorCurve(clip, b, null);
                        var nb = b;
                        nb.path = newPath;
                        AnimationUtility.SetEditorCurve(clip, nb, curve);
                        dirty = true;
                    }
                    if (dirty) EditorUtility.SetDirty(clip);
                }
            }
        }

        // ── ユーティリティ ────────────────────────────────

        private static string GetRelativePath(Transform root, Transform target)
        {
            var parts = new List<string>();
            var t = target;
            while (t != null && t != root)
            {
                parts.Insert(0, t.name);
                t = t.parent;
            }
            return string.Join("/", parts);
        }

        private static T FindTemplate<T>(string fileName) where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(fileName));

            int nameMatchCount = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)) continue;
                nameMatchCount++;

                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) return asset;

                // ファイル名は一致したが型としてロードできなかった場合
                // (Missing Script化、あるいは同名で型が異なる別アセットの可能性)。
                // ここで諦めず、他に候補がないか探し続ける。
                var mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
                string detectedType = mainAsset != null ? mainAsset.GetType().FullName : "null (Missing Scriptの可能性)";
                Debug.LogWarning($"[hinzka ARKit FT] '{path}' は名前が一致しましたが、" +
                                  $"{typeof(T).Name}として読み込めませんでした。検出された型: {detectedType}\n" +
                                  "スクリプトの.metaファイルのGUIDが変わっている(スクリプトを作り直した等)か、" +
                                  "同名で型の異なる別アセットの可能性があります。");
            }

            if (nameMatchCount == 0)
            {
                Debug.Log($"[hinzka ARKit FT] '{fileName}' という名前のアセットがプロジェクト内に" +
                          $"見つかりませんでした(名前検索のヒット数: {guids.Length}件)。" +
                          "Assetsフォルダ内に配置されているか、ファイル名が完全に一致しているかご確認ください。");
            }

            return null;
        }
    }
}
#endif
