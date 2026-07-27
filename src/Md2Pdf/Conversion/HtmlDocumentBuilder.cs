using System.Net;
using System.Text;
using Markdig;

namespace Md2Pdf.Conversion;

/// <summary>
/// 把 Markdown 組成可直接列印的獨立 HTML 文件（樣式內嵌、無外部資源）。
/// </summary>
public sealed class HtmlDocumentBuilder
{
    private static readonly MarkdownPipeline BodyPipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    // 資訊區塊的每一行都是獨立欄位（用途、單位、更新日期…），
    // 故此處把換行視為強制斷行，不與後續文字合併成同一段。
    private static readonly MarkdownPipeline MetaPipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseSoftlineBreakAsHardlineBreak()
            .Build();

    private readonly string _css;

    /// <summary>
    /// 以指定樣式表建立產生器。
    /// </summary>
    /// <param name="css">要內嵌的 CSS。</param>
    public HtmlDocumentBuilder(string css)
    {
        ArgumentNullException.ThrowIfNull(css);
        _css = css;
    }

    /// <summary>
    /// 產生完整 HTML 文件。
    /// </summary>
    /// <param name="markdown">Markdown 全文。</param>
    /// <param name="title">文件標題；null 時取內文第一個 H1，再無則取 <paramref name="fallbackTitle"/>。</param>
    /// <param name="fallbackTitle">標題的最後備援（通常是來源檔名）。</param>
    /// <param name="landscape">是否以橫向紙張排版。</param>
    /// <returns>可直接存檔或交給瀏覽器列印的 HTML。</returns>
    public string Build(string markdown, string? title, string fallbackTitle, bool landscape)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        var document = MarkdownDocument.Parse(markdown);
        var resolvedTitle = FirstNonEmpty(title, document.Title, fallbackTitle);
        var meta = document.MetaMarkdown.Length == 0
            ? string.Empty
            : StatusHighlighter.Apply(Markdown.ToHtml(document.MetaMarkdown, MetaPipeline));
        var body = StatusHighlighter.Apply(Markdown.ToHtml(document.BodyMarkdown, BodyPipeline));

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("""<html lang="zh-Hant">""");
        html.AppendLine("<head>");
        html.AppendLine("""<meta charset="utf-8">""");
        html.AppendLine($"<title>{WebUtility.HtmlEncode(resolvedTitle)}</title>");
        html.AppendLine("<style>");
        html.AppendLine(_css);
        if (landscape)
            html.AppendLine("@page { size: A4 landscape; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine($"""<h1 class="doc-title">{WebUtility.HtmlEncode(resolvedTitle)}</h1>""");
        if (meta.Length > 0)
            html.AppendLine($"""<div class="doc-meta">{meta}</div>""");
        html.AppendLine(body);
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    private static string FirstNonEmpty(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate.Trim();
        }

        return string.Empty;
    }
}
