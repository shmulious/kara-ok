using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class GentleInstaller : MonoBehaviour
{
    public Button installButton;
    private string pythonScriptRelativePath = "StreamingAssets/ExternalScripts/KaraOK_1.0/scripts/main/gentle_installer.py";

    void Start()
    {
        // Add a listener to the button to trigger the installation
        installButton.onClick.AddListener(async () => await InstallGentle());
    }

    async Task InstallGentle()
    {
        // Create the full path based on the Unity project
        string pythonScriptFullPath = System.IO.Path.Combine(Application.dataPath, pythonScriptRelativePath);
        string installPath = Path.Combine(Application.persistentDataPath, "gentle_folder");
        // Create process to run the Python gentle_installer.py script with install path argument
        Process process = new Process();
        process.StartInfo.FileName = "python3";
        process.StartInfo.Arguments = $"{pythonScriptFullPath} '{installPath}'";  // Pass the install path as an argument
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        KaraokLogger.Log(process.StartInfo.Arguments);
        process.OutputDataReceived += (sender, args) => KaraokLogger.Log("Output: {0}", args.Data);
        process.ErrorDataReceived += (sender, args) => KaraokLogger.LogError("Error: {0}", args.Data);

        // Start the process asynchronously and wait for it to finish
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await Task.Run(() => process.WaitForExit());

        // Check the exit code to log success or failure
        if (process.ExitCode == 0)
        {
            KaraokLogger.Log("Gentle installation completed successfully.");
        }
        else
        {
            KaraokLogger.LogError("Gentle installation failed with exit code {0}.", process.ExitCode);
        }
    }
}