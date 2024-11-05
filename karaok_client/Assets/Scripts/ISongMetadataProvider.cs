using System.Threading.Tasks;
using DataClasses;

public interface ISongMetadataProvider
{
    Task<SongMetadata> FetchMetadata(string url);
}