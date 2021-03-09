using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
namespace libc.hwid
{
    public static class HwId
    {
        private enum Hardware
        {
            Motherboard,
            Cpuid
        }
        public static string Generate()
        {
            var res = new[] {
                GetInfo(Hardware.Cpuid),
                GetInfo(Hardware.Motherboard)
            };
            var input = string.Join("\n", res);
            var result = Hash(input);
            return result;
        }
        private static string Hash(string input)
        {
            using (var sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }
        private static string Wmi(string wmiClass, string wmiProperty)
        {
            var result = "";
            var mc = new ManagementClass(wmiClass);
            var moc = mc.GetInstances();
            foreach (var o in moc)
            {
                var mo = (ManagementObject)o;
                //Only get the first one
                if (result != "") 
                    continue;
                try
                {
                    result = mo[wmiProperty].ToString();
                    break;
                }
                catch
                {
                    // ignored
                }
            }
            return result;
        }
        private static string Dmidecode(string query, string find)
        {
            var cmd = new Cmd();
            var k = cmd.Run("/usr/bin/sudo", $" {query}", new CmdOptions
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                RedirectStdOut = true,
                UseOsShell = false
            }, true);
            find = find.EndsWith(":") ? find : $"{find}:";
            var lines = k.Output.Split(new[] {
                    Environment.NewLine
                }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim(' ', '\t'));
            var line = lines.First(a => a.StartsWith(find));
            var res = line.Substring(line.IndexOf(find, StringComparison.Ordinal) + find.Length).Trim(' ', '\t');
            return res;
        }
        private static string GetIoregOutput(string node)
        {
            var proc = new Process();
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/sh"
            };
            var command = @"/usr/sbin/ioreg -rd1 -c IOPlatformExpertDevice | awk -F'\""' '/" + node + "/{ print $(NF-1) }'";
            psi.Arguments = $"-c \"{command}\"";
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            string result = null;
            proc.StartInfo = psi;

            proc.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    result = e.Data;
            };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            return result;
        }
        private static string GetInfo(Hardware hw)
        {
            switch (hw)
            {
                case Hardware.Motherboard when AppInfo.IsLinux:
                    {
                        var result = Dmidecode("dmidecode -t 2", "Manufacturer");
                        return result;
                    }
                case Hardware.Motherboard when AppInfo.IsWindows:
                    return Wmi("Win32_BaseBoard", "Manufacturer");
                case Hardware.Motherboard when AppInfo.IsMacOs:
                    var macSerial = GetIoregOutput("IOPlatformSerialNumber");
                    return macSerial;
                case Hardware.Cpuid when AppInfo.IsLinux:
                    {
                        var res = Dmidecode("dmidecode -t 4", "ID");
                        var parts = res.Split(' ').Reverse();
                        var result = string.Join("", parts);
                        return result;
                    }
                case Hardware.Cpuid when AppInfo.IsWindows:
                    // We try by asm but fallback with wmi if it fails.
                    var asmCpuId = Helpers.Asm.GetProcessorId();
                    return asmCpuId?.Length > 2 ? asmCpuId : Wmi("Win32_Processor", "ProcessorId");
                case Hardware.Cpuid when AppInfo.IsMacOs:
                    var uuid = GetIoregOutput("IOPlatformUUID");
                    return uuid;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
}