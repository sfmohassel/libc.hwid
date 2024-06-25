using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using libc.hwid.Helpers;

namespace libc.hwid
{
    public static class HwId
    {
        public static string Generate(HashAlgorithm hashAlgo = null, bool includeMAC = false, bool disposeHashAlgo = true)
        {
            try
            {
                if (hashAlgo is null)
                {
                    hashAlgo = new SHA1Managed();
                    disposeHashAlgo = true; // Since we're creating it locally, we have to dispose it
                }

                using (MemoryStream ms = new MemoryStream(512))
                {
                    GetCpuInfo(ms);
                    GetMotherboardInfo(ms);
                    if (includeMAC)
                        GetMacInfo(ms);

                    ms.Position = 0;
                    return ByteArrayToHexViaLookup32(hashAlgo.ComputeHash(ms));
                }
            }
            finally
            {
                if (disposeHashAlgo)
                    hashAlgo.Dispose();
            }
        }

        private static string Wmi(string wmiClass, string wmiProperty)
        {
            var result = "";
            var mc = new ManagementClass(wmiClass);
            var moc = mc.GetInstances();

            foreach (var o in moc)
            {
                var mo = (ManagementObject) o;

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

        private readonly static string[] separators = { Environment.NewLine };
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

            var lines = k.Output.Split(separators, StringSplitOptions.RemoveEmptyEntries).Select(a => a.Trim(' ', '\t'));

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

            var command = @"/usr/sbin/ioreg -rd1 -c IOPlatformExpertDevice | awk -F'\""' '/" + node +
                          "/{ print $(NF-1) }'";

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

        private static void GetCpuInfo(MemoryStream ms)
        {
            if (AppInfo.IsLinux)
            {
                var res = Dmidecode("dmidecode -t 4", "ID");
                var parts = res.Split(' ').Reverse();
                var result = string.Join("", parts);
                ms.Write(Encoding.UTF8.GetBytes(result), 0, result.Length);
            }
            else if (AppInfo.IsWindows)
            {
                // We try by asm but fallback with wmi if it fails.
                var asmCpuId = Asm.GetProcessorId();
                if (asmCpuId is null)
                {
                    var cpuId = Wmi("Win32_Processor", "ProcessorId");
                    ms.Write(Encoding.UTF8.GetBytes(cpuId), 0, cpuId.Length);
                }
                else
                {
                    ms.Write(asmCpuId, 4, 4);
                    ms.Write(asmCpuId, 0, 4);
                }
            }
            else if (AppInfo.IsMacOs)
            {
                var uuid = GetIoregOutput("IOPlatformUUID");
                ms.Write(Encoding.UTF8.GetBytes(uuid), 0, uuid.Length);
            }
            else
                throw new PlatformNotSupportedException();
        }

        private static void GetMacInfo(MemoryStream ms)
        {
            var macAddr =
            (
                from nic in NetworkInterface.GetAllNetworkInterfaces()
                where nic.OperationalStatus == OperationalStatus.Up
                select nic.GetPhysicalAddress().GetAddressBytes()
            ).FirstOrDefault();

            ms.Write(macAddr, 0, macAddr.Length);
        }

        private static void GetMotherboardInfo(MemoryStream ms)
        {
            if (AppInfo.IsLinux)
            {
                var result = Dmidecode("dmidecode -t 2", "Manufacturer");
                ms.Write(Encoding.UTF8.GetBytes(result), 0, result.Length);
            }
            else if (AppInfo.IsWindows)
            {
                var motherboardId = Wmi("Win32_BaseBoard", "Manufacturer");
                ms.Write(Encoding.UTF8.GetBytes(motherboardId), 0, motherboardId.Length);
            }
            else if (AppInfo.IsMacOs)
            {
                var macSerial = GetIoregOutput("IOPlatformSerialNumber");
                ms.Write(Encoding.UTF8.GetBytes(macSerial), 0, macSerial.Length);
            }
            else
                throw new PlatformNotSupportedException();
        }

        private static readonly uint[] _lookup32 = CreateLookup32();

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }

        private static string ByteArrayToHexViaLookup32(byte[] bytes)
        {
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }
    }
}