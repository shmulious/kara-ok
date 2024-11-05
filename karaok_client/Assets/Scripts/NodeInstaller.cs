using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class NodeInstaller
{
    private string nodePath;

    public NodeInstaller()
    {
        nodePath = null;
    }

    public async Task<bool> IsNodeInstalledAsync()
    {
        nodePath = await FindNodePathAsync();
        return !string.IsNullOrEmpty(nodePath);
    }

    public async Task InstallNodeAsync()
    {
        if (await IsNodeInstalledAsync())
        {
            KaraokLogger.Log("Node.js is already installed.");
            return;
        }

        KaraokLogger.Log("Node.js is not installed. Installing Node.js...");

        string url = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "https://nodejs.org/dist/v16.14.0/node-v16.14.0-x64.msi"
            : "https://nodejs.org/dist/v16.14.0/node-v16.14.0.pkg";

        string installerPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(url));

        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            await using (var fileStream = new FileStream(installerPath, FileMode.Create))
            {
                await response.Content.CopyToAsync(fileStream);
            }
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            await RunProcessAsync("msiexec", $"/i \"{installerPath}\" /quiet /norestart");
        }
        else
        {
            await RunProcessAsync("sudo", $"installer -pkg \"{installerPath}\" -target /");
        }

        // Update nodePath after installation
        nodePath = await FindNodePathAsync();
    }

    private async Task<string> FindNodePathAsync()
    {
        string command = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
        var val = await RunCommandAsync(command, "node");
        return val?.Trim();
    }

    private async Task RunProcessAsync(string command, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
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
                    output += args.Data;
                    KaraokLogger.Log($"process Output: {args.Data}");
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                {
                    error += $"{args.Data}\n";
                    KaraokLogger.LogError($"process Error: {args.Data}");
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
            KaraokLogger.Log($"Process Output: {exitCode}");
        }
        catch (System.Exception ex)
        {
            KaraokLogger.LogError($"Error running Python script: {ex.Message}");
        }
    }

    private async Task<string> RunCommandAsync(string command, string args)
    {
        // Log the command and arguments for debugging
        KaraokLogger.Log($"Executing command: {command} {args}");

        ProcessStartInfo procStartInfo = new ProcessStartInfo(command, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process process = new Process { StartInfo = procStartInfo };

        {
            string output = string.Empty;
            string error = string.Empty;
            try
            {
                process.OutputDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        output += args.Data;
                        KaraokLogger.Log($"process Output: {args.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        error += $"{args.Data}\n";
                        KaraokLogger.LogError($"process Error: {args.Data}");
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
                KaraokLogger.Log($"Process Output: {exitCode}");
            }
            catch (System.Exception ex)
            {
                KaraokLogger.LogError($"Error running Python script: {ex.Message}");
            }

            return output;
        }

    }

    public string GetNodePath()
    {
        return nodePath;
    }
}