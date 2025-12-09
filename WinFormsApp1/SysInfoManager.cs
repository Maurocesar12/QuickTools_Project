using System;
using System.Management;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    public class SysInfoManager
    {
        // 1. Adicionamos o campo 'Hostname' aqui
        public class PCInfo
        {
             // <--- NOVO
            public string OSName, CPU, RAM, GPU, SerialNumber, Model, DiskInfo, Hostname;
        }

        public static Task<PCInfo> GetInfoAsync()
        {
            return Task.Run(() =>
            {
                var info = new PCInfo();

                // 2. Capturamos o nome da máquina aqui (Mais rápido que WMI)
                info.Hostname = Environment.MachineName; // <--- NOVO

                info.OSName = GetWMI("Win32_OperatingSystem", "Caption");
                info.CPU = GetWMI("Win32_Processor", "Name");
                info.SerialNumber = GetWMI("Win32_Bios", "SerialNumber");
                info.Model = GetWMI("Win32_ComputerSystem", "Model");
                info.GPU = GetWMI("Win32_VideoController", "Name");

                try
                {
                    long ram = long.Parse(GetWMI("Win32_ComputerSystem", "TotalPhysicalMemory"));
                    info.RAM = $"{ram / (1024 * 1024 * 1024)} GB";
                }
                catch { info.RAM = "N/A"; }

                info.DiskInfo = GetDiskSpace();
                return info;
            });
        }

        private static string GetWMI(string table, string prop)
        {
            try
            {
                using (var s = new ManagementObjectSearcher($"SELECT {prop} FROM {table}"))
                    foreach (ManagementObject o in s.Get()) return o[prop]?.ToString();
            }
            catch { }
            return "N/A";
        }

        private static string GetDiskSpace()
        {
            string d = "";
            try
            {
                using (var s = new ManagementObjectSearcher("SELECT Name, FreeSpace, Size FROM Win32_LogicalDisk WHERE DriveType=3"))
                    foreach (ManagementObject o in s.Get())
                    {
                        double f = Convert.ToDouble(o["FreeSpace"]) / 1e9;
                        double t = Convert.ToDouble(o["Size"]) / 1e9;
                        d += $"{o["Name"]} Livre: {f:0.0}GB / {t:0.0}GB\r\n";
                    }
            }
            catch { }
            return d;
        }
    }
}