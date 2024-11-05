using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace DataClasses
{
    public class ThumbnailData
    {
        [JsonIgnore]  // This prevents direct serialization of the Texture2D
        public Texture2D Texture { get; set; }

        // This property will store the path where the texture is cached
        public string TexturePath { get; set; }

        public string Url { get; set; }

        public string TextureName { get; set; }

        // Prepare the object for serialization by caching the texture and setting the path
        public void PrepareForSerialization(string cachePath, int index)
        {
            if (Texture != null)
            {
                // Cache the texture and store the cache path in TexturePath
                TexturePath = CacheManager.CacheTexture(Texture, cachePath, index);
                TextureName = Path.GetFileNameWithoutExtension(TexturePath);
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
            data.Texture = Resources.Load<Texture2D>("thumbnail_0");
            data.Url = null;
            data.TextureName = "thumbnail_0";
            return data;
        }
    }
}