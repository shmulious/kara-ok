using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class YouTubeURLListItemView : MonoBehaviour
{
    [SerializeField] private TMP_InputField _youtubeUrlInputField;
    [SerializeField] private Button _processButton;
    [SerializeField] private Button _removeButton;
    [SerializeField] private Image _itemStatusIndicator;

    private UnityAction<YouTubeURLListItemView> _onRemove;
    private UnityAction<YouTubeURLListItemView, string> _onProcess;

    public void RegisterOnRemove(UnityAction<YouTubeURLListItemView> onRemove)
    {
        _onRemove += onRemove;
    }
    public void RegisterOnProcess(UnityAction<YouTubeURLListItemView,string> onProcess)
    {
        _onProcess += onProcess;
    }

    // Start is called before the first frame update
    void Start()
    {
        _removeButton.onClick.AddListener(OnRemoveButtonClicked);
        _processButton.onClick.AddListener(OnProcessButtonClicked);
    }

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
        _onRemove = null;
    }
    public string GetText()
    {
        return _youtubeUrlInputField.text;
    }
}
