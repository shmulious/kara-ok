using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DataClasses
{
    public class SongMetadata
    {
        private const string DATA_FILE_NAME = "metadata.json";
        public string URL => _URL;
        public string Artist => GetPreferredMetadata(m => m.Artist);
        public string Title => GetPreferredMetadata(m => m.Title);
        public List<string> ThumbnailsURLs => GetCombinedThumbnails();
        public string Lyrics => GetPreferredMetadata(m => (m as GeniusSongMetadata)?.Lyrics);

        public string _URL;

        public List<ISongMetadata> MetadataSources { get; set; }
        public List<ThumbnailData> ThumbnailDatas { get; set; }
        public CachedSongFiles CachedFiles { get; set; }

        public string MetadataPath { get; set; }
        public string CachePath { get; set; }

        public SongMetadata(string url, List<ISongMetadata> metadataSources)
        {
            MetadataSources = metadataSources;
            this._URL = url;
            MetadataPath = Path.Combine(CacheManager.CachePath, YTMetadata.id, DATA_FILE_NAME);
            CachePath = Path.Combine(CacheManager.CachePath, YTMetadata.id);
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
            await DownloadThumbnails();
            TryGetCachedSongFiles();
        }

        private void TryGetCachedSongFiles()
        {
            if (Directory.Exists(CachePath))
            {
                CachedFiles = new CachedSongFiles(CachePath);
            }
        }

        private async Task DownloadThumbnails()
        {
            if (ThumbnailsURLs != null)
            {
                ThumbnailDatas = new List<ThumbnailData>();
                foreach (var item in ThumbnailsURLs)
                {
                    try
                    {
                        var thumbnailData = await ThumbnailsDownloader.DownloadThumbnail(item);
                        if (thumbnailData != null)
                        {
                            ThumbnailDatas.Add(thumbnailData);
                        }
                    }
                    catch (Exception ex)
                    {
                        KaraokLogger.LogError($"[SongMetadata] - failed to download thumbnail {item}. Exception: {ex.Message}");
                    }
                }
                if (ThumbnailDatas.Count == 0)
                {
                    ThumbnailDatas.Add(ThumbnailData.Fallback());
                }
            }
        }

        public GeniusSongMetadata GeniusMetadata => MetadataSources.OfType<GeniusSongMetadata>().FirstOrDefault();
        public YTMetadataData YTMetadata => MetadataSources.OfType<YTMetadataData>().FirstOrDefault();

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
            if (CachedFiles == null) CachedFiles = new CachedSongFiles();
            PrepareThumbnailDatasForSerialization();
            SaveLyricsToFile();
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto
            });
        }

        private async void SaveLyricsToFile()
        {
            var lyricsFilePath = await CacheManager.WriteFileAsync(Lyrics, Path.Combine(CachePath, "lyrics.txt"));
            CachedSongFile f = new CachedSongFile(lyricsFilePath, CachedSongFile.FileType.Text);
            CachedFiles[CachedSongFiles.LyricsKey] = f;
        }

        private void PrepareThumbnailDatasForSerialization()
        {
            int index = 0;
            CachedFiles = new CachedSongFiles();
            foreach (var item in ThumbnailDatas)
            {
                item.PrepareForSerialization(CachePath, index);
                var f = new CachedSongFile(item.TexturePath, CachedSongFile.FileType.Image);
                CachedFiles[item.TexturePath] = f;
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
}