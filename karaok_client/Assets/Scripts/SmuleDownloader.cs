using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class SmuleDownloader
{
    private readonly string nodePath;

    public SmuleDownloader(string nodePath)
    {
        if (string.IsNullOrEmpty(nodePath))
        {
            throw new ArgumentException("Node.js path is invalid.");
        }

        this.nodePath = nodePath;
    }

    public async Task<List<MediaUrlData>> FetchMediaUrlsAsync(string url)
    {
        var result = await FetchUrlsAsync<List<MediaUrlData>>(url);
        return result ?? new List<MediaUrlData>();
    }

    public async Task<PlaylistData> FetchPlaylistItemsUrls(string url)
    {
        var result = await FetchUrlsAsync<PlaylistData>(url);
        return result ?? new PlaylistData();
    }

    private async Task<T> FetchUrlsAsync<T>(string url)
    {
        string scriptPath = Path.Combine(Application.streamingAssetsPath, PythonRunner.PYTHON_SCRIPTS_ROOT, "nodejs", "getMediaUrl.js");

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = nodePath,
                Arguments = $"\"{scriptPath}\" \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                UseShellExecute = false,
                CreateNoWindow = true
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
                    KaraokLogger.Log($"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\nOutput: {args.Data}");
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    error += $"{args.Data}\n";
                    KaraokLogger.LogError($"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\nError: {args.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Run(() => process.WaitForExit());

            if (process.ExitCode == 0)
            {
                var outputLines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                var jsonOutput = outputLines[outputLines.Length - 1];
                return ParseUrls<T>(jsonOutput);
            }
            else
            {
                KaraokLogger.LogError($"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\nexited with code {process.ExitCode}");
                return default;
            }
        }
        catch (System.Exception ex)
        {
            KaraokLogger.LogError($"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\n exception running script: {ex.Message}");
            return default;
        }
    }

    private T ParseUrls<T>(string jsonOutput)
    {
        KaraokLogger.Log($"JSON string: {jsonOutput} to {typeof(T)}");
        try
        {
            return JsonConvert.DeserializeObject<T>(jsonOutput);
        }
        catch (System.Exception ex)
        {
            KaraokLogger.LogError($"Failed to parse URLs: {ex.Message}");
            return default;
        }
    }

    private static string venvPath = Path.Combine(ProcessRunnerBase.ENV_PATH, "venvs", "smule-env"); // Path to virtual environment
    private static string venvActivate = Path.Combine(venvPath, "bin", "activate"); // Path to Python executable

    public async Task<ProcessResult<string>> DownloadAndExtractMediaAsync(string mediaUrl, string outputPath)
    {
        string pythonScriptPath = Path.Combine(Application.streamingAssetsPath, PythonRunner.PYTHON_SCRIPTS_ROOT, "main/download_and_extract_audio.py");

        // Ensure the output directory exists
        Directory.CreateDirectory(outputPath);

        // Build the command arguments to pass to the Python script
        string arguments = $"\"{pythonScriptPath}\" \"{mediaUrl}\" \"{outputPath}\" \"{venvPath}\"";

        // Configure the process to run the Python script
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "python3", // Assumes 'python' is in the system PATH
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var process = new Process() { StartInfo = startInfo };
        // Start the process
        string output = string.Empty;
        string error = string.Empty;
        try
        {
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    output += args.Data;
                    KaraokLogger.Log($"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\nOutput: {args.Data}");
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    error += $"{args.Data}\n";
                    KaraokLogger.LogError($"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\nError: {args.Data}");
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
            KaraokLogger.Log($"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\nExitCode: {exitCode}");
            var returnValue = exitCode == 0 ? outputPath : null;
            return new ProcessResult<string>(output, error, exitCode){StringVal = returnValue};
        }
        catch (System.Exception ex)
        {
            KaraokLogger.LogError($"[{process.StartInfo.FileName} - {process.StartInfo.Arguments}]\n error running Python script: {ex.Message}");
            return null;
        }
    }
}