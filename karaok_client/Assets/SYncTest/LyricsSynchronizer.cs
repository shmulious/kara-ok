using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SYncTest
{
    public class LyricsSynchronizer : IDisposable
    {
        public event Action OnStart;
        public event Action OnStop;
        public event Action OnFinished;
        public event Action<string, int> OnRegisteredLine;

        private readonly string[] _lyricsInLines;
        private readonly AudioClip _audioClip;
        private readonly Dictionary<float, string> _syncedLyrics = new Dictionary<float, string>();
        private readonly AudioSource _audioSource;
        private int _currentLyricIndex = -1;
        private bool _isSynchronizing = false;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly GameObject _keyDetector;

        public LyricsSynchronizer(string[] lyricsInLines, AudioClip audioClip)
        {
            this._lyricsInLines = lyricsInLines;
            this._audioClip = audioClip;

            // Initialize AudioSource dynamically
            GameObject audioObject = new GameObject("AudioSourceObject");
            _audioSource = audioObject.AddComponent<AudioSource>();
            _audioSource.clip = this._audioClip;
            CreateKeyDetector(KeyCode.Space, RegisterLineAndTime);
        }
        
        private void CreateKeyDetector(KeyCode keyCode, Action action)
        {
            if (_keyDetector != null)
            {
                GameObject.Destroy(_keyDetector);
            }
            
            var keyDetector = new GameObject("KeyDetector").AddComponent<KeyDetector>();
            keyDetector.Subscribe(keyCode, action);
        }

        public async Task StartSynchronizingAsync()
        {
            if (_isSynchronizing || _audioClip == null || _lyricsInLines == null || _lyricsInLines.Length == 0)
            {
                Debug.LogError("Cannot start synchronizing. Ensure lyrics and audio clip are provided and valid.");
                return;
            }

            _isSynchronizing = true;
            _currentLyricIndex = 0;

            OnStart?.Invoke(); // Trigger OnStart event

            _audioSource.Play();

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            try
            {
                // Wait for the audio to finish playing if not completed
                await WaitForAudioToFinishAsync(token);

                OnFinished?.Invoke(); // Trigger OnFinished event when synchronization is completed
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Synchronization process was canceled.");
            }
            finally
            {
                _isSynchronizing = false;
                _cancellationTokenSource.Dispose();
                OnStop?.Invoke(); // Trigger OnStop event when synchronization stops
            }

            Debug.Log("Lyrics synchronization completed.");
        }
        private void RegisterLineAndTime()
        {
            if (!_isSynchronizing || _currentLyricIndex >= _lyricsInLines.Length)
                return;
            
            _currentLyricIndex++;
            float currentTime = _audioSource.time;
            string currentLine = _lyricsInLines[_currentLyricIndex];
            _syncedLyrics[currentTime] = currentLine;

            OnRegisteredLine?.Invoke(FormatTimestamp(currentTime), _currentLyricIndex); // Trigger OnRegisteredLine event

            if (_currentLyricIndex == _lyricsInLines.Length - 1)
            {
                GameObject.Destroy(_keyDetector);
                _isSynchronizing = false;
                OnFinished?.Invoke(); // Trigger OnFinished event
            }
        }

        private async Task WaitForAudioToFinishAsync(CancellationToken token)
        {
            while (_audioSource.isPlaying)
            {
                await Task.Delay(100, token); // Check every 100ms
            }
        }

        public void StopSynchronizing()
        {
            if (_isSynchronizing && _cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        public void ExportToSRT(string filePath)
        {
            if (_syncedLyrics.Count == 0)
            {
                Debug.LogError("No synchronized lyrics to export.");
                return;
            }

            try
            {
                // Ensure the directory exists
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write the SRT file, overwriting if it already exists
                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    int index = 1;
                    foreach (var entry in _syncedLyrics)
                    {
                        writer.WriteLine(index);
                        writer.WriteLine($"{FormatTimestamp(entry.Key)} --> {FormatTimestamp(entry.Key + 2)}"); // Assuming 2 seconds per line
                        writer.WriteLine(entry.Value);
                        writer.WriteLine();
                        index++;
                    }
                }

                Debug.Log($"SRT file successfully exported to {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export SRT file: {ex.Message}");
            }
        }

        private string FormatTimestamp(float time)
        {
            int hours = (int)(time / 3600);
            int minutes = (int)((time % 3600) / 60);
            int seconds = (int)(time % 60);
            int milliseconds = (int)((time % 1) * 1000);

            return $"{hours:D2}:{minutes:D2}:{seconds:D2},{milliseconds:D3}";
        }

        public void Dispose()
        {
            OnStop = null;
            OnRegisteredLine = null;
            OnFinished = null;
            OnStart = null;
            
            StopSynchronizing();
            GameObject.Destroy(_audioSource.gameObject);
            
        }
    }
}