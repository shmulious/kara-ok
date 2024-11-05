using System;
using System.Collections.Generic;
using System.IO;
using DataClasses;

public class CachedSongFiles
{
    // Define constants for dictionary keys
    public const string LyricsKey = "Lyrics";
    public const string NoVocalsKey = "NoVocals";
    public const string VocalsKey = "Vocals";
    public const string OriginalKey = "Original";
    public const string PlaybackKey = "Playback";

    // Main dictionary to store cached files
    private Dictionary<string, CachedSongFile> _cachedFiles;

    public CachedSongFiles()
    {
        _cachedFiles = new Dictionary<string, CachedSongFile>(StringComparer.OrdinalIgnoreCase);
    }

    // Constructor that gets a cache path and scans for files accordingly
    public CachedSongFiles(string cachePath) : this()
    {
        if (Directory.Exists(cachePath))
        {
            var filesInCache = Directory.GetFiles(cachePath);
            ProcessFilesInDirectory(filesInCache, isCacheDirectory: true);

            var subdirectories = Directory.GetDirectories(cachePath);
            if (subdirectories.Length > 0)
            {
                string subdirectoryPath = subdirectories[0];
                var filesInSubdirectory = Directory.GetFiles(subdirectoryPath);
                string subdirectoryName = new DirectoryInfo(subdirectoryPath).Name;

                ProcessFilesInDirectory(filesInSubdirectory, isCacheDirectory: false, subdirectoryName: subdirectoryName);
            }
        }
        else
        {
            throw new DirectoryNotFoundException($"Directory does not exist: {cachePath}");
        }
    }

    // Indexer to access cached files by key
    public CachedSongFile this[string key]
    {
        get => _cachedFiles.ContainsKey(key) ? _cachedFiles[key] : null;
        set => _cachedFiles[key] = value;
    }

    // Method to process files in a directory
    private void ProcessFilesInDirectory(string[] files, bool isCacheDirectory, string subdirectoryName = "")
    {
        foreach (var file in files)
        {
            string fileName = Path.GetFileName(file);

            if (isCacheDirectory)
            {
                if (fileName.Equals("lyrics.txt", StringComparison.OrdinalIgnoreCase))
                {
                    AddFileToCache(LyricsKey, file, CachedSongFile.FileType.Text);
                }
                else if (IsImageFile(fileName))
                {
                    AddFileToCache(fileName, file, CachedSongFile.FileType.Image);
                }
            }
            else
            {
                if (fileName.Equals("no_vocals.wav", StringComparison.OrdinalIgnoreCase))
                {
                    AddFileToCache(NoVocalsKey, file, CachedSongFile.FileType.Audio);
                }
                else if (fileName.Equals("vocals.wav", StringComparison.OrdinalIgnoreCase))
                {
                    AddFileToCache(VocalsKey, file, CachedSongFile.FileType.Audio);
                }
                else if (fileName.Equals($"{subdirectoryName}.wav", StringComparison.OrdinalIgnoreCase))
                {
                    AddFileToCache(OriginalKey, file, CachedSongFile.FileType.Audio);
                }
                else if (fileName.Equals($"{subdirectoryName}_no_vocals.m4a", StringComparison.OrdinalIgnoreCase))
                {
                    AddFileToCache(PlaybackKey, file, CachedSongFile.FileType.Audio);
                }
            }
        }
    }

    // Helper method to add files to the dictionary with error handling for duplicates
    private void AddFileToCache(string key, string filePath, CachedSongFile.FileType fileType)
    {
        if (!_cachedFiles.ContainsKey(key))
        {
            _cachedFiles[key] = new CachedSongFile(filePath, fileType);
            KaraokLogger.Log($"Cached '{key}' file: {filePath}");
        }
        else
        {
            KaraokLogger.Log($"Warning: Duplicate file key '{key}' encountered for path {filePath}. Existing entry will be kept.");
        }
    }

    // Helper methods to identify file types
    private bool IsImageFile(string fileName)
    {
        return fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
    }

    // Method to get the count of cached files
    public int Count()
    {
        return _cachedFiles.Count;
    }
}