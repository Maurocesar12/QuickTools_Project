namespace WinFormsApp1;

public static class SystemCleaner
{
    public record Candidate(string Path, long Size, DateTime LastWriteUtc);
    public record CleanResult(long BytesFreed, int FilesDeleted, int Errors);
    public static List<string> GetCleanLocations() => new[] { Path.GetTempPath(), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp") }
        .Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static bool IsInsideRoot(string path, string root)
    {
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasLinkedAncestor(string path)
    {
        for (var directory = new FileInfo(path).Directory; directory != null; directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0) return true;
        return false;
    }

    public static Task<IReadOnlyList<Candidate>> ScanAsync() => Task.Run<IReadOnlyList<Candidate>>(() =>
    {
        var candidates = new Dictionary<string, Candidate>(StringComparer.OrdinalIgnoreCase);
        var cutoff = DateTime.UtcNow.AddHours(-24);
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.ReparsePoint };
        foreach (var root in GetCleanLocations())
        {
            try
            {
                if (!Directory.Exists(root) || (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0) continue;
                foreach (var file in new DirectoryInfo(root).EnumerateFiles("*", options))
                {
                    try { if (file.LastWriteTimeUtc < cutoff && IsInsideRoot(file.FullName, root) && !HasLinkedAncestor(file.FullName)) candidates[file.FullName] = new(file.FullName, file.Length, file.LastWriteTimeUtc); }
                    catch (IOException) { } catch (UnauthorizedAccessException) { }
                }
            }
            catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        return candidates.Values.ToList();
    });

    public static Task<CleanResult> CleanAsync(IReadOnlyList<Candidate> candidates) => Task.Run(() =>
    {
        long bytes = 0; int deleted = 0, skipped = 0;
        var roots = GetCleanLocations();
        foreach (var item in candidates.DistinctBy(c => c.Path, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var file = new FileInfo(item.Path);
                if (!roots.Any(root => IsInsideRoot(item.Path, root)) || !file.Exists || HasLinkedAncestor(item.Path) || (file.Attributes & FileAttributes.ReparsePoint) != 0 || file.Length != item.Size || file.LastWriteTimeUtc != item.LastWriteUtc || file.LastWriteTimeUtc >= DateTime.UtcNow.AddHours(-24)) { skipped++; continue; }
                file.Delete(); bytes += item.Size; deleted++;
            }
            catch (IOException) { skipped++; } catch (UnauthorizedAccessException) { skipped++; }
        }
        return new CleanResult(bytes, deleted, skipped);
    });
}
