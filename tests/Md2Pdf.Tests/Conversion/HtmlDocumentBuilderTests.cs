using Md2Pdf.Conversion;
using Xunit;

namespace Md2Pdf.Tests.Conversion;

public class HtmlDocumentBuilderTests
{
    private static HtmlDocumentBuilder CreateBuilder() => new(StyleSheet.Default());

    [Fact]
    public void Build_產出自足的HTML_樣式內嵌且無外部資源()
    {
        var html = CreateBuilder().Build("# 標題\n\n內容。", null, "fallback", landscape: false);

        Assert.StartsWith("<!doctype html>", html, StringComparison.Ordinal);
        Assert.Contains("<style>", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<link", html, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_標題優先序_指定值大於H1大於備援()
    {
        var fromOption = CreateBuilder().Build("# 內文標題\n", "指定標題", "備援", landscape: false);
        var fromHeading = CreateBuilder().Build("# 內文標題\n", null, "備援", landscape: false);
        var fromFallback = CreateBuilder().Build("內容。", null, "備援", landscape: false);

        Assert.Contains("<title>指定標題</title>", fromOption, StringComparison.Ordinal);
        Assert.Contains("<title>內文標題</title>", fromHeading, StringComparison.Ordinal);
        Assert.Contains("<title>備援</title>", fromFallback, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_標題含HTML字元_經過編碼()
    {
        var html = CreateBuilder().Build("內容。", "<script>x</script>", "備援", landscape: false);

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_開頭blockquote_成為資訊卡()
    {
        var html = CreateBuilder().Build("# 標題\n\n> **用途**：報告\n\n內文。", null, "x", landscape: false);

        Assert.Contains("""<div class="doc-meta">""", html, StringComparison.Ordinal);
        Assert.Contains("<strong>用途</strong>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_表格語法_轉成table()
    {
        var markdown = """
            | 階段 | 狀態 |
            |------|------|
            | P | ✅ 已完成 |
            """;

        var html = CreateBuilder().Build(markdown, null, "x", landscape: false);

        Assert.Contains("<table>", html, StringComparison.Ordinal);
        Assert.Contains("<th>階段</th>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_狀態標記_加上樣式()
    {
        var html = CreateBuilder().Build("- ✅ 已完成\n- ⬜ 未開始", null, "x", landscape: false);

        Assert.Contains("""<span class="status status-done">✅ 已完成</span>""", html, StringComparison.Ordinal);
        Assert.Contains("""<span class="status status-todo">⬜ 未開始</span>""", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_橫向_加入landscape版面規則()
    {
        var portrait = CreateBuilder().Build("內容。", null, "x", landscape: false);
        var landscape = CreateBuilder().Build("內容。", null, "x", landscape: true);

        Assert.DoesNotContain("size: A4 landscape", portrait, StringComparison.Ordinal);
        Assert.Contains("size: A4 landscape", landscape, StringComparison.Ordinal);
    }
}
