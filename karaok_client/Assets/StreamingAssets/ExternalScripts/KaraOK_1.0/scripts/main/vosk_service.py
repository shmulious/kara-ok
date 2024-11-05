import os
import subprocess
import vosk
import json
import argparse
import shutil
import wave
import sys
from moviepy.editor import TextClip, CompositeVideoClip, AudioFileClip, ColorClip
from config_manager import get_from_config, load_config
import logging
logging.basicConfig(level=logging.DEBUG)

0.0
VENV_NAME = "smule-env"

def activate_venv():
    """
    Activate the virtual environment by sourcing its activate script and save the venv_path.
    """
    global _venv_path
    load_config()
    venv_path = get_from_config('venv_path')
    print(f"Referring to virtual environment '{VENV_NAME}' located at: {venv_path}")
    
    if sys.platform == "win32":
        activate_script = os.path.join(venv_path, "Scripts", "activate.bat")
    else:
        activate_script = os.path.join(venv_path, "bin", "activate")

    if os.path.exists(activate_script):
        print(f"Activating virtual environment: {VENV_NAME}")
        
        # Activate the virtual environment using subprocess
        if sys.platform == "win32":
            subprocess.call([activate_script], shell=True)
        else:
            # For Linux/Mac, source the activate script
            subprocess.call(f'source "{activate_script}"', shell=True, executable="/bin/bash")
        
        # Save the virtual environment path after successful activation
        _venv_path = venv_path
        os.environ["IMAGEIO_FFMPEG_EXE"] = get_from_config("ffmpeg_path")
    else:
        print(f"Error: Activate script not found in virtual environment '{VENV_NAME}' at: {venv_path}")
        sys.exit(112)

def ensure_ffmpeg_and_imagemagick_paths():
    """
    Ensures both ffmpeg and ImageMagick paths are set by fetching them from the config.
    Raises an error if any of them are not found or not set correctly.
    """
    ffmpeg_path = os.getenv("FFMPEG_PATH")
    if not ffmpeg_path or ffmpeg_path == "unset":
        print("FFMPEG_PATH is not set correctly")

    # Fetch ffmpeg and ImageMagick paths from the config
    ffmpeg_path = "/Users/shmuelvachnish/Library/Application Support/Shmulious/Kara-OK/setup_folder/venvs/vosk-env/bin/ffmpeg"
    #imagemagick_path = get_from_config("imagemagick_path", None)

    # Check if ffmpeg path is valid
    if not ffmpeg_path or not os.path.exists(ffmpeg_path):
        raise FileNotFoundError(f"FFmpeg path '{ffmpeg_path}' is not set or invalid. Please ensure it is correctly specified in the configuration.")
    
    # Check if the ffmpeg binary is executable
    if shutil.which(ffmpeg_path) is None:
        raise EnvironmentError(f"FFmpeg binary not found at '{ffmpeg_path}'. Please ensure it is installed and the correct path is specified in the configuration.")

    # Check if ImageMagick path is valid
    i#f not imagemagick_path or not os.path.exists(imagemagick_path):
      #  raise FileNotFoundError(f"ImageMagick path '{imagemagick_path}' is not set or invalid. Please ensure it is correctly specified in the configuration.")
    
    # Check if the ImageMagick 'magick' binary is executable
    #if shutil.which(imagemagick_path) is None:
      #  raise EnvironmentError(f"ImageMagick binary not found at '{imagemagick_path}'. Please ensure it is installed and the correct path is specified in the configuration.")

    # Set environment variables for ffmpeg and ImageMagick
    os.environ["FFMPEG_PATH"] = ffmpeg_path
    #os.environ["IMAGEMAGICK_PATH"] = imagemagick_path

    print(f"FFmpeg path set to: {ffmpeg_path}")
    #print(f"ImageMagick path set to: {imagemagick_path}")


def transcribe_audio(vocal_track_path, vosk_model_path):
    """ Transcribe the vocal track using Vosk and return word timestamps. """
    model = vosk.Model(vosk_model_path)
    wf = wave.open(vocal_track_path, "rb")

    if wf.getnchannels() != 1 or wf.getsampwidth() != 2 or wf.getframerate() not in [8000, 16000, 44100]:
        raise ValueError("Unsupported audio format for Vosk. Use mono, 16-bit, and either 8000, 16000, or 44100 Hz. converting..,")

    rec = vosk.KaldiRecognizer(model, wf.getframerate())
    results = []

    while True:
        data = wf.readframes(4000)
        if len(data) == 0:
            break
        if rec.AcceptWaveform(data):
            results.append(json.loads(rec.Result()))
        else:
            results.append(json.loads(rec.PartialResult()))
    print("[VOSK RETURN]")
    return results

