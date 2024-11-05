namespace DataClasses
{
    public class CachedSongFile
    {
        public string LocalPath;
        public FileType OfType { get; }

        public enum FileType
        {
            Audio,
            Text,
            Image
        }

        public CachedSongFile(string localPath, FileType type)
        {
            LocalPath = localPath;
            OfType = type;
        }
        public override string ToString() => LocalPath;
    }
}