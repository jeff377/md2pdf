using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Md2Pdf;

/// <summary>
/// 以系統預設程式開啟檔案（<c>--open</c> 選項）。
/// </summary>
public static class FileOpener
{
    /// <summary>
    /// 嘗試開啟指定檔案；失敗時不擲出例外（檔案已產生，開啟失敗不算轉換失敗）。
    /// </summary>
    /// <param name="path">要開啟的檔案路徑。</param>
    /// <returns>成功啟動外部程式時為 true。</returns>
    public static bool TryOpen(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            var startInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? new ProcessStartInfo(path) { UseShellExecute = true }
                : new ProcessStartInfo(
                    RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "open" : "xdg-open", [path]);

            using var process = Process.Start(startInfo);
            return process is not null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
