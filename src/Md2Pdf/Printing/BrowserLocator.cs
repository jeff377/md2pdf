using System.Runtime.InteropServices;

namespace Md2Pdf.Printing;

/// <summary>
/// 尋找可用於列印的 Chromium 系瀏覽器（Chrome / Edge / Chromium / Brave）。
/// </summary>
/// <remarks>
/// 本工具不自帶瀏覽器核心——PDF 由系統上已安裝的瀏覽器以無頭模式列印，
/// 換得極小的產物體積，代價是目標機器必須裝有其中一種瀏覽器。
/// </remarks>
public static class BrowserLocator
{
    /// <summary>可指定瀏覽器路徑的環境變數名稱。</summary>
    public const string EnvironmentVariableName = "MD2PDF_BROWSER";

    /// <summary>
    /// 依序以「明確指定 → 環境變數 → 平台預設安裝位置」尋找瀏覽器。
    /// </summary>
    /// <param name="explicitPath">命令列指定的路徑；null 表示未指定。</param>
    /// <returns>瀏覽器執行檔的完整路徑。</returns>
    /// <exception cref="BrowserNotFoundException">找不到任何可用瀏覽器時擲出。</exception>
    public static string Locate(string? explicitPath = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return File.Exists(explicitPath)
                ? explicitPath
                : throw new BrowserNotFoundException($"指定的瀏覽器不存在：{explicitPath}");
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return File.Exists(fromEnvironment)
                ? fromEnvironment
                : throw new BrowserNotFoundException(
                    $"環境變數 {EnvironmentVariableName} 指向的瀏覽器不存在：{fromEnvironment}");
        }

        foreach (var candidate in Candidates())
        {
            if (File.Exists(candidate))
                return candidate;
        }

        foreach (var name in CommandNames())
        {
            var resolved = FindOnPath(name);
            if (resolved is not null)
                return resolved;
        }

        throw new BrowserNotFoundException(
            $"找不到 Chrome / Edge / Chromium。請安裝其中一種，或以 --browser 指定路徑" +
            $"（亦可設定環境變數 {EnvironmentVariableName}）。");
    }

    private static IEnumerable<string> Candidates()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
            yield return "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge";
            yield return "/Applications/Chromium.app/Contents/MacOS/Chromium";
            yield return "/Applications/Brave Browser.app/Contents/MacOS/Brave Browser";
            yield break;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (var root in WindowsRoots())
            {
                yield return Path.Combine(root, "Google", "Chrome", "Application", "chrome.exe");
                yield return Path.Combine(root, "Microsoft", "Edge", "Application", "msedge.exe");
                yield return Path.Combine(root, "BraveSoftware", "Brave-Browser", "Application", "brave.exe");
                yield return Path.Combine(root, "Chromium", "Application", "chrome.exe");
            }

            yield break;
        }

        yield return "/opt/google/chrome/chrome";
        yield return "/usr/bin/google-chrome";
        yield return "/usr/bin/chromium";
        yield return "/usr/bin/chromium-browser";
        yield return "/usr/bin/microsoft-edge";
    }

    private static IEnumerable<string> WindowsRoots()
    {
        // 使用者層級安裝落在 LocalAppData，系統層級則在 Program Files；兩者都要找。
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    private static IEnumerable<string> CommandNames()
    {
        yield return "google-chrome";
        yield return "google-chrome-stable";
        yield return "chromium";
        yield return "chromium-browser";
        yield return "microsoft-edge";
    }

    private static string? FindOnPath(string command)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVariable))
            return null;

        foreach (var directory in pathVariable.Split(Path.PathSeparator))
        {
            if (directory.Length == 0)
                continue;

            var candidate = Path.Combine(directory, command);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
