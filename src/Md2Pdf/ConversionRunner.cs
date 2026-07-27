using Md2Pdf.Cli;
using Md2Pdf.Conversion;
using Md2Pdf.Printing;

namespace Md2Pdf;

/// <summary>
/// 依選項執行一次轉換：Markdown → HTML →（視格式）PDF。
/// </summary>
public sealed class ConversionRunner
{
    /// <summary>
    /// 執行轉換。
    /// </summary>
    /// <param name="options">轉換選項。</param>
    /// <returns>實際產生的檔案路徑。</returns>
    /// <exception cref="FileNotFoundException">來源 Markdown 或自訂樣式表不存在時擲出。</exception>
    /// <exception cref="BrowserNotFoundException">需要列印 PDF 但找不到瀏覽器時擲出。</exception>
    /// <exception cref="PdfPrintException">瀏覽器列印失敗時擲出。</exception>
    public string Run(CommandLineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var inputPath = Path.GetFullPath(options.InputPath);
        if (!File.Exists(inputPath))
            throw new FileNotFoundException($"找不到來源檔：{options.InputPath}", inputPath);

        var outputPath = Path.GetFullPath(options.OutputPath);
        EnsureDirectory(outputPath);

        var markdown = File.ReadAllText(inputPath);
        var builder = new HtmlDocumentBuilder(StyleSheet.Load(options.CssPath));
        var html = builder.Build(
            markdown,
            options.Title,
            Path.GetFileNameWithoutExtension(inputPath),
            options.Landscape);

        if (options.Format == OutputFormat.Html)
        {
            File.WriteAllText(outputPath, html);
            return outputPath;
        }

        var browserPath = BrowserLocator.Locate(options.BrowserPath);
        var temporaryHtml = Path.Combine(Path.GetTempPath(), $"md2pdf-{Guid.NewGuid():N}.html");
        try
        {
            File.WriteAllText(temporaryHtml, html);
            new ChromiumPdfPrinter(browserPath).Print(temporaryHtml, outputPath);
        }
        finally
        {
            TryDelete(temporaryHtml);
        }

        return outputPath;
    }

    private static void EnsureDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // 臨時檔殘留不影響結果。
        }
        catch (UnauthorizedAccessException)
        {
            // 同上。
        }
    }
}
