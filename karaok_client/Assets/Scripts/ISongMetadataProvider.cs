using System.Threading.Tasks;

public interface ISongMetadataProvider
{
    Task<SongMetadataData> FetchMetadata(string url);
}