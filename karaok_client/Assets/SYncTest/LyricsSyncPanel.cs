using System;
using System.Collections.Generic;
using System.IO;
using DataClasses;
using UnityEngine;
using UnityEngine.UI;

namespace SYncTest
{
    public class LyricsSyncPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _scrollViewContent;
        [SerializeField] private LyricsSynchronizerLine _linePrefab;
        [SerializeField] private Button _xButton;
        
        private AudioClip _audioClip;
        private string[] _lines;
        private List<LyricsSynchronizerLine> _linesList = new List<LyricsSynchronizerLine>();
        private LyricsSynchronizer _synchronizer;
        private GameObject _keyDetector;

        [SerializeField]
        private Vector2 _scrollSteps;

        private bool _finishedSynching;
        private SongMetadata _metadata;

        private void Start()
        {
            _xButton.onClick.AddListener(OnXButtonClicked);
        }

        public async void Init(SongMetadata songMetadata)
        {
            _metadata = songMetadata;
            _lines = songMetadata.Lyrics.Split('\n');
            _audioClip = await songMetadata.CachedFiles.LoadMediaAsync<AudioClip>(CachedSongFiles.FileKey.Original.ToString());
            foreach (var line in _lines)
            {
                var lineObject = Instantiate(_linePrefab, _scrollViewContent);
                lineObject.SetLyrics(line);
                _linesList.Add(lineObject);
            }
        
            CreateKeyDetector(KeyCode.Space, StartSync);
        }

        private void CreateKeyDetector(KeyCode keyCode, Action action)
        {
            if (_keyDetector != null)
            {
                Destroy(_keyDetector);
            }
            
            var keyDetector = new GameObject("KeyDetector").AddComponent<KeyDetector>();
            keyDetector.Subscribe(keyCode, action);
        }

        private void StartSync()
        {
            Destroy(_keyDetector);
            _synchronizer = new LyricsSynchronizer(_lines, _audioClip);
            _synchronizer.OnStart += OnStartSync;
            _synchronizer.OnFinished += OnFinishSync;
            _synchronizer.OnRegisteredLine += OnRegisteredLine;
        
            _synchronizer.StartSynchronizingAsync();
        }

        private void OnXButtonClicked()
        {
            _synchronizer?.Dispose();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _xButton.onClick.RemoveListener(OnXButtonClicked);
            Destroy(_keyDetector);
            _synchronizer.Dispose();
        }

        private async void CreateVideo()
        {
            if (!_finishedSynching) return;
            
            var result = await SmuleService.CreateVideo(
                Path.Combine(_metadata.CachePath, "subtitles.srt"),
                _metadata.CachedFiles[CachedSongFiles.FileKey.FinalPlayback].LocalPath);
            KaraokLogger.Log($"CreateVideo: {result.Success}");
        }

        private void OnRegisteredLine(string timeStamp, int lineIndex)
        {
            _linesList[lineIndex].SetTimeStamp(timeStamp);
            _linesList[lineIndex].SetCurrentLineImage(false);

            if (_linesList.Count - 1 > lineIndex + 1)
            {
                _linesList[lineIndex + 1].SetCurrentLineImage(true);
            }

            if (lineIndex >= 4)
            {
                _scrollViewContent.anchoredPosition += _scrollSteps;
            }
                
        }

        private void OnStartSync()
        {
            KaraokLogger.Log("StartSync");
        }

        private void OnFinishSync()
        {
            _finishedSynching = true;
            KaraokLogger.Log("FinishSync");
            _synchronizer.ExportToSRT(Path.Combine(_metadata.CachePath, "subtitles.srt"));
            CreateKeyDetector(KeyCode.KeypadEnter, CreateVideo);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
