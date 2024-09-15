using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using UI;

public class MainSceneView : MonoBehaviour
{
    [SerializeField] private Button _pythonButton;
    [SerializeField] private Button _envInstallerButton;
    [SerializeField] private Button _demoButton;
    [SerializeField] private Button _processListButton;
    [SerializeField] private URLItemsListView _listHolder;

    private PythonInstaller _pythonInstaller;
    private PythonRunner _pythonRunner;

    private async void Awake()
    {
        Debug.Log("[MainSceneView] - Awake method started");

        _pythonInstaller = new PythonInstaller();
        _pythonRunner = new PythonRunner();

        // Ensure Python installation check is asynchronous
        var isPythonInstalled = await PythonInstaller.IsPythonInstalled();
        Debug.Log($"[MainSceneView] - Is Python installed: {isPythonInstalled}");

        _pythonButton.interactable = !isPythonInstalled;

        RegisterButtonAction(_pythonButton, InstallPython);
        RegisterButtonAction(_envInstallerButton, InstallEnvironment);
        RegisterButtonAction(_demoButton, RunDemo);
        RegisterButtonAction(_processListButton, ProcessList);

        var res1 = await _pythonRunner.RunProcess<string>("main/smule.py", $"--init '{ProcessRunnerBase.ENV_PATH}'");
        Debug.Log($"[MainSceneView] - smule.py init result: {res1.Output}");

        var envExistsRes = await _pythonRunner.RunProcess<bool>("main/smule.py", "--environmentExists");
        var envExistsValid = envExistsRes.TryGetValues<bool>(out var possibleVals);
        var envExists = possibleVals.FirstOrDefault() && envExistsValid;
        Debug.Log($"[MainSceneView] - Environment exists: {envExists}");

        var isFfmpegInstalled = FFmpegInstaller.IsFFmpegInstalled();
        Debug.Log($"[MainSceneView] - Is FFmpeg installed: {isFfmpegInstalled}");

        Debug.Log($"[MainSceneView] - Awake conditions: isPythonInstalled: {isPythonInstalled}; envExists: {envExists}; isFfmpegInstalled: {isFfmpegInstalled}");

        _envInstallerButton.interactable = isPythonInstalled && (!envExists || !FFmpegInstaller.IsFFmpegInstalled());
        _demoButton.interactable = isPythonInstalled && envExists && FFmpegInstaller.IsFFmpegInstalled();
        _processListButton.interactable = isPythonInstalled && envExists && FFmpegInstaller.IsFFmpegInstalled();
    }

    private Dictionary<int, Func<Task>> _buttonsTasks = new Dictionary<int, Func<Task>>();

    private void RegisterButtonAction(Button button, Func<Task> task)
    {
        Debug.Log($"[MainSceneView] - RegisterButtonAction for button: {button.name}");
        _buttonsTasks[button.GetInstanceID()] = task;
        button.onClick.AddListener(async () =>
        {
            await OnMainButtonClicked(button);
        });
    }

    private async Task OnMainButtonClicked(Button button)
    {
        Debug.Log($"[MainSceneView] - OnMainButtonClicked for button: {button.name}");
        button.interactable = false;

        if (_buttonsTasks.TryGetValue(button.GetInstanceID(), out var task))
        {
            try
            {
                Debug.Log($"[MainSceneView] - Executing task for button: {button.name}");
                await task();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainSceneView] - Error executing task for button: {button.name} - {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[MainSceneView] - No task found for button: {button.name}");
        }

        button.interactable = true;
    }

    private async Task ProcessList()
    {
        Debug.Log("[MainSceneView] - ProcessList method started");

        var youtubeUrls = _listHolder._listItems.Select(ipf => ipf.GetText());

        foreach (var item in youtubeUrls)
        {
            Debug.Log($"[MainSceneView] - Processing URL: {item}");
            var res = await _pythonRunner.RunProcess<string>("main/smule.py", $"--convert '{item}' '{Path.Combine(ProcessRunnerBase.ENV_PATH, "output")}' 2");
            Debug.Log($"[MainSceneView] - Finished processing {item}");
            Debug.Log($"[MainSceneView] - Result: {JsonConvert.SerializeObject(res)}");
        }
    }

    private async Task RunDemo()
    {
        Debug.Log("[MainSceneView] - RunDemo method started");
        var res = await _pythonRunner.RunProcess<string>("main/smule.py", "--demo");
        Debug.Log($"[MainSceneView] - Demo result: {res}");
    }

    private async Task InstallEnvironment()
    {
        Debug.Log("[MainSceneView] - InstallEnvironment method started");
        var res = await _pythonRunner.RunProcess<string>("main/smule.py", "--install");
        Debug.Log($"[MainSceneView] - Environment Install Result: {res.Output}");
        _demoButton.interactable = res.Success;
    }

    private async Task InstallPython()
    {
        Debug.Log("[MainSceneView] - InstallPython method started");
        var res = await _pythonInstaller.RunProcess<string>(null, null);
        Debug.Log($"[MainSceneView] - Python Install Result: {res.Output}");
        _envInstallerButton.interactable = res.Success;
    }

    private void OnDestroy()
    {
        Debug.Log("[MainSceneView] - OnDestroy method called");
        _pythonButton.onClick.RemoveAllListeners();
        _envInstallerButton.onClick.RemoveAllListeners();
        _demoButton.onClick.RemoveAllListeners();
        _processListButton.onClick.RemoveAllListeners();
    }
}