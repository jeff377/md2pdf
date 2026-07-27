namespace Md2Pdf.Cli;

/// <summary>
/// 輸出格式。
/// </summary>
public enum OutputFormat
{
    /// <summary>PDF 文件（預設）。</summary>
    Pdf,

    /// <summary>樣式化的獨立 HTML 檔（PDF 轉換過程的中介產物）。</summary>
    Html,
}
