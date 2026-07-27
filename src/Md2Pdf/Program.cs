using System.Reflection;
using Md2Pdf;
using Md2Pdf.Cli;
using Md2Pdf.Printing;

if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
{
    Console.WriteLine(CommandLineParser.UsageText);
    return args.Length == 0 ? 1 : 0;
}

if (args.Contains("-v") || args.Contains("--version"))
{
    Console.WriteLine($"md2pdf {Version()}");
    return 0;
}

try
{
    var options = CommandLineParser.Parse(args);
    var outputPath = new ConversionRunner().Run(options);
    Console.WriteLine($"已產生：{outputPath}");

    if (options.OpenAfterConvert && !FileOpener.TryOpen(outputPath))
        Console.Error.WriteLine("提示：無法自動開啟輸出檔，請手動開啟。");

    return 0;
}
catch (CommandLineException ex)
{
    Console.Error.WriteLine($"參數錯誤：{ex.Message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(CommandLineParser.UsageText);
    return 2;
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"錯誤：{ex.Message}");
    return 3;
}
catch (BrowserNotFoundException ex)
{
    Console.Error.WriteLine($"錯誤：{ex.Message}");
    return 4;
}
catch (PdfPrintException ex)
{
    Console.Error.WriteLine($"錯誤：{ex.Message}");
    return 5;
}
catch (IOException ex)
{
    Console.Error.WriteLine($"檔案存取失敗：{ex.Message}");
    return 6;
}
catch (UnauthorizedAccessException ex)
{
    Console.Error.WriteLine($"權限不足：{ex.Message}");
    return 6;
}

static string Version()
{
    var assembly = Assembly.GetExecutingAssembly();
    return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion.Split('+')[0]
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";
}
