import os
import subprocess
import moviepy.editor as mp
from aeneas.executetask import ExecuteTask
from aeneas.task import Task
from aeneas.tools.execute_task import ExecuteTaskCLI
from aeneas.runtimeconfiguration import RuntimeConfiguration
from config_manager import load_config, save_config

def synchronize_lyrics(vocal_track_path, lyrics_text):
    """
    Synchronize lyrics with the vocal track using aeneas.
    
    :param vocal_track_path: Path to the vocal track (wav)
    :param lyrics_text: Lyrics to be synchronized (string)
    :return: Path to the generated subtitle file (srt)
    """
    # Define the output subtitle file
    srt_output_path = os.path.join(os.path.dirname(vocal_track_path), "synchronized_lyrics.srt")

    # Create a Task object with input file, text, and output options
    task = Task(config_string="task_language=eng|is_text_type=plain|os_task_file_format=srt")
    task.audio_file_path_absolute = vocal_track_path
    task.text_file_path_absolute = lyrics_text
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

def process_aeneas_service(instrumental_track, vocal_track, lyrics):
    """
    Orchestrate the entire process: synchronize lyrics, and generate video with instrumental track.
    
    :param instrumental_track: Path to the instrumental track (wav)
    :param vocal_track: Path to the vocal track (wav)
    :param lyrics: Lyrics to be synchronized (string)
    """
    # Create a temp file for lyrics
    lyrics_temp_file = "lyrics.txt"
    with open(lyrics_temp_file, 'w') as f:
        f.write(lyrics)

    # Step 1: Synchronize lyrics with the vocal track
    print("Synchronizing lyrics with the vocal track...")
    srt_file_path = synchronize_lyrics(vocal_track, lyrics_temp_file)
    print(f"Lyrics synchronized and saved to {srt_file_path}")

    # Step 2: Generate video with instrumental track and synchronized lyrics
    output_video_path = os.path.join(os.path.dirname(instrumental_track), "lyrics_video.mp4")
    print("Generating video with instrumental track and synchronized lyrics...")
    generate_lyrics_video(instrumental_track, srt_file_path, output_video_path)
    print(f"Video generated and saved to {output_video_path}")

    # Clean up temp files
    os.remove(lyrics_temp_file)