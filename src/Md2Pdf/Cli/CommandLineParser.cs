namespace Md2Pdf.Cli;

/// <summary>
/// 把命令列引數解析成 <see cref="CommandLineOptions"/>。
/// </summary>
public static class CommandLineParser
{
    /// <summary>使用說明文字。</summary>
    public const string UsageText = """
        md2pdf — 把 Markdown 轉成可直接列印的 PDF（或樣式化 HTML）。

        用法：
          md2pdf <input.md> [選項]

        選項：
          -o, --output <path>   輸出檔路徑（預設：與來源同名、換副檔名）
              --to <pdf|html>   輸出格式（預設：由 --output 副檔名推斷，再無則 pdf）
              --title <text>    文件標題（預設：內文第一個 H1，再無則檔名）
              --css <path>      自訂樣式表，完全取代內建樣式
              --landscape       以橫向紙張排版（寬表格適用）
              --open            完成後以系統預設程式開啟輸出檔
              --browser <path>  指定 Chrome / Edge 執行檔（預設：自動搜尋）
          -h, --help            顯示本說明
          -v, --version         顯示版本

        範例：
          md2pdf report.md
          md2pdf report.md -o ~/Downloads/報告.pdf --open
          md2pdf report.md --to html -o preview.html
          md2pdf 變更紀錄.md --landscape
        """;

    /// <summary>
    /// 解析引數。
    /// </summary>
    /// <param name="args">命令列引數。</param>
    /// <returns>解析後的選項。</returns>
    /// <exception cref="CommandLineException">引數缺漏、重複或無法辨識時擲出。</exception>
    public static CommandLineOptions Parse(string[] args)
    {
        string? input = null;
        string? output = null;
        string? format = null;
        string? title = null;
        string? css = null;
        string? browser = null;
        var landscape = false;
        var open = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-o" or "--output":
                    output = ReadValue(args, ref i, arg);
                    break;
                case "--to" or "--format":
                    format = ReadValue(args, ref i, arg);
                    break;
                case "--title":
                    title = ReadValue(args, ref i, arg);
                    break;
                case "--css":
                    css = ReadValue(args, ref i, arg);
                    break;
                case "--browser":
                    browser = ReadValue(args, ref i, arg);
                    break;
                case "--landscape":
                    landscape = true;
                    break;
                case "--open":
                    open = true;
                    break;
                default:
                    if (arg.StartsWith('-'))
                        throw new CommandLineException($"無法辨識的選項：{arg}");
                    if (input is not null)
                        throw new CommandLineException($"一次只能轉換一個檔案（多收到「{arg}」）。");
                    input = arg;
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(input))
            throw new CommandLineException("缺少來源 Markdown 檔。");

        var resolvedFormat = ResolveFormat(format, output);
        return new CommandLineOptions
        {
            InputPath = input,
            OutputPath = output ?? DefaultOutputPath(input, resolvedFormat),
            Format = resolvedFormat,
            Title = title,
            CssPath = css,
            Landscape = landscape,
            OpenAfterConvert = open,
            BrowserPath = browser,
        };
    }

    private static string ReadValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
            throw new CommandLineException($"選項 {optionName} 缺少值。");

        index++;
        return args[index];
    }

    /// <summary>
    /// 決定輸出格式：明確指定優先，其次由輸出副檔名推斷，都沒有則預設 PDF。
    /// </summary>
    private static OutputFormat ResolveFormat(string? format, string? outputPath)
    {
        if (!string.IsNullOrWhiteSpace(format))
        {
            return format.ToLowerInvariant() switch
            {
                "pdf" => OutputFormat.Pdf,
                "html" or "htm" => OutputFormat.Html,
                _ => throw new CommandLineException($"不支援的輸出格式：{format}（可用：pdf、html）"),
            };
        }

        var extension = Path.GetExtension(outputPath ?? string.Empty).ToLowerInvariant();
        return extension is ".html" or ".htm" ? OutputFormat.Html : OutputFormat.Pdf;
    }

    private static string DefaultOutputPath(string inputPath, OutputFormat format)
    {
        var extension = format == OutputFormat.Html ? ".html" : ".pdf";
        return Path.ChangeExtension(Path.GetFullPath(inputPath), extension);
    }
}
