namespace Md2Pdf.Conversion;

/// <summary>
/// 拆解後的 Markdown 來源：標題、開頭資訊區塊、其餘內文。
/// </summary>
/// <remarks>
/// 報告型文件慣以「# 標題」開頭、緊接一段 blockquote 交代用途與更新日期。
/// 這兩段在版面上的角色與一般內文不同（標題列與資訊卡），故於轉換前先抽出。
/// </remarks>
public sealed class MarkdownDocument
{
    private MarkdownDocument(string? title, string metaMarkdown, string bodyMarkdown)
    {
        Title = title;
        MetaMarkdown = metaMarkdown;
        BodyMarkdown = bodyMarkdown;
    }

    /// <summary>文件標題；來源未以 H1 開頭時為 null。</summary>
    public string? Title { get; }

    /// <summary>開頭資訊區塊（已去除 blockquote 標記）；沒有時為空字串。</summary>
    public string MetaMarkdown { get; }

    /// <summary>其餘內文。</summary>
    public string BodyMarkdown { get; }

    /// <summary>
    /// 拆解 Markdown 來源。
    /// </summary>
    /// <param name="markdown">Markdown 全文。</param>
    /// <returns>拆解結果。</returns>
    public static MarkdownDocument Parse(string markdown)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var lines = markdown.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var index = 0;

        while (index < lines.Length && lines[index].Trim().Length == 0)
            index++;

        string? title = null;
        if (index < lines.Length && lines[index].StartsWith("# ", StringComparison.Ordinal))
        {
            title = lines[index][2..].Trim();
            index++;
        }

        while (index < lines.Length && lines[index].Trim().Length == 0)
            index++;

        var meta = new List<string>();
        while (index < lines.Length && lines[index].StartsWith('>'))
        {
            meta.Add(lines[index].TrimStart('>').TrimStart());
            index++;
        }

        var body = string.Join("\n", lines[index..]);
        return new MarkdownDocument(title, string.Join("\n", meta).Trim(), body);
    }
}
