using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PythonRunner : ProcessRunnerBase
{
    const string PYTHON_SCRIPTS_ROOT = "ExternalScripts/KaraOK_1.0/scripts";
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
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = processStartInfo };
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
                        try
                        {
                            var val = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(stringVal);
                            res.Value = val;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[PythonRunner] - Failed to deserialize data to {typeof(T)}: {ex.Message}");
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
    }
}