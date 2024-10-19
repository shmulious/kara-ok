import yt_dlp
import os
from .logger import log_message
from scripts.main.config_manager import get_from_config

def download_audio_from_youtube(song_metadata, output_folder):
    """
    Download audio from a YouTube URL and save it as an WAV file in the specified folder.
    
    :param youtube_url: The YouTube URL to download.
    :param output_folder: The folder where the audio file will be saved.
    """
    
    # Retrieve the ffmpeg path from the configuration
    ffmpeg_path = get_from_config("ffmpeg_path")
    print(f"ffmpeg_path: {ffmpeg_path}")
    if not ffmpeg_path:
        raise ValueError("FFmpeg path not set. Please run 'smule.py --init' to configure the environment.")
    artist = song_metadata["Artist"]
    title = song_metadata["Title"]
    youtube_url = song_metadata["URL"]
    audio_file_name = f'{artist} - {title}'
    # YT-DLP options
    ydl_opts = {
        'format': 'bestaudio/best',
        'outtmpl': os.path.join(output_folder, audio_file_name+'.%(ext)s'),
        'ffmpeg_location': ffmpeg_path,  # Use the ffmpeg path from config
        'postprocessors': [{
            'key': 'FFmpegExtractAudio',
            'preferredcodec': 'wav',
            'preferredquality': '192',
        }],
    }

    try:
        # Use yt-dlp to download
        with yt_dlp.YoutubeDL(ydl_opts) as ydl:
            ydl.download([youtube_url])
        log_message(f"Downloaded audio from {youtube_url}")
    except Exception as e:
        log_message(f"Failed to download audio: {e}")
        raise

    # Find the downloaded wav
    return os.path.join(output_folder, audio_file_name+".wav")