using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
namespace libc.hwid {
    public static class HwId {
        private enum Hardware {
            Motherboard,
            CPUID
        }
        public static string Generate() {
            var res = new[] {
                getInfo(Hardware.CPUID),
                getInfo(Hardware.Motherboard)
            };
            var input = string.Join("\n", res);
            var result = hash(input);
            return result;
        }
        private static string hash(string input) {
            using (var sha1 = new SHA1Managed()) {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                return sb.ToString();
            }
        }
        private static string wmi(string wmiClass, string wmiProperty) {
            var result = "";
            var mc = new ManagementClass(wmiClass);
            var moc = mc.GetInstances();
            foreach (var o in moc) {
                var mo = (ManagementObject) o;
                //Only get the first one
                if (result == "")
                    try {
                        result = mo[wmiProperty].ToString();
                        break;
                    } catch {
                        // ignored
                    }
            }
            return result;
        }
        private static string dmidecode(string query, string find) {
            var cmd = new Cmd();
            var k = cmd.Run("/usr/bin/sudo", $" {query}", new CmdOptions {
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                RedirectStdOut = true,
                UseOSShell = false
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
        public static string GetIoregOutput(string node)
        {
            Process proc = new Process();
            ProcessStartInfo psi = new ProcessStartInfo
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

            proc.OutputDataReceived += new DataReceivedEventHandler((s, e) => {
                if (!String.IsNullOrEmpty(e.Data))
                    result = e.Data;
            });

            proc.Start();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            return result;
        }
        private static string getInfo(Hardware hw)
        {
            switch (hw)
            {
                case Hardware.Motherboard when AppInfo.IsLinux:
                    {
                        var result = dmidecode("dmidecode -t 2", "Manufacturer");
                        return result;
                    }
                case Hardware.Motherboard when AppInfo.IsWindows:
                    return wmi("Win32_BaseBoard", "Manufacturer");
                case Hardware.Motherboard when AppInfo.IsMacOS:
                    var macSerial = GetIoregOutput("IOPlatformSerialNumber");
                    return macSerial;
                case Hardware.CPUID when AppInfo.IsLinux:
                    {
                        var res = dmidecode("dmidecode -t 4", "ID");
                        var parts = res.Split(' ').Reverse();
                        var result = string.Join("", parts);
                        return result;
                    }
                case Hardware.CPUID when AppInfo.IsWindows:
                    return wmi("Win32_Processor", "ProcessorId");
                case Hardware.CPUID when AppInfo.IsMacOS:
                    var uuid = GetIoregOutput("IOPlatformUUID");
                    return uuid;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
}