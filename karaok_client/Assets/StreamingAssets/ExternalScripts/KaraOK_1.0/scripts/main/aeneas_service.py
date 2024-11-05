import os
import argparse
import subprocess
import sys
import moviepy.editor as mp
from aeneas.executetask import ExecuteTask
from aeneas.task import Task
from aeneas.tools.execute_task import ExecuteTaskCLI
from aeneas.runtimeconfiguration import RuntimeConfiguration
from config_manager import load_config, save_config, get_from_config

# Constant for virtual environment name
VENV_NAME = "smule-env"


def set_ffmpeg_paths():
    ffmpeg_path = get_from_config("ffmpeg_path")
    ffprobe_path = "/opt/homebrew/bin/ffprobe"
    print(ffmpeg_path)
    print(ffprobe_path)
    # Set ffmpeg and ffprobe in environment variables
    os.environ["IMAGEIO_FFMPEG_EXE"] = ffmpeg_path
    os.environ["FFPROBE_PATH"] = ffprobe_path

    # Set ffprobe path in aeneas config
    config = RuntimeConfiguration()
    config[RuntimeConfiguration.FFPROBE_PATH] = f'\"{ffprobe_path}\"'

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

def synchronize_lyrics(vocal_track_path, lyrics_file_path, config):
    """
    Synchronize lyrics with the vocal track using aeneas.
    
    :param vocal_track_path: Path to the vocal track (wav)
    :param lyrics_file_path: Path to the lyrics text file
    :param config: Aeneas RuntimeConfiguration object
    :return: Path to the generated subtitle file (srt)
    """
    activate_venv()
    set_ffmpeg_paths()
    configa = RuntimeConfiguration()
    configa[RuntimeConfiguration.FFPROBE_PATH] = "/opt/homebrew/bin/ffprobe"
    print(f"Updated FFPROBE_PATH: {configa[RuntimeConfiguration.FFPROBE_PATH]}")
    # Define the output subtitle file
    srt_output_path = os.path.join(os.path.dirname(vocal_track_path), "synchronized_lyrics.srt")

    # Create a Task object with input file, text, and output options, and pass the configuration
    task = Task(config_string="task_language=eng|is_text_type=plain|os_task_file_format=srt", rconf=configa)
    task.audio_file_path_absolute = vocal_track_path
    task.text_file_path_absolute = lyrics_file_path
    task.sync_map_file_path_absolute = srt_output_path

    # Process the task
    ExecuteTask(task).execute()
    task.output_sync_map_file()

    return srt_output_path

def generate_lyrics_video(instrumental_track_path, srt_file_path, output_video_path):
    """
    Generate a video with synchronized lyrics from the subtitle file and the instrumental track.
    
    :param instrumental_track_path: Path to the instrumental track (wav)
    :param srt_file_path: Path to the synchronized subtitle file (srt)
    :param output_video_path: Path where the generated video will be saved
    """
    # Create a blank video with lyrics
    video = mp.ColorClip(size=(1280, 720), color=(0, 0, 0), duration=mp.AudioFileClip(instrumental_track_path).duration)
    
    # Add instrumental audio
    audio = mp.AudioFileClip(instrumental_track_path)
    video = video.set_audio(audio)

    # Add lyrics (subtitles)
    subtitles = mp.TextClip(txt=open(srt_file_path).read(), fontsize=24, color='white', size=(1280, 720)).set_pos('center')
    video = mp.CompositeVideoClip([video, subtitles])

    # Export final video with audio
    video.write_videofile(output_video_path, fps=24)

def process_aeneas_service(instrumental_track, vocal_track, lyrics_file):
    """
    Orchestrate the entire process: synchronize lyrics, and generate video with instrumental track.
    
    :param instrumental_track: Path to the instrumental track (wav)
    :param vocal_track: Path to the vocal track (wav)
    :param lyrics_file: Path to the lyrics text file
    """
    #activate_venv()

    # Create an instance of RuntimeConfiguration
    config = RuntimeConfiguration()

    # Set the new ffprobe path
    load_config()
    new_ffprobe_path = ""  # Get the ffprobe path from the config
    config[RuntimeConfiguration.FFPROBE_PATH] = new_ffprobe_path

    # Verify the change
    print(f"Updated FFPROBE_PATH: {config[RuntimeConfiguration.FFPROBE_PATH]}")
    
    # Step 1: Synchronize lyrics with the vocal track
    print("Synchronizing lyrics with the vocal track...")
    srt_file_path = synchronize_lyrics(vocal_track, lyrics_file, config)
    print(f"Lyrics synchronized and saved to {srt_file_path}")

    # Step 2: Generate video with instrumental track and synchronized lyrics
    output_video_path = os.path.join(os.path.dirname(instrumental_track), "lyrics_video.mp4")
    print("Generating video with instrumental track and synchronized lyrics...")
    generate_lyrics_video(instrumental_track, srt_file_path, output_video_path)
    print(f"Video generated and saved to {output_video_path}")
    print(f"Return Value: {output_video_path}")

def main():
    # Set up argument parser for command-line arguments
    parser = argparse.ArgumentParser(description="Synchronize lyrics with vocal track and generate a lyrics video.")
    parser.add_argument("instrumental_track", help="Path to the instrumental track (wav file)")
    parser.add_argument("vocal_track", help="Path to the vocal track (wav file)")
    parser.add_argument("lyrics_file", help="Path to the lyrics text file")

    # Parse the arguments
    args = parser.parse_args()

    # Call the process_aeneas_service function
    process_aeneas_service(args.instrumental_track, args.vocal_track, args.lyrics_file)

if __name__ == "__main__":
    main()