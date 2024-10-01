# converter.py (Handles the conversion process)
from scripts.functional.youtube_downloader import download_audio_from_youtube
from scripts.functional.audio_processing import separate_vocals, convert_wav_to_m4a
from scripts.functional.logger import log_message
from scripts.functional.report import print_report
import os

# Demucs models mapping
DEMUCS_MODELS = {
    1: "htdemucs",      # Fastest
    2: "htdemucs_ft",   # Default
    3: "mdx_extra",     # Slower
    4: "mdx_extra_q"    # Slowest
}

def temp_output_path():
    """
    Verifies if the demucs_temp path exists, and creates it if it does not.
    The path is <home>/<user>/demucs_temp.
    """
    # Get the user's home directory
    home_dir = os.path.expanduser('~')

    # Define the path to <home>/<user>/demucs_temp
    demucs_temp_path = os.path.join(home_dir, "kara-ok_temp")

     # Check if the path exists
    if not os.path.exists(demucs_temp_path):
        try:
            # If it doesn't exist, create the directory
            os.makedirs(demucs_temp_path)
            print(f"Path created: {demucs_temp_path}")
        except Exception as e:
            print(f"Error creating path: {e}")
            return False
    else:
        print(f"Path already exists: {demucs_temp_path}")

    return demucs_temp_path

def convert(youtube_url, output_folder, model_int=2):
    temp_output_folder = temp_output_path()
    """
    Run the actual process (downloading, converting) with YouTube URL, output folder, and Demucs model.
    """
    # Log the start of the conversion process
    log_message(f"Starting conversion for YouTube URL: {youtube_url}")
    print(f"Output folder sanitized and created (if not existing): {temp_output_folder}")

    try:
        # Step 1: Download the audio from YouTube
        audio_file_path = download_audio_from_youtube(youtube_url, temp_output_folder)
        log_message(f"Downloaded file: {audio_file_path}")

        # Step 2: Parse the model integer into the Demucs model name
        demucs_model = DEMUCS_MODELS.get(model_int, "htdemucs_ft")  # Default to "htdemucs_ft" if invalid
        log_message(f"Using Demucs model: {demucs_model}")
        # Step 3: Separate vocals using the audio processing script
        log_message("Starting vocal separation process.")
        separate_vocals(audio_file_path, temp_output_folder, demucs_model)

        # Step 4: Convert WAV files to M4A
        from files_utils import move, delete
        song_name = os.path.basename(audio_file_path).replace(".mp3", "")
        demucs_outputs = os.path.join(temp_output_folder, demucs_model, song_name)
        destination_folder = os.path.join(output_folder, "outputs")
        move(demucs_outputs, destination_folder)
        song_outputs_path = os.path.join(destination_folder, song_name)
        wav_file_path = os.path.join(song_outputs_path, "no_vocals.wav")
        delete(temp_output_path())

        log_message(f"Starting conversion from WAV to M4A. WAV file path: {wav_file_path}, Output folder path: {song_outputs_path}")

        # Call the convert_wav_to_m4a method
        m4a_file_path = convert_wav_to_m4a(wav_file_path, song_outputs_path)

        # Log the final file creation
        log_message(f"Final M4A file created at: {m4a_file_path}")
        
        # Step 5: Generate and print the final report
        print_report(youtube_url, True, True, m4a_file_path, 0)
        
    except Exception as e:
        log_message(f"An error occurred: {e}")
        print_report(youtube_url, False, False, "", 1)
        raise e