using Md2Pdf.Cli;
using Xunit;

namespace Md2Pdf.Tests.Cli;

public class CommandLineParserTests
{
    [Fact]
    public void Parse_只給輸入檔_預設輸出同名pdf()
    {
        var options = CommandLineParser.Parse(["report.md"]);

        Assert.Equal(OutputFormat.Pdf, options.Format);
        Assert.Equal(Path.ChangeExtension(Path.GetFullPath("report.md"), ".pdf"), options.OutputPath);
    }

    [Fact]
    public void Parse_輸出副檔名為html_推斷為html格式()
    {
        var options = CommandLineParser.Parse(["report.md", "-o", "preview.html"]);

        Assert.Equal(OutputFormat.Html, options.Format);
        Assert.Equal("preview.html", options.OutputPath);
    }

    [Fact]
    public void Parse_明確指定格式_優先於副檔名推斷()
    {
        var options = CommandLineParser.Parse(["report.md", "--to", "html", "-o", "out.bin"]);

        Assert.Equal(OutputFormat.Html, options.Format);
    }

    [Fact]
    public void Parse_只給tohtml_未給輸出_預設同名html()
    {
        var options = CommandLineParser.Parse(["report.md", "--to", "html"]);

        Assert.Equal(Path.ChangeExtension(Path.GetFullPath("report.md"), ".html"), options.OutputPath);
    }

    [Fact]
    public void Parse_旗標與具名選項全數帶入()
    {
        var options = CommandLineParser.Parse(
            ["report.md", "--landscape", "--open", "--title", "季報", "--css", "my.css", "--browser", "/bin/chrome"]);

        Assert.True(options.Landscape);
        Assert.True(options.OpenAfterConvert);
        Assert.Equal("季報", options.Title);
        Assert.Equal("my.css", options.CssPath);
        Assert.Equal("/bin/chrome", options.BrowserPath);
    }

    [Fact]
    public void Parse_未給輸入檔_擲出()
    {
        Assert.Throws<CommandLineException>(() => CommandLineParser.Parse(["--landscape"]));
    }

    [Fact]
    public void Parse_給兩個輸入檔_擲出()
    {
        Assert.Throws<CommandLineException>(() => CommandLineParser.Parse(["a.md", "b.md"]));
    }

    [Fact]
    public void Parse_無法辨識的選項_擲出()
    {
        Assert.Throws<CommandLineException>(() => CommandLineParser.Parse(["a.md", "--nope"]));
    }

    [Fact]
    public void Parse_選項缺值_擲出()
    {
        Assert.Throws<CommandLineException>(() => CommandLineParser.Parse(["a.md", "-o"]));
    }

    [Fact]
    public void Parse_不支援的格式_擲出()
    {
        Assert.Throws<CommandLineException>(() => CommandLineParser.Parse(["a.md", "--to", "docx"]));
    }
}
