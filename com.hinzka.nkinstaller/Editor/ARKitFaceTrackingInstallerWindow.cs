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
            ["アバターによって、Blendshape名に接頭辞が付いているためにARKitのシェイプキーを\n正しく検出できないことがあります。接頭辞を指定すると、該当の文字列を除去して検索します。"] = new[] { "On some avatars, a prefix on Blendshape names can prevent ARKit shape keys from being detected correctly.\nSpecifying the prefix will strip that text before searching.", "根据角色不同，Blendshape名称可能带有前缀，导致无法正确检测到ARKit形态键。\n指定前缀后，将在搜索时去除该字符串。", "아바타에 따라 Blendshape 이름에 접두사가 붙어 있어 ARKit 쉐이프 키를\n올바르게 감지하지 못할 수 있습니다. 접두사를 지정하면 해당 문자열을 제거하고 검색합니다." },
            ["空 / 未検出シェイプの同期をオフにする"] = new[] { "Disable sync for empty / undetected shapes", "关闭空/未检测到的形态键的同步", "비어 있음/미검출 쉐이프의 동기화 끄기" },
            ["見た目に影響しないARKitシェイプに対応するNetwork Syncedパラメータをオフにし、bit予算を節約します。"] = new[] { "Turns off Network Synced parameters for ARKit shapes that have no visual effect, saving bit budget.", "关闭对外观没有影响的ARKit形态键所对应的Network Synced参数，以节省bit预算。", "외형에 영향을 주지 않는 ARKit 쉐이프에 대응하는 Network Synced 파라미터를 꺼서 bit 예산을 절약합니다." },
            ["空 / 未検出のシェイプキーを検出した場合、対応する同期パラメータをオフにして節約します。"] = new[] { "When empty / undetected shape keys are found, the corresponding sync parameters are turned off to save budget.", "检测到空/未检测到的形态键时，将关闭对应的同步参数以节省预算。", "비어 있음/미검출 쉐이프 키가 감지되면 해당 동기화 파라미터를 꺼서 절약합니다." },
            ["表情メッシュと目メッシュが別々"] = new[] { "Face mesh and eye mesh are separate", "表情网格与眼部网格是分离的", "표정 메시와 눈 메시가 분리됨" },
            ["にっこり目"] = new[] { "Smile Eyes", "笑眼", "스마일 아이" },
            ["任意のシェイプキーを「にっこり目」として指定できます。\n未指定の場合はARKitのeyeSquintLeft・eyeSquintRightが設定されます。"] = new[] { "You can designate any shape key as \"Smile Eyes\".\nIf left unspecified, ARKit's eyeSquintLeft/eyeSquintRight will be used.", "可以将任意形态键指定为“笑眼”。\n如果未指定，将使用ARKit的eyeSquintLeft・eyeSquintRight。", "임의의 쉐이프 키를 \"스마일 아이\"로 지정할 수 있습니다.\n지정하지 않으면 ARKit의 eyeSquintLeft・eyeSquintRight가 사용됩니다。" },
            ["アバターを選択するとShape Keyを指定できます。"] = new[] { "Select an avatar to specify Shape Keys.", "选择角色后即可指定Shape Key。", "아바타를 선택하면 Shape Key를 지정할 수 있습니다." },
            ["検索"] = new[] { "Search", "搜索", "검색" },
            ["＋ Shape Keyを追加"] = new[] { "+ Add Shape Key", "＋ 添加Shape Key", "+ Shape Key 추가" },
            ["ジェスチャー表情の抑制"] = new[] { "Suppress Gesture Expressions", "抑制手势表情", "제스처 표정 억제" },
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
            ["抑制レイヤーが未選択です。フェイストラッキング中もジェスチャー表情が混ざります。"] = new[] { "No suppression layer is selected. Gesture expressions will mix in even during Face Tracking.", "未选择抑制图层。即使在FaceTracking期间，手势表情也会混入。", "억제 레이어가 선택되지 않았습니다. 페이스 트래킹 중에도 제스처 표정이 섞입니다." },
            ["EyeLookシェイプキーを自動生成します。"] = new[] { "Automatically generates EyeLook shape keys.", "自动生成EyeLook形态键。", "EyeLook 쉐이프 키를 자동 생성합니다." },
            ["AvatarDescriptorのEye Lookを無効化し、VRChat標準との競合を回避します。"] = new[] { "Disables AvatarDescriptor's Eye Look, avoiding conflicts with VRChat's standard.", "禁用AvatarDescriptor的Eye Look，避免与VRChat标准产生冲突。", "AvatarDescriptor의 Eye Look을 비활성화하여 VRChat 표준과의 충돌을 회피합니다。" },
            ["VRChat標準のEye Lookを維持します。アバターによってはFT中に競合する場合があります。"] = new[] { "Keeps VRChat's standard Eye Look. On some avatars this may conflict while FT is active.", "保持VRChat标准的Eye Look。根据角色不同，在FT期间可能会产生冲突。", "VRChat 표준 Eye Look을 유지합니다. 아바타에 따라 FT 중 충돌이 발생할 수 있습니다." },
            ["Viseme"] = new[] { "Viseme", "Viseme", "Viseme" },
            ["逆Viseme補償シェイプキーを生成します。"] = new[] { "Generates inverse-Viseme compensation shape keys.", "生成逆Viseme补偿形态键。", "역 Viseme 보정 쉐이프 키를 생성합니다." },
            ["まばたき連動の眉アシストシェイプキーを生成します。"] = new[] { "Generates blink-linked brow assist shape keys.", "生成与眨眼联动的眉毛辅助形态键。", "눈 깜빡임 연동 눈썹 보조 쉐이프 키를 생성합니다." },
            ["✓ Avatar"] = new[] { "✓ Avatar", "✓ Avatar", "✓ Avatar" },
            ["Avatarが選択されています。"] = new[] { "An Avatar is selected.", "已选择Avatar。", "Avatar가 선택되어 있습니다." },
            ["⚠ Eye競合 {0}"] = new[] { "⚠ Eye conflict {0}", "⚠ Eye冲突 {0}", "⚠ Eye 충돌 {0}" },
            ["VRCAnimatorTrackingControlでEyesを直接書き換えているFXレイヤー"] = new[] { "FX layers that directly overwrite Eyes via VRCAnimatorTrackingControl", "通过VRCAnimatorTrackingControl直接改写Eyes的FX图层", "VRCAnimatorTrackingControl로 Eyes를 직접 덮어쓰는 FX 레이어" },
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
            ["まばたきするたびに1回だけ再生される「おまけ」の演出です。\nテンプレートFXに既定の演出は同梱されていません(アバターによってシェイプキーの構成が\n異なり、汎用的な演出クリップを用意できないためです)。アバター固有のシェイプキーで\n構成したエフェクト用のAnimationClipを別途ご用意のうえ、下欄で指定してください。"] = new[] { "A \"bonus\" effect that plays once each time you blink.\nNo default effect is bundled with the template FX (since shape keys differ between avatars,\nno generic effect clip can be provided). Please prepare your own AnimationClip built from\nyour avatar's own shape keys and specify it below.", "这是每次眨眼时播放一次的“附加”演出。\n模板FX中并未内置默认演出(因为不同角色的形态键构成各不相同，\n无法提供通用的演出片段)。请另行准备使用你自己角色专属形态键制作的\nAnimationClip，并在下方指定。", "눈을 깜빡일 때마다 한 번씩 재생되는 \"보너스\" 연출입니다.\n템플릿 FX에는 기본 연출이 포함되어 있지 않습니다(아바타마다 쉐이프 키 구성이\n달라 범용적인 연출 클립을 제공할 수 없기 때문입니다). 아바타 고유의 쉐이프 키로\n구성한 이펙트용 AnimationClip을 별도로 준비하여 아래에서 지정해 주세요." },
            ["アバター固有のシェイプキーで構成したAnimationClipを指定してください。\n未指定のままだと、まばたき時に何も再生されません。"] = new[] { "Specify an AnimationClip built from your avatar's own shape keys.\nIf left unspecified, nothing will play when blinking.", "请指定使用你角色专属形态键制作的AnimationClip。\n如果保持未指定，眨眼时将不会播放任何内容。", "아바타 고유의 쉐이프 키로 구성한 AnimationClip을 지정하세요.\n지정하지 않으면 눈을 깜빡일 때 아무것도 재생되지 않습니다." },
            ["まばたきで揺れる瞳ハイライトのアニメーションなどを設定してください。"] = new[] { "Set something like an animation of the eye highlight swaying with each blink.", "请设置例如随眨眼摆动的瞳孔高光动画等演出。", "눈을 깜빡일 때 흔들리는 눈동자 하이라이트 애니메이션 등을 설정해 주세요." },
            ["有効 / クリップ設定済み ({0})"] = new[] { "Enabled / clip set ({0})", "有效 / 已设置片段 ({0})", "활성화 / 클립 설정됨 ({0})" },
            ["有効 / クリップ未設定(演出は再生されません)"] = new[] { "Enabled / no clip set (nothing will play)", "有效 / 未设置片段(不会播放演出)", "활성화 / 클립 미설정 (연출이 재생되지 않음)" },
            ["⚠ エフェクトクリップ未設定"] = new[] { "⚠ No effect clip set", "⚠ 未设置特效片段", "⚠ 이펙트 클립 미설정" },
            ["クリップが未指定のため、まばたき時に演出は再生されません。"] = new[] { "Since no clip is specified, nothing will play when blinking.", "由于未指定片段，眨眼时不会播放任何演出。", "클립이 지정되지 않아 눈을 깜빡일 때 연출이 재생되지 않습니다." },
            ["識別タグ (任意)"] = new[] { "Match Tag (Optional)", "识别标签(可选)", "식별 태그 (선택)" },
            ["このProfileを自動選択する際の目印となる文字列。Avatar名にこの文字列が含まれていれば、\nバージョン名や接頭辞・接尾辞が付いていても最優先でこのProfileが選ばれる。\nカンマ区切りで複数指定できる(いずれか1つでも一致すれば選ばれる)。\n空の場合はファイル名からの推測にフォールバックする。\n「保存」ボタンで他の設定と一緒に保存される。"] = new[] { "A string used as a marker for automatically selecting this Profile. If the Avatar's name\ncontains this string, this Profile is chosen first, even with version numbers or a\nprefix/suffix attached.\nMultiple tags can be given, separated by commas (a match on any one of them is enough).\nIf left blank, matching falls back to guessing from the file name.\nSaved together with the other settings via the \"Save\" button.", "用于自动选择该Profile的标记字符串。只要Avatar名称中包含此字符串，\n即使带有版本号或前后缀，也会优先选中该Profile。\n可用逗号分隔指定多个标签(只要其中任意一个匹配即可)。\n留空时将回退为根据文件名进行推测。\n会通过“保存”按钮与其他设置一起保存。", "이 Profile을 자동으로 선택할 때 기준이 되는 문자열입니다. Avatar 이름에 이 문자열이\n포함되어 있으면, 버전 이름이나 접두사・접미사가 붙어 있어도 이 Profile이\n최우선으로 선택됩니다.\n쉼표로 구분하여 여러 개를 지정할 수 있습니다(그 중 하나라도 일치하면 선택됩니다).\n비워두면 파일 이름 기반 추측으로 대체됩니다.\n\"저장\" 버튼으로 다른 설정과 함께 저장됩니다." },
            ["Profileに保存されたFace Meshが現在のアバター上に見つかりませんでした(アバターの階層が変わった可能性があります)。下のFace Meshを確認し、必要なら選び直してください。"] = new[] { "The Face Mesh saved in the Profile could not be found on the current avatar (its hierarchy may have changed). Please check the Face Mesh below and reselect it if needed.", "在当前角色上未找到Profile中保存的Face Mesh(角色的层级结构可能已发生变化)。请确认下方的Face Mesh，如有需要请重新选择。", "Profile에 저장된 Face Mesh를 현재 아바타에서 찾을 수 없습니다(아바타의 계층 구조가 변경되었을 수 있습니다). 아래의 Face Mesh를 확인하고 필요하면 다시 선택해 주세요." },
            ["このProfileを読み込む"] = new[] { "Load this Profile", "读取此Profile", "이 Profile 불러오기" },
            ["識別タグを編集... (読み込まない)"] = new[] { "Edit Match Tag... (don't load)", "编辑识别标签...(不读取)", "식별 태그 편집... (불러오지 않음)" },
            ["識別タグを編集"] = new[] { "Edit Match Tag", "编辑识别标签", "식별 태그 편집" },
            ["キャンセル"] = new[] { "Cancel", "取消", "취소" },
            ["Profileの内容が現在のアバターと一致しない可能性があります"] = new[] { "This Profile's contents may not match the current avatar", "该Profile的内容可能与当前角色不匹配", "이 Profile의 내용이 현재 아바타와 일치하지 않을 수 있습니다" },
            ["適用する"] = new[] { "Apply", "应用", "적용" },
            ["適用しない"] = new[] { "Don't Apply", "不应用", "적용 안 함" },
            ["選択したProfile('{0}')に保存されているFace Mesh('{1}')が、\n現在選択中のアバターには見つかりません。\n\nこのまま適用すると、現在のアバターに対する作業内容がこのProfileの設定で\n上書きされます。識別タグの編集だけが目的の場合は「適用しない」を選び、\n「既存Profile」メニューの「識別タグを編集...」をご利用ください。"] = new[] { "The Face Mesh ('{1}') saved in the selected Profile ('{0}') could not be found on the\ncurrently selected avatar.\n\nApplying it now will overwrite your current work on this avatar with this Profile's\nsettings. If you only wanted to edit the match tag, choose \"Don't Apply\" and use\n\"Edit Match Tag...\" from the \"Existing Profile\" menu instead.", "所选Profile(“{0}”)中保存的Face Mesh(“{1}”)在当前选择的角色上未找到。\n\n如果继续应用，当前角色的作业内容将被该Profile的设置覆盖。\n如果只是想编辑识别标签，请选择“不应用”，并改用“已有Profile”菜单中的\n“编辑识别标签...”。", "선택한 Profile('{0}')에 저장된 Face Mesh('{1}')를 현재 선택된 아바타에서\n찾을 수 없습니다.\n\n지금 적용하면 현재 아바타에 대한 작업 내용이 이 Profile의 설정으로\n덮어써집니다. 식별 태그 편집만 하려는 경우 \"적용 안 함\"을 선택하고\n\"기존 Profile\" 메뉴의 \"식별 태그 편집...\"을 이용해 주세요." },
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
        private bool _gestureSuppressOnEyesOrMouth = false; // false=Mouthのみで抑制(既定)、true=Eyes OR Mouthで抑制
        private List<string> _lastEyeLookEmptyDeltaShapes = new List<string>();
        private bool _eyeSmrSeparate = false;
        private int _eyeSmrIndex = 0;
        private bool _eyeUsesConstraint = false;
        private Transform _leftEyeConstraintTarget;
        private Transform _rightEyeConstraintTarget;
        private float _eyeLookIntensity = 1f;
        private bool _disableNativeEyeLook = false; // false=標準Eye Lookを維持、true=無効化(ラジオボタンで選択)
        private string _arkitShapePrefix = "";
        private bool _hasBlendshapePrefix = false;
        // ARKit標準シェイプ名が見つからない場合、UE(Unified Expressions)側の代替名も
        // 検索するかどうか。ONの場合、_missingArkitShapesのうちUE代替名で解決できたものは
        // 「不足」から除外し、_ueFallbackResolvedShapesに記録する(Install時に実際の
        // カーブ複製もこの情報を使って行う)。
        private bool _ueFallbackEnabled = false;
        private Dictionary<string, string[]> _ueFallbackResolvedShapes = new Dictionary<string, string[]>();
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

        // 眉アシスト
        private bool _generateBrowAssistShapes = false;
        private float _browAssistIntensity = 0.5f;

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
        private Toggle _uiGestureSuppressToggle;
        private VisualElement _uiGestureSuppressDetail;
        private VisualElement _uiGestureAvatarGate;
        private VisualElement _uiGestureNoAvatarHint;

        private Toggle _uiVisemeToggle;
        private Slider _uiVisemeSlider;
        private FloatField _uiVisemeValue;
        private VisualElement _uiVisemeDetail;

        private Toggle _uiEyeLookToggle;
        private Slider _uiEyeLookSlider;
        private FloatField _uiEyeLookValue;
        private VisualElement _uiEyeLookDetail;
        private Toggle _uiEyeConstraintToggle;
        private VisualElement _uiEyeConstraintFields;
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
            BuildToolkitAssistCard();
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
                "カンマ区切りで複数指定できる(いずれか1つでも一致すれば選ばれる)。\n" +
                "空の場合はファイル名からの推測にフォールバックする。\n" +
                "「保存」ボタンで他の設定と一緒に保存される。");
            _uiAvatarMatchTagField.RegisterValueChangedCallback(evt => { _avatarMatchTag = evt.newValue ?? ""; });
            tagRow.Add(_uiAvatarMatchTagField);
            _uiAvatarMatchTagRow = tagRow;
            hero.Add(tagRow);

            _uiHeaderHost.Add(hero);
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

            _uiArkitPrefixField = new TextField("Blendshape Prefix");
            _uiArkitPrefixField.RegisterValueChangedCallback(evt =>
            {
                _arkitShapePrefix = evt.newValue ?? "";
                RefreshArkitCheck();
                RefreshToolkitUI();
            });
            _uiBlendshapePrefixDetail.Add(_uiArkitPrefixField);
            _uiBlendshapePrefixDetail.Add(MakeHint(
                ArkitFTLoc.T("アバターによって、Blendshape名に接頭辞が付いているためにARKitのシェイプキーを\n" +
                "正しく検出できないことがあります。接頭辞を指定すると、該当の文字列を除去して検索します。"),
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
            });
            _uiEyeSmrRow.Add(_uiEyeSmrField);
            _uiFaceDetail.Add(_uiEyeSmrRow);
            _uiFaceAvatarGate.Add(_uiFaceDetail);
            card.Add(_uiFaceAvatarGate);

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

            _uiBasicPage.Add(card);
        }

        private void BuildToolkitGestureCard()
        {
            var card = MakeCard("GESTURE", ArkitFTLoc.T("ジェスチャー表情の抑制"), "accent-primary");
            _uiGestureCard = card;
            card.Add(MakeHint(
                ArkitFTLoc.T("フェイストラッキング(MouthTracking)実行中は、ジェスチャー表情が動かないように設定できます。\n" +
                "混ざってほしくないFXレイヤーをすべて選択してください。"),
                "soft"));

            _uiGestureNoAvatarHint = MakeHint(ArkitFTLoc.T("アバターを選択するとFXレイヤーを指定できます。"), "soft");
            card.Add(_uiGestureNoAvatarHint);

            _uiGestureAvatarGate = new VisualElement();

            _uiGestureSearchField = new TextField(ArkitFTLoc.T("検索"));
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
                    RebuildGestureList();
                    RefreshReadyToInstallChips();
                    RefreshCardAccents(_avatarPrefab != null);
                }
            }) { text = ArkitFTLoc.T("＋ Layerを追加") };
            _uiGestureAddButton.AddToClassList("add-button");
            _uiGestureAvatarGate.Add(_uiGestureAddButton);

            _uiGestureSuppressToggle = new Toggle(ArkitFTLoc.T("EyeTrackingだけでも抑制する"));
            _uiGestureSuppressToggle.tooltip =
                ArkitFTLoc.T("OFF(既定): MouthTracking時のみ抑制します。\n" +
                "ON: Eyes / Mouthどちらかが有効なら抑制します。\n" +
                "ジェスチャー中も目を動かし続けたい場合はOFFのままにしてください。");
            _uiGestureSuppressToggle.RegisterValueChangedCallback(evt =>
            {
                _gestureSuppressOnEyesOrMouth = evt.newValue;
            });

            // 抑制レイヤーが1つも登録されていない段階では、この設定は意味を持たないため
            // まとめて非表示にできるコンテナに入れる(RebuildGestureListで表示制御)。
            _uiGestureSuppressDetail = new VisualElement();
            _uiGestureSuppressDetail.Add(_uiGestureSuppressToggle);
            _uiGestureSuppressDetail.Add(MakeHint(
                ArkitFTLoc.T("ONにするとEyeTrackingだけでもジェスチャー表情を抑制します。\n" +
                "ジェスチャー中も目を動かしたい場合はOFFのままにしてください。"),
                "soft"));
            _uiGestureAvatarGate.Add(_uiGestureSuppressDetail);
            card.Add(_uiGestureAvatarGate);

            _uiBasicPage.Add(card);
        }

        private void BuildToolkitMouthCard()
        {
            var card = MakeCard("MOUTH", ArkitFTLoc.T("音声リップシンク形状の抑制"), "accent-primary");
            _uiMouthCard = card;

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
                ArkitFTLoc.T("FT OFF時もVRChat標準の目線へ戻ります。\nアバターによってはFT中に競合する場合があります。")));
            _uiEyeCompatCard.RegisterCallback<MouseDownEvent>(_ =>
            {
                _disableNativeEyeLook = false;
                RefreshToolkitUI();
            });

            _uiEyeStableCard = new VisualElement();
            _uiEyeStableCard.AddToClassList("mode-card");
            _uiEyeStableCard.AddToClassList("recommended-card");
            var recommended = new Label("RECOMMENDED");
            recommended.AddToClassList("recommended-badge");
            _uiEyeStableCard.Add(recommended);
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
                ArkitFTLoc.T("まばたきするたびに1回だけ再生される「おまけ」の演出です。\n" +
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

                var row = new VisualElement();
                row.AddToClassList("selection-row");

                var popup = new PopupField<int>(
                    $"Layer {rowIndex + 1}", choices, selectedIndex,
                    formatSelectedValueCallback: i => (i >= 0 && i < _fxLayerNames.Length) ? _fxLayerNames[i] : "?",
                    formatListItemCallback: i => (i >= 0 && i < _fxLayerNames.Length) ? _fxLayerNames[i] : "?");
                popup.AddToClassList("grow-field");

                var badge = new Label($"#{selectedIndex}");
                badge.AddToClassList("index-badge");

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

                row.Add(popup);
                row.Add(badge);
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
            if (_uiAvatarMatchTagRow != null)
                _uiAvatarMatchTagRow.style.display = _profile != null ? DisplayStyle.Flex : DisplayStyle.None;
            _uiAvatarField?.SetValueWithoutNotify(_avatarPrefab);

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

            _uiSquintSearchField?.SetValueWithoutNotify(_squintSearchQuery ?? "");
            RebuildSquintList();

            _uiGestureSearchField?.SetValueWithoutNotify(_gestureSearchQuery ?? "");
            _uiGestureSuppressToggle?.SetValueWithoutNotify(_gestureSuppressOnEyesOrMouth);
            RebuildGestureList();

            _uiVisemeToggle?.SetValueWithoutNotify(_generateVisemeCompensation);
            _uiVisemeSlider?.SetValueWithoutNotify(_visemeScale);
            _uiVisemeValue?.SetValueWithoutNotify(_visemeScale);
            if (_uiVisemeDetail != null)
                _uiVisemeDetail.style.display = _generateVisemeCompensation ? DisplayStyle.Flex : DisplayStyle.None;

            _uiEyeLookToggle?.SetValueWithoutNotify(_generateEyeLookShapes);
            _uiEyeLookSlider?.SetValueWithoutNotify(_eyeLookIntensity);
            _uiEyeLookValue?.SetValueWithoutNotify(_eyeLookIntensity);
            if (_uiEyeLookDetail != null)
                _uiEyeLookDetail.style.display = _generateEyeLookShapes ? DisplayStyle.Flex : DisplayStyle.None;

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
            _uiBlinkEffectCard?.EnableInClassList("accent-unset", !_addBlinkEffect);
            _uiInstallResultCard?.EnableInClassList("accent-unset", _lastInstallResult == null);
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
                    ArkitFTLoc.T("抑制レイヤーが未選択です。フェイストラッキング中もジェスチャー表情が混ざります。")));

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
                            ArkitFTLoc.T("VRCAnimatorTrackingControlでEyesを直接書き換えているFXレイヤー") +
                            ArkitFTLoc.T("(ジェスチャー切替のたびに再発火し、VRChat標準の目線制御へ一時的に") +
                            ArkitFTLoc.T("戻ってしまう可能性があります):\n") +
                            string.Join("\n", _eyeTrackingControlLayerNames);
                        _uiAvatarStatusRow.Add(eyeConflictChip);
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
                    // Stableモード(AvatarDescriptor Eye Look無効化)を選択している場合、
                    // 競合の原因であるVRChat標準Eye Look自体が動作しないため、
                    // TrackingControl競合候補が存在しても実害はない。警告色ではなく
                    // 安全であることが伝わる表示に切り替える。
                    _uiEyeConflictBox.AddToClassList("warning-card-safe");
                    _uiEyeConflictText.text =
                        string.Format(ArkitFTLoc.T("✓ TrackingControl競合候補 {0}件を検出していますが、\n"),
                            _eyeTrackingControlLayerNames.Count)
                        + ArkitFTLoc.T("Stableモードが選択されているため影響を受けません。");
                }
                else
                {
                    _uiEyeConflictBox.AddToClassList("warning-card-alert");
                    _uiEyeConflictText.text =
                        string.Format(ArkitFTLoc.T("⚠ TrackingControl競合候補 {0}件\n"),
                            _eyeTrackingControlLayerNames.Count)
                        + string.Join(" / ", _eyeTrackingControlLayerNames)
                        + ArkitFTLoc.T("\nStableモードを推奨します。");
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

            // アバターに依存しない項目は常に適用
            _generateVisemeCompensation = _profile.generateVisemeCompensation;
            _visemeScale = _profile.visemeScale;
            _generateEyeLookShapes = _profile.generateEyeLookShapes;
            _generateBrowAssistShapes = _profile.generateBrowAssistShapes;
            _browAssistIntensity = _profile.browAssistIntensity;
            _addBlinkEffect = _profile.addBlinkEffect;
            _blinkEffectClip = _profile.blinkEffectClip;
            _eyeLookIntensity = _profile.eyeLookIntensity;
            _disableNativeEyeLook = _profile.disableNativeEyeLook;
            _gestureSuppressOnEyesOrMouth = _profile.gestureSuppressOnEyesOrMouth;
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

            _profile.gestureLayerIndices = new List<int>(_gestureLayerIndices.Distinct()); // 旧Profile互換
            SetProfileGestureLayerNames(_profile, _gestureLayerIndices
                .Where(i => i >= 0 && i < _fxLayerNames.Length)
                .Select(i => _fxLayerNames[i])
                .Distinct()
                .ToList());
            _profile.generateVisemeCompensation = _generateVisemeCompensation;
            _profile.visemeScale = _visemeScale;
            _profile.generateEyeLookShapes = _generateEyeLookShapes;
            _profile.generateBrowAssistShapes = _generateBrowAssistShapes;
            _profile.browAssistIntensity = _browAssistIntensity;
            _profile.addBlinkEffect = _addBlinkEffect;
            _profile.blinkEffectClip = _blinkEffectClip;
            _profile.eyeLookIntensity = _eyeLookIntensity;
            _profile.disableNativeEyeLook = _disableNativeEyeLook;
            _profile.gestureSuppressOnEyesOrMouth = _gestureSuppressOnEyesOrMouth;
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
        /// 各Profileは「読み込む」(=現在のアバターへ全設定を適用)と「識別タグを編集...」
        /// (=Profileを読み込まず、タグだけを直接書き換える)の2アクションを持つサブメニューに
        /// なっている。後者は、「複数のProfileにタグだけ付け直したい」といった作業のときに、
        /// 誤って別アバター用の設定一式を今のアバターへ適用してしまう事故を防ぐためのもの。
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

                    // GenericMenuは項目名に "/" を含めるとサブメニューとして扱われる。
                    menu.AddItem(new GUIContent($"{entry.name}/{ArkitFTLoc.T("このProfileを読み込む")}"), isCurrent, () =>
                    {
                        var loaded = AssetDatabase.LoadAssetAtPath<ARKitFTProfile>(capturedPath);
                        if (loaded != null) SetCurrentProfile(loaded, true);
                    });
                    menu.AddItem(new GUIContent($"{entry.name}/{ArkitFTLoc.T("識別タグを編集... (読み込まない)")}"), false, () =>
                    {
                        var loaded = AssetDatabase.LoadAssetAtPath<ARKitFTProfile>(capturedPath);
                        if (loaded != null) ProfileTagEditPopup.Open(loaded);
                    });
                }
            }

            menu.AddSeparator("");
            menu.AddItem(new GUIContent(ArkitFTLoc.T("ファイルから選択...")), false, LoadExistingProfile);

            menu.ShowAsContext();
        }

        /// <summary>
        /// 既存のARKitFTProfileアセットをファイル選択ダイアログから読み込む。
        /// ドロップダウンメニュー(ShowExistingProfileMenu)の「ファイルから選択...」から、
        /// または通常の検索範囲外にあるアセットを明示的に指定したい場合に使う。
        /// </summary>
        /// <summary>
        /// Profileを読み込む(=現在のアバターへ全設定を適用する)ことなく、識別タグだけを
        /// 直接編集・保存できる小さなポップアップウィンドウ。「複数のProfileにタグだけ
        /// 付け直したい」というような作業のときに、意図せず他アバター用の設定一式を
        /// 現在の作業内容へ上書きしてしまう事故を避けるために用意している。
        /// </summary>
        private class ProfileTagEditPopup : EditorWindow
        {
            private ARKitFTProfile _target;
            private string _tag;

            public static void Open(ARKitFTProfile target)
            {
                var win = CreateInstance<ProfileTagEditPopup>();
                win.titleContent = new GUIContent(ArkitFTLoc.T("識別タグを編集"));
                win._target = target;
                win._tag = target.avatarMatchTag ?? "";
                win.minSize = new Vector2(380, 100);
                win.maxSize = new Vector2(380, 100);
                win.ShowUtility();
            }

            private void OnGUI()
            {
                if (_target == null) { Close(); return; }

                EditorGUILayout.LabelField(_target.name, EditorStyles.boldLabel);
                EditorGUILayout.Space(4);
                _tag = EditorGUILayout.TextField(ArkitFTLoc.T("識別タグ (任意)"), _tag);
                EditorGUILayout.Space(8);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(ArkitFTLoc.T("保存"), GUILayout.Width(90)))
                    {
                        Undo.RecordObject(_target, "Edit Avatar Match Tag");
                        _target.avatarMatchTag = _tag;
                        EditorUtility.SetDirty(_target);
                        AssetDatabase.SaveAssets();
                        Close();
                    }
                    if (GUILayout.Button(ArkitFTLoc.T("キャンセル"), GUILayout.Width(90)))
                        Close();
                }
            }
        }

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
                "上書きされます。識別タグの編集だけが目的の場合は「適用しない」を選び、\n" +
                "「既存Profile」メニューの「識別タグを編集...」をご利用ください。"),
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

            // 識別タグは、前に読み込んでいた別Profileの値を誤って引き継がないよう、
            // 現在選択中のアバター名から新たに設定し直す(なければ空のまま)。
            _avatarMatchTag = _avatarPrefab != null ? _avatarPrefab.name : "";

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
            TryAutoSelectProfileForAvatar();

            if (_profile != null &&
                (_profile != _lastAppliedProfile || _avatarPrefab != _lastAppliedProfileAvatar))
            {
                ApplyProfileSelections();
                _lastAppliedProfile = _profile;
                _lastAppliedProfileAvatar = _avatarPrefab;
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
        ///      タグさえ登録しておけば確実にマッチする)
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
            var tier0 = allProfiles
                .Select(e => (e.path, e.name, profile: AssetDatabase.LoadAssetAtPath<ARKitFTProfile>(e.path)))
                .Where(e => e.profile != null && !string.IsNullOrWhiteSpace(e.profile.avatarMatchTag))
                .Select(e => (e.path, e.name,
                    bestTag: e.profile.avatarMatchTag
                        .Split(',')
                        .Select(t => t.Trim())
                        .Where(t => t.Length > 0 && avatarName.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0)
                        // 一致したタグの中で最も長い(=より具体的な)ものを代表値にする
                        .OrderByDescending(t => t.Length)
                        .FirstOrDefault()))
                .Where(e => e.bestTag != null)
                // Profile間では、それぞれの最良一致タグが長い(=より具体的)ものを優先する
                .OrderByDescending(e => e.bestTag.Length)
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
                    // enum値0=NoChangeという前提(他機能で確認済みの並び)
                    if (val != null && Convert.ToInt32(val) != 0) return true;
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
        /// 抽出したトリガーグループ(OR of ANDs)をもとに、指定したシェイプキー群を
        /// 「いずれかのグループが成立している間だけ0に固定する」ガードレイヤーをfxへ追加する。
        /// トリガーが1つも無い場合(該当するTrackingControl設定を使っていないアバター)は
        /// 何もしない。
        ///
        /// 【背景】VRCAnimatorTrackingControlはState入場時に一度だけ発火しその後は値が
        /// 残り続ける仕様であり、かつVRC標準のMouth/Eyes=Animation設定はNK Installer自身が
        /// 駆動する逆Viseme・EyeLookシェイプキーには影響しない(これらは独自FXレイヤーであり
        /// TrackingControlの管轄外のため)。そこで、ジェスチャー側がAnimationへ切り替える
        /// 条件そのものをこちら側にも複製し、同じ条件下ではこちらのシェイプキーを0に固定する
        /// ことで、両者が同時に効いてしまう表情の破綻を防ぐ。
        /// </summary>
        private static void AddTrackingAnimationGuardLayer(
            AnimatorController fx,
            List<List<AnimatorCondition>> triggerGroups,
            string smrPath,
            List<string> shapesToZero,
            string layerName)
        {
            if (fx == null || triggerGroups == null || triggerGroups.Count == 0) return;
            if (shapesToZero == null || shapesToZero.Count == 0) return;

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
            foreach (var shapeName in shapesToZero)
                SetCurve(zeroClip, smrPath, shapeName, 0f);

            var inactiveState = sm.AddState(layerName + "_Inactive", new Vector3(200f, 80f,  0f));
            var activeState   = sm.AddState(layerName + "_Active",   new Vector3(200f, 200f, 0f));
            inactiveState.motion = emptyClip;
            activeState.motion   = zeroClip;
            sm.defaultState       = inactiveState;

            // 各トリガーグループ → Active(AND条件そのまま)
            foreach (var group in triggerGroups)
            {
                var t = sm.AddAnyStateTransition(activeState);
                t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false;
                foreach (var c in group)
                    t.AddCondition(c.mode, c.threshold, c.parameter);
            }

            // 全トリガーグループの否定をANDした遷移 → Inactive
            // (ド・モルガンの法則: NOT(A or B or ...) = NOT A and NOT B and ...)
            var allConditions = triggerGroups.SelectMany(g => g).ToList();
            if (allConditions.Count > 0)
            {
                var tOff = sm.AddAnyStateTransition(inactiveState);
                tOff.hasExitTime = false; tOff.duration = 0f; tOff.canTransitionToSelf = false;
                foreach (var c in allConditions)
                    tOff.AddCondition(NegateConditionMode(c.mode), c.threshold, c.parameter);
            }

            fx.AddLayer(new AnimatorControllerLayer
            {
                name          = layerName,
                stateMachine  = sm,
                defaultWeight = 1f,
                blendingMode  = AnimatorLayerBlendingMode.Override,
            });

            EditorUtility.SetDirty(fx);

            Debug.Log($"[hinzka ARKit FT] '{layerName}'ガードレイヤーを追加しました " +
                      $"(トリガー{triggerGroups.Count}件 / 対象シェイプ{shapesToZero.Count}個)。");
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
                if (nameLookup.ContainsKey(ResolveArkitShapeName(name).Trim())) continue;

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
        /// 標準ARKitシェイプ名に、指定された接頭辞(独自命名のアバター対応)を付けて返す。
        /// 接頭辞が空ならそのまま返す。
        /// </summary>
        private string ResolveArkitShapeName(string standardName)
        {
            return string.IsNullOrEmpty(_arkitShapePrefix) ? standardName : _arkitShapePrefix + standardName;
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

            // --- 段階1: 存在チェック(Read/Write不要) ---
            // 存在しないシェイプは、中身が空である以上に確実に何の効果もないため、
            // あえてINSTALLを続行した場合は対応する同期パラメータもオフにしてよい。
            var existingShapes = new List<KeyValuePair<string, int>>(); // (標準名, blendShapeIndex)
            foreach (var standardName in ARKIT_SHAPE_NAMES)
            {
                string resolvedName = string.IsNullOrEmpty(arkitPrefix) ? standardName : arkitPrefix + standardName;
                // 前後の空白を無視して探し、実際の(空白込みの)名前でインデックスを引く
                // (Blender等で作成されたシェイプキーに紛れ込みがちなため)。
                int idx = nameLookup.TryGetValue(resolvedName.Trim(), out var actualName)
                    ? mesh.GetBlendShapeIndex(actualName)
                    : -1;
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
            if (_smrIndex >= _smrs.Length) return;

            var smr = _smrs[_smrIndex];
            if (smr == null || smr.sharedMesh == null) return;

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
        }

        // ── インストール本体 ──────────────────────────────

        private void Install()
        {
            if (_avatarPrefab == null) return;
            _lastEyeLookEmptyDeltaShapes = new List<string>();

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
                if (!string.IsNullOrEmpty(_arkitShapePrefix))
                    RewriteArkitBlendShapeNames(fx, _arkitShapePrefix);

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
                    ApplyGestureSuppressionDirectly(workingDesc, distinctGestureLayers, realSmrPath, installOutputFolder, _gestureSuppressOnEyesOrMouth, fx);
                else
                    Debug.Log("[hinzka ARKit FT] ジェスチャーレイヤーは未指定です。ジェスチャー抑制を生成しません。");

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
                    GenerateEyeLookShapesIfNeeded(
                        workingDesc, eyeLookTargetSmr, installOutputFolder,
                        workingLeftEyeConstraintTarget, workingRightEyeConstraintTarget, _eyeLookIntensity);

                    // 【検証の結果、目線には不採用】Mouthの逆Visemeと同様に、ジェスチャーが
                    // Eyes=Animationへ切り替える条件下でEyeLookシェイプキーを0に固定する
                    // ガードレイヤーも試したが、目線トラッキングは常時継続的に動いているため、
                    // ガードでリセットするたびに目が振動して見える不具合が生じ、不採用とした。
                    // 目線については、VRChat標準のEye Look自体を無効化する(下記の
                    // _disableNativeEyeLook)方が確実な対策になる。
                }

                // ネイティブアイルック無効化(EyeLookベイクで角度情報を使い終えた後に行う)。
                // ジェスチャー変化等をきっかけにVRChat標準の自動目線制御へフォールバックしてしまう
                // 問題を、フォールバック先自体を無くすことで確実に防ぐ。
                if (_disableNativeEyeLook)
                {
                    workingDesc.enableEyeLook = false;
                    EditorUtility.SetDirty(workingDesc);
                    Debug.Log("[hinzka ARKit FT] AvatarDescriptorのEye Lookを無効化しました。");
                }

                // 眉アシスト
                if (_generateBrowAssistShapes)
                {
                    GenerateBrowAssistShapesIfNeeded(workingFaceSmr, installOutputFolder, _arkitShapePrefix, _ueFallbackEnabled);
                    InjectBrowAssistBinding(fx, realSmrPath, _browAssistIntensity);
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
        /// 一致するものだけ、"prefix + 元の名前" へ書き換える。アバターのARKitシェイプキーに
        /// 独自の接頭辞が付いている場合、Driver自体が実在するシェイプキー名へ値を書き込むように
        /// するために必要(接頭辞なしでは、存在しない標準名へ値を書き込み続けてしまい、
        /// トラッキングが根本的に効かない)。
        /// </summary>
        private static void RewriteArkitBlendShapeNames(AnimatorController fx, string prefix)
        {
            if (fx == null || string.IsNullOrEmpty(prefix)) return;

            var arkitSet = new HashSet<string>(ARKIT_SHAPE_NAMES);
            const string bsPrefix = "blendShape.";

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
                    if (shapeName.StartsWith(prefix)) continue;  // 既に接頭辞付きなら何もしない(念のため)

                    var curve = AnimationUtility.GetEditorCurve(clip, b);
                    AnimationUtility.SetEditorCurve(clip, b, null);
                    var nb = b;
                    nb.propertyName = bsPrefix + prefix + shapeName; // EditorCurveBindingはstruct
                    AnimationUtility.SetEditorCurve(clip, nb, curve);
                    dirty = true;
                    rewrittenCount++;
                }
                if (dirty) EditorUtility.SetDirty(clip);
            }

            if (rewrittenCount > 0)
                Debug.Log($"[hinzka ARKit FT] FX内の標準ARKitシェイプキーカーブ{rewrittenCount}個に接頭辞 '{prefix}' を付与しました。");
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

            const string bsPrefix = "blendShape.";
            var clips = new HashSet<AnimationClip>();
            foreach (var layer in fx.layers)
                CollectClips(layer.stateMachine, clips);

            int resolvedShapeCount = 0;
            int rewrittenCurveCount = 0;

            foreach (var arkitName in ARKIT_SHAPE_NAMES)
            {
                string resolvedName = string.IsNullOrEmpty(arkitPrefix) ? arkitName : arkitPrefix + arkitName;
                if (nameLookup.ContainsKey(resolvedName.Trim())) continue; // 標準名で見つかっているので対象外

                if (!ARKIT_TO_UE_FALLBACK.TryGetValue(arkitName, out var candidateGroups)) continue;
                var chosenGroup = candidateGroups.FirstOrDefault(g => g.All(n => nameLookup.ContainsKey(n.Trim())));
                if (chosenGroup == null) continue; // UE代替名も見つからない(引き続き不足のまま)

                // 実際にメッシュ上にある名前(前後の空白を含む可能性がある)に変換する。
                // ここを取り違えると、Unityが実在しない名前のプロパティとして扱い、
                // 見た目上は解決できたはずのシェイプが実際には全く動かなくなってしまう。
                var actualTargetNames = chosenGroup.Select(n => nameLookup[n.Trim()]).ToArray();

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
        private static Dictionary<string, string> BuildTrimmedShapeNameLookup(Mesh mesh)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (mesh == null) return map;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                var raw = mesh.GetBlendShapeName(i);
                var trimmed = raw.Trim();
                if (!map.ContainsKey(trimmed)) map[trimmed] = raw; // 先に見つかった方を採用
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
            float intensity = 1f)
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

            var added = EyeLookBoneToBlendShapeBaker.GenerateMissingShapesAdditive(
                desc, faceSmr, newMesh, frameWeight, EYELOOK_BONE_PREFIX,
                leftConstraintTarget, rightConstraintTarget, out var emptyDeltaNames);
            _lastEyeLookEmptyDeltaShapes = emptyDeltaNames;

            if (added.Count == 0)
            {
                Debug.Log("[hinzka ARKit FT] EyeLook: 追加対象のシェイプキーがありませんでした。");
                UnityEngine.Object.DestroyImmediate(newMesh);
                return;
            }

            var meshPath = AssetDatabase.GenerateUniqueAssetPath(outputFolder + "/" + newMesh.name + ".asset");
            AssetDatabase.CreateAsset(newMesh, meshPath);
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

            // (標準ARKitシェイプ名, 複製先のsub_名) ※srcは実際の検索時にarkitPrefixを前置する
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

                string resolvedSrc = string.IsNullOrEmpty(arkitPrefix) ? p.src : arkitPrefix + p.src;
                int directIdx = srcNameLookup.TryGetValue(resolvedSrc.Trim(), out var directActualName)
                    ? srcMesh.GetBlendShapeIndex(directActualName)
                    : -1;

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
                        Debug.Log($"[hinzka ARKit FT] 眉アシスト: 複製元 '{resolvedSrc}' の代わりに" +
                                  $"UE代替名 [{string.Join(", ", chosenGroup)}] を合算して使用します。");
                    }
                }

                if (sourceIndices.Count == 0)
                {
                    Debug.LogWarning($"[hinzka ARKit FT] 複製元のシェイプキー '{resolvedSrc}' が見つからないため '{p.dst}' の生成をスキップしました。");
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
            bool suppressOnEyesOrMouth,
            AnimatorController ftFx)
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
            var customShapes = ScanCustomShapeNamesFromLayers(srcController, gestureLayerIndices, smrPath, ftFx);
            AddGestureSuppressionLayer(dstController, smrPath, gestureLayerIndices, customShapes, suppressOnEyesOrMouth);
            if (customShapes.Count > 0)
            {
                Debug.Log($"[hinzka ARKit FT] ジェスチャーレイヤーから検出したカスタムシェイプキー" +
                          $"{customShapes.Count}個を、FT有効中は0に固定します: " +
                          $"{string.Join(", ", customShapes)}");
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
        /// ジェスチャーレイヤーの抑制を1つのレイヤーにまとめて行う。
        /// FT_Active/FT_Stoppedの各Stateに「対象レイヤーのweight制御(VRC_AnimatorLayerControl)」と
        /// 「検出したカスタムシェイプキーのリセット(Motionのクリップ)」を両方持たせる。
        /// 両者は同じ条件(FT_MenuEnableMouth、またはEyes OR Mouth)で切り替わるため、
        /// 別々のレイヤーに分ける理由がない。
        /// </summary>
        private static void AddGestureSuppressionLayer(
            AnimatorController fx,
            string smrPath,
            List<int> gestureLayerIndices,
            List<string> shapeNames,
            bool suppressOnEyesOrMouth)
        {
            const string LAYER_NAME  = "hinzkaFT_GestureSuppression";
            const string PARAM_EYES  = "FT_MenuEnableEyes";
            const string PARAM_MOUTH = "FT_MenuEnableMouth";

            bool hasLayers = gestureLayerIndices != null && gestureLayerIndices.Count > 0;
            bool hasShapes = shapeNames != null && shapeNames.Count > 0;
            if (!hasLayers && !hasShapes) return;

            // 既存なら削除して再生成
            var layers = fx.layers.ToList();
            var existing = layers.FindIndex(l => l.name == LAYER_NAME);
            if (existing >= 0)
            {
                fx.RemoveLayer(existing);
            }

            // パラメータを確保
            foreach (var pName in new[] { PARAM_EYES, PARAM_MOUTH })
            {
                if (!fx.parameters.Any(p => p.name == pName))
                    fx.AddParameter(new AnimatorControllerParameter
                    { name = pName, type = AnimatorControllerParameterType.Bool, defaultBool = true });
            }

            var sm = new AnimatorStateMachine { name = LAYER_NAME + "_SM" };
            AssetDatabase.AddObjectToAsset(sm, fx);
            HideGeneratedSubAsset(sm);

            var emptyClip = new AnimationClip { name = LAYER_NAME + "_Empty" };
            AssetDatabase.AddObjectToAsset(emptyClip, fx);
            HideGeneratedSubAsset(emptyClip);

            AnimationClip resetClip = emptyClip;
            if (hasShapes)
            {
                resetClip = new AnimationClip { name = LAYER_NAME + "_Reset" };
                AssetDatabase.AddObjectToAsset(resetClip, fx);
                HideGeneratedSubAsset(resetClip);
                foreach (var shapeName in shapeNames)
                    SetCurve(resetClip, smrPath, shapeName, 0f);
            }

            var activeState = sm.AddState("FT_Active",  new Vector3(200f, 80f,  0f));
            var stopState   = sm.AddState("FT_Stopped", new Vector3(200f, 200f, 0f));
            activeState.motion = resetClip;
            stopState.motion   = emptyClip;
            sm.defaultState     = stopState;

            // レイヤーweight制御: FT_Active → 対象レイヤーweight=0、FT_Stopped → weight=1
            // (1つのStateに複数のBehaviourを積めるので、レイヤー数だけ繰り返しAttachする)
            if (hasLayers)
            {
                foreach (var gestureLayerIndex in gestureLayerIndices.Distinct())
                {
                    AttachLayerControl(activeState, fx, gestureLayerIndex, 0f);
                    AttachLayerControl(stopState,   fx, gestureLayerIndex, 1f);
                }

                // 抑制対象レイヤーがVRCAnimatorTrackingControlでMouthをAnimation/Tracking間で
                // 切り替えている場合、レイヤーweightを0にしただけではその切り替え自体は
                // 止まらない(Behaviourはweightと無関係に発火する)。FT有効中はこの抑制レイヤー
                // (アバター本来のFXの末尾に追加される)がMouth=Trackingで強制的に上書きすることで、
                // ジェスチャー側の切り替えを無効化し、Visemeを常に正しく動作させる。
                //
                // 【重要】VRCAnimatorTrackingControlは「毎フレーム再評価」ではなく「ステートに
                // 入った瞬間に一度だけ発火し、その後は値が残り続ける」仕様であることを実機検証で
                // 確認した。そのため、FT_Active側にだけ上書きを付けてFT_Stopped側を素通り
                // (何もBehaviourを付けない)にすると、FT終了後もMouth=Trackingのまま固定されて
                // しまい、「主張を降ろした」ことにならない。FT_Stopped側にも明示的にMouth=Tracking
                // を書き込むことで、少なくとも常に一貫した状態(FT中もFT外もTracking)を維持する。
                // なお、ジェスチャーを押しっぱなしのままFTだけをオフにした場合、そのジェスチャーが
                // 本来持つMouth=Animationは、ジェスチャーを一度離して再度発火させるまでは反映
                // されない(元のジェスチャーレイヤー自身のステート再入場でしか復元できないため)。
                AttachMouthTrackingOverride(activeState);
                AttachMouthTrackingOverride(stopState);
            }

            if (suppressOnEyesOrMouth)
            {
                // Eyes または Mouth のどちらか一方でも有効なら抑制(ORはTransition2本で表現)
                foreach (var activeParam in new[] { PARAM_EYES, PARAM_MOUTH })
                {
                    var t = sm.AddAnyStateTransition(activeState);
                    t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false;
                    t.AddCondition(AnimatorConditionMode.If, 0f, activeParam);
                }

                var tOff = sm.AddAnyStateTransition(stopState);
                tOff.hasExitTime = false; tOff.duration = 0f; tOff.canTransitionToSelf = false;
                tOff.AddCondition(AnimatorConditionMode.IfNot, 0f, PARAM_EYES);
                tOff.AddCondition(AnimatorConditionMode.IfNot, 0f, PARAM_MOUTH);
            }
            else
            {
                // Mouthが有効なときだけ抑制(Eyesのみ有効ではジェスチャーを残す)
                var t = sm.AddAnyStateTransition(activeState);
                t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false;
                t.AddCondition(AnimatorConditionMode.If, 0f, PARAM_MOUTH);

                var tOff = sm.AddAnyStateTransition(stopState);
                tOff.hasExitTime = false; tOff.duration = 0f; tOff.canTransitionToSelf = false;
                tOff.AddCondition(AnimatorConditionMode.IfNot, 0f, PARAM_MOUTH);
            }

            fx.AddLayer(new AnimatorControllerLayer
            {
                name          = LAYER_NAME,
                stateMachine  = sm,
                defaultWeight = 1f,
                blendingMode  = AnimatorLayerBlendingMode.Override,
            });

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
        private static void AttachMouthTrackingOverride(AnimatorState state)
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
                Debug.LogWarning("[hinzka ARKit FT] VRCAnimatorTrackingControl not found. Mouth tracking override skipped.");
                return;
            }

            var mouthField = trackingControlType.GetField("trackingMouth");
            if (mouthField == null)
            {
                Debug.LogWarning("[hinzka ARKit FT] trackingMouth field not found. Mouth tracking override skipped.");
                return;
            }

            object trackingValue;
            try
            {
                trackingValue = Enum.Parse(mouthField.FieldType, "Tracking");
            }
            catch
            {
                Debug.LogWarning("[hinzka ARKit FT] TrackingType.Tracking の解決に失敗しました。Mouth tracking override をスキップします。");
                return;
            }

            var tc = state.AddStateMachineBehaviour(trackingControlType);
            mouthField.SetValue(tc, trackingValue);
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
