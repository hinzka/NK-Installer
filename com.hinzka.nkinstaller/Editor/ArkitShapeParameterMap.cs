#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace hinzka.FaceTracking.Editor
{
    /// <summary>
    /// ARKit標準シェイプ名 ⇔ OSCmoothの生パラメータ名(v2/*)の、1対1対応が確認できるものだけを
    /// 記録した対応表。OSCmoothConfigアセットから開発者側(FXジェネレータ)で生成し、
    /// テンプレートFXと一緒にAssets内に配置しておく。ARKit Installer側はこれを自動で読み込んで
    /// 参照するだけで、エンドユーザーがOSCmoothConfigを直接指定する必要はない。
    /// BrowOuterUp・EyeSquint・CheekPuffSuck等、左右/正負を1パラメータにまとめている
    /// (単純な1対1ではない)ものはこの対応表には含まれない。
    /// </summary>
    public class ArkitShapeParameterMap : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string arkitShapeName;
            public string oscmoothParamName;
        }

        public List<Entry> entries = new List<Entry>();
    }
}
#endif
