using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class SongMetadataGenius : SongMetadataBase
{
    private const string GeniusApiUrl = "https://api.genius.com";
    private readonly string _geniusAccessToken;

    public SongMetadataGenius(PythonRunner pythonRunner) : base(pythonRunner)
    {
        _geniusAccessToken = "sXqAWwSObRu8jXtBlLLVqQLXGyrfMMi3Ptr0UTILSNwo-Fn7I6ZXGSIdfxd1Gjnj";  // Set your Genius API key here
        _lyricsProviderHandler = new LyricsProviderHandler(new GeniusProvider(_geniusAccessToken), new LyricsOvhProvider());
    }


    // Override ComposeDataObject to search the song on Genius API and update the data object
    protected override async Task<SongMetadataData> ComposeDataObject(SongMetadataData dataObject, ProcessResult<YoutubeMetadata> res)
    {
        // First, fetch song metadata from the Genius API using artist and title
        var geniusMetadata = await FetchGeniusMetadata(dataObject.Artist, dataObject.Title);

        if (geniusMetadata != null)
        {
            // Update the data object with information from the Genius API
            dataObject.Artist = geniusMetadata.ArtistName;
            dataObject.Title = geniusMetadata.SongTitle;
            //dataObject.Album = geniusMetadata.AlbumName;
            //dataObject.ReleaseDate = geniusMetadata.ReleaseDate;

            // Instead of using the thumbnail from `res`, we'll now fetch thumbnails from Genius
            if (geniusMetadata.AlbumArtUrls != null && geniusMetadata.AlbumArtUrls.Count > 0)
            {
                var thumbnailDatas = await FetchThumbnail(geniusMetadata.AlbumArtUrls);
                dataObject.ThumbnailData = thumbnailDatas;
            }
        }

        // Update lyrics based on Genius metadata
        dataObject.Lyrics = await FetchLyrics(dataObject);

        // Log the updated metadata object
        Debug.Log(dataObject.ToString());

        return dataObject;
    }

    // Main method that fetches metadata from Genius API
    private async Task<GeniusSongMetadata> FetchGeniusMetadata(string artist, string title)
    {
        // Step 1: Construct the search URL
        string searchUrl = ConstructSearchUrl(artist, title);

        // Step 2: Send API request and get the response
        string jsonResponse = await SendGeniusApiRequest(searchUrl);

        // Step 3: If response is valid, parse the JSON to extract metadata
        if (!string.IsNullOrEmpty(jsonResponse))
        {
            return ParseGeniusMetadata(jsonResponse);
        }

        Debug.LogError("Failed to fetch song metadata from Genius.");
        return null;
    }

 
    // Step 1: Construct the Genius API search URL
    private string ConstructSearchUrl(string artist, string title)
    {
        string query = UnityWebRequest.EscapeURL($"{artist} {title}");
        return $"{GeniusApiUrl}/search?q={query}";  // No access token needed in URL, it will be sent in the header
    }

    // Step 2: Send a request to the Genius API and return the JSON response as a string
    private async Task<string> SendGeniusApiRequest(string searchUrl)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(searchUrl))
        {
            // Add the Authorization header with the access token
            request.SetRequestHeader("Authorization", $"Bearer {_geniusAccessToken}");

            // Send the request and await completion
            var asyncOp = request.SendWebRequest();

            // Wait for the request to complete without blocking the main thread
            while (!asyncOp.isDone)
                await Task.Yield();

            // Check if there were any errors during the request
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching metadata from Genius API: {request.error}");
                return null;
            }

            // Return the JSON response
            return request.downloadHandler.text;
        }
    }
    // Step 3: Parse the JSON response and return the extracted metadata as a GeniusSongMetadata object
    private GeniusSongMetadata ParseGeniusMetadata(string jsonResponse)
    {
        // Parse the response using Newtonsoft.Json
        JObject responseObject = JObject.Parse(jsonResponse);

        // Extract the first song result
        var firstHit = responseObject["response"]?["hits"]?.FirstOrDefault()?["result"];
        if (firstHit == null)
        {
            Debug.LogError("No song results found in Genius API response.");
            return null;
        }

        // Extract relevant metadata
        string songTitle = firstHit["title"]?.ToString();
        string artistName = firstHit["primary_artist"]?["name"]?.ToString();
        string albumArtUrl = firstHit["song_art_image_url"]?.ToString();
        string releaseDate = firstHit["release_date"]?.ToString() ?? "Unknown";

        // Return the metadata as a GeniusSongMetadata object
        return new GeniusSongMetadata
        {
            ArtistName = artistName,
            SongTitle = songTitle,
            AlbumName = "Unknown Album",  // Assume the album name isn't provided
            ReleaseDate = releaseDate,
            AlbumArtUrls = new List<string> { albumArtUrl }  // Assume only one URL for album art
        };
    }

    // Override FetchThumbnail to use the Genius metadata's album art URLs for downloading
    protected override async Task<ThumbnailData> FetchThumbnail(List<string> albumArtUrls)
    {
        Debug.Log("Fetching thumbnails from Genius metadata...");

        // Utilize the base class's DownloadThumbnails method to download the images from the URLs
        var downloadedThumbnails = await DownloadThumbnails(albumArtUrls);
        return downloadedThumbnails[0];
    }

    // Fetch thumbnail using Genius data, now using multiple URLs
    //private async Task<List<ThumbnailData>> DownloadThumbnails(List<string> albumArtUrls)
    //{
    //    List<ThumbnailData> thumbnailDataList = new List<ThumbnailData>();
    //    Debug.Log("Starting Genius thumbnail download process...");

    //    foreach (var thumbnailUrl in albumArtUrls)
    //    {
    //        if (string.IsNullOrEmpty(thumbnailUrl))
    //        {
    //            Debug.LogError($"Thumbnail URL is empty or null.");
    //            continue;
    //        }

    //        Debug.Log($"Attempting to download thumbnail from Genius URL: {thumbnailUrl}");

    //        // Download the image and store it as ThumbnailData
    //        ThumbnailData thumbnailData = await DownloadImage(thumbnailUrl);
    //        if (thumbnailData != null)
    //        {
    //            thumbnailDataList.Add(thumbnailData);
    //            Debug.Log($"Successfully downloaded and processed Genius thumbnail from URL: {thumbnailUrl}");
    //        }
    //        else
    //        {
    //            Debug.LogError($"Failed to download or process Genius thumbnail from URL: {thumbnailUrl}");
    //        }
    //    }

    //    Debug.Log($"Genius thumbnail download process complete. Total successfully downloaded: {thumbnailDataList.Count}");

    //    return thumbnailDataList;
    //}

    // Override FetchLyrics to use the lyrics directly from the Genius metadata
    protected override async Task<string> FetchLyrics(SongMetadataData dataObject)
    {
        // Fetch lyrics directly from Genius metadata if available
        if (!string.IsNullOrEmpty(dataObject.Lyrics))
        {
            return dataObject.Lyrics;
        }

        // Otherwise, fallback to the base implementation
        return await base.FetchLyrics(dataObject);
    }
}

// Helper class for holding Genius song metadata, extended to include multiple thumbnail URLs
public class GeniusSongMetadata
{
    public string ArtistName { get; set; }
    public string SongTitle { get; set; }
    public string AlbumName { get; set; }
    public string ReleaseDate { get; set; }
    public List<string> AlbumArtUrls { get; set; } = new List<string>(); // Allow multiple thumbnail URLs
}