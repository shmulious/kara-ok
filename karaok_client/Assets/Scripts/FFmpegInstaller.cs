using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

public class FFmpegInstaller : ProcessRunnerBase
{
    private static string ffmpegDownloadUrlMac = "https://evermeet.cx/ffmpeg/ffmpeg-4.4.1.zip";  // Example for macOS
    private static string ffmpegDownloadUrlWindows = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";  // Example for Windows
    private static string ffmpegExtractPath = Path.Combine(ENV_PATH, "ffmpeg");

    public FFmpegInstaller() : base() { }

    // Implementing the abstract method, returning the InstallFFmpeg result
    public override async Task<ProcessResult> RunProcess<T>(string scriptPath, string arguments = "")
    {
        return await InstallFFmpeg();
    }

    // Method to install FFmpeg
    public async Task<ProcessResult> InstallFFmpeg()
    {
        string url = GetFFmpegUrl();
        string ffmpegZipPath = Path.Combine(Application.persistentDataPath, "ffmpeg.zip");

        try
        {
            // Check if FFmpeg is already installed
            if (IsFFmpegInstalled())
            {
                string output = "FFmpeg is already installed.";
                Log(output);
                return new ProcessResult(output, "", 0);
            }

            // Download ffmpeg zip
            await DownloadFileAsync(url, ffmpegZipPath);

            // Extract ffmpeg zip
            ExtractFFmpeg(ffmpegZipPath, ffmpegExtractPath);

            // Set permissions for macOS
            if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
            {
                SetFFmpegPermissions();
            }

            // Clean up the zip file
            if (File.Exists(ffmpegZipPath))
            {
                File.Delete(ffmpegZipPath);
            }

            string successMessage = "FFmpeg installation completed.";
            Log(successMessage);
            return new ProcessResult(successMessage, "", 0);
        }
        catch (Exception ex)
        {
            LogError($"Failed to install FFmpeg: {ex.Message}");
            return new ProcessResult("", ex.Message, 1);
        }
    }

    // Determine the correct FFmpeg URL based on the platform
    private string GetFFmpegUrl()
    {
        if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
        {
            return ffmpegDownloadUrlMac;
        }
        else if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            return ffmpegDownloadUrlWindows;
        }
        else
        {
            throw new PlatformNotSupportedException("This platform is not supported.");
        }
    }

    // Check if FFmpeg is installed by verifying if the binary exists
    public static bool IsFFmpegInstalled()
    {
        string ffmpegPath = Path.Combine(ffmpegExtractPath, "ffmpeg");
        return File.Exists(ffmpegPath);
    }

    // Get the FFmpeg path for use
    public static string GetFFmpegPath()
    {
        return Path.Combine(ffmpegExtractPath, "ffmpeg");
    }

    // Asynchronously download FFmpeg
    private async Task DownloadFileAsync(string url, string outputPath)
    {
        using (WebClient client = new WebClient())
        {
            Log($"Downloading FFmpeg from {url}...");
            await client.DownloadFileTaskAsync(new Uri(url), outputPath);
        }
    }

    // Extract the downloaded FFmpeg archive
    private void ExtractFFmpeg(string zipPath, string extractPath)
    {
        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }

        Log("Extracting FFmpeg...");
        ZipFile.ExtractToDirectory(zipPath, extractPath);
    }

    // Set execute permissions on macOS
    private void SetFFmpegPermissions()
    {
        string ffmpegBinaryPath = Path.Combine(ffmpegExtractPath, "ffmpeg");
        if (File.Exists(ffmpegBinaryPath))
        {
            Log("Setting execute permissions for FFmpeg on macOS...");
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"chmod +x '{ffmpegBinaryPath}'\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process process = Process.Start(psi);
            process.WaitForExit();
        }
    }
}