﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DataClasses;
using Newtonsoft.Json;
using UnityEngine;

public class CacheManager
{
    public static string CachePath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, "cache");
        }
    }
    private static Dictionary<string, string> Cache = new Dictionary<string, string>();
    public static async Task<SongMetadata> LoadMetadata(string uRL)
    {
        if (!Cache.ContainsKey(uRL))
        {
            var metadata = await FetchMetadata(uRL);
            Cache[uRL] = metadata.ToJson();
        }
        var cache = Cache[uRL];
        UnityEngine.Debug.Log($"Cached val: {cache}");
        return SongMetadata.FromJson(Cache[uRL]);
    }

    public static async void UpdateCachedFiles(string url)
    {
        if (Cache.ContainsKey(url))
        {
            var metadata = await LoadMetadata(url);
            metadata.TryGetCachedSongFiles();
        }
    }

    private static async Task<SongMetadata> FetchMetadata(string url)
    {
        var pythonRunner = new PythonRunner();
        var tyMetadata = await YoutubeMetadata.CreateAsync<YoutubeMetadata>(url, pythonRunner => new YoutubeMetadata(pythonRunner) );
        var geniusMetaData = await SongMetadataGenius.CreateAsync<SongMetadataGenius>(url, pythonRunner => new SongMetadataGenius(pythonRunner, tyMetadata.Data));
        var songMetadata = await SongMetadata.CreateAsync(url, new List<ISongMetadata> { tyMetadata.Data, geniusMetaData.Data});
        await WriteFileAsync(songMetadata.ToJson(), songMetadata.MetadataPath);
        
        return songMetadata;
    }

    public static async Task<string> WriteFileAsync(string content, string filePath)
    {
        string directoryPath = Path.GetDirectoryName(filePath);

        // Create the directory if it doesn't exist
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        // Write the JSON to the file asynchronously
        await File.WriteAllTextAsync(filePath, content);
        return filePath;
    }

    // Method to cache the texture and return the file path
    public static string CacheTexture(Texture2D texture, string cachePath, int index)
    {
        // Ensure the cache directory exists
        string textureDirectory = cachePath;
        if (!Directory.Exists(textureDirectory))
        {
            Directory.CreateDirectory(textureDirectory);
        }

        // Generate the path for the cached texture file
        string texturePath = Path.Combine(textureDirectory, $"thumbnail_{index}.png");

        // Convert Texture2D to PNG byte array
        byte[] pngData = texture.EncodeToPNG();

        // Write the texture file to disk
        File.WriteAllBytes(texturePath, pngData);

        // Return the path to the cached texture
        return texturePath;
    }

    // Method to load a cached texture from a file path
    public static Texture2D LoadCachedTexture(string texturePath)
    {
        // Check if the file exists
        if (File.Exists(texturePath))
        {
            // Load the texture bytes from the file
            byte[] textureBytes = File.ReadAllBytes(texturePath);

            // Create a new Texture2D and load the image data
            Texture2D texture = new Texture2D(2, 2); // Size will be overwritten by LoadImage
            texture.LoadImage(textureBytes); // Load texture data into Texture2D

            return texture;
        }
        else
        {
            UnityEngine.Debug.LogError($"Texture file not found at path: {texturePath}");
            return null;
        }
    }
}


