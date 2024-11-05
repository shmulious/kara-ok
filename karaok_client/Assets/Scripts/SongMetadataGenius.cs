using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using DataClasses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class SongMetadataGenius : SongMetadataBase
{
    private const string GeniusApiUrl = "https://api.genius.com";
    private readonly string _geniusAccessToken;
    private ISongMetadata _basicMetadata;

    public SongMetadataGenius(PythonRunner pythonRunner, ISongMetadata basicMetadata) : base(pythonRunner)
    {
        _geniusAccessToken = "sXqAWwSObRu8jXtBlLLVqQLXGyrfMMi3Ptr0UTILSNwo-Fn7I6ZXGSIdfxd1Gjnj";  // Set your Genius API key here
        _lyricsProviderHandler = new LyricsProviderHandler(new GeniusProvider(_geniusAccessToken), new LyricsOvhProvider());
        _basicMetadata = basicMetadata;
    }

    public override async Task Compose(string url)
    {
        if (_basicMetadata == null) throw new System.Exception($"[SongMetadataGenius] - Compose failed for null _basicMetadata");

        Data = await FetchGeniusMetadata(_basicMetadata.Artist, _basicMetadata.Title);
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
            var geniusData = ParseGeniusMetadata(jsonResponse);
            if (geniusData == null)
            {
                KaraokLogger.LogWarning($"[FetchGeniusMetadata] - failed to find song for {artist} - {title}");
                if (StringCleaner.ContainsBraces(artist))
                {
                    var cleanArtist = StringCleaner.RemoveContentInBracesAndMergeSpaces(artist);
                    KaraokLogger.LogWarning($"Retrying with clean artist name. was: {artist}. retrying with: {cleanArtist}");
                    return await FetchGeniusMetadata(cleanArtist, title);
                }
                if (StringCleaner.ContainsBraces(title))
                {
                    var cleanTitle = StringCleaner.RemoveContentInBracesAndMergeSpaces(title);
                    KaraokLogger.LogWarning($"Retrying with clean title name. was: {title}. retrying with: {cleanTitle}");
                    return await FetchGeniusMetadata(artist, cleanTitle);
                }
                else
                {
                    geniusData = new GeniusSongMetadata
                    {
                        ArtistName = artist,
                        SongTitle = title
                    };
                    KaraokLogger.LogWarning($"After retries, Failed to fetch song metadata from Genius. applying basic metadata: {JsonConvert.SerializeObject(geniusData)}");
                }
            }
            geniusData.ArtistName = artist;
            geniusData.SongTitle = title;
            geniusData.Lyrics = await FetchLyrics(geniusData);
            return geniusData;
        }
        return null;
    }


    // Step 1: Construct the Genius API search URL
    private string ConstructSearchUrl(string artist, string title)
    {
        // Log the artist and title arguments
        KaraokLogger.Log($"Constructing Genius API search URL with artist: {artist}, title: {title}");

        // Construct the query
        string query = UnityWebRequest.EscapeURL($"{artist} {title}");

        // Construct the search URL
        string searchUrl = $"{GeniusApiUrl}/search?q={query}";

        // Log the constructed search URL
        KaraokLogger.Log($"Constructed Genius API search URL: {searchUrl}");

        return searchUrl;
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
        string albumArtUrl = firstHit["song_art_image_url"]?.ToString(); // Primary image URL
        string thumbnailUrl = firstHit["header_image_thumbnail_url"]?.ToString(); // Smaller thumbnail image
        string headerImageUrl = firstHit["header_image_url"]?.ToString(); // Banner image

        string releaseDate = firstHit["release_date"]?.ToString() ?? "Unknown";

        // Create a list to hold multiple image URLs
        var albumArtUrls = new List<string>();

        // Add available image URLs to the list
        if (!string.IsNullOrEmpty(albumArtUrl)) albumArtUrls.Add(albumArtUrl);
        if (!string.IsNullOrEmpty(thumbnailUrl)) albumArtUrls.Add(thumbnailUrl);
        if (!string.IsNullOrEmpty(headerImageUrl)) albumArtUrls.Add(headerImageUrl);

        // Return the metadata as a GeniusSongMetadata object
        var metadata =  new GeniusSongMetadata
        {
            ArtistName = artistName,
            SongTitle = songTitle,
            AlbumName = "Unknown Album",  // Assume the album name isn't provided
            ReleaseDate = releaseDate,
            AlbumArtUrls = albumArtUrls  // Return the list of available album art URLs
        };
        // Log the firstHit object using KaraokLogger
        KaraokLogger.Log($"Found a song on genius: {metadata.ArtistName}");
        return metadata;
    }

    // Override FetchThumbnail to use the Genius metadata's album art URLs for downloading
    protected override async Task<List<ThumbnailData>> FetchThumbnail(List<string> albumArtUrls)
    {
        return null;
        //Debug.Log("Fetching thumbnails from Genius metadata...");

        //// Utilize the base class's DownloadThumbnails method to download the images from the URLs
        //var downloadedThumbnails = await DownloadThumbnails(albumArtUrls);
        //return downloadedThumbnails;
    }
}