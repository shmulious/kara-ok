using System.Collections.Generic;

namespace DataClasses
{
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
}