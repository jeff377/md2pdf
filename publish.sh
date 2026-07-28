#!/usr/bin/env bash
#
# 發佈 md2pdf 單一執行檔。
#
#   ./publish.sh                              # 預設三個 RID
#   ./publish.sh osx-arm64                    # 只發佈指定 RID（可給多個）
#   ./publish.sh --self-contained             # 改發佈自含 .NET 執行階段的獨立版
#
# 產物落在 publish/<rid>/md2pdf（Windows 為 md2pdf.exe）。
#
set -euo pipefail

cd "$(dirname "$0")"

PROJECT="src/Md2Pdf/Md2Pdf.csproj"
OUTPUT_ROOT="publish"
DEFAULT_RIDS=(osx-arm64 win-x64 linux-x64)

self_contained=false
rids=()

for arg in "$@"; do
    case "$arg" in
        --framework-dependent)
            self_contained=false
            ;;
        --self-contained)
            self_contained=true
            ;;
        -h|--help)
            sed -n '2,10p' "$0" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        -*)
            echo "無法辨識的選項：$arg" >&2
            exit 2
            ;;
        *)
            rids+=("$arg")
            ;;
    esac
done

if [ ${#rids[@]} -eq 0 ]; then
    rids=("${DEFAULT_RIDS[@]}")
fi

if [ "$self_contained" = true ]; then
    # 自含執行階段並裁剪：目標機不需安裝 .NET，代價是產物約 12 MB。
    mode_args=(--self-contained true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true)
    mode_label="self-contained（不需 .NET 執行階段，約 12 MB）"
else
    # 預設不含 .NET 組件：產物僅數百 KB，但目標機須裝有 .NET 10 執行階段。
    mode_args=(--self-contained false)
    mode_label="framework-dependent（需 .NET 10 執行階段，約 650 KB）"
fi

echo "發佈模式：$mode_label"
echo "目標 RID：${rids[*]}"
echo

rm -rf "$OUTPUT_ROOT"

for rid in "${rids[@]}"; do
    echo "── 發佈 $rid ──"
    dotnet publish "$PROJECT" \
        --configuration Release \
        --runtime "$rid" \
        -p:PublishSingleFile=true \
        -p:DebugType=none \
        -p:GenerateDocumentationFile=false \
        "${mode_args[@]}" \
        --output "$OUTPUT_ROOT/$rid"
done

echo
echo "完成。產物："
find "$OUTPUT_ROOT" -type f \( -name 'md2pdf' -o -name 'md2pdf.exe' \) -exec ls -lh {} \;
