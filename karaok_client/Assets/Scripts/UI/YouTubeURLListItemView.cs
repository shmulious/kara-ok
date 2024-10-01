using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class YouTubeURLListItemView : MonoBehaviour
{
    [SerializeField] private TMP_InputField _youtubeUrlInputField;
    [SerializeField] private Button _processButton;
    [SerializeField] private Button _removeButton;
    [SerializeField] private Image _itemStatusIndicator;
    [SerializeField] private TMP_Text _itemArtist;
    [SerializeField] private TMP_Text _itemTitle;
    [SerializeField] private Button _thumbnailButton; // Changed from Image to Button

    private SongMetadataData _dataObject;

    public string SongTitle
    {
        get { return _itemTitle.text; }
    }

    //// Public property for ThumbnailsData with private set
    //public List<ThumbnailData> ThumbnailsData { get; private set; } = new List<ThumbnailData>();
    ///// <summary>
    ///// Public getter for the current ThumbnailData object.
    ///// </summary>
    //public ThumbnailData CurrentThumbnailData
    //{
    //    get
    //    {
    //        if (ThumbnailsData != null && ThumbnailsData.Count > 0)
    //        {
    //            return ThumbnailsData[_currentThumbnailIndex];
    //        }
    //        return null;
    //    }
    //}
    private int _currentThumbnailIndex = 0; // Tracks the current thumbnail for cycling

    private UnityAction<YouTubeURLListItemView> _onRemove;
    private UnityAction<YouTubeURLListItemView, string> _onProcess;
    private PythonRunner _pythonRunner;
    private SongMetadataGenius _metadataProvider;

    public void RegisterOnRemove(UnityAction<YouTubeURLListItemView> onRemove)
    {
        _onRemove += onRemove;
    }

    public void RegisterOnProcess(UnityAction<YouTubeURLListItemView, string> onProcess)
    {
        _onProcess += onProcess;
    }

    // Start is called before the first frame update
    void Start()
    {
        _pythonRunner = new PythonRunner();
        _removeButton.onClick.AddListener(OnRemoveButtonClicked);
        _processButton.onClick.AddListener(OnProcessButtonClicked);
        _youtubeUrlInputField.onDeselect.AddListener(OnURLInputDeselect);
       // _thumbnailButton.onClick.AddListener(OnThumbnailButtonClicked); // Add listener for thumbnail button click
        _metadataProvider = new SongMetadataGenius(_pythonRunner);
    }

    private bool IsValidYoutubeUrl(string url)
    {
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

    private async void OnURLInputDeselect(string url)
    {
        if (!IsValidYoutubeUrl(url)) return;
        
        var metadata = await _metadataProvider.FetchMetadata(url);
        _itemTitle.text = metadata.Title;
        _itemArtist.text = metadata.Artist;
        _thumbnailButton.image.sprite = metadata.ThumbnailData.ThumbnailSprite;
        _dataObject = metadata;
    }
    //private async void OnURLInputDeselect(string arg0)
    //{
    //    if (!IsValidYoutubeUrl(arg0)) return;

    //    var res = await _pythonRunner.RunProcess<YoutubeMetadata>("main/smule.py", $"--getmetadata \"{arg0}\"");
    //    if (res.Success && res.Value != null)
    //    {
    //        _dataObject = new SongMetadataData(arg0, res.Value.artist, res.Value.title);
    //        if (res.Value.thumbnails != null)
    //        {
    //            _itemArtist.text = res.Value.artist;
    //            _itemTitle.text = res.Value.title;

    //            // Download the thumbnails and save them to ThumbnailsData
    //            ThumbnailsData = await res.Value.DownloadThumbnails();
    //            if (ThumbnailsData.Count > 0)
    //            {
    //                _thumbnailButton.image.sprite = ThumbnailsData[0].ThumbnailSprite; // Set the first thumbnail
    //                _currentThumbnailIndex = 0; // Reset index to the first item
    //                _dataObject.ThumbnailData = ThumbnailsData[0];
    //            }
    //        }
    //    }
    //}

    //private void OnThumbnailButtonClicked()
    //{
    //    if (ThumbnailsData == null || ThumbnailsData.Count == 0)
    //    {
    //        Debug.LogWarning("No thumbnails to display.");
    //        return;
    //    }

    //    // Cycle to the next thumbnail
    //    _currentThumbnailIndex = (_currentThumbnailIndex + 1) % ThumbnailsData.Count; // Loop back to the start after the last item
    //    _thumbnailButton.image.sprite = ThumbnailsData[_currentThumbnailIndex].ThumbnailSprite; // Update button image
    //    if (_dataObject != null) _dataObject.ThumbnailData = ThumbnailsData[_currentThumbnailIndex];
    //}

    private void OnProcessButtonClicked()
    {
        if (ValidateLinkInput())
        {
            _onProcess?.Invoke(this, _youtubeUrlInputField.text);
        }
    }

    private bool ValidateLinkInput()
    {
        //todo: regex for common youtube link formats
        bool isYoutubeLinkFormat = true;

        return !string.IsNullOrWhiteSpace(_youtubeUrlInputField.text) && isYoutubeLinkFormat;
    }

    private void OnRemoveButtonClicked()
    {
        _onRemove?.Invoke(this);
        //todo: implement remove;
    }

    private void OnDestroy()
    {
        _removeButton.onClick.RemoveAllListeners();
        _processButton.onClick.RemoveAllListeners();
        _thumbnailButton.onClick.RemoveAllListeners(); // Remove listener for thumbnail button
        _onRemove = null;
        _youtubeUrlInputField.onValueChanged.RemoveAllListeners();
    }

    public string GetText()
    {
        return _youtubeUrlInputField.text;
    }

    public async Task<ProcessResult<string>> Process(string outputFolderPath, int modelNumber)
    {
        return await SmuleService.ProcessSong(_dataObject, outputFolderPath, modelNumber);
    }
}

public class SongMetadataData
{
    public string URL;
    public string Artist;
    public string Title;
    public ThumbnailData ThumbnailData;
    public string Lyrics;

    public SongMetadataData(string uRL, string artist, string title)
    {
        URL = uRL;
        Artist = artist;
        Title = title;
    }
}