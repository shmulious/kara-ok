using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DataClasses;
using Newtonsoft.Json;
using RTLTMPro;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class YouTubeURLListItemView : MonoBehaviour
{
    [SerializeField] private GameObject _thumbnailStatusContainer;
    [SerializeField] private Button _thumbnailButton;
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

    private int _currentThumbnailIndex = 0;
    private UnityAction<YouTubeURLListItemView> _onRemove;
    private UnityAction<YouTubeURLListItemView, string> _onProcess;
    private LoadingAnimationManager _loadingAnimation;
    private PythonRunner _pythonRunner;
    private string _openFolderPath;
    private SongMetadata _metadata;

    public string URL => _youtubeUrlInputField.text;

    private SongMetadata Metadata
    {
        get => _metadata;
        set
        {
            _metadata = value;
            _openFolderButton.interactable = _metadata?.CachedFiles?.Count() > 0;
            _openFolderPath = _metadata?.CachePath;
        }
    }

    public void RegisterOnRemove(UnityAction<YouTubeURLListItemView> onRemove) => _onRemove += onRemove;

    public void RegisterOnProcess(UnityAction<YouTubeURLListItemView, string> onProcess) => _onProcess += onProcess;

    public void SetLoadingAnimationManager(LoadingAnimationManager loadingAnimationManager) => _loadingAnimation = loadingAnimationManager;

    void Start()
    {
        _pythonRunner = new PythonRunner();
        InitializeButtonListeners();
        SetDefaultTexts();
        FocusOnInputField();
    }

    private void InitializeButtonListeners()
    {
        _removeButton.onClick.AddListener(OnRemoveButtonClicked);
        _processButton.onClick.AddListener(OnProcessButtonClicked);
        _getInfoButton.onClick.AddListener(OnGetInfoButtonClicked);
        _openFolderButton.onClick.AddListener(OnOpenButtonClicked);
        _pasteButton.onClick.AddListener(OnPasteButtonClicked);
        _youtubeUrlInputField.onDeselect.AddListener(OnYouTubeURLDeselect);
    }

    private void UnregisterButtonListeners()
    {
        _removeButton.onClick.RemoveAllListeners();
        _processButton.onClick.RemoveAllListeners();
        _getInfoButton.onClick.RemoveAllListeners();
        _openFolderButton.onClick.RemoveAllListeners();
        _pasteButton.onClick.RemoveAllListeners();
        _youtubeUrlInputField.onDeselect.RemoveAllListeners();
    }

    private void SetDefaultTexts()
    {
        _itemArtist.text = "No Data";
        _itemTitle.text = "No Data";
    }

    private void FocusOnInputField()
    {
        _youtubeUrlInputField.ActivateInputField();
        EventSystem.current.SetSelectedGameObject(_youtubeUrlInputField.gameObject, null);
    }

    private void OnYouTubeURLDeselect(string arg0) => _getInfoButton.interactable = GetUrlType(_youtubeUrlInputField.text) != UrlType.Invalid;

    private void OnPasteButtonClicked()
    {
        string clipboardContent = GUIUtility.systemCopyBuffer;
        KaraokLogger.Log($"Pasted value: {clipboardContent}");
        SetURL(clipboardContent);
    }

    private async void OnOpenButtonClicked()
    {
        _openFolderButton.interactable = false;
        PathOpener.OpenPath(Metadata.CachePath);
        await Task.Delay(2000);
        _openFolderButton.interactable = true;
    }

    private void OnGetInfoButtonClicked()
    {
        ExtractGetMetadataAndUpdateUI();
    }

    private void ExtractGetMetadataAndUpdateUI()
    {
        if (GetUrlType(URL) == UrlType.Invalid) return;
        
        _getInfoButton.interactable = false;
        _youtubeUrlInputField.interactable = false;
        GetMetadata();
    }

    private UrlType GetUrlType(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return UrlType.Invalid;

        string youtubePattern = @"^(https?://)?(www\.)?(youtube\.com|youtu\.be)/(watch\?v=|embed/|v/|.+\?v=)?([a-zA-Z0-9_-]{11})";
        string smulePattern = @"^(https?://)?(www\.)?smule\.com/";

        if (Regex.IsMatch(url, youtubePattern)) return UrlType.YouTube;
        if (Regex.IsMatch(url, smulePattern)) return UrlType.Smule;

        return UrlType.Invalid;
    }

    private async void GetMetadata()
    {
        _statusText.text = "Fetching song's metadata...";
        _thumbnailStatusContainer.SetActive(true);

        switch (GetUrlType(_youtubeUrlInputField.text))
        {
            case UrlType.YouTube:
                _getInfoButton.interactable = false;
                await FetchMetadataAsync();
                break;

            case UrlType.Smule:
                _statusText.text = "Downloading from Smule...";
                LockUI(true);
                await DownloadFromSmule(_youtubeUrlInputField.text);
                LockUI(false);
                break;

            case UrlType.Invalid:
                _statusText.text = "URL is invalid";
                _youtubeUrlInputField.interactable = true;
                break;
        }
    }

    private async Task FetchMetadataAsync()
    {
        var isDone = false;
        _loadingAnimation.StartLoadingScreen(_thumbnailButton.transform, () => isDone, Color.black);
        var metadata = await CacheManager.LoadMetadata(URL);
        isDone = true;

        if (metadata != null)
        {
            UpdateMetadataUI(true, metadata);
            _statusText.text = "Metadata fetched!";
        }
        else
        {
            UpdateMetadataUI(false, null);
            _statusText.text = "Error loading song's metadata. Try again!";
        }
    }

    private void UpdateMetadataUI(bool isSuccess, SongMetadata metadata)
    {
        _youtubeUrlInputField.gameObject.SetActive(!isSuccess);
        _metadataContainer.SetActive(isSuccess);
        _processButton.interactable = isSuccess;
        _thumbnailStatusContainer.SetActive(isSuccess);
        _getInfoButton.interactable = true;

        if (isSuccess && metadata != null)
        {
            _itemTitle.text = metadata.Title;
            _itemArtist.text = metadata.Artist;
            _thumbnailButton.image.sprite = ThumbnailsDownloader.SpriteFromTexture(metadata.ThumbnailDatas[0].Texture);
            Metadata = metadata;
        }
    }

    private async void OnProcessButtonClicked()
    {
        var outputFolderPath = Path.Combine(ProcessRunnerBase.ENV_PATH, "output");
        var model = 3;
        var result = await Process(outputFolderPath, model);

        var urlType = GetUrlType(_youtubeUrlInputField.text);
        switch (urlType){
            case UrlType.Invalid:
                break;
            case UrlType.YouTube:
                break;
            case UrlType.Smule:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void OnRemoveButtonClicked() => _onRemove?.Invoke(this);

    private void OnDestroy() => UnregisterButtonListeners();

    public async Task<ProcessResult<string>> Process(string outputFolderPath, int modelNumber)
    {
        LockUI(true);
        _statusText.text = "Processing song...";
        var isDone = false;
        _loadingAnimation.StartLoadingScreen(_thumbnailStatusContainer.transform, () => isDone, null);
        _onProcess?.Invoke(this, _youtubeUrlInputField.text);
        ProcessResult<string> result = null;
        switch (GetUrlType(URL))
        {
            case UrlType.YouTube:
                _statusText.text = "Processing youTube URL...";
                Metadata = await CacheManager.LoadMetadata(URL);
                result = await SmuleService.ProcessSongFromYoutube(Metadata, outputFolderPath, modelNumber);
                break;
            case UrlType.Smule:
                _statusText.text = "Processing Smule URL...";
                result = await SmuleService.ProcessSmuleUrl(URL);
                break;
            case UrlType.Invalid:
            default:
                throw new ArgumentOutOfRangeException();
        }
        isDone = true;
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

    internal void SetURL(string urlValue)
    {
        if (GetUrlType(urlValue) == UrlType.Invalid) return;
        
        _youtubeUrlInputField.text = urlValue;
    }
    
    public async Task<ProcessResult<string>> DownloadFromSmule(string smuleUrl)
    {
        _statusText.text = "Downloading from Smule...";
        var isDone = false;
        _loadingAnimation.StartLoadingScreen(_thumbnailStatusContainer.transform, () => isDone, null);

        // Smule download logic
        var result = await SmuleService.ProcessSmuleUrl(smuleUrl);
        
        isDone = true;
        
        if (result.Success)
        {
            _statusText.text = "Download completed successfully!";
            _openFolderPath = result.StringVal;
        }
        else
        {
            _statusText.text = "Failed to download from Smule.";
        }

        LockUI(false);
        return result;
    }

    private enum UrlType
    {
        Invalid,
        YouTube,
        Smule
    }
}