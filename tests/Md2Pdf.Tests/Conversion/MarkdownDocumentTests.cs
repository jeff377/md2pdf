using Md2Pdf.Conversion;
using Xunit;

namespace Md2Pdf.Tests.Conversion;

public class MarkdownDocumentTests
{
    [Fact]
    public void Parse_抽出H1標題與開頭資訊區塊()
    {
        var document = MarkdownDocument.Parse("""
            # 第二階段時程表

            > **用途**：開會進度報告。
            > **更新**：2026-07-20

            ## 一、前置阻擋
            內容。
            """);

        Assert.Equal("第二階段時程表", document.Title);
        Assert.Contains("**用途**：開會進度報告。", document.MetaMarkdown, StringComparison.Ordinal);
        Assert.Contains("**更新**：2026-07-20", document.MetaMarkdown, StringComparison.Ordinal);
        Assert.Contains("## 一、前置阻擋", document.BodyMarkdown, StringComparison.Ordinal);
        Assert.DoesNotContain("第二階段時程表", document.BodyMarkdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_沒有標題與資訊區塊_內文原封不動()
    {
        var document = MarkdownDocument.Parse("## 小節\n內容。");

        Assert.Null(document.Title);
        Assert.Equal(string.Empty, document.MetaMarkdown);
        Assert.Contains("## 小節", document.BodyMarkdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_內文中的blockquote_不被當成資訊區塊()
    {
        var document = MarkdownDocument.Parse("# 標題\n\n## 小節\n\n> 這是內文引言。");

        Assert.Equal(string.Empty, document.MetaMarkdown);
        Assert.Contains("> 這是內文引言。", document.BodyMarkdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_支援CRLF換行()
    {
        var document = MarkdownDocument.Parse("# 標題\r\n\r\n> 資訊\r\n\r\n內容。");

        Assert.Equal("標題", document.Title);
        Assert.Equal("資訊", document.MetaMarkdown);
    }
}
