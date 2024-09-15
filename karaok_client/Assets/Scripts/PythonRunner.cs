using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PythonRunner : ProcessRunnerBase
{
    const string PYTHON_SCRIPTS_ROOT = "ExternalScripts/KaraOK_1.0/scripts";

    // Override the abstract method to run a Python script with optional arguments
    public override async Task<ProcessResult> RunProcess<T>(string relativeScriptPath, string arguments = "")
    {
        string pythonPath = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor ? "python" : "python3";

        // Combine the relative path with the StreamingAssets path
        string scriptPath = Path.Combine(Application.streamingAssetsPath, PYTHON_SCRIPTS_ROOT, relativeScriptPath);

        if (!File.Exists(scriptPath))
        {
            LogError($"Python script not found at: {scriptPath}");
            return new ProcessResult(null, "Python script not found", -1);
        }

        // Append the provided arguments to the Python script command
        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"\"{scriptPath}\" {arguments}", // Pass the script and arguments
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = processStartInfo };
        Log($"Python Output: {process.StartInfo.Arguments}");
        ProcessResult res = new ProcessResult();
        try
        {
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    if (typeof(T) != typeof(string))
                    {
                        try
                        {
                            var val = (T)Convert.ChangeType(args.Data.ToLower(), typeof(T));
                            res.Value = val;
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[PythonRunner] - can't convert data ({args.Data}) to T({typeof(T)})");
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
            //res.Output += "Process completed.";
            return res;
        }
        catch (System.Exception ex)
        {
            LogError($"Error running Python script: {ex.Message}");
            return new ProcessResult(null, ex.Message, -1);
        }
    }
}