using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class SongMetadata
{
    public string URL => _URL;
    public string Artist => GetPreferredMetadata(m => m.Artist);
    public string Title => GetPreferredMetadata(m => m.Title);
    public List<string> ThumbnailsURLs => GetCombinedThumbnails();
    public string Lyrics => GetPreferredMetadata(m => (m as GeniusSongMetadata)?.Lyrics);

    public string _URL;

    public List<ISongMetadata> MetadataSources { get; set; }
    public List<ThumbnailData> ThumbnailDatas { get; set; }

    public SongMetadata(string url, List<ISongMetadata> metadataSources)
    {
        MetadataSources = metadataSources;
        this._URL = url;
    }
    public static async Task<SongMetadata> CreateAsync(string url, List<ISongMetadata> reference)
    {
        // Step 2: Use the factory to create an instance of T with the PythonRunner
        var instance = new SongMetadata(url, reference);

        // Step 3: Call the Compose method to initialize the instance
        await instance.Compose();

        // Step 4: Return the initialized instance
        return instance;
    }

    private async Task Compose()
    {
        if (ThumbnailsURLs != null)
        {
            ThumbnailDatas = new List<ThumbnailData>();
            foreach (var item in ThumbnailsURLs)
            {
                try
                {
                    var thumbnailData = await ThumbnailsDownloader.DownloadThumbnail(item);
                    if (thumbnailData!= null)
                    {
                        ThumbnailDatas.Add(thumbnailData);
                    }
                }
                catch (Exception ex)
                {
                    KaraokLogger.LogError($"[SongMetadata] - failed to download thumbnail {item}. Exception: {ex.Message}");
                }
            }
            if (ThumbnailDatas.Count==0)
            {
                ThumbnailDatas.Add(ThumbnailData.Fallback());
            }
        }
    }

    public GeniusSongMetadata GeniusMetadata => MetadataSources.OfType<GeniusSongMetadata>().FirstOrDefault();
    public YTMetadataData YTMetadata => MetadataSources.OfType<YTMetadataData>().FirstOrDefault();

    public string Path { get; set; }

    public string ToSmallJson()
    {
        var small = new
        {
            URL = this._URL,
            Artist = this.Artist,
            Title = this.Title
        };
        return JsonConvert.SerializeObject(small);
    }

    // Convert SongMetadata to JSON
    public string ToJson()
    {
        PrepareThumbnailDatasForSerialization();
        return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
    }

    private void PrepareThumbnailDatasForSerialization()
    {
        int index = 0;
        foreach (var item in ThumbnailDatas)
        {
            item.PrepareForSerialization(this.YTMetadata.id, index);
            index++;
        }
    }

    // Convert JSON string back to SongMetadata object
    public static SongMetadata FromJson(string json)
    {
        var metadata = JsonConvert.DeserializeObject<SongMetadata>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
        metadata.RestoreThumbnailDatasAfterDeserialization();
        return metadata;
    }

    private void RestoreThumbnailDatasAfterDeserialization()
    {
        foreach (var item in ThumbnailDatas)
        {
            item.RestoreAfterDeserialization();
        }
    }

    private string GetPreferredMetadata(Func<ISongMetadata, string> selector)
    {
        // Define a priority order of metadata sources
        var prioritizedMetadataSources = new List<ISongMetadata>
    {
        GeniusMetadata,  // Highest priority
        YTMetadata       // Lower priority
    };

        // Iterate over the prioritized metadata sources
        foreach (var metadata in prioritizedMetadataSources)
        {
            if (metadata != null) // Check if the metadata source exists
            {
                var result = selector(metadata);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    return result;
                }
            }
        }
        return null;
    }

    private List<string> GetCombinedThumbnails()
    {
        return MetadataSources.SelectMany(m => m.ThumbnailsURLs ?? Enumerable.Empty<string>()).ToList();
    }
}
public class GeniusSongMetadata : ISongMetadata
{
    public string ArtistName { get; set; }
    public string SongTitle { get; set; }
    public string AlbumName { get; set; }
    public string ReleaseDate { get; set; }
    public List<string> AlbumArtUrls { get; set; } = new List<string>(); // Allow multiple thumbnail URLs
    public string Lyrics { get; set; }

    public string URL => null;
    public string Artist => ArtistName;
    public string Title => SongTitle;
    public List<string> ThumbnailsURLs => AlbumArtUrls ?? new List<string>();
}

public class YTMetadataData : ISongMetadata
{
    public string id;
    public string artist;
    public string title;
    public List<string> thumbnails;
    public string url;

    public string URL => url;
    public string Artist => artist;
    public string Title => title;
    public List<string> ThumbnailsURLs => thumbnails ?? new List<string>();
    public string Lyrics => null;
}

public class CachedSongFiles
{
    public List<CachedSongFile> ThumbnailsImages;
    public CachedSongFile Original;
    public CachedSongFile Vocals;
    public CachedSongFile NoVocals;
    public CachedSongFile Lyrics;
}

public class CachedSongFile
{
    public string LocalPath;
    public string URL;
    public byte[] Bytes;
}

public interface ISongMetadata
{
    string URL { get; }
    string Artist { get; }
    string Title { get; }
    List<string> ThumbnailsURLs { get; }
    string Lyrics { get; }
}

public class ThumbnailData
{
    [JsonIgnore]  // This prevents direct serialization of the Texture2D
    public Texture2D Texture { get; set; }

    // This property will store the path where the texture is cached
    public string TexturePath { get; set; }

    public string Url { get; set; }

    // Prepare the object for serialization by caching the texture and setting the path
    public void PrepareForSerialization(string id, int index)
    {
        if (Texture != null)
        {
            // Cache the texture and store the cache path in TexturePath
            TexturePath = CacheManager.CacheTexture(Texture, id, index);
        }
    }

    // Restore the texture after deserialization by loading it from the cache
    public void RestoreAfterDeserialization()
    {
        if (!string.IsNullOrEmpty(TexturePath))
        {
            // Load the texture from the cache using the path
            Texture = CacheManager.LoadCachedTexture(TexturePath);
        }
    }

    internal static ThumbnailData Fallback()
    {
        var data = new ThumbnailData();
        data.Texture = Resources.Load<Texture2D>("default_thumbnail");
        data.Url = null;
        return data;
    }
}