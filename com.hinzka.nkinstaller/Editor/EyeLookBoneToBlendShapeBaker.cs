#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

#if VRC_SDK_VRCSDK3
using VRC.SDK3.Avatars.Components;
#endif

namespace hinzka.FaceTracking.DevTools
{
    /// <summary>
    /// VRCAvatarDescriptor の Eye Look 設定(目ボーンの回転角度)を読み取り、
    /// 実際にボーンを回転させてベイクした差分を ARKit の eyeLook 系 8 シェイプキーとして
    /// SkinnedMeshRenderer へ一括追加する処理本体。
    /// GUIを持たない静的クラスとして切り出すことで、単体ツール(EyeLookBoneToBlendShapeWindow)からも
    /// ARKit Installer 等の他ツールからも同じロジックを呼び出せる。
    ///
    /// 符号規約(hinzka FaceTracking 全体で統一):
    ///   ・「向かって右」= AvatarDescriptor の eyesLookingRight
    ///   ・左目の In(鼻側)  = 向かって右 = eyesLookingRight.left
    ///   ・左目の Out(外側) = 向かって左 = eyesLookingLeft.left
    ///   ・右目の In(鼻側)  = 向かって左 = eyesLookingLeft.right
    ///   ・右目の Out(外側) = 向かって右 = eyesLookingRight.right
    ///   ・Up/Down は左右共通で eyesLookingUp / eyesLookingDown の対応する eye を使用
    /// </summary>
#if VRC_SDK_VRCSDK3
    public static class EyeLookBoneToBlendShapeBaker
    {
        public enum EyeSide { Left, Right }
        public enum LookDir { Up, Down, In, Out }

        /// <summary>
        /// モデラーが既に用意しているARKit標準シェイプキー(eyeLookUpLeft等)と衝突しないよう、
        /// ボーン由来のシェイプキーにはデフォルトでこの接頭辞を付ける。
        /// FaceTracking全体の命名規則(Visemeの FT_v_ 接頭辞)に合わせている。
        /// </summary>
        public const string DefaultBonePrefix = "FT_EyeBone_";

        public struct ShapeSpec
        {
            /// <summary>ARKit標準名(接頭辞なし)。例: "eyeLookUpLeft"</summary>
            public readonly string baseName;
            public readonly EyeSide side;
            public readonly LookDir dir;
            public ShapeSpec(string n, EyeSide s, LookDir d) { baseName = n; side = s; dir = d; }
        }

        public struct ShapeFrameData
        {
            public string name;
            public float weight;
            public Vector3[] dv;
            public Vector3[] dn;
            public Vector3[] dt;
        }

        public struct BakeResult
        {
            public bool success;
            public int generatedCount;
            public int skippedCount;
            public string message;
            public Mesh outputMesh;
        }

        /// <summary>8シェイプの定義。全部揃っているかの存在チェックにも使う。</summary>
        public static readonly ShapeSpec[] Specs = new[]
        {
            new ShapeSpec("eyeLookUpLeft",    EyeSide.Left,  LookDir.Up),
            new ShapeSpec("eyeLookDownLeft",  EyeSide.Left,  LookDir.Down),
            new ShapeSpec("eyeLookInLeft",    EyeSide.Left,  LookDir.In),
            new ShapeSpec("eyeLookOutLeft",   EyeSide.Left,  LookDir.Out),
            new ShapeSpec("eyeLookUpRight",   EyeSide.Right, LookDir.Up),
            new ShapeSpec("eyeLookDownRight", EyeSide.Right, LookDir.Down),
            new ShapeSpec("eyeLookInRight",   EyeSide.Right, LookDir.In),
            new ShapeSpec("eyeLookOutRight",  EyeSide.Right, LookDir.Out),
        };

