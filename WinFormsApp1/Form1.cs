using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Management;

namespace WinFormsApp1;

public partial class Form1 : Form
{
    private readonly Color canvas = Color.FromArgb(14, 19, 30), surface = Color.FromArgb(23, 31, 46), muted = Color.FromArgb(155, 171, 193), accent = Color.FromArgb(87, 222, 184);
    private readonly Dictionary<string, Control> pages = new();
    private readonly Dictionary<string, Button> navigation = new();
    private readonly List<(Button Button, string Keywords)> tools = new();
    private readonly Panel content = new() { Dock = DockStyle.Fill };
    private readonly Label heading = new(), status = new();
    private readonly TextBox search = new();
    private readonly CancellationTokenSource lifetime = new();
    private CancellationTokenSource? pingCancellation;
    private RichTextBox networkLog = null!, systemLog = null!, maintenanceLog = null!;
    private TextBox host = null!, notes = null!;
    private CheckedListBox tasks = null!;
    private Label uptime = null!, disk = null!, taskSummary = null!;
    private readonly System.Windows.Forms.Timer clock = new() { Interval = 30000 };
    private readonly System.Windows.Forms.Timer saveTimer = new() { Interval = 700 };
    private WorkspaceData workspace = new();
    private bool dirty, working;

    public Form1()
    {
        InitializeComponent();
        using (var iconStream = typeof(Form1).Assembly.GetManifestResourceStream("QuickTools.Icon"))
        {
            if (iconStream != null)
            {
                using var appIcon = new Icon(iconStream, new Size(32, 32));
                Icon = (Icon)appIcon.Clone();
            }
        }
        Text = "QuickTools • Central de suporte";
        Font = new Font("Segoe UI", 10);
        BackColor = canvas; ForeColor = Color.FromArgb(231, 238, 248);
        ClientSize = new Size(1180, 780); MinimumSize = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterScreen;
        BuildShell();
        BuildDashboard(); BuildNetwork(); BuildSystem(); BuildCleanup(); BuildMaintenance(); BuildShortcuts(); BuildWorkspace();
        ShowPage("Visão geral");
        clock.Tick += (_, _) => RefreshOverview(); saveTimer.Tick += (_, _) => SaveWorkspace();
        clock.Start(); RefreshOverview();
        FormClosing += OnClosing;
        FormClosed += (_, _) => { clock.Dispose(); saveTimer.Dispose(); lifetime.Cancel(); lifetime.Dispose(); };
    }

    private Label Label(string text, float size = 10, Color? color = null) => new()
    {
        Text = text, AutoSize = true, ForeColor = color ?? ForeColor, BackColor = Color.Transparent,
        Font = new Font("Segoe UI", size, size >= 16 ? FontStyle.Bold : FontStyle.Regular), Margin = new Padding(0, 0, 0, 12)
    };

    private Button Button(string text, Action action, bool primary = false)
    {
        var b = new Button { Text = text, AutoSize = true, MinimumSize = new Size(145, 40), Padding = new Padding(12, 6, 12, 6),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? accent : surface, ForeColor = primary ? canvas : ForeColor,
            Cursor = Cursors.Hand, Margin = new Padding(0, 0, 10, 10), AccessibleName = text };
        b.FlatAppearance.BorderColor = Color.FromArgb(48, 64, 84); b.FlatAppearance.BorderSize = primary ? 0 : 1;
        b.Click += (_, _) => action(); return b;
    }

    private TextBox Input(string placeholder) => new() { PlaceholderText = placeholder, BackColor = surface, ForeColor = ForeColor,
        BorderStyle = BorderStyle.FixedSingle, Width = 270, Margin = new Padding(0, 4, 12, 12), AccessibleName = placeholder };
    private RichTextBox Output() => new() { Dock = DockStyle.Fill, BackColor = surface, ForeColor = ForeColor, BorderStyle = BorderStyle.None,
        ReadOnly = true, Font = new Font("Consolas", 10), DetectUrls = false, WordWrap = true, AccessibleName = "Resultados" };
    private FlowLayoutPanel Row() => new() { AutoSize = true, Dock = DockStyle.Top, WrapContents = true, Margin = Padding.Empty };

