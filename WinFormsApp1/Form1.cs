using System;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO; // Necessário para ler arquivos (Bateria)

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        // Controles Globais
        private TabControl tabs;

        // Controles - Aba Rede
        private TextBox txtPingHost;
        private Button btnPing;
        private Button btnFlushDNS;
        private RichTextBox rtbNetLog;
        private CancellationTokenSource _pingToken;

        // Controles - Aba Sistema
        private Button btnSysInfo;
        private TextBox txtSysResult;

        // Controles - Aba Limpeza
        private Button btnAnalyze, btnClean;
        private Label lblCleanStatus;
        private ProgressBar progClean;

        // Controles - Aba Manutenção
        private Button btnSFC;
        private Button btnDism;
        private Button btnSpooler;
        private RichTextBox rtbConsole;

        // Controles - Aba Extras (NOVA)
        private Button btnRedeCpl;
        private Button btnAppCpl;
        private Button btnDevMgmt;
        private Button btnBattery;
        private Button btnProductKey;
        private TextBox txtExtrasLog;

        public Form1()
        {
            InitializeComponent();
            BuildInterface();
            CheckAdminStatus();

            try { txtPingHost.Text = GetLocalIPAddress(); }
            catch { txtPingHost.Text = "127.0.0.1"; }
        }

        // --- AUXILIARES ---
        private string GetLocalIPAddress()
        {
            string localIP = "127.0.0.1";
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    localIP = endPoint.Address.ToString();
                }
            }
            catch { }
            return localIP;
        }

        private string GetUptime()
        {
            try
            {
                TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
                return $"{uptime.Days} dias, {uptime.Hours} horas, {uptime.Minutes} min";
            }
            catch { return "N/A"; }
        }

        // --- 1. CONSTRUÇÃO DA INTERFACE ---
        private void BuildInterface()
        {
            this.Text = "IT QuickTools - Ultimate Edition"; // Nome atualizado ;)
            this.Size = new Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            tabs = new TabControl { Dock = DockStyle.Fill };

            // === ABA 1: REDE ===
            TabPage tabNet = new TabPage("Rede");
            Label lblH = new Label { Text = "Host/IP:", Location = new Point(10, 15), AutoSize = true };
            txtPingHost = new TextBox { Text = "...", Location = new Point(70, 12), Width = 150 };
            btnPing = new Button { Text = "Ping Contínuo", Location = new Point(230, 10), Width = 120 };
            btnFlushDNS = new Button { Text = "Resetar Rede / DNS", Location = new Point(360, 10), Width = 150 };
            rtbNetLog = new RichTextBox { Location = new Point(10, 50), Size = new Size(560, 380), BackColor = Color.WhiteSmoke, ReadOnly = true, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

            btnPing.Click += BtnPing_Click;
            btnFlushDNS.Click += BtnFlushDNS_Click;

            tabNet.Controls.Add(lblH); tabNet.Controls.Add(txtPingHost); tabNet.Controls.Add(btnPing); tabNet.Controls.Add(btnFlushDNS); tabNet.Controls.Add(rtbNetLog);

            // === ABA 2: SISTEMA ===
            TabPage tabSys = new TabPage("Sistema");
            btnSysInfo = new Button { Text = "Ler Hardware (WMI)", Location = new Point(10, 10), Width = 150, Height = 30 };
            txtSysResult = new TextBox { Location = new Point(10, 50), Size = new Size(560, 380), Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = new Font("Consolas", 10) };

            btnSysInfo.Click += BtnSysInfo_Click;
            tabSys.Controls.Add(btnSysInfo); tabSys.Controls.Add(txtSysResult);

            // === ABA 3: LIMPEZA ===
            TabPage tabClean = new TabPage("Limpeza");
            btnAnalyze = new Button { Text = "Analisar Lixo", Location = new Point(10, 20), Width = 120 };
            btnClean = new Button { Text = "LIMPAR TUDO", Location = new Point(140, 20), Width = 120, Enabled = false };
            lblCleanStatus = new Label { Text = "Aguardando análise...", Location = new Point(10, 60), AutoSize = true };
            progClean = new ProgressBar { Location = new Point(10, 90), Width = 560, Height = 20 };

            btnAnalyze.Click += BtnAnalyze_Click;
            btnClean.Click += BtnClean_Click;
            tabClean.Controls.Add(btnAnalyze); tabClean.Controls.Add(btnClean); tabClean.Controls.Add(lblCleanStatus); tabClean.Controls.Add(progClean);

            // === ABA 4: MANUTENÇÃO ===
            TabPage tabAdmin = new TabPage("Manutenção");
            btnSFC = new Button { Text = "SFC /Scannow", Location = new Point(10, 10), Width = 120, Height = 30 };
            btnDism = new Button { Text = "Reparar (DISM)", Location = new Point(140, 10), Width = 150, Height = 30 };
            btnSpooler = new Button { Text = "Resetar Impressora", Location = new Point(300, 10), Width = 140, Height = 30 };
            rtbConsole = new RichTextBox { Location = new Point(10, 50), Size = new Size(560, 380), BackColor = Color.Black, ForeColor = Color.LimeGreen, Font = new Font("Consolas", 10), ReadOnly = true };

            btnSFC.Click += BtnSFC_Click;
            btnDism.Click += BtnDism_Click;
            btnSpooler.Click += BtnSpooler_Click;
            tabAdmin.Controls.Add(btnSFC); tabAdmin.Controls.Add(btnDism); tabAdmin.Controls.Add(btnSpooler); tabAdmin.Controls.Add(rtbConsole);

            // === ABA 5: EXTRAS (NOVA) ===
            TabPage tabExtras = new TabPage("Atalhos & Extras");

            // Grupo de Botões de Atalho
            Label lblShort = new Label { Text = "Atalhos Rápidos:", Location = new Point(10, 15), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };

            btnRedeCpl = new Button { Text = "Adaptadores de Rede", Location = new Point(10, 40), Width = 180, Height = 30 };
            btnAppCpl = new Button { Text = "Remover Programas", Location = new Point(200, 40), Width = 180, Height = 30 };
            btnDevMgmt = new Button { Text = "Gerenciador Dispositivos", Location = new Point(390, 40), Width = 180, Height = 30 };

            Label lblUtils = new Label { Text = "Utilitários:", Location = new Point(10, 90), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };

            btnBattery = new Button { Text = "Relatório de Bateria", Location = new Point(10, 115), Width = 180, Height = 30 };
            btnProductKey = new Button { Text = "Ver Chave Win (BIOS)", Location = new Point(200, 115), Width = 180, Height = 30 };

            txtExtrasLog = new TextBox { Location = new Point(10, 160), Size = new Size(560, 270), Multiline = true, ReadOnly = true, Font = new Font("Consolas", 10) };
            txtExtrasLog.Text = "Área de resultados dos utilitários...";

            // Eventos Extras
            btnRedeCpl.Click += (s, e) => Process.Start("control", "ncpa.cpl");
            btnAppCpl.Click += (s, e) => Process.Start("control", "appwiz.cpl");
            btnDevMgmt.Click += (s, e) => { ProcessStartInfo psi = new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true }; Process.Start(psi); };

            btnBattery.Click += BtnBattery_Click;
            btnProductKey.Click += BtnProductKey_Click;

            tabExtras.Controls.Add(lblShort);
            tabExtras.Controls.Add(btnRedeCpl); tabExtras.Controls.Add(btnAppCpl); tabExtras.Controls.Add(btnDevMgmt);
            tabExtras.Controls.Add(lblUtils);
            tabExtras.Controls.Add(btnBattery); tabExtras.Controls.Add(btnProductKey);
            tabExtras.Controls.Add(txtExtrasLog);

            // Adiciona todas as abas
            tabs.TabPages.AddRange(new TabPage[] { tabNet, tabSys, tabClean, tabAdmin, tabExtras });
            this.Controls.Add(tabs);
        }

        private void CheckAdminStatus()
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                bool isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                if (isAdmin) this.Text += " (ADMINISTRADOR)";
                else this.Text += " (Modo Usuário)";
            }
        }

        // --- EVENTOS DE EXTRAS (NOVO) ---

        private async void BtnBattery_Click(object sender, EventArgs e)
        {
            txtExtrasLog.Text = "Gerando relatório de bateria (powercfg /batteryreport)...";
            btnBattery.Enabled = false;

            await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "cmd.exe";
                    psi.Arguments = "/c powercfg /batteryreport /output \"battery_report.html\"";
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    Process.Start(psi).WaitForExit();

                    // Abre o arquivo gerado
                    if (File.Exists("battery_report.html"))
                    {
                        ProcessStartInfo openInfo = new ProcessStartInfo("battery_report.html") { UseShellExecute = true };
                        Process.Start(openInfo);
                    }
                }
                catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
            });

            txtExtrasLog.Text += "\r\nRelatório gerado e aberto no navegador!";
            btnBattery.Enabled = true;
        }

        private async void BtnProductKey_Click(object sender, EventArgs e)
        {
            txtExtrasLog.Text = "Buscando chave OEM na BIOS...";
            btnProductKey.Enabled = false;

            await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo();
                    psi.FileName = "wmic";
                    psi.Arguments = "path softwarelicensingservice get OA3xOriginalProductKey";
                    psi.RedirectStandardOutput = true;
                    psi.UseShellExecute = false;
                    psi.CreateNoWindow = true;

                    var proc = Process.Start(psi);
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit();

                    // Limpa o resultado para mostrar apenas a chave
                    string key = output.Replace("OA3xOriginalProductKey", "").Trim();

                    this.Invoke((Action)(() =>
                    {
                        if (string.IsNullOrWhiteSpace(key))
                            txtExtrasLog.Text = "Nenhuma chave OEM encontrada na BIOS (Talvez seja licença digital ou Retail).";
                        else
                            txtExtrasLog.Text = "🔑 CHAVE DO WINDOWS ENCONTRADA:\r\n\r\n" + key;
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke((Action)(() => txtExtrasLog.Text = "Erro: " + ex.Message));
                }
            });
            btnProductKey.Enabled = true;
        }


        // --- DEMAIS EVENTOS (MANTIDOS DO ANTERIOR) ---

        private async void BtnPing_Click(object sender, EventArgs e)
        {
            if (_pingToken != null) { _pingToken.Cancel(); _pingToken = null; btnPing.Text = "Ping Contínuo"; return; }
            btnPing.Text = "PARAR";
            _pingToken = new CancellationTokenSource();
            Ping ping = new Ping();
            try
            {
                while (!_pingToken.Token.IsCancellationRequested)
                {
                    try
                    {
                        var reply = await ping.SendPingAsync(txtPingHost.Text, 1000);
                        AppendLog(rtbNetLog, $"Resposta de {reply.Address}: bytes={reply.Buffer.Length} tempo={reply.RoundtripTime}ms", reply.Status == IPStatus.Success ? Color.Green : Color.Red);
                    }
                    catch (Exception ex) { AppendLog(rtbNetLog, "Erro: " + ex.Message, Color.Red); }
                    await Task.Delay(1000, _pingToken.Token);
                }
            }
            catch (TaskCanceledException) { AppendLog(rtbNetLog, "Ping Parado.", Color.Orange); }
        }

        private async void BtnFlushDNS_Click(object sender, EventArgs e)
        {
            btnFlushDNS.Enabled = false;
            AppendLog(rtbNetLog, ">>> INICIANDO RESET DE REDE...", Color.Blue);
            await Task.Run(() =>
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/C ipconfig /release && ipconfig /renew && ipconfig /flushdns");
                    psi.CreateNoWindow = true; psi.UseShellExecute = false;
                    Process.Start(psi).WaitForExit();
                }
                catch (Exception ex) { MessageBox.Show("Erro: " + ex.Message); }
            });
            txtPingHost.Text = GetLocalIPAddress();
            AppendLog(rtbNetLog, ">>> REDE RESETADA.", Color.Blue);
            MessageBox.Show("Rede reiniciada!");
            btnFlushDNS.Enabled = true;
        }

        private void AppendLog(RichTextBox rtb, string msg, Color c)
        {
            rtb.SelectionStart = rtb.TextLength; rtb.SelectionLength = 0; rtb.SelectionColor = c;
            rtb.AppendText($"{DateTime.Now:HH:mm:ss} > {msg}\r\n"); rtb.ScrollToCaret();
        }

        private async void BtnSysInfo_Click(object sender, EventArgs e)
        {
            btnSysInfo.Enabled = false; txtSysResult.Text = "Carregando WMI...";
            try
            {
                var info = await SysInfoManager.GetInfoAsync();
                txtSysResult.Text = $"⏱️ TEMPO LIGADO: {GetUptime()}\r\n----------------------------------------\r\n🖥️ NOME PC: {info.Hostname}\r\nMOD: {info.Model}\r\nSN: {info.SerialNumber}\r\nCPU: {info.CPU}\r\nRAM: {info.RAM}\r\nGPU: {info.GPU}\r\nOS: {info.OSName}\r\n\r\nDISCOS:\r\n{info.DiskInfo}";
            }
            catch (Exception ex) { txtSysResult.Text = "Erro: " + ex.Message; }
            btnSysInfo.Enabled = true;
        }

        private async void BtnAnalyze_Click(object sender, EventArgs e)
        {
            lblCleanStatus.Text = "Calculando...";
            long bytes = await SystemCleaner.AnalyzeSpaceAsync();
            lblCleanStatus.Text = $"Lixo Encontrado: {bytes / 1024 / 1024} MB";
            btnClean.Enabled = true;
        }

        private async void BtnClean_Click(object sender, EventArgs e)
        {
            progClean.Style = ProgressBarStyle.Marquee;
            var progress = new Progress<string>(s => lblCleanStatus.Text = s);
            var res = await SystemCleaner.RunCleaningAsync(progress);
            progClean.Style = ProgressBarStyle.Blocks;
            MessageBox.Show($"Limpeza concluída!\nLibertado: {res.BytesFreed / 1024 / 1024} MB");
            lblCleanStatus.Text = "Limpo.";
        }

        private bool IsAdministrator()
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }

        private void RestartAsAdmin()
        {
            var proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            proc.FileName = Application.ExecutablePath;
            proc.Verb = "runas";
            try { Process.Start(proc); Application.Exit(); }
            catch { MessageBox.Show("Cancelado."); }
        }

        private async void BtnSFC_Click(object sender, EventArgs e)
        {
            if (!IsAdministrator()) { if (MessageBox.Show("Reiniciar como Admin?", "Permissão", MessageBoxButtons.YesNo) == DialogResult.Yes) RestartAsAdmin(); return; }
            btnSFC.Enabled = false; rtbConsole.Clear(); rtbConsole.AppendText(">>> INICIANDO SFC EXTERNO <<<\r\n");
            await Task.Run(() => { ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/k sfc /scannow") { UseShellExecute = true, Verb = "runas" }; Process.Start(psi).WaitForExit(); });
            rtbConsole.AppendText(">>> SFC CONCLUÍDO <<<"); btnSFC.Enabled = true; MessageBox.Show("SFC Finalizado!");
        }

        private async void BtnDism_Click(object sender, EventArgs e)
        {
            if (!IsAdministrator()) { if (MessageBox.Show("Reiniciar como Admin?", "Permissão", MessageBoxButtons.YesNo) == DialogResult.Yes) RestartAsAdmin(); return; }
            btnDism.Enabled = false; rtbConsole.Clear(); rtbConsole.AppendText(">>> INICIANDO DISM EXTERNO <<<\r\n");
            await Task.Run(() => { ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/k dism /online /cleanup-image /restorehealth") { UseShellExecute = true, Verb = "runas" }; Process.Start(psi).WaitForExit(); });
            rtbConsole.AppendText(">>> DISM CONCLUÍDO <<<"); btnDism.Enabled = true; MessageBox.Show("DISM Finalizado!");
        }

        private async void BtnSpooler_Click(object sender, EventArgs e)
        {
            if (!IsAdministrator()) { if (MessageBox.Show("Reiniciar como Admin?", "Permissão", MessageBoxButtons.YesNo) == DialogResult.Yes) RestartAsAdmin(); return; }
            btnSpooler.Enabled = false; rtbConsole.Clear(); rtbConsole.AppendText(">>> RESETANDO SPOOLER <<<\r\n");
            await Task.Run(() => { ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/k \"net stop spooler && del /Q /F /S \\\"%systemroot%\\System32\\Spool\\Printers\\*.*\\\" && net start spooler\"") { UseShellExecute = true, Verb = "runas" }; Process.Start(psi).WaitForExit(); });
            rtbConsole.AppendText(">>> SPOOLER RESETADO <<<"); btnSpooler.Enabled = true; MessageBox.Show("Spooler Resetado!");
        }
    }
}