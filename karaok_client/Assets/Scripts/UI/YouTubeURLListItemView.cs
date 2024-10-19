using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class YouTubeURLListItemView : MonoBehaviour
{
    [SerializeField] private GameObject _thumbnailStatusContainer;
    [SerializeField] private Button _thumbnailButton; // Changed from Image to Button
    [SerializeField] private GameObject _metadataContainer;
    [SerializeField] private RTLTextMeshPro _itemArtist;
    [SerializeField] private RTLTextMeshPro _itemTitle;
    [SerializeField] private TMP_InputField _youtubeUrlInputField;
    [SerializeField] private Button _pasteButton;
    [SerializeField] private Button _processButton;
    [SerializeField] private Button _getInfoButton;
    [SerializeField] private Button _openFolderButton;
    [SerializeField] private Button _removeButton;
    [SerializeField] private TMP_Text _statusText;

    public string URL
    {
        get { return _youtubeUrlInputField.text; }
    }

    private int _currentThumbnailIndex = 0; // Tracks the current thumbnail for cycling

    private UnityAction<YouTubeURLListItemView> _onRemove;
    private UnityAction<YouTubeURLListItemView, string> _onProcess;
    [SerializeField] private LoadingAnimationManager _loadingAnimation;
    private PythonRunner _pythonRunner;
    private string _openFolderPath;

    public void RegisterOnRemove(UnityAction<YouTubeURLListItemView> onRemove)
    {
        _onRemove += onRemove;
    }

    public void RegisterOnProcess(UnityAction<YouTubeURLListItemView, string> onProcess)
    {
        _onProcess += onProcess;
    }

    public void SetLoadingANimationManager(LoadingAnimationManager loadingAnimationManager)
    {
        _loadingAnimation = loadingAnimationManager;
    }

    // Start is called before the first frame update
    void Start()
    {
        _pythonRunner = new PythonRunner();
        _removeButton.onClick.AddListener(OnRemoveButtonClicked);
        _processButton.onClick.AddListener(OnProcessButtonClicked);
        _getInfoButton.onClick.AddListener(OnGetInfoButtonClicked);
        _openFolderButton.onClick.AddListener(OnOpenButtonClicked);
        _pasteButton.onClick.AddListener(OnPasteButtonClicked);
        _youtubeUrlInputField.onDeselect.AddListener(OnYouTubeURLDeselect);
        // _thumbnailButton.onClick.AddListener(OnThumbnailButtonClicked); // Add listener for thumbnail button click
        _itemArtist.text = "No Data";
        _itemTitle.text = "No Data";
    }

    private void OnYouTubeURLDeselect(string arg0)
    {
        _getInfoButton.interactable = IsValidYoutubeUrl();
    }

    private void OnPasteButtonClicked()
    {
        string clipboardContent = GUIUtility.systemCopyBuffer;
        KaraokLogger.Log($"Pasted value: {clipboardContent}");
        _youtubeUrlInputField.text = clipboardContent;
        if (IsValidYoutubeUrl())
        {
            OnGetInfoButtonClicked();
        }
    }

    private async void OnOpenButtonClicked()
    {
        _openFolderButton.interactable = false;
        PathOpener.OpenPath(_openFolderPath);
        await Task.Delay(2000);
        _openFolderButton.interactable = true;
    }

    private void OnGetInfoButtonClicked()
    {
        _getInfoButton.interactable = false;
        _youtubeUrlInputField.interactable = false;
        _statusText.text = "Fetching song's metadata...";
        GetMetadata();
    }

    private bool IsValidYoutubeUrl()
    {
        var url = _youtubeUrlInputField.text;
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Regular expression pattern to validate YouTube URLs
        string pattern = @"^(https?://)?(www\.)?(youtube\.com|youtu\.be)/(watch\?v=|embed/|v/|.+\?v=)?([a-zA-Z0-9_-]{11})";

        // Use Regex to match the URL pattern
        Regex regex = new Regex(pattern);
        return regex.IsMatch(url);
    }

    private async void GetMetadata()
    {
        if (!IsValidYoutubeUrl())
        {
            _statusText.text = "YouTube URL is invalid";
            _youtubeUrlInputField.interactable = true;
            return;
        }
        
        _thumbnailStatusContainer.SetActive(true);
        _getInfoButton.interactable = false;

        var isDone = false;

        _loadingAnimation.StartLoadingScreen(_thumbnailButton.transform, () => { return isDone; }, Color.black);
        var metadata = await CacheManager.LoadMetadata(URL);
        isDone = true;

        if (metadata != null)
        {
            UpdateGetMetadataUI(true);
            _itemTitle.text = metadata.Title;
            _itemTitle.UpdateText();
            _itemArtist.text = metadata.Artist;
            _itemArtist.UpdateText();

            _thumbnailButton.image.sprite = ThumbnailsDownloader.SpriteFromTexture(metadata.ThumbnailDatas[0].Texture);
            
        }
        else
        {
            UpdateGetMetadataUI(false);
            _statusText.text = "Error loading song's metadata. Try again!";
            //return null;
        }
    }

    private void UpdateGetMetadataUI(bool isMetadataSuccess)
    {
        _youtubeUrlInputField.gameObject.SetActive(!isMetadataSuccess);
        _metadataContainer.SetActive(isMetadataSuccess);
        _processButton.interactable = isMetadataSuccess;
        _thumbnailStatusContainer.SetActive(isMetadataSuccess);
        _getInfoButton.interactable = true;
    }

    private async void OnProcessButtonClicked()
    {
        if (IsValidYoutubeUrl())
        {
            _onProcess?.Invoke(this, URL);
            var metadata = await CacheManager.LoadMetadata(URL);
        }
    }

    private void OnRemoveButtonClicked()
    {
        _onRemove?.Invoke(this);
    }

    private void OnDestroy()
    {
        _removeButton.onClick.RemoveAllListeners();
        _processButton.onClick.RemoveAllListeners();
        _openFolderButton.onClick.RemoveAllListeners(); // Remove listener for thumbnail button
        _pasteButton.onClick.RemoveAllListeners(); // Remove listener for thumbnail button
        _getInfoButton.onClick.RemoveAllListeners(); // Remove listener for thumbnail button
        _onRemove = null;
    }

    public string GetText()
    {
        return _youtubeUrlInputField.text;
    }

    public async Task<ProcessResult<string>> Process(string outputFolderPath, int modelNumber)
    {
        LockUI(true);


        var isDone = false;
        _loadingAnimation.StartLoadingScreen(_thumbnailStatusContainer.transform, () => { return isDone; }, null);
        _onProcess?.Invoke(this, _youtubeUrlInputField.text);
        var metadata = await CacheManager.LoadMetadata(URL);
        var result = await SmuleService.ProcessSong(metadata, outputFolderPath, modelNumber);
        isDone = true;
        _openFolderButton.interactable = result.Success;
        _statusText.text = result.Success ? "Finished successfully!" : "Failed with some errors!";
        _openFolderPath = result.Success ? result.StringVal : null; 
        return result;

    }

    internal void LockUI(bool shouldLock)
    {
        var isInteractable = !shouldLock;
        _removeButton.interactable = isInteractable;
        _processButton.interactable = isInteractable;
        _getInfoButton.interactable = isInteractable;
        _openFolderButton.interactable = isInteractable;
        _pasteButton.interactable = isInteractable;
        _youtubeUrlInputField.interactable = isInteractable;
    }
}