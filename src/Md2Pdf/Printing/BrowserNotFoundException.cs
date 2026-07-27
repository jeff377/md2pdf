namespace Md2Pdf.Printing;

/// <summary>
/// 找不到可用於列印的瀏覽器時擲出。
/// </summary>
public sealed class BrowserNotFoundException : Exception
{
    /// <summary>以錯誤訊息建立例外。</summary>
    public BrowserNotFoundException(string message) : base(message)
    {
    }

    /// <summary>以錯誤訊息與內部例外建立例外。</summary>
    public BrowserNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
