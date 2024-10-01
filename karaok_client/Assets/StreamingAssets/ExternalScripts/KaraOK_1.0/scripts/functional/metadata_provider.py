import yt_dlp
import os
import json
import re
from .logger import log_message
from scripts.main.config_manager import get_from_config

def validate_json_data(result):
    """
    Validate the structure of the JSON data.
    Ensures that the required fields are present and properly formatted.
    """
    if 'thumbnails' in result and not isinstance(result['thumbnails'], list):
        raise ValueError(f"Invalid data format for thumbnails. Expected a list, got {type(result['thumbnails'])}.")

    if not isinstance(result.get('artist', ''), str):
        raise ValueError(f"Invalid data format for artist. Expected a string, got {type(result['artist'])}.")
    if not isinstance(result.get('title', ''), str):
        raise ValueError(f"Invalid data format for title. Expected a string, got {type(result['title'])}.")

    return True


def extract_artist_title_from_title(title):
    """
    Attempt to extract artist and title from the video title if the artist is missing or marked as unknown.
    Common formats:
    - "Artist - Title"
    - "Artist: Title"
    - "Artist | Title"
    - "Artist-Title"
    - "Artist -Title (Some Other Text)"
    - "Artist - Title (Some Other Text)"
    """
    # Define possible delimiters between artist and title, with optional spaces around the delimiters
    # Match patterns with or without spaces around the delimiters
    delimiters_pattern = r'\s*[-:|–—]\s*'

    # Remove any trailing text in parentheses, like "(Official Video)" or "(Lyrics)"
    title = re.sub(r'\(.*?\)', '', title).strip()

    # Split the title using the delimiters, allowing for spaces or no spaces around the delimiters
    parts = re.split(delimiters_pattern, title, 1)
    
    if len(parts) == 2:
        artist = parts[0].strip()
        song_title = parts[1].strip()

        # Ensure the extracted artist and title are reasonable
        if len(artist) > 0 and len(song_title) > 0:
            return artist, song_title

    # If no artist could be found, return the original title as the title
    return "Unknown Artist", title.strip()


def get_song_metadata(youtube_url):
    """
    Extract song metadata (artist, title, and a list of thumbnails) from a YouTube video using yt-dlp.
    
    :param youtube_url: The YouTube URL to extract metadata from.
    :return: A dictionary containing artist, title, and a list of thumbnails.
    """
    ydl_opts = {
        'quiet': True,  # Suppress verbose output
        'skip_download': True,  # Do not download the video, just extract metadata
        'extract_flat': True,  # Avoid downloading any additional media (if it's a playlist)
    }

    try:
        with yt_dlp.YoutubeDL(ydl_opts) as ydl:
            info_dict = ydl.extract_info(youtube_url, download=False)

            # Extract relevant metadata
            title = info_dict.get('title', 'Unknown Title')
            artist = info_dict.get('artist', 'Unknown Artist')

            # Check if the artist is unknown, and try to extract it from the title
            if artist == 'Unknown Artist':
                artist, title = extract_artist_title_from_title(title)

            # Extract all available thumbnails (YouTube may provide multiple resolutions)
            thumbnails = info_dict.get('thumbnails', [])
            thumbnail_urls = [thumbnail['url'] for thumbnail in thumbnails if thumbnail['url'].endswith((".jpg", ".jpeg"))]

            # Sort and limit to the top 5 highest-quality thumbnails
            thumbnail_urls = sorted(thumbnail_urls, key=lambda url: ('maxresdefault' in url, 'hq' in url), reverse=True)[:5]

            # If the main thumbnail is a JPG, prioritize it by placing it at the top
            main_thumbnail = info_dict.get('thumbnail')
            if main_thumbnail and main_thumbnail.endswith(".jpg") and main_thumbnail not in thumbnail_urls:
                thumbnail_urls.insert(0, main_thumbnail)

            # Create a dictionary for JSON output
            result = {
                "artist": artist,
                "title": title,
                "thumbnails": thumbnail_urls
            }

            # Validate the JSON structure
            validate_json_data(result)

            # Log the result
            log_message(f"Extracted metadata from {youtube_url} - Artist: {result['artist']}, Title: {result['title']}, Thumbnails: {result['thumbnails']}")

            # Print the JSON formatted result
            print(f"Return Value: {json.dumps(result)}")

            return result

    except Exception as e:
        log_message(f"Failed to extract metadata: {e}")
        raise

# Example usage:
# metadata = get_song_metadata("https://www.youtube.com/watch?v=dQw4w9WgXcQ")
# print(f"Metadata: {metadata}")