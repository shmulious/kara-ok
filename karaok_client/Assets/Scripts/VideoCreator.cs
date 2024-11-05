using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DataClasses;
using UnityEngine;

public class VideoCreator
{
    const string RETURN_VALUE_PREFIX = "Return Value: ";
    public static async Task<ProcessResult<string>> RunPythonScriptAsync(SongMetadata metadata)
    {
        string pythonExePath = Path.Combine(ProcessRunnerBase.ENV_PATH, "venvs", "vosk-env", "bin/python3");
        string scriptPath = Path.Combine(Application.streamingAssetsPath, PythonRunner.PYTHON_SCRIPTS_ROOT, "main/vosk_service.py");

        var vocalTrackPath = metadata.CachedFiles[CachedSongFiles.VocalsKey].LocalPath;
        var lyricsFilePath = metadata.CachedFiles[CachedSongFiles.LyricsKey].LocalPath;
        var instrumentalTrackPath = metadata.CachedFiles[CachedSongFiles.NoVocalsKey].LocalPath;
        var baseName = Path.Combine(metadata.CachePath, "video", $"{metadata.Artist} - {metadata.Title}"); 
        var outputSrtPath = Path.Combine(baseName, ".srt");
        var outputVideoPath = Path.Combine(baseName, ".mp4");

        // Prepare the arguments for the Python script
        string arguments = $"\"{scriptPath}\" --vocal_track_path \"{vocalTrackPath}\" --lyrics_file \"{lyricsFilePath}\" --instrumental_track_path \"{instrumentalTrackPath}\" --output_srt_path \"{outputSrtPath}\" --output_video_path \"{outputVideoPath}\"";

        // Set up the process to run the Python script
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonExePath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (Process process = new Process { StartInfo = startInfo })
        {
            KaraokLogger.Log($"Command: {process.StartInfo.Arguments}");
            ProcessResult<string> res = new ProcessResult<string>();

            try
            {
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        // Check if the data starts with the return value prefix
                        if (args.Data.StartsWith(RETURN_VALUE_PREFIX))
                        {
                            // Remove the prefix and attempt to deserialize the remaining data into type T
                            var stringVal = args.Data.Replace(RETURN_VALUE_PREFIX, string.Empty);
                            res.StringVal = stringVal;
                            //if (typeof(T) == typeof(string))
                            //{
                            //    res.StringVal = stringVal;
                            //}
                            //else
                            //{
                            //    try
                            //    {
                            //        var val = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(stringVal);
                            //        res.Value = val;
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        KaraokLogger.LogWarning($"[PythonRunner] - Failed to deserialize data to {typeof(T)}: {ex.Message}");
                            //    }
                            //}
                        }

                        res.Output += args.Data;
                        KaraokLogger.Log($"Python Output: {args.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        res.Error += $"{args.Data}\n";
                        KaraokLogger.LogError($"Python Error: {args.Data}");
                    }
                };

                process.Start();

                // Begin reading the output and error streams asynchronously
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Run WaitForExit in a task to avoid blocking the main thread
                await Task.Run(() => process.WaitForExit());

                // After the process has completed
                int exitCode = process.ExitCode;
                res.ExitCode = exitCode;
                KaraokLogger.Log($"Python Output: {exitCode}");
                return res;
            }
            catch (Exception e)
            {
                KaraokLogger.LogError($"Failed to create Video!");
                throw e;
            }
        }
    }

    private static Task WaitForExitAsync(Process process)
    {
        var tcs = new TaskCompletionSource<bool>();
        process.EnableRaisingEvents = true;

        process.Exited += (sender, args) =>
        {
            tcs.SetResult(true); // Process has exited
        };

        if (process.HasExited) // In case the process has already exited
        {
            tcs.SetResult(true);
        }

        return tcs.Task;
    }
}