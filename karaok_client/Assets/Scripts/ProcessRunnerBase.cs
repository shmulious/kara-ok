using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public abstract class ProcessRunnerBase
{
    public static string ENV_PATH
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, "setup_folder");
        }
    }
    private static string _type;

    // Log method to prepend class name to the log message
    protected static void Log(string message)
    {
        UnityEngine.Debug.Log($"[{_type}] {message}");
    }

    // LogError method to prepend class name to the error message
    protected static void LogError(string message)
    {
        UnityEngine.Debug.LogError($"[{_type}] {message}");
    }

    // Constructor
    public ProcessRunnerBase()
    {
        _type = GetType().Name;
    }

    // Abstract method to be implemented by derived classes, now accepts arguments
    public abstract Task<ProcessResult<T>> RunProcess<T>(string scriptPath, string arguments = "");

    // Async static method to check if Python is installed
    public static async Task<bool> IsPythonInstalled()
    {
        string pythonCommand = Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor ? "python" : "python3";

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonCommand,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Asynchronously read the standard output and error
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(outputTask, errorTask);

            string output = outputTask.Result;
            string error = errorTask.Result;

            // Check if output or error contains "Python"
            if ((!string.IsNullOrEmpty(output) && output.ToLower().Contains("python")) ||
                (!string.IsNullOrEmpty(error) && error.ToLower().Contains("python")))
            {
                Log($"Python is installed: {output.Trim()}{error.Trim()}");
                return true;
            }

            LogError("Python is not installed.");
        }
        catch (System.Exception ex)
        {
            LogError($"Error checking Python installation: {ex.Message}");
        }

        return false;
    }
}