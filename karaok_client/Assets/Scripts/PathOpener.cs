using UnityEngine;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

public static class PathOpener
{
    // Static method to open a specific path
    public static void OpenPath(string path)
    {
        if (Directory.Exists(path) || File.Exists(path)) // Check if the path exists
        {
            using (Process process = new Process())
            {
                try
                {
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
                    process.StartInfo.FileName = "open";
                    process.StartInfo.Arguments = $"-R \"{path}\""; // "-R" reveals the file in Finder
                    process.StartInfo.UseShellExecute = false;
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                    process.StartInfo.FileName = "explorer";
                    process.StartInfo.Arguments = $"/select,\"{path}\""; // Selects the file in Explorer
                    process.StartInfo.UseShellExecute = true; // Required for explorer.exe
#else
                    Debug.LogWarning("Platform not supported for opening paths.");
                    return;
#endif

                    process.Start();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Error opening path: " + e.Message);
                }
            } // Process is disposed automatically when exiting the using block
        }
        else
        {
            Debug.LogWarning("Path does not exist: " + path);
        }
    }
}