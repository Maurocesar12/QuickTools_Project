using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    public class CommandExecutor
    {
        public event Action<string> OnOutputReceived;

        public async Task RunAdminCommandAsync(string command, string args)
        {
            await Task.Run(async () =>
            {
                // 1. Achar o System32 real (correção 32/64 bits)
                string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                {
                    string sysNative = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Sysnative");
                    if (Directory.Exists(sysNative)) systemPath = sysNative;
                }

                string cmdPath = Path.Combine(systemPath, "cmd.exe");

                using (Process p = new Process())
                {
                    p.StartInfo.FileName = cmdPath;
                    // Removemos o "chcp 65001" que estava causando o bug do texto estranho
                    p.StartInfo.Arguments = $"/c {command} {args}";

                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                    p.StartInfo.RedirectStandardError = true;
                    p.StartInfo.CreateNoWindow = true;

                    // --- A CORREÇÃO DO TEXTO ---
                    // No Brasil, o CMD usa a página de código 850.
                    // Isso vai fazer "Verificação" aparecer certo, em vez de "duco1i.n".
                    try
                    {
                        p.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(850);
                        p.StartInfo.StandardErrorEncoding = Encoding.GetEncoding(850);
                    }
                    catch
                    {
                        p.StartInfo.StandardOutputEncoding = Encoding.Default;
                    }

                    p.Start();

                    // Lemos caractere por caractere para pegar a porcentagem em tempo real
                    char[] buffer = new char[1024];
                    while (!p.StandardOutput.EndOfStream)
                    {
                        int charsRead = await p.StandardOutput.ReadAsync(buffer, 0, buffer.Length);
                        if (charsRead > 0)
                        {
                            string text = new string(buffer, 0, charsRead);
                            OnOutputReceived?.Invoke(text);
                        }
                    }

                    string err = await p.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(err)) OnOutputReceived?.Invoke("INFO: " + err);

                    p.WaitForExit();
                }
            });
        }
    }
}