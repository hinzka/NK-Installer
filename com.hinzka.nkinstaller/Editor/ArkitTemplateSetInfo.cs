using UnityEngine;

namespace hinzka.FaceTracking
{
    /// <summary>
    /// テンプレートセット1つ(ARKit_FT_Template.controller・ARKit_FT_Parameters.asset・
    /// ARKit_FT_Menu.asset・ARKit_FT_ShapeParamMap.assetの組)に対して、UI上に表示する
    /// 表示名・概要テキストを付与するためのメタデータ。テンプレートセットの各フォルダに
    /// 1つ配置する(このアセットが置かれているフォルダ自体が、そのセットのルートとして
    /// 扱われる)。
    ///
    /// 例えば「軽量版」「アバター固有シェイプキー活用版」等、複数のテンプレートセットを
    /// 用意し、NK Installer側のUIから選んで使い分けられるようにするための仕組み。
    /// </summary>
    [CreateAssetMenu(fileName = "ARKit_FT_TemplateSetInfo", menuName = "hinzka/ARKit FT Template Set Info")]
    public class ArkitTemplateSetInfo : ScriptableObject
    {
        [Tooltip("NK InstallerのUI上でこのテンプレートセットを選ぶ際に表示される名前。\n" +
                 "空の場合は、このアセットが置かれているフォルダ名がそのまま使われる。")]
        public string displayName = "";

        [Tooltip("このテンプレートセットの用途・特徴を説明する概要テキスト。\n" +
                 "UI上でセットを選択した際、補足説明として表示される。")]
        [TextArea(2, 6)]
        public string description = "";
    }
}
