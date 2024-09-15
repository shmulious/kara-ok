import yt_dlp
import os
from .logger import log_message
from scripts.main.config_manager import get_from_config

def download_audio_from_youtube(youtube_url, output_folder):
    """
    Download audio from a YouTube URL and save it as an MP3 file in the specified folder.
    
    :param youtube_url: The YouTube URL to download.
    :param output_folder: The folder where the audio file will be saved.
    """
    
    # Retrieve the ffmpeg path from the configuration
    ffmpeg_path = get_from_config("ffmpeg_path")
    if not ffmpeg_path:
        raise ValueError("FFmpeg path not set. Please run 'smule.py --init' to configure the environment.")

    # YT-DLP options
    ydl_opts = {
        'format': 'bestaudio/best',
        'outtmpl': os.path.join(output_folder, '%(title)s.%(ext)s'),
        'ffmpeg_location': ffmpeg_path,  # Use the ffmpeg path from config
        'postprocessors': [{
            'key': 'FFmpegExtractAudio',
            'preferredcodec': 'mp3',
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

    # Find the downloaded MP3 file
    downloaded_files = [f for f in os.listdir(output_folder) if f.endswith('.mp3')]
    if not downloaded_files:
        raise FileNotFoundError("No MP3 file was downloaded.")
    
    # Return the path of the most recent file
    downloaded_file = max(downloaded_files, key=lambda f: os.path.getctime(os.path.join(output_folder, f)))
    return os.path.join(output_folder, downloaded_file)