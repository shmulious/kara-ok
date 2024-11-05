using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public static class SmuleUrlProcessor
{
    private static SmuleDownloader _smuleDownloader;

    private static async Task InitializeDownloaderAsync()
    {
        if (_smuleDownloader != null) return;
        _smuleDownloader = new SmuleDownloader("/opt/homebrew/bin/node");
        KaraokLogger.Log("[SmuleUrlProcessor] - Downloader initialized");
    }

    public static async Task<List<MediaUrlData>> ProcessSmuleLinkAsync(string smuleUrl)
    {
        await InitializeDownloaderAsync();
        var mediaUrls = new List<MediaUrlData>();

        if (IsRecordingUrl(smuleUrl))
        {
            KaraokLogger.Log($"[SmuleUrlProcessor] - Processing recording URL: {smuleUrl}");
            mediaUrls.AddRange(await FetchAndDownloadMediaUrlsAsync(smuleUrl));
        }
        else if (IsPlaylistUrl(smuleUrl))
        {
            KaraokLogger.Log($"[SmuleUrlProcessor] - Processing playlist URL: {smuleUrl}");
            var playlistItems = await FetchPlaylistItemsAsync(smuleUrl);
            foreach (var playlistItem in playlistItems.Playlist)
            {
                KaraokLogger.Log($"[SmuleUrlProcessor] - Processing playlist item URL: {playlistItem.WebUrl}");
                mediaUrls.AddRange(await FetchAndDownloadMediaUrlsAsync(playlistItem.WebUrl));
            }
        }

        return mediaUrls;
    }

    private static bool IsRecordingUrl(string url) => url.Contains("/recording/");
    
    private static bool IsPlaylistUrl(string url) => url.Contains("/playlist/");

    private static async Task<PlaylistData> FetchPlaylistItemsAsync(string playlistUrl)
    {
        return await _smuleDownloader.FetchPlaylistItemsUrls(playlistUrl);
    }

    private static async Task<List<MediaUrlData>> FetchAndDownloadMediaUrlsAsync(string mediaUrl)
    {
        var fetchedMediaUrls = await _smuleDownloader.FetchMediaUrlsAsync(mediaUrl);
        var successfullyDownloadedUrls = new List<MediaUrlData>();

        foreach (var url in fetchedMediaUrls)
        {
            var outputPath = GenerateOutputPath(url);
            var downloadResult = await _smuleDownloader.DownloadAndExtractMediaAsync(url.MediaUrl, outputPath);
            if (downloadResult.Success)
            {
                successfullyDownloadedUrls.Add(url);
                KaraokLogger.Log($"[SmuleUrlProcessor] - Successfully downloaded: {url.MediaUrl}");
            }
            else
            {
                KaraokLogger.Log($"[SmuleUrlProcessor] - Failed to download: {url.MediaUrl}");
            }
        }

        return successfullyDownloadedUrls;
    }

    private static string GenerateOutputPath(MediaUrlData mediaUrl)
    {
        var directoryName = $"{mediaUrl.ArtistName} - {mediaUrl.Title}";
        return Path.Combine(CacheManager.CachePath, "smule", directoryName);
    }
}