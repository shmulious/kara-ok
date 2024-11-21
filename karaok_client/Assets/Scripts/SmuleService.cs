using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataClasses;
using UnityEngine;

public enum ProcessType
{
    Convert,
}

public static class SmuleService
{
    private static List<Process> _processes = new List<Process>();
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

    public static async Task<ProcessResult<string>> CreateVideo(string srtFilePath,
        string audioFilePath)
    {
        KaraokLogger.Log($"[SmuleService] - Creating Karaoke video for: {Path.GetFileName(audioFilePath)}");
        var outputFilePath = Path.Combine(Path.GetDirectoryName(audioFilePath), "karaoke.mp4");
        if (!Directory.Exists(outputFilePath))
        {
            Directory.CreateDirectory(outputFilePath);
        }

        var video_file_path = Path.GetDirectoryName(audioFilePath);
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/opt/homebrew/bin/ffmpeg",
                Arguments = $"-f lavfi -i color=c=black:s=900x1600:d=175 -i \"{audioFilePath}\" -vf subtitles=\"{srtFilePath}\" -shortest \"{audioFilePath.Replace(".m4a","")}_output.mp4\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = CacheManager.CachePath
            }
        };


        string output = string.Empty;
        string error = string.Empty;

        try
        {
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    output += $"{args.Data}\n";
                    KaraokLogger.Log(
                        $"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\nOutput: {args.Data}");
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    error += $"{args.Data}\n";
                    KaraokLogger.LogError(
                        $"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\nError: {args.Data}");
                }
            };
            _processes.Add(process);
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode == 0)
            {
                var outputLines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                var jsonOutput = outputLines[^1];
                //return ParseUrls<T>(jsonOutput);
                return new ProcessResult<string>() { ExitCode = process.ExitCode, Value = jsonOutput, Error = error };
            }
            else
            {
                KaraokLogger.LogError(
                    $"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\nexited with code {process.ExitCode}");
                return default;
            }
        }
        catch (System.Exception ex)
        {
            KaraokLogger.LogError(
                $"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\n exception running script: {ex.Message}");
            return default;
        }
        finally
        {
            _processes.Remove(process);
            if (!process.HasExited)
            {
                process.Kill();
            }
        }
    }

    public static void Dispose()
    {
        foreach (var process in _processes)
        {
            process.Kill();
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