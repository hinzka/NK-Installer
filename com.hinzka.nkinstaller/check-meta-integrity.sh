#!/usr/bin/env bash
#
# .metaファイル整合性チェックスクリプト
#
# 使い方（git-repoのルート = package.jsonがある場所で実行）:
#   bash check-meta-integrity.sh [比較対象のgitタグ]
#
# 例:
#   bash check-meta-integrity.sh v1.0.8
#
# チェック内容:
#   1) .metaファイルが存在しないアセット/フォルダが無いか
#   2) 前回リリース(タグ)と比べて .cs.meta の guid が変わっていないか
#      → guidが変わると、既存のProfile(.asset)がMissing Scriptになる

set -uo pipefail

PACKAGE_ROOT="${PACKAGE_ROOT:-.}"
COMPARE_REF="${1:-}"
cd "$PACKAGE_ROOT" || { echo "指定フォルダに移動できません: $PACKAGE_ROOT"; exit 1; }

if [ -z "$COMPARE_REF" ]; then
  COMPARE_REF=$(git describe --tags --abbrev=0 2>/dev/null || true)
fi

echo "=== 1. .metaファイルが無いアセットのチェック ==="

# このスクリプト自身や、Unityのアセットとして扱われない/扱う必要のないファイルを除外
EXCLUDE_NAMES=("./check-meta-integrity.sh")

mapfile -t missing_files < <(
  find . -type f ! -name "*.meta" ! -path "*/.git/*" ! -path "*~*" \
  | while IFS= read -r f; do
      skip=0
      for ex in "${EXCLUDE_NAMES[@]}"; do
        [ "$f" = "$ex" ] && skip=1 && break
      done
      [ "$skip" -eq 1 ] && continue
      [ -f "${f}.meta" ] || echo "$f"
    done
)
mapfile -t missing_dirs < <(
  find . -type d ! -path "*/.git*" ! -path "*~*" ! -name "." \
  | while IFS= read -r d; do [ -f "${d}.meta" ] || echo "$d"; done
)

for f in "${missing_files[@]:-}"; do
  [ -n "$f" ] && echo "  [MISSING] $f"
done
for d in "${missing_dirs[@]:-}"; do
  [ -n "$d" ] && echo "  [MISSING] $d/ (フォルダ)"
done

meta_count=$(( ${#missing_files[@]} + ${#missing_dirs[@]} ))
[ "$meta_count" -eq 0 ] && echo "  問題なし"

echo ""
echo "=== 2. C#スクリプトのGUID変化チェック（比較対象: ${COMPARE_REF:-なし}） ==="

guid_changed=0
if [ -z "$COMPARE_REF" ]; then
  echo "  gitタグが見つからないためスキップしました。"
  echo "  引数で前回リリースのタグ名を指定してください（例: v1.0.8）"
else
  while IFS= read -r m; do
    rel="${m#./}"
    old_guid=$(git show "${COMPARE_REF}:${rel}" 2>/dev/null | grep -m1 '^guid:' || true)
    new_guid=$(grep -m1 '^guid:' "$m" || true)
    if [ -n "$old_guid" ] && [ "$old_guid" != "$new_guid" ]; then
      echo "  [GUID CHANGED] $rel"
      echo "      前回: $old_guid"
      echo "      今回: $new_guid"
      guid_changed=1
    fi
  done < <(find . -name "*.cs.meta" ! -path "*/.git/*")

  [ "$guid_changed" -eq 0 ] && echo "  問題なし"
fi

echo ""
if [ "$meta_count" -eq 0 ] && [ "$guid_changed" -eq 0 ]; then
  echo "全てのチェックをパスしました。"
  exit 0
else
  echo "上記の項目を確認してください。"
  exit 1
fi
