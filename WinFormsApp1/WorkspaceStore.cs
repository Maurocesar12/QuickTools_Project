using System.Text.Json;

namespace WinFormsApp1;

public sealed class WorkItem
{
    public string Text { get; set; } = "";
    public bool Done { get; set; }
    public override string ToString() => Text;
}

public sealed class WorkspaceData
{
    public List<WorkItem> Tasks { get; set; } = new();
    public string Notes { get; set; } = "";
}

public static class WorkspaceStore
{
    public static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ITQuickTools", "workspace.json");
    public static WorkspaceData Load(string? filePath = null)
    {
        filePath ??= FilePath;
        if (!File.Exists(filePath)) return new();
        var data = JsonSerializer.Deserialize<WorkspaceData>(File.ReadAllText(filePath)) ?? throw new InvalidDataException("Arquivo de notas inválido.");
        if (data.Tasks == null || data.Notes == null || data.Tasks.Any(t => t == null || t.Text == null)) throw new InvalidDataException("Conteúdo de notas inválido.");
        return data;
    }
    public static void Save(WorkspaceData data, string? filePath = null)
    {
        filePath ??= FilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var temporary = filePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        if (File.Exists(filePath)) File.Replace(temporary, filePath, filePath + ".bak");
        else File.Move(temporary, filePath);
    }
}
