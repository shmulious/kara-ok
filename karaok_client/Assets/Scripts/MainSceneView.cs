using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using UI;
using TMPro;

public class MainSceneView : MonoBehaviour
{
    [SerializeField] private Button _pythonButton;
    [SerializeField] private Button _envInstallerButton;
    [SerializeField] private Button _demoButton;
    [SerializeField] private Button _processListButton;
    [SerializeField] private URLItemsListView _listHolder;
    [SerializeField] private TMP_Dropdown _modelDropdown;

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

        var res1 = await _pythonRunner.RunProcess<string>("main/smule.py", $"--init \"{ProcessRunnerBase.ENV_PATH}\"");
        Debug.Log($"[MainSceneView] - smule.py init result: {res1.Output}");

        var envExistsRes = await _pythonRunner.RunProcess<bool>("main/smule.py", "--environmentExists");
        var envExistsValid = envExistsRes.Value;
        var envExists = envExistsRes.Success;// && envExistsValid;
        Debug.Log($"[MainSceneView] - Environment exists: {envExists}");


        Debug.Log($"[MainSceneView] - Awake conditions: isPythonInstalled: {isPythonInstalled}; envExists: {envExists};");

        _envInstallerButton.interactable = isPythonInstalled && (!envExists);
        _demoButton.interactable = isPythonInstalled && envExists;
        _processListButton.interactable = isPythonInstalled && envExists;
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
        if (_buttonsTasks.TryGetValue(button.GetInstanceID(), out var task))
        {
            button.interactable = false;
            try
            {
                Debug.Log($"[MainSceneView] - Executing task for button: {button.name}");
                await task();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainSceneView] - Error executing task for button: {button.name} - {ex.Message}");
            }
            finally
            {
                button.interactable = true;
            }
        }
        else
        {
            Debug.LogWarning($"[MainSceneView] - No task found for button: {button.name}");
        }
    }

    private void OnApplicationQuit()
    {
        SmuleService.Dispose();
        SmuleDownloader.Dispose();
        PythonRunner.Dispose();
    }

    private async Task ProcessList()
    {
        Debug.Log("[MainSceneView] - ProcessList method started");

        var youtubeUrls = _listHolder._listItems;
        var modelNumber = _modelDropdown.value + 1;
        var outputFolderPath = Path.Combine(ProcessRunnerBase.ENV_PATH, "output");
        _listHolder.LockListItemsExcept(null, true);
        foreach (var item in youtubeUrls)
        {
            var result = await item.Process(outputFolderPath, modelNumber);
        }
        _listHolder.LockListItemsExcept(null, false);
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
        _processListButton.interactable = res.Success;
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