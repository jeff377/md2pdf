using System.Diagnostics;
using System.Runtime.Versioning;
using Md2Pdf.Printing;
using Xunit;

namespace Md2Pdf.Tests.Printing;

public class ChromiumPdfPrinterTests : IDisposable
{
    private readonly string _workDirectory =
        Path.Combine(Path.GetTempPath(), $"md2pdf-test-{Guid.NewGuid():N}");

    public ChromiumPdfPrinterTests() => Directory.CreateDirectory(_workDirectory);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        try
        {
            Directory.Delete(_workDirectory, recursive: true);
        }
        catch (IOException)
        {
            // 臨時目錄殘留不影響測試結果。
        }
    }

    [Fact]
    public void Print_瀏覽器路徑不存在_擲出PdfPrintException()
    {
        var printer = new ChromiumPdfPrinter(Path.Combine(_workDirectory, "沒有這個瀏覽器"));

        Assert.Throws<PdfPrintException>(
            () => printer.Print(CreateHtml(), Path.Combine(_workDirectory, "out.pdf")));
    }

    [Fact]
    public void Print_已存在同名輸出檔_先行移除()
    {
        var pdfPath = Path.Combine(_workDirectory, "out.pdf");
        File.WriteAllText(pdfPath, "前一次執行留下的舊檔");
        var printer = new ChromiumPdfPrinter(Path.Combine(_workDirectory, "沒有這個瀏覽器"));

        Assert.Throws<PdfPrintException>(() => printer.Print(CreateHtml(), pdfPath));

        // 舊檔若留著，「檔案已出現且大小穩定」的完成條件會被誤判成立。
        Assert.False(File.Exists(pdfPath));
    }

    /// <summary>
    /// 重現實測到的行為：Chrome 以全新的 user-data-dir 啟動時會喚起子行程而遲遲不退場，
    /// 但 PDF 早已寫完。完成判定必須看輸出檔而非行程結束，否則會一路等到硬逾時。
    /// </summary>
    [Fact]
    public void Print_瀏覽器寫完檔案卻不退場_仍在數秒內完成()
    {
        // 以 shell 腳本假冒瀏覽器，僅適用於類 Unix 平台。
        if (OperatingSystem.IsWindows())
            return;

        var pdfPath = Path.Combine(_workDirectory, "out.pdf");
        var printer = new ChromiumPdfPrinter(CreateLingeringFakeBrowser());

        var stopwatch = Stopwatch.StartNew();
        printer.Print(CreateHtml(), pdfPath);
        stopwatch.Stop();

        Assert.True(File.Exists(pdfPath));
        Assert.StartsWith("%PDF-", File.ReadAllText(pdfPath), StringComparison.Ordinal);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(30),
            $"應在檔案大小穩定後立即完成，實際耗時 {stopwatch.Elapsed.TotalSeconds:0.0} 秒。");
    }

    private string CreateHtml()
    {
        var htmlPath = Path.Combine(_workDirectory, "source.html");
        File.WriteAllText(htmlPath, "<!doctype html><html><body>測試</body></html>");
        return htmlPath;
    }

    /// <summary>
    /// 產生一個「寫出檔案後就賴著不走」的假瀏覽器腳本。
    /// </summary>
    [UnsupportedOSPlatform("windows")]
    private string CreateLingeringFakeBrowser()
    {
        var scriptPath = Path.Combine(_workDirectory, "fake-browser.sh");
        File.WriteAllText(scriptPath, """
            #!/bin/sh
            for arg in "$@"; do
                case "$arg" in
                    --print-to-pdf=*) out="${arg#--print-to-pdf=}" ;;
                esac
            done
            printf '%%PDF-1.4 假造的輸出' > "$out"
            sleep 60

            """);

        File.SetUnixFileMode(
            scriptPath,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return scriptPath;
    }
}
