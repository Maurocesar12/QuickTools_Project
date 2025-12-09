using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WinFormsApp1 // Note: Mantive o namespace padrão do seu projeto
{
    public class SystemCleaner
    {
        public struct CleanResult { public long BytesFreed; public int FilesDeleted; public int Errors; }

        public static List<string> GetCleanLocations()
        {
            return new List<string> { Path.GetTempPath(), @"C:\Windows\Temp", @"C:\Windows\%temp%" };
        }

        public static Task<long> AnalyzeSpaceAsync()
        {
            return Task.Run(() =>
            {
                long total = 0;
                foreach (var path in GetCleanLocations())
                {
                    if (Directory.Exists(path))
                    {
                        try { total += new DirectoryInfo(path).EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length); }
                        catch { }
                    }
                }
                return total;
            });
        }

        public static Task<CleanResult> RunCleaningAsync(IProgress<string> progress)
        {
            return Task.Run(() =>
            {
                var res = new CleanResult();
                foreach (var dirPath in GetCleanLocations())
                {
                    if (!Directory.Exists(dirPath)) continue;
                    progress?.Report($"Varrendo: {dirPath}");
                    DirectoryInfo di = new DirectoryInfo(dirPath);

                    foreach (FileInfo file in di.GetFiles())
                    {
                        try { long s = file.Length; file.Delete(); res.BytesFreed += s; res.FilesDeleted++; } catch { res.Errors++; }
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        try { dir.Delete(true); res.FilesDeleted++; } catch { res.Errors++; }
                    }
                }
                return res;
            });
        }
    }
}