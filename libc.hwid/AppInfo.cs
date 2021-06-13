using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace libc.hwid
{
    /// <summary>
    ///     Helper for OS, Application and Registry
    /// </summary>
    internal static class AppInfo
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsMacOs => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        ///     e.g: C:\
        /// </summary>
        public static string WindowsInstallationDirectory => Path.GetPathRoot(Environment.SystemDirectory);

        public static bool Is64 => Environment.Is64BitOperatingSystem;
        public static string OsArch => Is64 ? "64" : "32";
        public static bool IsInDesignMode => LicenseManager.UsageMode == LicenseUsageMode.Designtime;
    }
}