using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataClasses;
using UnityEngine;

public enum ProcessType
{
    Convert,
}

public static class SmuleService
{
    private static PythonRunner _pythonRunner = new PythonRunner(); // Assuming PythonRunner is already defined elsewhere
    private static string venvPath = Path.Combine(ProcessRunnerBase.ENV_PATH, "venvs", "smule-env"); // Path to virtual environment
    private static string pythonExecutable = Path.Combine(venvPath, "bin", "python3"); // Path to Python executable
    // For Windows: private static string pythonExecutable = Path.Combine(venvPath, "Scripts", "python.exe");

    /// <summary>
    /// Process a song by running the specified process on smule.py.
    /// After a successful conversion, the current thumbnail is saved as a JPG file.
    /// </summary>
    public static async Task<ProcessResult<string>> ProcessSongFromYoutube(SongMetadata metadata, string outputFolderPath, int modelNumber)
    {
        try
        {
            KaraokLogger.Log($"[SmuleService] - Processing song for URL: {metadata.URL}");
            outputFolderPath = metadata.CachePath;
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            var res = await _pythonRunner.RunProcess<string>(
                "main/smule.py",
                string.Format("--convert \"{0}\" \"{1}\" {2}", metadata.MetadataPath, outputFolderPath, modelNumber)
            );

            KaraokLogger.Log($"[SmuleService] - Finished processing {metadata.URL}");
            KaraokLogger.Log($"[SmuleService] - Result: {res.Output ?? "No output returned."}");

            if (res.Success)
            {
                var artifacts_path = res.StringVal;
                KaraokLogger.Log($"[SmuleService] - Conversion successful.");
                KaraokLogger.Log($"[SmuleService] - Outputs are placed under: {artifacts_path}");
                return new ProcessResult<string>() { ExitCode = 0, Value = artifacts_path };
            }
            else
            {
                KaraokLogger.LogError($"[SmuleService] - Error processing song: {res.Error}");
                return res;
            }
        }
        catch (Exception ex)
        {
            KaraokLogger.LogError($"[SmuleService] - Exception occurred while processing song: {ex.Message}");
            return new ProcessResult<string>() { ExitCode = 1, Value = ex.Message, Error = ex.Message };
        }
    }
    

    public static async Task<ProcessResult<string>> ProcessSmuleUrl(string smuleUrl)
    {
        KaraokLogger.Log($"[SmuleService] - Processing URL: {smuleUrl}");
        var mediUrls = await SmuleUrlProcessor.ProcessSmuleLinkAsync(smuleUrl);
        var downloadedMedia = string.Join(';', mediUrls.Select(i => i.ToString()));
        KaraokLogger.Log($"[SmuleService] - downloaded successfully: {downloadedMedia}");
        return new ProcessResult<string>(){ExitCode = mediUrls.Count>0 ? 0 : 1, Value = downloadedMedia, StringVal = downloadedMedia};
    }
}