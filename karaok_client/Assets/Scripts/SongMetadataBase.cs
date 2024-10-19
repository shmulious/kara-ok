using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using KtxUnity;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

public abstract class SongMetadataBase
{
    protected PythonRunner _pythonRunner;
    protected LyricsProviderHandler _lyricsProviderHandler;
    protected ISongMetadata _metadata;
    public ISongMetadata Data
    {
        get { return _metadata; }
        protected set
        {
            _metadata = value;
        }
    }

    public SongMetadataBase(PythonRunner pythonRunner)
    {
        _pythonRunner = pythonRunner;
    }

    public abstract Task Compose(string url);

    // CreateAsync method to asynchronously create and initialize an instance of T
    public static async Task<T> CreateAsync<T>(string url, Func<PythonRunner, T> factory) where T : SongMetadataBase
    {
        // Step 1: Create a PythonRunner instance
        var pythonRunner = new PythonRunner();

        // Step 2: Use the factory to create an instance of T with the PythonRunner
        T instance = factory(pythonRunner);

        // Step 3: Call the Compose method to initialize the instance
        await instance.Compose(url);

        // Step 4: Return the initialized instance
        return instance;
    }

    //public Task<SongMetadataData> FetchMetadata(string url)
    //{
    //    throw new NotImplementedException();
    //}

    protected virtual async Task<string> FetchLyrics(ISongMetadata _basicMetadata)
    {
        if (_lyricsProviderHandler == null) throw new Exception($"[{this.GetType()}] - This metadata provider does not support lyrics fetching");
        if (_basicMetadata == null || string.IsNullOrWhiteSpace(_basicMetadata.Artist) || string.IsNullOrWhiteSpace(_basicMetadata.Title)) KaraokLogger.LogError($"[{this.GetType()}] - Fetch lyrics failed for null values!");
        try
        {
            var l = await _lyricsProviderHandler.GetLyricsAsync(_basicMetadata.Artist, _basicMetadata.Title);
            return StringCleaner.RemoveContentInBracesAndMergeSpaces(l);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    protected virtual async Task<List<ThumbnailData>> FetchThumbnail(List<string> thumbnails)
    {
        if (thumbnails == null || thumbnails.Count == 0)
        {
            KaraokLogger.LogError($"[{this.GetType()}] - Fetch thumbnails failed! no thumbnails reference urls data collected");
        }
        try
        {
            var pool = new List<ThumbnailData>();// await DownloadThumbnails(thumbnails);
            if (pool.Count > 0)
            {
                return pool;
            }
            else
            {
                KaraokLogger.LogError($"[{this.GetType()}] - Fetch thumbnails failed! Couldn't download any thumbnail");
                return null;
            }
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}