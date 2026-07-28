#!/usr/bin/env bash
#
# 發佈 md2pdf 單一執行檔。
#
#   ./publish.sh                              # 預設四個 RID
#   ./publish.sh osx-arm64                    # 只發佈指定 RID（可給多個）
#   ./publish.sh --self-contained             # 改發佈自含 .NET 執行階段的獨立版
#   ./publish.sh --version 0.2.0 --archive    # 帶入版號並打包成壓縮檔（發佈 Release 用）
#
# 產物落在 publish/<rid>/md2pdf（Windows 為 md2pdf.exe）；
# 加上 --archive 時另在 publish/ 下產生壓縮檔與 SHA256SUMS.txt。
#
set -euo pipefail

cd "$(dirname "$0")"

PROJECT="src/Md2Pdf/Md2Pdf.csproj"
OUTPUT_ROOT="publish"
DEFAULT_RIDS=(osx-arm64 osx-x64 win-x64 linux-x64)

self_contained=false
archive=false
version=""
rids=()

while [ $# -gt 0 ]; do
    case "$1" in
        --self-contained)
            self_contained=true
            ;;
        --framework-dependent)
            self_contained=false
            ;;
        --archive)
            archive=true
            ;;
        --version)
            [ $# -ge 2 ] || { echo "選項 --version 缺少值。" >&2; exit 2; }
            version="$2"
            shift
            ;;
        -h|--help)
            sed -n '2,12p' "$0" | sed 's/^# \{0,1\}//'
            exit 0
            ;;
        -*)
            echo "無法辨識的選項：$1" >&2
            exit 2
            ;;
        *)
            rids+=("$1")
            ;;
    esac
    shift
done

if [ ${#rids[@]} -eq 0 ]; then
    rids=("${DEFAULT_RIDS[@]}")
fi

version_args=()
if [ -n "$version" ]; then
    version_args=(-p:Version="$version")
fi

if [ "$self_contained" = true ]; then
    # 自含執行階段並裁剪：目標機不需安裝 .NET，代價是產物約 12 MB。
    mode_args=(--self-contained true -p:PublishTrimmed=true -p:EnableCompressionInSingleFile=true)
    mode_label="self-contained（不需 .NET 執行階段，約 12 MB）"
    archive_suffix=""
else
    # 預設不含 .NET 組件：產物僅數百 KB，但目標機須裝有 .NET 10 執行階段。
    mode_args=(--self-contained false)
    mode_label="framework-dependent（需 .NET 10 執行階段，約 650 KB）"
    archive_suffix="-slim"
fi

archive_name() {
    local rid="$1"
    echo "md2pdf-${version:-dev}-${rid}${archive_suffix}"
}

# 把單一 RID 的產物連同授權條款打包；Windows 用 zip，其餘用 tar.gz。
create_archive() {
    local rid="$1"
    local name
    name="$(archive_name "$rid")"

    cp LICENSE "$OUTPUT_ROOT/$rid/"

    if [ "${rid#win-}" != "$rid" ]; then
        (cd "$OUTPUT_ROOT/$rid" && zip -q "../$name.zip" md2pdf.exe LICENSE)
    else
        tar -czf "$OUTPUT_ROOT/$name.tar.gz" -C "$OUTPUT_ROOT/$rid" md2pdf LICENSE
    fi
}

write_checksums() {
    local sums="SHA256SUMS.txt"
    local tool

    if command -v sha256sum >/dev/null 2>&1; then
        tool=(sha256sum)
    else
        tool=(shasum -a 256)
    fi

    (
        cd "$OUTPUT_ROOT"
        # shellcheck disable=SC2035
        "${tool[@]}" *.tar.gz *.zip 2>/dev/null > "$sums" || true
    )
}

echo "發佈模式：$mode_label"
echo "目標 RID：${rids[*]}"
[ -n "$version" ] && echo "版號：$version"
echo

# NOTE: 只清理本次要重建的目錄，好讓兩種模式接力執行時，先前產生的壓縮檔留在 publish/ 下。
for rid in "${rids[@]}"; do
    echo "── 發佈 $rid ──"
    rm -rf "${OUTPUT_ROOT:?}/$rid"
    dotnet publish "$PROJECT" \
        --configuration Release \
        --runtime "$rid" \
        -p:PublishSingleFile=true \
        -p:DebugType=none \
        -p:GenerateDocumentationFile=false \
        "${mode_args[@]}" \
        "${version_args[@]}" \
        --output "$OUTPUT_ROOT/$rid"

    [ "$archive" = true ] && create_archive "$rid"
done

echo
if [ "$archive" = true ]; then
    write_checksums
    echo "完成。壓縮檔："
    ls -lh "$OUTPUT_ROOT"/*.tar.gz "$OUTPUT_ROOT"/*.zip 2>/dev/null || true
else
    echo "完成。產物："
    find "$OUTPUT_ROOT" -type f \( -name 'md2pdf' -o -name 'md2pdf.exe' \) -exec ls -lh {} \;
fi
