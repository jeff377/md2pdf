using System.Text.RegularExpressions;

namespace Md2Pdf.Conversion;

/// <summary>
/// 把進度表常見的狀態標記（✅ 已完成、🔄 進行中、⬜ 未開始…）包成可上色的 span。
/// </summary>
/// <remarks>
/// CSS 無法依文字內容選取元素，故只能在 HTML 產生後以字串處理補上 class。
/// 僅比對「標記符號 + 緊接的短詞」，不影響其他內文。
/// </remarks>
public static partial class StatusHighlighter
{
    private static readonly Dictionary<string, string> ClassByMarker = new(StringComparer.Ordinal)
    {
        ["✅"] = "status-done",
        ["✔"] = "status-done",
        ["🔄"] = "status-active",
        ["🚧"] = "status-active",
        ["⏳"] = "status-active",
        ["⬜"] = "status-todo",
        ["◻"] = "status-todo",
        ["⏸"] = "status-todo",
        ["❌"] = "status-blocked",
        ["⛔"] = "status-blocked",
    };

    /// <summary>
    /// 為 HTML 中的狀態標記加上樣式。
    /// </summary>
    /// <param name="html">已轉換完成的 HTML 片段。</param>
    /// <returns>加上 span 的 HTML。</returns>
    public static string Apply(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        return MarkerRegex().Replace(html, match =>
        {
            var marker = match.Groups["marker"].Value;
            if (!ClassByMarker.TryGetValue(marker, out var cssClass))
                return match.Value;

            var label = match.Groups["label"].Value;
            return $"""<span class="status {cssClass}">{marker}{label}</span>""";
        });
    }

    [GeneratedRegex(
        @"(?<marker>✅|✔|🔄|🚧|⏳|⬜|◻|⏸|❌|⛔)️?(?<label>\s*[\p{L}\p{N}]{0,6})",
        RegexOptions.CultureInvariant)]
    private static partial Regex MarkerRegex();
}