        /// <summary>Eye Look に必要な前提(ボーン設定・SMR Read/Write)を満たしているか確認する。</summary>
        public static string Validate(VRCAvatarDescriptor avatarDescriptor, SkinnedMeshRenderer targetSmr)
        {
            if (avatarDescriptor == null) return "Avatar Descriptor が指定されていません。";
            if (targetSmr == null) return "対象の SkinnedMeshRenderer が指定されていません。";
            if (targetSmr.sharedMesh == null) return "対象SMRにMeshが割り当てられていません。";

            var s = avatarDescriptor.customEyeLookSettings;
            if (s.leftEye == null || s.rightEye == null)
                return "AvatarDescriptor に Left Eye / Right Eye ボーンが設定されていません。";

            if (!targetSmr.sharedMesh.isReadable)
                return "対象メッシュの Read/Write が無効です(Import Settings で有効にしてください)。";

            return null;
        }

        /// <summary>対象メッシュに8シェイプ(接頭辞付き)が全て既に存在するかどうか。</summary>
        public static bool AllShapesExist(Mesh mesh, string namePrefix = DefaultBonePrefix)
        {
            if (mesh == null) return false;
            foreach (var spec in Specs)
            {
                if (mesh.GetBlendShapeIndex(namePrefix + spec.baseName) < 0) return false;
            }
            return true;
        }

        /// <summary>いくつのシェイプ(接頭辞付き)が既に存在するか(部分適用の判断用)。</summary>
        public static int CountExistingShapes(Mesh mesh, string namePrefix = DefaultBonePrefix)
        {
            if (mesh == null) return 0;
            int count = 0;
            foreach (var spec in Specs)
                if (mesh.GetBlendShapeIndex(namePrefix + spec.baseName) >= 0) count++;
            return count;
        }

        /// <summary>
        /// eyeLook系8シェイプキーを生成する。
        /// </summary>
        /// <param name="workingMesh">
        /// 既に複製・準備済みの書き込み対象メッシュ。nullの場合は本メソッド内で複製して
        /// targetSmr.sharedMesh を差し替える(単体使用向け)。
        /// Installer等、複数ステップで同一メッシュを使い回したい場合は呼び出し側で複製したものを渡すこと。
        /// </param>
        /// <param name="overwriteExistingShapes">
        /// true: 同名シェイプが既にあれば上書き再生成する。
        /// false: 同名シェイプが既にあればスキップし、未生成分のみ追加する(再インストール時の冪等性を保ちたい場合はfalse推奨)。
        /// </param>
        public static BakeResult GenerateAll(
            VRCAvatarDescriptor avatarDescriptor,
            SkinnedMeshRenderer targetSmr,
            Mesh workingMesh = null,
            string outputFolderIfCreating = "Assets/_Generated/BakedMeshes",
            float frameWeight = 100f,
            bool overwriteExistingShapes = true,
            string namePrefix = DefaultBonePrefix)
        {
            var err = Validate(avatarDescriptor, targetSmr);
            if (err != null)
                return new BakeResult { success = false, message = err };

            var eyeSettings = avatarDescriptor.customEyeLookSettings;
            var leftEye = eyeSettings.leftEye;
            var rightEye = eyeSettings.rightEye;

            var leftRest = leftEye.localRotation;
            var rightRest = rightEye.localRotation;

            Mesh baseMesh = null;
            var posedMeshesToCleanup = new List<Mesh>();
            bool createdMeshHere = false;

            try
            {
                // ---- 基準(レスト)ベイク ----
                leftEye.localRotation = leftRest;
                rightEye.localRotation = rightRest;
                baseMesh = new Mesh();
                targetSmr.BakeMesh(baseMesh, true);
                var baseVerts = baseMesh.vertices;
                var baseNormals = baseMesh.normals;

                // ---- 書き込み対象メッシュの準備 ----
                var editableMesh = workingMesh;
                if (editableMesh == null)
                {
                    editableMesh = PrepareEditableMesh(targetSmr, outputFolderIfCreating);
                    createdMeshHere = true;
                }

                // ---- 既存シェイプキーを全て退避 ----
                var savedShapes = ExtractAllBlendShapes(editableMesh);

                var newShapes = new List<ShapeFrameData>();
                int skipped = 0;

                foreach (var spec in Specs)
                {
                    string outName = namePrefix + spec.baseName;
                    bool alreadyExists = savedShapes.Exists(s => s.name == outName);
                    if (alreadyExists && !overwriteExistingShapes)
                    {
                        skipped++;
                        continue;
                    }

                    leftEye.localRotation = leftRest;
                    rightEye.localRotation = rightRest;

                    var rot = GetTargetRotation(eyeSettings, spec);
                    if (spec.side == EyeSide.Left) leftEye.localRotation = rot;
                    else rightEye.localRotation = rot;

                    var posedMesh = new Mesh();
                    targetSmr.BakeMesh(posedMesh, true);
                    posedMeshesToCleanup.Add(posedMesh);

                    var posedVerts = posedMesh.vertices;
                    var posedNormals = posedMesh.normals;

                    if (posedVerts.Length != baseVerts.Length)
                    {
                        Debug.LogError($"[EyeLookBaker] 頂点数不一致のため '{outName}' をスキップしました。" +
                                        $"(base={baseVerts.Length}, posed={posedVerts.Length})");
                        continue;
                    }

                    var dv = new Vector3[posedVerts.Length];
                    var dn = new Vector3[posedVerts.Length];
                    var dt = new Vector3[posedVerts.Length];

                    for (int i = 0; i < posedVerts.Length; i++)
                    {
                        dv[i] = posedVerts[i] - baseVerts[i];
                        dn[i] = posedNormals[i] - baseNormals[i];
                    }

                    savedShapes.RemoveAll(s => s.name == outName);
                    newShapes.Add(new ShapeFrameData { name = outName, weight = frameWeight, dv = dv, dn = dn, dt = dt });
                }

                editableMesh.ClearBlendShapes();
                foreach (var s in savedShapes)
                    editableMesh.AddBlendShapeFrame(s.name, s.weight, s.dv, s.dn, s.dt);
                foreach (var s in newShapes)
                    editableMesh.AddBlendShapeFrame(s.name, s.weight, s.dv, s.dn, s.dt);

                EditorUtility.SetDirty(editableMesh);
                if (createdMeshHere) AssetDatabase.SaveAssets();

                return new BakeResult
                {
                    success = true,
                    generatedCount = newShapes.Count,
                    skippedCount = skipped,
                    message = $"{newShapes.Count}個生成 / {skipped}個スキップ",
                    outputMesh = editableMesh,
                };
            }
            finally
            {
                leftEye.localRotation = leftRest;
                rightEye.localRotation = rightRest;

                if (baseMesh != null) Object.DestroyImmediate(baseMesh);
                foreach (var m in posedMeshesToCleanup) Object.DestroyImmediate(m);
            }
        }

