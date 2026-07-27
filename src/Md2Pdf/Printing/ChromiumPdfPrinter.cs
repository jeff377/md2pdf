using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Md2Pdf.Printing;

/// <summary>
/// 以 Chromium 系瀏覽器的無頭模式把 HTML 列印成 PDF。
/// </summary>
public sealed class ChromiumPdfPrinter
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(2);

    private readonly string _browserPath;

    /// <summary>
    /// 以指定瀏覽器建立列印器。
    /// </summary>
    /// <param name="browserPath">瀏覽器執行檔路徑。</param>
    public ChromiumPdfPrinter(string browserPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(browserPath);
        _browserPath = browserPath;
    }

    /// <summary>
    /// 列印 HTML 檔成 PDF。
    /// </summary>
    /// <param name="htmlPath">來源 HTML 檔路徑。</param>
    /// <param name="pdfPath">輸出 PDF 檔路徑；已存在的同名檔會先被移除。</param>
    /// <exception cref="PdfPrintException">瀏覽器啟動失敗、逾時或未產生檔案時擲出。</exception>
    public void Print(string htmlPath, string pdfPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(htmlPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);

        var fullPdfPath = Path.GetFullPath(pdfPath);

        // 完成判斷靠「輸出檔出現且大小穩定」，殘留的舊檔會讓條件提前成立，故先移除。
        DeleteIfExists(fullPdfPath);

        // 另開臨時設定檔目錄，避免與使用者已開啟的瀏覽器共用 profile 而拒絕啟動。
        var profileDirectory = Path.Combine(Path.GetTempPath(), $"md2pdf-profile-{Guid.NewGuid():N}");

        try
        {
            using var process = Start(BuildArguments(htmlPath, fullPdfPath, profileDirectory));

            // WARNING: 兩個輸出串流都必須非同步讀取。改用同步 ReadToEnd 會阻塞到串流 EOF
            // （亦即行程結束），使下方的逾時形同虛設；只讀其一則可能因另一串流緩衝區填滿而死結。
            var stderrTask = process.StandardError.ReadToEndAsync();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();

            var finished = WaitForCompletion(process, fullPdfPath);
            var exitCode = process.HasExited ? process.ExitCode : (int?)null;

            // NOTE: 以全新的 --user-data-dir 啟動時，Chrome 會把它當成新安裝而喚起 GoogleUpdater
            // 子行程並遲遲不退場，但 PDF 此時早已寫完。因此不等行程結束，達成完成條件後
            // 主動收掉整棵行程樹。
            TryKill(process);

            var stderr = Drain(stderrTask);
            Drain(stdoutTask);

            if (!finished)
                throw new PdfPrintException($"瀏覽器列印逾時（超過 {Timeout.TotalSeconds:0} 秒）。{FormatDetail(exitCode, stderr)}");

            if (!File.Exists(fullPdfPath))
                throw new PdfPrintException($"瀏覽器未產生 PDF。{FormatDetail(exitCode, stderr)}");
        }
        finally
        {
            TryDeleteDirectory(profileDirectory);
        }
    }

    /// <summary>
    /// 輪詢等待列印完成：輸出檔已產生且大小連續兩次不變，或瀏覽器自行退場。
    /// </summary>
    /// <returns>在硬逾時內達成完成條件則為 true。</returns>
    private static bool WaitForCompletion(Process process, string pdfPath)
    {
        var stopwatch = Stopwatch.StartNew();
        long? previousSize = null;

        while (stopwatch.Elapsed < Timeout)
        {
            // 兼作輪詢間隔：等不到退場就回 false，接著檢查輸出檔。
            if (process.WaitForExit((int)PollInterval.TotalMilliseconds))
                return true;

            var size = TryGetFileSize(pdfPath);
            if (size is > 0 && size == previousSize)
                return true;

            previousSize = size;
        }

        return false;
    }

    private Process Start(IEnumerable<string> arguments)
    {
        var startInfo = new ProcessStartInfo(_browserPath)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        try
        {
            return Process.Start(startInfo)
                ?? throw new PdfPrintException($"無法啟動瀏覽器：{_browserPath}");
        }
        catch (Win32Exception ex)
        {
            throw new PdfPrintException($"無法啟動瀏覽器：{_browserPath}", ex);
        }
    }

    private static IEnumerable<string> BuildArguments(
        string htmlPath, string pdfPath, string profileDirectory)
    {
        yield return "--headless";
        yield return "--disable-gpu";
        yield return "--no-first-run";
        yield return "--no-pdf-header-footer";
        yield return $"--user-data-dir={profileDirectory}";

        // 列印本機檔案不需連外；關掉可減少啟動雜訊與非必要的背景連線。
        yield return "--disable-background-networking";
        yield return "--disable-component-update";

        // 讓頁面內的字型與樣式套用完成後才截圖，否則首次執行可能印出未套用樣式的版面。
        yield return "--virtual-time-budget=10000";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            yield return "--no-sandbox";

        yield return $"--print-to-pdf={pdfPath}";
        yield return new Uri(Path.GetFullPath(htmlPath)).AbsoluteUri;
    }

    private static long? TryGetFileSize(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return info.Exists ? info.Length : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// 取回已重導的輸出內容。行程樹若有殘留成員仍持有管線，讀取可能永不結束，故加上短逾時。
    /// </summary>
    private static string Drain(Task<string> readTask)
    {
        try
        {
            return readTask.Wait(DrainTimeout) ? readTask.Result : string.Empty;
        }
        catch (AggregateException)
        {
            // 串流因行程被收掉而中斷；診斷訊息缺一段不影響結果。
            return string.Empty;
        }
    }

    private static string FormatDetail(int? exitCode, string stderr)
    {
        var parts = new List<string>();
        if (exitCode is not null)
            parts.Add($"離開碼 {exitCode}");

        var trimmed = stderr.Trim();
        if (trimmed.Length > 0)
            parts.Add($"瀏覽器輸出：{trimmed}");

        return parts.Count == 0 ? string.Empty : string.Join("；", parts);
    }

    private static void TryKill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException)
        {
            // 行程已自行結束，無須處理。
        }
        catch (AggregateException)
        {
            // 行程樹中有成員無法終止；殘留行程不影響已寫出的 PDF。
        }
        catch (Win32Exception)
        {
            // 同上。
        }
    }

    private static void DeleteIfExists(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException ex)
        {
            throw new PdfPrintException($"無法覆寫既有的輸出檔：{path}", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new PdfPrintException($"無法覆寫既有的輸出檔：{path}", ex);
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
            // 臨時目錄殘留不影響結果，交給作業系統清理。
        }
        catch (UnauthorizedAccessException)
        {
            // 同上。
        }
    }
}
