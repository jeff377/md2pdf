namespace Md2Pdf.Printing;

/// <summary>
/// 瀏覽器列印 PDF 失敗時擲出。
/// </summary>
public sealed class PdfPrintException : Exception
{
    /// <summary>以錯誤訊息建立例外。</summary>
    public PdfPrintException(string message) : base(message)
    {
    }

    /// <summary>以錯誤訊息與內部例外建立例外。</summary>
    public PdfPrintException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