def align_lyrics_with_transcript(lyrics_file, transcript):
    """ Align lyrics with Vosk transcription and return synchronized words with timestamps. """
    with open(lyrics_file, 'r') as f:
        lyrics = f.read().split()

    words = []
    transcript_words = [res["text"] for res in transcript if "text" in res]
    transcript_start_times = [res["start"] for res in transcript if "start" in res]

    # Match words and align timestamps
    for word, start_time in zip(transcript_words, transcript_start_times):
        words.append({"word": word, "start": start_time})

    return words

def generate_srt_file(synchronized_words, output_srt_path):
    """ Generate an SRT file with lyrics timestamps. """
    
    # Ensure the directory for the SRT file exists, create if not
    os.makedirs(os.path.dirname(output_srt_path), exist_ok=True)

    # Create or override the SRT file
    with open(output_srt_path, 'w') as f:
        for i, word_info in enumerate(synchronized_words):
            start_time = word_info["start"]
            end_time = start_time + 0.5  # Estimate word length to 0.5s
            f.write(f"{i+1}\n")
            f.write(f"{format_timestamp(start_time)} --> {format_timestamp(end_time)}\n")
            f.write(f"{word_info['word']}\n\n")

def format_timestamp(seconds):
    """ Helper function to format seconds into SRT timestamp format. """
    h, m = divmod(int(seconds), 3600)
    m, s = divmod(m, 60)
    ms = int((seconds - int(seconds)) * 1000)
    return f"{h:02}:{m:02}:{s:02},{ms:03}"

def create_lyrics_video(instrumental_track_path, srt_file_path, output_video_path):
    """ Generate a video with synchronized lyrics from SRT file and instrumental audio. """
    
    # Ensure the directory for the output video exists, create if not
    os.makedirs(os.path.dirname(output_video_path), exist_ok=True)
    
    audio = AudioFileClip(instrumental_track_path)
    duration = audio.duration

    # Create a blank background for the lyrics video
    video = ColorClip(size=(1280, 720), color=(0, 0, 0), duration=duration)
    video = video.set_audio(audio)

    # Create subtitles from SRT file
    with open(srt_file_path, 'r') as f:
        srt_text = f.read()

    # Add subtitles as a text overlay
    subtitles = TextClip(srt_text, fontsize=24, color='white', size=(1280, 720)).set_pos('center')
    video = CompositeVideoClip([video, subtitles])

    # Create or override the MP4 file
    video.write_videofile(output_video_path, fps=24)

def main():
    parser = argparse.ArgumentParser(description="Sync lyrics to a vocal track and generate a video.")
    parser.add_argument("--vocal_track_path", required=True, help="Path to the vocal track (wav file)")
    parser.add_argument("--lyrics_file", required=True, help="Path to the lyrics file (txt file)")
    parser.add_argument("--instrumental_track_path", required=True, help="Path to the instrumental track (wav file)")
    parser.add_argument("--output_srt_path", required=True, help="Path to output SRT file")
    parser.add_argument("--output_video_path", required=True, help="Path to output video file")

    args = parser.parse_args()
   
    # Load Vosk model path from the configuration
    vosk_model_path = "/Users/shmuelvachnish/Library/Application Support/Shmulious/Kara-OK/setup_folder/venvs/vosk_models/vosk-model-en-us-0.22"
    os.environ["IMAGEIO_FFMPEG_EXE"] = "/Users/shmuelvachnish/Library/Application Support/Shmulious/Kara-OK/setup_folder/venvs/vosk-env/bin/ffmpeg"
    #ensure_ffmpeg_and_imagemagick_paths()

    if vosk_model_path is None:
        raise ValueError("Vosk model path not found in configuration.")

    # Step 1: Transcribe the vocals
    transcript = transcribe_audio(args.vocal_track_path, vosk_model_path)

    # Step 2: Align the lyrics with the transcription
    synchronized_words = align_lyrics_with_transcript(args.lyrics_file, transcript)

    # Step 3: Generate an SRT file
    generate_srt_file(synchronized_words, args.output_srt_path)

    # Step 4: Create a video with the lyrics
    create_lyrics_video(args.instrumental_track_path, args.output_srt_path, args.output_video_path)

if __name__ == "__main__":
    main()