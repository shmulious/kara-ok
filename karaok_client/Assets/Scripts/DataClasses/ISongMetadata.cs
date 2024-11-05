using System.Collections.Generic;

namespace DataClasses
{
    public interface ISongMetadata
    {
        string URL { get; }
        string Artist { get; }
        string Title { get; }
        List<string> ThumbnailsURLs { get; }
        string Lyrics { get; }
    }
}