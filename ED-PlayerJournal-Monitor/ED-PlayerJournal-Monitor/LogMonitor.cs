using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace ED_PlayerJournal_Monitor
{
    public abstract class LogMonitor
    {
        // What we are monitoring and what to do with it
        public string? Directory;
        public Regex? Filter;
        public static string? JournalFileName = null!;

        // Keep track of status
        private bool _running;

        protected LogMonitor(string filter) { Filter = new Regex(filter); }

        protected LogMonitor(string directory, string filter)
        {
            Directory = directory;
            Filter = new Regex(filter);
        }

        public abstract void EventCallback(string data);

        /// <summary>Monitor the netlog for changes, running a callback when the file changes</summary>
        public void Start()
        {
            if (Directory == null || Directory.Trim() == "")
            {
                return;
            }

            _running = true;

            // Start off by moving to the end of the file
            long lastSize = 0;
            string? lastName = null;
            FileInfo? fileInfo = null;
            try
            {
                fileInfo = FindLatestFile(Directory, Filter);
            }
            catch (NotSupportedException)
            {
                //Logging.Error("Directory " + Directory + " not supported: ", nsex);
            }
            if (fileInfo != null)
            {
                lastSize = fileInfo.Length;
                lastName = fileInfo.Name;
                JournalFileName = lastName;

                // Elite-specific: start off by grabbing the first line so that we know if we're in beta or live
                using var fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                var firstLine = reader.ReadLine() ?? "";
                // First line should be a file header
                if (firstLine.Contains("Fileheader"))
                {
                    // Pass this along as an event
                    EventCallback(firstLine);
                }
            }

            // Main loop
            while (_running)
            {
                fileInfo = FindLatestFile(Directory, Filter);
                if (fileInfo == null || fileInfo.Name != lastName)
                {
                    lastName = fileInfo?.Name;
                    lastSize = 0;
                    if (fileInfo != null)
                    {
                        JournalFileName = fileInfo.Name;
                    }
                    else
                    {
                        // A player journal file could not be found. Sleep until a player journal file is found.
                        //Logging.Info("Error locating Elite Dangerous player journal. Journal monitor is not active. Have you installed and run Elite Dangerous previously? ");
                        while (fileInfo == null)
                        {
                            Thread.Sleep(500);
                            fileInfo = FindLatestFile(Directory, Filter);
                        }
                        //Logging.Info("Elite Dangerous player journal found. Journal monitor activated.");
                        return;
                    }
                }
                else
                {
                    JournalFileName = fileInfo.Name;
                    var thisSize = fileInfo.Length;
                    long seekPos = 0;
                    var readLen = 0;
                    if (lastSize != thisSize)
                    {
                        if (thisSize > lastSize)
                        {
                            // File has been appended - read the remaining info
                            seekPos = lastSize;
                            readLen = (int)(thisSize - lastSize);
                        }
                        else if (thisSize < lastSize)
                        {
                            // File has been truncated - read all of the info
                            seekPos = 0;
                            readLen = (int)thisSize;
                        }

                        using var fs = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        fs.Seek(seekPos, SeekOrigin.Begin);
                        var bytes = new byte[readLen];
                        var haveRead = 0;
                        while (haveRead < readLen)
                        {
                            haveRead += fs.Read(bytes, haveRead, readLen - haveRead);
                            fs.Seek(seekPos + haveRead, SeekOrigin.Begin);
                        }
                        // Convert bytes to string
                        var s = Encoding.UTF8.GetString(bytes);
                        string[] lines = Regex.Split(s, "\r?\n");
                        foreach (var line in lines)
                        {
                            EventCallback(line);
                        }
                    }
                    lastSize = thisSize;
                }
                Thread.Sleep(100);
            }
        }

        public void Stop()
        {
            _running = false;
        }

        /// <summary>Find the latest file in a given directory matching a given expression, or null if no such file exists</summary>
        private static FileInfo? FindLatestFile(string? path, Regex? filter = null)
        {
            if (path == null)
            {
                // Configuration can be changed underneath us so we do have to check each time...
                return null;
            }

            var directory = new DirectoryInfo(path);
            try
            {
                var info = directory.GetFiles().Where(f => filter == null || filter.IsMatch(f.Name)).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                if (info != null)
                {
                    // This info can be cached so force a refresh
                    info.Refresh();
                }
                return info;
            }
            catch
            {
                // ignored
            }

            return null;
        }
    }
}