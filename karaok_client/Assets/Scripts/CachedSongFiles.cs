using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataClasses;
using UnityEngine;
using UnityEngine.Video;

public class CachedSongFiles
{
    // Define an enum for the file keys
    public enum FileKey
    {
        Lyrics,
        NoVocals,
        Vocals,
        Original,
        FinalPlayback
    }

    // Map enum values to string representations
    private static readonly Dictionary<FileKey, string> FileKeyStrings = new Dictionary<FileKey, string>
    {
        { FileKey.Lyrics, "Lyrics" },
        { FileKey.NoVocals, "NoVocals" },
        { FileKey.Vocals, "Vocals" },
        { FileKey.Original, "Original" },
        { FileKey.FinalPlayback, "Playback" }
    };

    // Main dictionary to store cached files
    public Dictionary<object, CachedSongFile> CachedFiles { get; set; }

    public CachedSongFiles()
    {
        CachedFiles = new Dictionary<object, CachedSongFile>();
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

    // Indexer to access cached files by key (enum or custom string)
    public CachedSongFile this[FileKey key]
    {
        get => CachedFiles.ContainsKey(key.ToString()) ? CachedFiles[key.ToString()] : null;
        set => CachedFiles[key.ToString()] = value;
    }
    
    // Indexer to access cached files by key (enum or custom string)
    public CachedSongFile this[string key]
    {
        get => CachedFiles.ContainsKey(key) ? CachedFiles[key] : null;
        set => CachedFiles[key] = value;
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
                    AddFileToCache(FileKey.Lyrics, file, CachedSongFile.FileType.Text);
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
                    AddFileToCache(FileKey.NoVocals, file, CachedSongFile.FileType.Audio);
                }
                else if (fileName.Equals("vocals.wav", StringComparison.OrdinalIgnoreCase))
                {
                    AddFileToCache(FileKey.Vocals, file, CachedSongFile.FileType.Audio);
                }
                else if (fileName.Equals($"{subdirectoryName}.wav", StringComparison.OrdinalIgnoreCase))
                {
                    AddFileToCache(FileKey.Original, file, CachedSongFile.FileType.Audio);
                }
                else if (fileName.Equals($"{subdirectoryName}_no_vocals.m4a", StringComparison.OrdinalIgnoreCase))
                {
                    AddFileToCache(FileKey.FinalPlayback, file, CachedSongFile.FileType.Audio);
                }
            }
        }
    }

    // Helper method to add files to the dictionary with enum keys
    private void AddFileToCache(FileKey key, string filePath, CachedSongFile.FileType fileType)
    {
        if (!CachedFiles.ContainsKey(key))
        {
            CachedFiles[key] = new CachedSongFile(filePath, fileType);
            KaraokLogger.Log($"Cached '{FileKeyStrings[key]}' file: {filePath}");
        }
        else
        {
            KaraokLogger.Log($"Warning: Duplicate file key '{FileKeyStrings[key]}' encountered for path {filePath}. Existing entry will be kept.");
        }
    }

    // Helper method to add files to the dictionary with custom string keys (e.g., image files)
    private void AddFileToCache(string customKey, string filePath, CachedSongFile.FileType fileType)
    {
        if (!CachedFiles.ContainsKey(customKey))
        {
            CachedFiles[customKey] = new CachedSongFile(filePath, fileType);
            KaraokLogger.Log($"Cached custom file '{customKey}': {filePath}");
        }
        else
        {
            KaraokLogger.Log($"Warning: Duplicate custom file key '{customKey}' encountered for path {filePath}. Existing entry will be kept.");
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
        return CachedFiles.Count;
    }
    
    // New method to load AudioClip, VideoPlayer, or TextAsset
    public async Task<T> LoadMediaAsync<T>(string key) where T : class
    {
        if (!CachedFiles.TryGetValue(key, out var cachedFile))
        {
            throw new KeyNotFoundException($"FileKey '{key}' not found in cached files.");
        }

        string filePath = cachedFile.LocalPath;
        Debug.Log($"Attempting to load media for key '{key}' from path: {filePath}");

        if (typeof(T) == typeof(TextAsset))
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found at path: {filePath}");
            }

            string textContent = await File.ReadAllTextAsync(filePath);
            return new TextAsset(textContent) as T;
        }
        else if (typeof(T) == typeof(AudioClip) || typeof(T) == typeof(VideoPlayer))
        {
            return await MediaLoader.LoadMediaFromPath<T>(filePath);
        }

        throw new NotSupportedException($"Unsupported media type: {typeof(T)}");
    }
}