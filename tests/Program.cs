using WinFormsApp1;
using System.Text;

// Child process exercises both redirected streams without invoking a shell.
if (args.Contains("--child"))
{
    for (var i = 0; i < 3000; i++) { Console.Out.WriteLine("stdout-" + i); Console.Error.WriteLine("stderr-" + i); }
    Environment.ExitCode = 7;
    return;
}

var root = Path.Combine(Path.GetTempPath(), "QuickToolsChecks-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS " + message); }
string Fixture(string name, string text, int ageHours)
{
    var path = Path.Combine(root, name); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, text); File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddHours(-ageHours)); return path;
}
SystemCleaner.Candidate Candidate(string path) { var file = new FileInfo(path); return new(path, file.Length, file.LastWriteTimeUtc); }

var old = Fixture("nested/old.txt", "temporary", 48);
var recent = Fixture("recent.txt", "keep recent", 0);
var changed = Fixture("changed.txt", "original", 48);
var changedCandidate = Candidate(changed); File.AppendAllText(changed, " edited");
var outside = Path.Combine(AppContext.BaseDirectory, "outside-" + Guid.NewGuid().ToString("N") + ".txt");
File.WriteAllText(outside, "must stay"); File.SetLastWriteTimeUtc(outside, DateTime.UtcNow.AddDays(-2));
var oldCandidate = Candidate(old);
var result = await SystemCleaner.CleanAsync(new[] { oldCandidate, oldCandidate, Candidate(recent), changedCandidate, Candidate(outside) });
Check(!File.Exists(old) && result.FilesDeleted == 1 && result.BytesFreed == oldCandidate.Size, "Nested deletion counts exact bytes and deduplicates");
Check(File.Exists(recent) && File.Exists(changed) && File.Exists(outside) && result.Errors == 3, "Recent, changed and outside-root files preserved");

var store = Path.Combine(root, "workspace.json");
var workspace = new WorkspaceData { Notes = "Anotação com acentos\nSegunda linha", Tasks = new() { new() { Text = "Revisar conexão", Done = true } } };
WorkspaceStore.Save(workspace, store);
var loaded = WorkspaceStore.Load(store);
Check(loaded.Notes == workspace.Notes && loaded.Tasks.Single().Done, "Tasks and Unicode notes survive reload");
workspace.Notes = "Atualizado"; WorkspaceStore.Save(workspace, store);
Check(WorkspaceStore.Load(store).Notes == "Atualizado" && WorkspaceStore.Load(store + ".bak").Notes == loaded.Notes, "Atomic save retains previous backup");
File.WriteAllText(store, "{ invalid json");
var rejected = false; try { WorkspaceStore.Load(store); } catch (System.Text.Json.JsonException) { rejected = true; }
Check(rejected, "Corrupt notes reported instead of silently discarded");

var output = new StringBuilder();
var progress = new ImmediateProgress(text => output.Append(text));
var executable = Environment.ProcessPath!;
var childArguments = Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase)
    ? new[] { typeof(WorkspaceData).Assembly.Location, "--child" } : new[] { "--child" };
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
var code = await CommandExecutor.RunAsync(executable, childArguments, progress, timeout.Token);
Check(code == 7 && output.ToString().Contains("stdout-2999") && output.ToString().Contains("stderr-2999"), "Both command streams drain without deadlock; nonzero exit retained");
// Delete only known fixtures created by this run; never recursively delete a computed tree.
foreach (var file in new[] { recent, changed, outside, store, store + ".bak" }) File.Delete(file);
Console.WriteLine("All 6 checks passed.");

sealed class ImmediateProgress(Action<string> callback) : IProgress<string>
{
    private readonly object gate = new();
    public void Report(string value) { lock (gate) callback(value); }
}
