using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class PythonInstaller : ProcessRunnerBase
{
    private readonly HttpClient client = new HttpClient();

    // Public method to initiate the Python installation process
    public override async Task<ProcessResult<T>> RunProcess<T>(string scriptPath, string args)
    {
        return await StartInstallationProcess<T>();
    }

    // Private method to start the installation process
    private async Task<ProcessResult<T>> StartInstallationProcess<T>()
    {
        ProcessResult<T> urlResult = await GetLatestPythonVersionUrl<T>();

        if (urlResult.Success && !string.IsNullOrEmpty(urlResult.Output))
        {
            // Proceed to download and install Python
            return await DownloadAndInstallPython<T>(urlResult.Output);
        }
        else
        {
            var browserUrl = "https://www.python.org/downloads/";
            Log($"Download Python manually from: {browserUrl}");
            Application.OpenURL(browserUrl);
            LogError("Failed to fetch the latest Python installer URL.");
            return new ProcessResult<T>(null, "Failed to fetch the Python installer URL", -1);
        }
    }

    // Fetch the latest Python installer URL
    private async Task<ProcessResult<T>> GetLatestPythonVersionUrl<T>()
    {
        string latestPythonApiUrl = "https://www.python.org/api/v2/downloads/release/?is_published=true&limit=1";

        try
        {
            var response = await client.GetStringAsync(latestPythonApiUrl);
            var json = JObject.Parse(response);
            var release = json["results"].FirstOrDefault();

            var windowsUrl = release?["files"]
                .FirstOrDefault(f => f["filename"].ToString().EndsWith(".exe"))?["url"].ToString();

            var macosUrl = release?["files"]
                .FirstOrDefault(f => f["filename"].ToString().EndsWith(".pkg"))?["url"].ToString();

            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                Log($"Latest Windows Installer: {windowsUrl}");
                return new ProcessResult<T>(windowsUrl, null, 0);
            }
            if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
            {
                Log($"Latest macOS Installer: {macosUrl}");
                return new ProcessResult<T>(macosUrl, null, 0);
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to fetch the latest Python version: {ex.Message}");
            return new ProcessResult<T>(null, $"Error fetching Python URL: {ex.Message}", -1);
        }

        return new ProcessResult<T>(null, "Unsupported platform or no URL found", -1);
    }

    // Download the Python installer and trigger installation
    private async Task<ProcessResult<T>> DownloadAndInstallPython<T>(string installerUrl)
    {
        try
        {
            string tempPath = Path.Combine(Application.temporaryCachePath, Path.GetFileName(installerUrl));

            Log($"Downloading Python installer from: {installerUrl}");
            Log($"Saving installer to: {tempPath}");

            using (var response = await client.GetAsync(installerUrl))
            using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream);
            }

            Log("Download completed. Starting installation...");

            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                return InstallOnWindows<T>(tempPath);
            }
            else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
            {
                return InstallOnMacOS<T>(tempPath);
            }
        }
        catch (Exception ex)
        {
            LogError($"Error during download and installation: {ex.Message}");
            return new ProcessResult<T>(null, $"Error downloading installer: {ex.Message}", -1);
        }

        return new ProcessResult<T>(null, "Platform not supported for installation", -1);
    }

    // Trigger installation on Windows
    private ProcessResult<T> InstallOnWindows<T>(string installerPath)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = installerPath,
                    Arguments = "/quiet InstallAllUsers=1 PrependPath=1",
                    UseShellExecute = true
                }
            };
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Log("Python installation completed successfully on Windows.");
                return new ProcessResult<T>("Python installed successfully", null, 0);
            }
            else
            {
                LogError($"Python installation failed on Windows with exit code: {process.ExitCode}");
                return new ProcessResult<T>(null, $"Installation failed with exit code: {process.ExitCode}", process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to start Python installation on Windows: {ex.Message}");
            return new ProcessResult<T>(null, $"Error during Windows installation: {ex.Message}", -1);
        }
    }

    // Trigger installation on macOS
    private ProcessResult<T> InstallOnMacOS<T>(string installerPath)
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo =
                {
                    FileName = "/usr/sbin/installer",
                    Arguments = $"-pkg \"{installerPath}\" -target /",
                    UseShellExecute = true
                }
            };
            process.Start();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                Log("Python installation completed successfully on macOS.");
                return new ProcessResult<T>("Python installed successfully", null, 0);
            }
            else
            {
                LogError($"Python installation failed on macOS with exit code: {process.ExitCode}");
                return new ProcessResult<T>(null, $"Installation failed with exit code: {process.ExitCode}", process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            LogError($"Failed to start Python installation on macOS: {ex.Message}");
            return new ProcessResult<T>(null, $"Error during macOS installation: {ex.Message}", -1);
        }
    }
}