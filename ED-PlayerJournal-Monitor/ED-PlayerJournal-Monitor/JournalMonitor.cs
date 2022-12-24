using System;
using System.Dynamic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ED_PlayerJournal_Monitor
{
    public class JournalEventArgs: EventArgs
    {
        public ExpandoObject Event { get; set; } = null!;
    }
    public class JournalMonitor : LogMonitor
    {
        private static readonly Regex JsonRegex = new Regex(@"^{.*}$");
        public JournalMonitor() : base(GetSavedGamesDir(), @"^Journal.*\.[0-9\.]+\.log$") { }

        public event EventHandler<JournalEventArgs>? JournalEvent;

        public override void EventCallback(string data)
        {
            var match = JsonRegex.Match(data);
            if (!match.Success)
            {
                return;
            }

            var converter = new ExpandoObjectConverter();
            var @event = JsonConvert.DeserializeObject<ExpandoObject>(data, converter)!;
            JournalEvent?.Invoke(this, new JournalEventArgs()
            {
                Event = @event
            });
        }

        private static string GetSavedGamesDir()
        {
            var result = NativeMethods.SHGetKnownFolderPath(new Guid("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"), 0, new IntPtr(0), out var path);
            if (result >= 0)
            {
                return Marshal.PtrToStringUni(path) + @"\Frontier Developments\Elite Dangerous";
            }

            throw new ExternalException("Failed to find the saved games directory.", result);
        }

        internal class NativeMethods
        {
            [DllImport("Shell32.dll")]
            internal static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)]Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
        }

    }
}