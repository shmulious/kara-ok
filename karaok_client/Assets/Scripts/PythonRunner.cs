﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PythonRunner : ProcessRunnerBase
{
    private static List<Process> _processes = new List<Process>();
    public const string PYTHON_SCRIPTS_ROOT = "ExternalScripts/KaraOK_1.0/scripts";
    const string RETURN_VALUE_PREFIX = "Return Value: ";

    // Override the abstract method to run a Python script with optional arguments
    public override async Task<ProcessResult<T>> RunProcess<T>(string relativeScriptPath, string arguments = "")
    {
        string pythonPath = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor ? "python" : "python3";

        // Combine the relative path with the StreamingAssets path
        string scriptPath = Path.Combine(Application.streamingAssetsPath, PYTHON_SCRIPTS_ROOT, relativeScriptPath);

        if (!File.Exists(scriptPath))
        {
            LogError($"Python script not found at: {scriptPath}");
            return new ProcessResult<T>(null, "Python script not found", -1);
        }

        // Append the provided arguments to the Python script command
        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{scriptPath}\" {arguments}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = processStartInfo };
        
        Log($"Command: {process.StartInfo.Arguments}");
        ProcessResult<T> res = new ProcessResult<T>();

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
                        if (typeof(T) == typeof(string))
                        {
                            res.StringVal = stringVal;
                        }
                        else
                        {
                            try
                            {
                                var val = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(stringVal);
                                res.Value = val;
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning(
                                    $"[PythonRunner] - Failed to deserialize data to {typeof(T)}: {ex.Message}");
                            }
                        }
                    }

                    res.Output += args.Data;
                    Log($"Python Output: {args.Data}");
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    res.Error += $"{args.Data}\n";
                    LogError($"Python Error: {args.Data}");
                }
            };
            _processes.Add(process);
            process.Start();

            // Begin reading the output and error streams asynchronously
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Run WaitForExit in a task to avoid blocking the main thread
            await Task.Run(() => process.WaitForExit());

            // After the process has completed
            int exitCode = process.ExitCode;
            res.ExitCode = exitCode;
            Log($"Python Output: {exitCode}");
            return res;
        }
        catch (System.Exception ex)
        {
            LogError($"Error running Python script: {ex.Message}");
            return new ProcessResult<T>(null, ex.Message, -1);
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
}