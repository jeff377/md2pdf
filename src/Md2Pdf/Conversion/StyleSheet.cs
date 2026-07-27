using System.Reflection;

namespace Md2Pdf.Conversion;

/// <summary>
/// 提供轉換時使用的 CSS。
/// </summary>
public static class StyleSheet
{
    private const string ResourceName = "Md2Pdf.Assets.report.css";

    /// <summary>
    /// 讀取內建樣式表。
    /// </summary>
    /// <returns>內建 CSS 內容。</returns>
    public static string Default()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"組件中找不到內嵌資源 {ResourceName}。");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// 讀取指定路徑的樣式表；路徑為 null 時回傳內建樣式。
    /// </summary>
    /// <param name="path">自訂 CSS 檔路徑。</param>
    /// <returns>CSS 內容。</returns>
    /// <exception cref="FileNotFoundException">指定的檔案不存在時擲出。</exception>
    public static string Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Default();

        if (!File.Exists(path))
            throw new FileNotFoundException($"找不到樣式表：{path}", path);

        return File.ReadAllText(path);
    }
}
