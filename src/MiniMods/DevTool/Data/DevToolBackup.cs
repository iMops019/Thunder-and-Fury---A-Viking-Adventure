using System;
using System.IO;

namespace ThunderFury.DevTool.Data
{
    // ---- Backup everything before doing anything drastic ----
    //
    // Simplest reliable option, on purpose: copies every JSON file
    // already sitting in DevToolData/ into a timestamped subfolder.
    // Restoring is just copying a file back from that subfolder over its
    // live counterpart -- no import UI to build, no format to design,
    // and it's easy to see exactly what a given backup contains since
    // it's just plain files, not an opaque bundle.
    public static class DevToolBackup
    {
        public static string CreateBackup()
        {
            DevToolPaths.EnsureDataDirectory();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");
            string backupDir = Path.Combine(DevToolPaths.DataDirectory, "Backups", timestamp);
            Directory.CreateDirectory(backupDir);

            int copied = 0;
            foreach (string file in Directory.GetFiles(DevToolPaths.DataDirectory, "*.json"))
            {
                File.Copy(file, Path.Combine(backupDir, Path.GetFileName(file)), overwrite: true);
                copied++;
            }

            Jotunn.Logger.LogInfo($"DevTool: backed up {copied} file(s) to {backupDir}");
            return backupDir;
        }
    }
}