        /// <summary>
        /// Installer等、他ツールから呼び出す追加専用モード。
        /// ・既存シェイプキーは一切上書きしない(Clear/Rebuildを行わない。Unityの制約上、
        ///   同名シェイプへの再AddBlendShapeFrameは別フレーム追加になり壊れるため)
        /// ・workingMesh は呼び出し側で複製済みの書き込み可能なメッシュを渡すこと
        /// ・アセットの保存・SkinnedMeshRendererへの再割当ては呼び出し側の責任(GenerateInverseVisemeShapesと同じ分担)
        /// ・戻り値: 実際に追加したシェイプキー名のリスト(空なら追加対象なし)
        /// </summary>
        public static List<string> GenerateMissingShapesAdditive(
            VRCAvatarDescriptor avatarDescriptor,
            SkinnedMeshRenderer targetSmr,
            Mesh workingMesh,
            float frameWeight = 100f,
            string namePrefix = DefaultBonePrefix,
            Transform leftConstraintTarget = null,
            Transform rightConstraintTarget = null)
        {
            return GenerateMissingShapesAdditive(
                avatarDescriptor, targetSmr, workingMesh, frameWeight, namePrefix,
                leftConstraintTarget, rightConstraintTarget, out _);
        }

        /// <summary>
        /// leftConstraintTarget / rightConstraintTarget: 目ボーン(leftEye/rightEye)がコンストレイントの
        /// ソースになっていて、実際にスキンウェイトが乗っているのは別のTransform(コンストレイントの
        /// ターゲット側)である場合に指定する。Unity EditモードではVRC Constraint等は自動解決されないため、
        /// 指定があれば「目ボーンのワールド回転を、このTransformへそのままコピーしてからベイクする」
        /// ことでコンストレイント解決をバイパスする。
        /// 前提: コンストレイントがWeight=1・Rest Offsetほぼ0・ワールド空間で単純にソースの回転を
        /// コピーしているだけの単純なケースのみ正しく再現できる(オフセットや複数ソース合成等がある
        /// 場合は正確に再現できない)。
        /// emptyDeltaNames: 生成はしたが頂点差分がほぼゼロ(=ボーンを回転させてもFace SMR側が
        /// 追従して変形しなかった)シェイプ名の一覧。Eye SMRとFace SMRが別メッシュな場合などに
        /// 発生する。生成自体は成功として扱うが、呼び出し側で目立つ警告を出すために使う。
        /// </summary>
        public static List<string> GenerateMissingShapesAdditive(
            VRCAvatarDescriptor avatarDescriptor,
            SkinnedMeshRenderer targetSmr,
            Mesh workingMesh,
            float frameWeight,
            string namePrefix,
            Transform leftConstraintTarget,
            Transform rightConstraintTarget,
            out List<string> emptyDeltaNames)
        {
            var added = new List<string>();
            emptyDeltaNames = new List<string>();

            var err = Validate(avatarDescriptor, targetSmr);
            if (err != null)
            {
                Debug.LogWarning($"[EyeLookBaker] {err}");
                return added;
            }
            if (workingMesh == null)
            {
                Debug.LogWarning("[EyeLookBaker] workingMesh が null です。");
                return added;
            }

            var eyeSettings = avatarDescriptor.customEyeLookSettings;
            var leftEye = eyeSettings.leftEye;
            var rightEye = eyeSettings.rightEye;
            var leftRest = leftEye.localRotation;
            var rightRest = rightEye.localRotation;

            // コンストレイントのターゲット側も、元の姿勢に必ず復元できるよう保存しておく
            var leftTargetRest = leftConstraintTarget != null ? leftConstraintTarget.rotation : (Quaternion?)null;
            var rightTargetRest = rightConstraintTarget != null ? rightConstraintTarget.rotation : (Quaternion?)null;

            Mesh baseMesh = null;
            var posedCleanup = new List<Mesh>();

            // 指定されたポーズを反映する(ボーン自身 + コンストレイントターゲットへのワールド回転コピー)
            void ApplyPose(Quaternion leftLocalRot, Quaternion rightLocalRot)
            {
                leftEye.localRotation = leftLocalRot;
                rightEye.localRotation = rightLocalRot;
                if (leftConstraintTarget != null) leftConstraintTarget.rotation = leftEye.rotation;
                if (rightConstraintTarget != null) rightConstraintTarget.rotation = rightEye.rotation;
            }

            try
            {
                ApplyPose(leftRest, rightRest);
                baseMesh = new Mesh();
                targetSmr.BakeMesh(baseMesh, true);
                var baseVerts = baseMesh.vertices;
                var baseNormals = baseMesh.normals;

                foreach (var spec in Specs)
                {
                    string outName = namePrefix + spec.baseName;
                    if (workingMesh.GetBlendShapeIndex(outName) >= 0)
                        continue; // 既存はスキップ(上書きしない)

                    var rot = GetTargetRotation(eyeSettings, spec);
                    ApplyPose(
                        spec.side == EyeSide.Left ? rot : leftRest,
                        spec.side == EyeSide.Right ? rot : rightRest);

                    var posed = new Mesh();
                    targetSmr.BakeMesh(posed, true);
                    posedCleanup.Add(posed);

                    var pv = posed.vertices;
                    var pn = posed.normals;
                    if (pv.Length != baseVerts.Length)
                    {
                        Debug.LogError($"[EyeLookBaker] 頂点数不一致のため '{outName}' をスキップしました。");
                        continue;
                    }

                    var dv = new Vector3[pv.Length];
                    var dn = new Vector3[pv.Length];
                    var dt = new Vector3[pv.Length];
                    bool anyDelta = false;
                    for (int i = 0; i < pv.Length; i++)
                    {
                        dv[i] = pv[i] - baseVerts[i];
                        dn[i] = pn[i] - baseNormals[i];
                        if (!anyDelta && dv[i].sqrMagnitude > 1e-12f) anyDelta = true;
                    }

                    if (!anyDelta)
                    {
                        emptyDeltaNames.Add(outName);
                        Debug.LogWarning(
                            $"[EyeLookBaker] ⚠ '{outName}' の頂点差分がほぼゼロでした。目ボーンを回転させても" +
                            "対象メッシュが追従して変形していない可能性があります" +
                            "(Face SMRと眼球メッシュが別々のSkinnedMeshRendererになっていないか確認してください)。");
                    }

                    workingMesh.AddBlendShapeFrame(outName, frameWeight, dv, dn, dt);
                    added.Add(outName);
                }

                return added;
            }
            finally
            {
                leftEye.localRotation = leftRest;
                rightEye.localRotation = rightRest;
                if (leftConstraintTarget != null && leftTargetRest.HasValue)
                    leftConstraintTarget.rotation = leftTargetRest.Value;
                if (rightConstraintTarget != null && rightTargetRest.HasValue)
                    rightConstraintTarget.rotation = rightTargetRest.Value;
                if (baseMesh != null) Object.DestroyImmediate(baseMesh);
                foreach (var m in posedCleanup) Object.DestroyImmediate(m);
            }
        }

