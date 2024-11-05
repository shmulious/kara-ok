using System.Threading.Tasks;
using System;
using DataClasses;

public abstract class JsonValueFromProcess
{
}

public class YoutubeMetadata : SongMetadataBase
{

    public YoutubeMetadata(PythonRunner pythonRunner) : base (pythonRunner)
    {
        _pythonRunner = pythonRunner;
    }

    public override async Task Compose(string url)
    {
        // Calling the Python script with the --getmetadata option
        var res = await _pythonRunner.RunProcess<YTMetadataData>("main/smule.py", $"--getmetadata \"{url}\"");
        if (res.Success)
        {
            Data = res.Value;

        }
        else
        {
            throw new Exception($"[YoutubeMetadata] - Compose() failed - Error: {res.ExitCode} - {res.Error}");
        }
    }
}