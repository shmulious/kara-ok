using System.Collections.Generic;

namespace DataClasses
{
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
}