        private static Quaternion GetTargetRotation(VRCAvatarDescriptor.CustomEyeLookSettings s, ShapeSpec spec)
        {
            VRCAvatarDescriptor.CustomEyeLookSettings.EyeRotations rotSet;
            switch (spec.dir)
            {
                case LookDir.Up:
                    rotSet = s.eyesLookingUp;
                    break;
                case LookDir.Down:
                    rotSet = s.eyesLookingDown;
                    break;
                case LookDir.In:
                    rotSet = (spec.side == EyeSide.Left) ? s.eyesLookingRight : s.eyesLookingLeft;
                    break;
                case LookDir.Out:
                    rotSet = (spec.side == EyeSide.Left) ? s.eyesLookingLeft : s.eyesLookingRight;
                    break;
                default:
                    rotSet = s.eyesLookingUp;
                    break;
            }
            return spec.side == EyeSide.Left ? rotSet.left : rotSet.right;
        }

        /// <summary>単体使用向け: メッシュを複製して新規アセットとして保存し、SMRへ再割当てする。</summary>
        public static Mesh PrepareEditableMesh(SkinnedMeshRenderer smr, string folder)
        {
            var src = smr.sharedMesh;

            if (!AssetDatabase.IsValidFolder(folder))
            {
                var parts = folder.Split('/');
                string cur = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = cur + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(cur, parts[i]);
                    cur = next;
                }
            }

