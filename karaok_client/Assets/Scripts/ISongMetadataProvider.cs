using System.Threading.Tasks;

public interface ISongMetadataProvider
{
    Task<SongMetadata> FetchMetadata(string url);
}