    private TableLayoutPanel Page(string name, string description)
    {
        var page = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(28, 16, 28, 24), BackColor = canvas };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.AutoSize)); page.RowStyles.Add(new RowStyle(SizeType.AutoSize)); page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var caption = Label(description, 10, muted); caption.MaximumSize = new Size(680, 0); page.Controls.Add(caption, 0, 0);
        pages.Add(name, page); content.Controls.Add(page); page.Hide(); return page;
    }

    private void BuildShell()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(shell);
        var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = surface, Padding = new Padding(20, 26, 12, 20), ColumnCount = 1, RowCount = 4 };
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 94)); sidebar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        sidebar.Controls.Add(Label("QUICKTOOLS\nSeu kit de suporte", 18, accent), 0, 0);
        var nav = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        foreach (var name in new[] { "Visão geral", "Rede", "Sistema", "Limpeza", "Manutenção", "Ferramentas", "Meu espaço" })
        {
            var button = Button(name, () => ShowPage(name)); button.Width = 182; button.Height = 46; button.TextAlign = ContentAlignment.MiddleLeft;
            navigation.Add(name, button); nav.Controls.Add(button);
        }
        sidebar.Controls.Add(nav, 0, 1);
        sidebar.Controls.Add(Label(IsAdministrator() ? "● Administrador\nPronto para manutenção" : "● Modo padrão\nElevação quando necessário", 9, muted), 0, 3);
        shell.Controls.Add(sidebar, 0, 0);
        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = Padding.Empty };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 86)); main.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); main.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(28, 20, 28, 0) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        heading.AutoSize = true; heading.Font = new Font("Segoe UI", 24, FontStyle.Bold);
        search.PlaceholderText = "Buscar ferramentas…  Ctrl+K"; search.Dock = DockStyle.Top; search.BackColor = surface; search.ForeColor = ForeColor;
        search.AccessibleName = "Buscar ferramentas"; search.Margin = new Padding(0, 9, 0, 0);
        search.TextChanged += (_, _) => { ShowPage("Ferramentas"); FilterTools(); };
        header.Controls.Add(heading, 0, 0); header.Controls.Add(search, 1, 0);
        status.Text = "Pronto  •  " + Environment.MachineName; status.ForeColor = muted; status.Dock = DockStyle.Fill; status.Padding = new Padding(28, 5, 0, 0);
        main.Controls.Add(header, 0, 0); main.Controls.Add(content, 0, 1); main.Controls.Add(status, 0, 2); shell.Controls.Add(main, 1, 0);
        KeyPreview = true; KeyDown += (_, e) => { if (e.Control && e.KeyCode == Keys.K) { search.Focus(); search.SelectAll(); e.SuppressKeyPress = true; } };
    }

    private void ShowPage(string name)
    {
        if (!pages.ContainsKey(name)) return;
        foreach (var page in pages) page.Value.Visible = page.Key == name;
        foreach (var item in navigation) { item.Value.BackColor = item.Key == name ? accent : surface; item.Value.ForeColor = item.Key == name ? canvas : ForeColor; }
        heading.Text = name;
    }

    private void BuildDashboard()
    {
        var page = Page("Visão geral", DateTime.Now.ToString("dddd, dd 'de' MMMM", new System.Globalization.CultureInfo("pt-BR")) + "  •  Tudo pronto para o próximo atendimento.");
        var body = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
        var hero = new SurfacePanel { Highlight = true, Width = 680, Height = 124, BackColor = surface, Padding = new Padding(22), Margin = new Padding(0, 12, 0, 24) };
        var intro = Label("Menos cliques. Mais soluções.", 23); intro.Location = new Point(22, 20);
        var subtitle = Label("Diagnóstico, manutenção e organização em um só lugar.", 11, muted); subtitle.Location = new Point(22, 72);
        hero.Controls.AddRange(new Control[] { intro, subtitle }); body.Controls.Add(hero);
        var stats = Row(); stats.Dock = DockStyle.None; uptime = Stat(stats, "TEMPO LIGADO"); disk = Stat(stats, "DISCO DO WINDOWS"); taskSummary = Stat(stats, "MINHAS TAREFAS"); body.Controls.Add(stats);
        body.Controls.Add(Label("Comece por aqui", 17));
        var actions = Row(); actions.Dock = DockStyle.None; actions.Controls.Add(Button("Diagnosticar rede", () => ShowPage("Rede"), true)); actions.Controls.Add(Button("Ver hardware", () => ShowPage("Sistema"))); actions.Controls.Add(Button("Organizar meu dia", () => ShowPage("Meu espaço"))); body.Controls.Add(actions);
        body.Controls.Add(Label("ROTINA DE SUPORTE", 10, accent));
        body.Controls.Add(Label("01   Confira a conexão e os adaptadores de rede.\n\n02   Consulte o hardware e exporte o diagnóstico.\n\n03   Registre pendências e próximos passos no Meu espaço.", 11, muted));
        page.Controls.Add(body, 0, 2);
    }

    private Label Stat(FlowLayoutPanel row, string title)
    {
        var card = new SurfacePanel { Width = 215, Height = 112, BackColor = surface, Margin = new Padding(0, 0, 14, 24) };
        var caption = Label(title, 9, muted); caption.Location = new Point(16, 16);
        var value = Label("—", 18); value.Location = new Point(16, 46); card.Controls.AddRange(new Control[] { caption, value }); row.Controls.Add(card); return value;
    }

    private void RefreshOverview()
    {
        var elapsed = TimeSpan.FromMilliseconds(Environment.TickCount64); uptime.Text = $"{elapsed.Days}d {elapsed.Hours}h {elapsed.Minutes}m";
        try { var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)!); disk.Text = $"{drive.AvailableFreeSpace / 1073741824d:0.0} GB livres"; } catch { disk.Text = "Indisponível"; }
        taskSummary.Text = $"{workspace.Tasks.Count(t => !t.Done)} pendentes";
    }

    private void BuildNetwork()
    {
        var page = Page("Rede", "Diagnóstico de conectividade • Informe um domínio ou IP, sem http:// ou caminhos.");
        var actions = Row(); host = Input("Host ou IP"); host.Text = "1.1.1.1"; actions.Controls.Add(host);
        var pingButton = Button("Iniciar ping", () => { }, true); pingButton.Click += async (_, _) => await TogglePing(pingButton); actions.Controls.Add(pingButton);
        actions.Controls.Add(Button("Consultar DNS", () => Run(async () => { var addresses = await Dns.GetHostAddressesAsync(ValidHost(), lifetime.Token); Log(networkLog, string.Join(Environment.NewLine, addresses.Select(a => a.ToString()))); })));
        var port = new NumericUpDown { Minimum = 1, Maximum = 65535, Value = 443, Width = 80, BackColor = surface, ForeColor = ForeColor, AccessibleName = "Porta TCP", Margin = new Padding(0, 4, 12, 12) }; actions.Controls.Add(port);
        actions.Controls.Add(Button("Testar porta", () => Run(async () => { var target = ValidHost(); var number = (int)port.Value; using var client = new TcpClient(); using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); timeout.CancelAfter(5000); await client.ConnectAsync(target, number, timeout.Token); Log(networkLog, $"{target}:{number} • conexão TCP aceita."); })));
        actions.Controls.Add(Button("Adaptadores / IP", () => Run(() => { Log(networkLog, NetworkSummary()); return Task.CompletedTask; })));
        actions.Controls.Add(Button("Limpar DNS", () => Run(async () => { if (RequireAdmin()) await Execute("ipconfig.exe", new[] { "/flushdns" }, networkLog); })));
        actions.Controls.Add(Button("Resetar rede", () => Run(async () => { if (!Confirm("A conexão será interrompida. Liberar e renovar o endereço IP?")) return; if (!RequireAdmin()) return; await Execute("ipconfig.exe", new[] { "/release" }, networkLog); await Execute("ipconfig.exe", new[] { "/renew" }, networkLog); await Execute("ipconfig.exe", new[] { "/flushdns" }, networkLog); })));
        actions.Controls.Add(Button("Exportar log", () => Export(networkLog.Text, "rede")));
        networkLog = Output(); page.Controls.Add(actions, 0, 1); page.Controls.Add(networkLog, 0, 2);
    }

    private string ValidHost()
    {
        var value = host.Text.Trim();
        if (Uri.CheckHostName(value) == UriHostNameType.Unknown) throw new ArgumentException("Informe um domínio ou endereço IP válido.");
        return value;
    }

    private async Task TogglePing(Button button)
    {
        if (pingCancellation != null) { pingCancellation.Cancel(); return; }
        string target; try { target = ValidHost(); } catch (Exception ex) { Error(ex); return; }
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); pingCancellation = cancellation;
        button.Text = "Parar ping"; host.Enabled = false;
        int sent = 0, received = 0; long total = 0; using var ping = new Ping();
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                sent++;
                try { var reply = await ping.SendPingAsync(target, 1500); cancellation.Token.ThrowIfCancellationRequested();
                    if (reply.Status == IPStatus.Success) { received++; total += reply.RoundtripTime; }
                    Log(networkLog, reply.Status == IPStatus.Success ? $"{reply.Address} • {reply.RoundtripTime} ms" : $"{target} • {reply.Status}");
                } catch (PingException ex) { Log(networkLog, ex.Message); }
                await Task.Delay(1000, cancellation.Token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!IsDisposed) Error(ex); }
        finally { pingCancellation = null; if (!IsDisposed) { button.Text = "Iniciar ping"; host.Enabled = true; Log(networkLog, $"Finalizado • {received}/{sent} respostas • média {(received == 0 ? "—" : (total / received) + " ms")}"); } }
    }

    private static string NetworkSummary() => string.Join("\r\n\r\n", NetworkInterface.GetAllNetworkInterfaces().Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback).Select(n =>
    {
        var ip = n.GetIPProperties();
        return $"{n.Name} • {n.OperationalStatus}\r\nIP: {string.Join(", ", ip.UnicastAddresses.Select(a => a.Address))}\r\nGateway: {string.Join(", ", ip.GatewayAddresses.Select(a => a.Address))}\r\nDNS: {string.Join(", ", ip.DnsAddresses)}";
    }));

    private void BuildSystem()
    {
        var page = Page("Sistema", "Inventário do computador • Gere um relatório para anexar ao atendimento."); var actions = Row();
        actions.Controls.Add(Button("Atualizar diagnóstico", () => Run(async () => { var info = await SysInfoManager.GetInfoAsync(); systemLog.Text = $"QUICKTOOLS • {DateTime.Now:g}\r\n\r\nCOMPUTADOR: {info.Hostname}\r\nMODELO: {info.Model}\r\nSERIAL: {info.SerialNumber}\r\nWINDOWS: {info.OSName}\r\nCPU: {info.CPU}\r\nMEMÓRIA: {info.RAM}\r\nGPU: {info.GPU}\r\nTEMPO LIGADO: {TimeSpan.FromMilliseconds(Environment.TickCount64):g}\r\n\r\nARMAZENAMENTO\r\n{info.DiskInfo}\r\nREDE\r\n{NetworkSummary()}"; }), true));
        actions.Controls.Add(Button("Exportar relatório", () => Export(systemLog.Text, "diagnostico")));
        systemLog = Output(); page.Controls.Add(actions, 0, 1); page.Controls.Add(systemLog, 0, 2);
    }

    private void BuildCleanup()
    {
        var page = Page("Limpeza", "Temporários com mais de 24 horas • Arquivos recentes, bloqueados e links são preservados.");
        var actions = Row(); var result = Output(); IReadOnlyList<SystemCleaner.Candidate> candidates = Array.Empty<SystemCleaner.Candidate>();
        var clean = Button("Limpar itens analisados", () => { }); clean.Enabled = false;
        actions.Controls.Add(Button("Analisar temporários", () => Run(async () => { clean.Enabled = false; candidates = await SystemCleaner.ScanAsync(); result.Text = $"{candidates.Count} arquivos elegíveis\r\n{candidates.Sum(f => f.Size) / 1048576d:0.00} MB podem ser liberados.\r\n\r\nLocais analisados:\r\n{string.Join("\r\n", SystemCleaner.GetCleanLocations())}\r\n\r\nPastas sem acesso são ignoradas. A limpeza usa esta análise e verifica novamente cada arquivo."; clean.Enabled = candidates.Count > 0; }), true));
        clean.Click += (_, _) => Run(async () => { if (!Confirm($"Excluir permanentemente {candidates.Count} arquivos temporários da análise?")) return; clean.Enabled = false; var summary = await SystemCleaner.CleanAsync(candidates); result.Text = $"Limpeza finalizada\r\n\r\n{summary.FilesDeleted} arquivos excluídos\r\n{summary.BytesFreed / 1048576d:0.00} MB liberados\r\n{summary.Errors} itens preservados ou inacessíveis"; RefreshOverview(); });
        actions.Controls.Add(clean); page.Controls.Add(actions, 0, 1); page.Controls.Add(result, 0, 2);
    }

    private void BuildMaintenance()
    {
        var page = Page("Manutenção", "Reparos do Windows • Requer administrador. As operações podem levar vários minutos."); var actions = Row();
        actions.Controls.Add(Button("Verificar arquivos • SFC", () => Run(async () => { if (RequireAdmin() && Confirm("Iniciar verificação e reparo dos arquivos do Windows?")) await Execute("sfc.exe", new[] { "/scannow" }, maintenanceLog); })));
        actions.Controls.Add(Button("Reparar imagem • DISM", () => Run(async () => { if (RequireAdmin() && Confirm("Iniciar reparo da imagem do Windows? Pode exigir internet.")) await Execute("dism.exe", new[] { "/online", "/cleanup-image", "/restorehealth" }, maintenanceLog); })));
        actions.Controls.Add(Button("Reiniciar impressão", () => Run(async () => { if (!RequireAdmin() || !Confirm("Reiniciar o serviço de impressão? Trabalhos serão interrompidos; a fila será preservada.")) return; await Execute("net.exe", new[] { "stop", "spooler" }, maintenanceLog); await Execute("net.exe", new[] { "start", "spooler" }, maintenanceLog); })));
        actions.Controls.Add(Button("Exportar log", () => Export(maintenanceLog.Text, "manutencao")));
        maintenanceLog = Output(); page.Controls.Add(actions, 0, 1); page.Controls.Add(maintenanceLog, 0, 2);
    }

    private void BuildShortcuts()
    {
        var page = Page("Ferramentas", "Seu acesso rápido ao Windows • Use a busca no topo para encontrar uma ferramenta.");
        var grid = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
        void Add(string title, string keywords, Action action) { var b = Button(title, action); b.Size = new Size(220, 70); b.AutoSize = false; tools.Add((b, title + " " + keywords)); grid.Controls.Add(b); }
        Add("Adaptadores de rede", "ip conexão", () => Launch("control.exe", "ncpa.cpl"));
        Add("Aplicativos instalados", "remover programas", () => Launch("control.exe", "appwiz.cpl"));
        Add("Gerenciador de dispositivos", "hardware driver", () => Launch("devmgmt.msc"));
        Add("Gerenciador de tarefas", "processos desempenho", () => Launch("taskmgr.exe"));
        Add("Serviços do Windows", "serviço", () => Launch("services.msc"));
        Add("Visualizador de eventos", "logs erros", () => Launch("eventvwr.msc"));
        Add("Área de trabalho remota", "rdp acesso", () => Launch("mstsc.exe"));
        Add("Windows Update", "atualização", () => Launch("ms-settings:windowsupdate"));
        Add("Captura de tela", "recorte screenshot", () => Launch("ms-screenclip:"));
        Add("Calculadora", "contas", () => Launch("calc.exe"));
        Add("Relatório de bateria", "notebook saúde", () => Run(async () => { using var dialog = new SaveFileDialog { Filter = "Relatório HTML|*.html", FileName = "bateria.html" }; if (dialog.ShowDialog(this) != DialogResult.OK) return; await Execute("powercfg.exe", new[] { "/batteryreport", "/output", dialog.FileName }, maintenanceLog); Launch(dialog.FileName); }));
        Add("Chave OEM do Windows", "licença bios", () => Run(async () => { var key = await Task.Run(() => { using var query = new ManagementObjectSearcher("SELECT OA3xOriginalProductKey FROM SoftwareLicensingService"); using var results = query.Get(); foreach (ManagementObject item in results) { using (item) { var value = item["OA3xOriginalProductKey"]?.ToString(); if (!string.IsNullOrWhiteSpace(value)) return value; } } return "Nenhuma chave OEM disponível. A licença pode ser digital ou Retail."; }); MessageBox.Show(this, key, "Licença OEM", MessageBoxButtons.OK, MessageBoxIcon.Information); }));
        Add("Verificar arquivo • SHA-256", "hash integridade checksum", () => Run(async () => { using var dialog = new OpenFileDialog(); if (dialog.ShowDialog(this) != DialogResult.OK) return; using var stream = File.OpenRead(dialog.FileName); var hash = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(stream, lifetime.Token)); ShowPage("Sistema"); systemLog.Text = $"SHA-256\r\n{Path.GetFileName(dialog.FileName)}\r\n\r\n{hash}"; }));
        page.Controls.Add(grid, 0, 2);
    }

    private void FilterTools()
    {
        var compare = System.Globalization.CultureInfo.GetCultureInfo("pt-BR").CompareInfo; var count = 0;
        foreach (var tool in tools) { var match = compare.IndexOf(tool.Keywords, search.Text.Trim(), System.Globalization.CompareOptions.IgnoreCase | System.Globalization.CompareOptions.IgnoreNonSpace) >= 0; tool.Button.Visible = match; if (match) count++; }
        status.Text = count > 0 ? $"{count} ferramentas encontradas" : "Nenhuma ferramenta encontrada. Tente outro termo.";
    }

    private void BuildWorkspace()
    {
        string? loadError = null; try { workspace = WorkspaceStore.Load(); } catch (Exception ex) { loadError = ex.Message; }
        var page = Page("Meu espaço", "Tarefas e notas locais • Salvas automaticamente para o seu usuário do Windows.");
        var actions = Row(); var entry = Input("Nova tarefa de suporte"); actions.Controls.Add(entry);
        void AddTask() { if (string.IsNullOrWhiteSpace(entry.Text)) return; var item = new WorkItem { Text = entry.Text.Trim() }; workspace.Tasks.Add(item); tasks.Items.Add(item, false); entry.Clear(); Changed(); }
        actions.Controls.Add(Button("Adicionar tarefa", AddTask, true));
        entry.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { AddTask(); e.SuppressKeyPress = true; } };
        actions.Controls.Add(Button("Remover selecionada", () => { if (tasks.SelectedIndex < 0) return; workspace.Tasks.RemoveAt(tasks.SelectedIndex); tasks.Items.RemoveAt(tasks.SelectedIndex); Changed(); }));
        var columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48)); columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52)); columns.RowStyles.Add(new RowStyle(SizeType.AutoSize)); columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        columns.Controls.Add(Label("Checklist do dia", 16), 0, 0); columns.Controls.Add(Label("Bloco de notas", 16), 1, 0);
        tasks = new CheckedListBox { Dock = DockStyle.Fill, BackColor = surface, ForeColor = ForeColor, BorderStyle = BorderStyle.None, CheckOnClick = true, IntegralHeight = false, HorizontalScrollbar = true, Margin = new Padding(0, 0, 16, 0), AccessibleName = "Checklist do dia" };
        foreach (var task in workspace.Tasks) tasks.Items.Add(task, task.Done);
        tasks.ItemCheck += (_, e) => { workspace.Tasks[e.Index].Done = e.NewValue == CheckState.Checked; Changed(); };
        notes = Input("Anotações do atendimento…"); notes.Multiline = true; notes.AcceptsReturn = true; notes.ScrollBars = ScrollBars.Vertical; notes.Dock = DockStyle.Fill; notes.Text = workspace.Notes;
        notes.TextChanged += (_, _) => { workspace.Notes = notes.Text; Changed(); };
        columns.Controls.Add(tasks, 0, 1); columns.Controls.Add(notes, 1, 1); page.Controls.Add(actions, 0, 1); page.Controls.Add(columns, 0, 2);
        if (loadError != null) Shown += (_, _) => MessageBox.Show(this, "Não foi possível carregar as notas. O arquivo original será preservado em backup ao salvar.\n" + loadError, "Meu espaço");
    }

    private void Changed() { dirty = true; saveTimer.Stop(); saveTimer.Start(); RefreshOverview(); }
    private bool SaveWorkspace()
    {
        saveTimer.Stop(); if (!dirty) return true;
        try { WorkspaceStore.Save(workspace); dirty = false; status.Text = "Meu espaço salvo • " + DateTime.Now.ToString("HH:mm"); return true; }
        catch (Exception ex) { status.Text = "Falha ao salvar notas: " + ex.Message; return false; }
    }
    private void OnClosing(object? sender, FormClosingEventArgs e)
    {
        if (working) { MessageBox.Show(this, "Aguarde a operação atual terminar antes de fechar.", "Operação em andamento"); e.Cancel = true; return; }
        if (!SaveWorkspace()) { MessageBox.Show(this, "Não foi possível salvar. Copie suas notas antes de fechar ou tente novamente.", "Falha ao salvar"); e.Cancel = true; return; }
        pingCancellation?.Cancel();
    }
    private async void Run(Func<Task> operation)
    {
        if (working) { status.Text = "Aguarde a operação atual terminar."; return; }
        working = true; UseWaitCursor = true; status.Text = "Operação em andamento…";
        try { await operation(); if (!IsDisposed) status.Text = "Pronto • " + DateTime.Now.ToString("HH:mm"); }
        catch (OperationCanceledException) { if (!IsDisposed) status.Text = "Operação cancelada ou tempo limite atingido."; }
        catch (Exception ex) { if (!IsDisposed) Error(ex); }
        finally { working = false; if (!IsDisposed) UseWaitCursor = false; }
    }
    private async Task Execute(string file, string[] arguments, RichTextBox output)
    {
        Log(output, "Executando " + file + " " + string.Join(" ", arguments));
        var code = await CommandExecutor.RunAsync(file, arguments, new Progress<string>(text => Log(output, text)), lifetime.Token);
        Log(output, "Código de saída: " + code);
        if (code != 0) throw new InvalidOperationException($"{file} terminou com código {code}. Consulte o log da operação.");
    }
    private void Log(RichTextBox output, string text)
    {
        if (output.IsDisposed) return;
        if (output.TextLength > 120000) output.Text = output.Text[^60000..];
        output.AppendText($"[{DateTime.Now:HH:mm:ss}] {text.TrimEnd()}\r\n"); output.SelectionStart = output.TextLength; output.ScrollToCaret();
    }
    private void Export(string text, string name)
    {
        if (string.IsNullOrWhiteSpace(text)) { status.Text = "Gere resultados antes de exportar."; return; }
        using var dialog = new SaveFileDialog { Filter = "Arquivo de texto|*.txt", FileName = $"quicktools-{name}-{DateTime.Now:yyyyMMdd-HHmm}.txt" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try { File.WriteAllText(dialog.FileName, text, System.Text.Encoding.UTF8); status.Text = "Relatório exportado."; } catch (Exception ex) { Error(ex); }
    }
    private void Launch(string file, params string[] args)
    {
        try { var start = new ProcessStartInfo(file) { UseShellExecute = true }; foreach (var arg in args) start.ArgumentList.Add(arg); Process.Start(start)?.Dispose(); } catch (Exception ex) { Error(ex); }
    }
    private void Error(Exception ex) { status.Text = "Não foi possível concluir a operação."; MessageBox.Show(this, ex.Message, "QuickTools", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    private bool Confirm(string text) => MessageBox.Show(this, text, "Confirmar operação", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    private static bool IsAdministrator() { using var identity = WindowsIdentity.GetCurrent(); return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator); }
    private bool RequireAdmin()
    {
        if (IsAdministrator()) return true;
        if (Confirm("Esta ferramenta precisa de administrador. Salvar notas e reabrir com elevação?"))
        {
            if (!SaveWorkspace()) throw new IOException("Não foi possível salvar as notas antes de reiniciar.");
            Process.Start(new ProcessStartInfo(Application.ExecutablePath) { UseShellExecute = true, Verb = "runas" })?.Dispose();
            working = false; Close();
        }
        return false;
    }
}
