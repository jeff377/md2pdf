namespace Md2Pdf.Cli;

/// <summary>
/// 命令列參數有誤時擲出；由 Program 轉為使用說明與非零離開碼。
/// </summary>
public sealed class CommandLineException : Exception
{
    /// <summary>以錯誤訊息建立例外。</summary>
    public CommandLineException(string message) : base(message)
    {
    }

    /// <summary>以錯誤訊息與內部例外建立例外。</summary>
    public CommandLineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
