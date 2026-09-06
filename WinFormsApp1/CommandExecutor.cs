using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace WinFormsApp1;

public static class CommandExecutor
{
    public static async Task<int> RunAsync(string executable, IEnumerable<string> arguments, IProgress<string> progress, CancellationToken cancellationToken)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Path.GetFileName(executable).Equals("sfc.exe", StringComparison.OrdinalIgnoreCase)
            ? Encoding.Unicode : Encoding.GetEncoding(CultureInfo.CurrentCulture.TextInfo.OEMCodePage);
        var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true, StandardOutputEncoding = encoding, StandardErrorEncoding = encoding };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var process = new Process { StartInfo = start };
        process.Start();
        async Task Read(StreamReader reader)
        {
            var buffer = new char[2048]; int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0) progress.Report(new string(buffer, 0, count));
        }
        try { await Task.WhenAll(Read(process.StandardOutput), Read(process.StandardError), process.WaitForExitAsync(cancellationToken)); return process.ExitCode; }
        catch (OperationCanceledException) { if (!process.HasExited) process.Kill(true); throw; }
    }
}
