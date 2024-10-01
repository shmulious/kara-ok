public interface ILyricsProvider
{
    System.Threading.Tasks.Task<string> GetLyricsAsync(string artist, string songTitle);
}