            var dup = Object.Instantiate(src);
            dup.name = src.name + "_EyeLookBaked";
            var path = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(folder, dup.name + ".asset").Replace("\\", "/"));
            AssetDatabase.CreateAsset(dup, path);

            smr.sharedMesh = dup;
            EditorUtility.SetDirty(smr);

            return dup;
        }

        public static List<ShapeFrameData> ExtractAllBlendShapes(Mesh mesh)
        {
            var list = new List<ShapeFrameData>();
            int shapeCount = mesh.blendShapeCount;
            int vCount = mesh.vertexCount;

            for (int si = 0; si < shapeCount; si++)
            {
                string name = mesh.GetBlendShapeName(si);
                int frameCount = mesh.GetBlendShapeFrameCount(si);
                for (int fi = 0; fi < frameCount; fi++)
                {
                    float w = mesh.GetBlendShapeFrameWeight(si, fi);
                    var dv = new Vector3[vCount];
                    var dn = new Vector3[vCount];
                    var dt = new Vector3[vCount];
                    mesh.GetBlendShapeFrameVertices(si, fi, dv, dn, dt);
                    list.Add(new ShapeFrameData { name = name, weight = w, dv = dv, dn = dn, dt = dt });
                }
            }
            return list;
        }
    }
#endif
}
#endif
