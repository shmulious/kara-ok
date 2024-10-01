using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class SongMetadataBase : ISongMetadataProvider
{
    private readonly PythonRunner _pythonRunner;
    protected LyricsProviderHandler _lyricsProviderHandler;

    public SongMetadataBase(PythonRunner pythonRunner)
    {
        _pythonRunner = pythonRunner;
        _lyricsProviderHandler = new LyricsProviderHandler(new LyricsOvhProvider(), null);
    }

    public async Task<SongMetadataData> FetchMetadata(string url)
    {
        // Calling the Python script with the --getmetadata option
        var res = await _pythonRunner.RunProcess<YoutubeMetadata>("main/smule.py", $"--getmetadata \"{url}\"");

        if (res.Success && res.Value != null)
        {
            // If Python script returns metadata successfully, create SongMetadataObject
            var dataObject = new SongMetadataData(url, res.Value.artist, res.Value.title);
            return await ComposeDataObject(dataObject, res);
        }
        else
        {
            Debug.LogError("Failed to fetch song metadata from the Python script.");
        }
        return null;
    }

    protected virtual async Task<SongMetadataData> ComposeDataObject(SongMetadataData dataObject, ProcessResult<YoutubeMetadata> res)
    {
        dataObject.ThumbnailData = await FetchThumbnail(res.Value.thumbnails);
        dataObject.Lyrics = await FetchLyrics(dataObject);
        // Example usage: Log the metadata to Unity console
        Debug.Log(dataObject.ToString());
        return dataObject;
    }

    protected virtual async Task<string> FetchLyrics(SongMetadataData dataObject)
    {
        var l = await _lyricsProviderHandler.GetLyricsAsync(dataObject.Artist, dataObject.Title);
        return l;
    }

    protected virtual async Task<ThumbnailData> FetchThumbnail(List<string> thumbnails)
    {
        var pool = await DownloadThumbnails(thumbnails);
        if (pool.Count > 0)
        {
            return pool[0];
        }
        return null;
    }

    /// <summary>
    /// Downloads the images from the thumbnail URLs and stores them as a list of ThumbnailData.
    /// </summary>
    /// <returns>A list of ThumbnailData representing the downloaded thumbnails.</returns>
    protected async Task<List<ThumbnailData>> DownloadThumbnails(List<string> thumbnails)
    {
        List<ThumbnailData> thumbnailDataList = new List<ThumbnailData>();
        Debug.Log("Starting thumbnail download process...");

        foreach (var thumbnailUrl in thumbnails)
        {
            if (string.IsNullOrEmpty(thumbnailUrl))
            {
                Debug.LogError($"Thumbnail URL is empty or null.");
                continue;
            }

            Debug.Log($"Attempting to download thumbnail from URL: {thumbnailUrl}");

            // Download the image and store it as ThumbnailData
            ThumbnailData thumbnailData = await DownloadImage(thumbnailUrl);
            if (thumbnailData != null)
            {
                thumbnailDataList.Add(thumbnailData);
                Debug.Log($"Successfully downloaded and processed thumbnail from URL: {thumbnailUrl}");
            }
            else
            {
                Debug.LogError($"Failed to download or process thumbnail from URL: {thumbnailUrl}");
            }
        }

        Debug.Log($"Thumbnail download process complete. Total successfully downloaded: {thumbnailDataList.Count}");

        return thumbnailDataList;
    }

    /// <summary>
    /// Downloads a single image from a URL and stores it as a ThumbnailData object, including the sprite.
    /// </summary>
    /// <param name="url">The URL of the image.</param>
    /// <returns>A ThumbnailData object if successful, or null if the download fails.</returns>
    private async Task<ThumbnailData> DownloadImage(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            // Send the web request and wait for it to complete
            var operation = request.SendWebRequest();

            while (!operation.isDone)
                await Task.Yield(); // Await the task to avoid blocking

            // Check if there were any errors in the request
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download image: {request.error}");
                return null;
            }

            // Get the texture from the request
            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            // If texture is valid, create ThumbnailData
            if (texture != null)
            {
                Debug.Log($"Successfully downloaded image from URL: {url}");

                // Create a sprite from the texture
                Sprite sprite = SpriteFromTexture(texture);

                ThumbnailData thumbnailData = new ThumbnailData
                {
                    OriginalSize = new Vector2(texture.width, texture.height),
                    Texture = texture,
                    ThumbnailSprite = sprite, // Store the sprite
                    Url = url
                };

                //downloadedThumbnails.Add(thumbnailData); // Add to the class-wide list for future reference
                return thumbnailData;
            }

            Debug.LogError("Failed to create texture from downloaded image.");
            return null;
        }
    }

    /// <summary>
    /// Converts a Texture2D to a Sprite.
    /// </summary>
    /// <param name="texture">The Texture2D to convert.</param>
    /// <returns>A Sprite created from the texture.</returns>
    private Sprite SpriteFromTexture(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}