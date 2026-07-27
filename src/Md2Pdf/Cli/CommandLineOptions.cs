namespace Md2Pdf.Cli;

/// <summary>
/// 一次轉換所需的全部參數，由 <see cref="CommandLineParser"/> 產生。
/// </summary>
public sealed class CommandLineOptions
{
    /// <summary>來源 Markdown 檔路徑。</summary>
    public required string InputPath { get; init; }

    /// <summary>輸出檔路徑。</summary>
    public required string OutputPath { get; init; }

    /// <summary>輸出格式。</summary>
    public required OutputFormat Format { get; init; }

    /// <summary>文件標題；未指定時取內文第一個 H1，再無則取輸入檔名。</summary>
    public string? Title { get; init; }

    /// <summary>自訂樣式表路徑；指定時完全取代內建樣式。</summary>
    public string? CssPath { get; init; }

    /// <summary>是否以橫向紙張排版。</summary>
    public bool Landscape { get; init; }

    /// <summary>完成後是否以系統預設程式開啟輸出檔。</summary>
    public bool OpenAfterConvert { get; init; }

    /// <summary>指定瀏覽器執行檔路徑；未指定時自動搜尋。</summary>
    public string? BrowserPath { get; init; }
}
