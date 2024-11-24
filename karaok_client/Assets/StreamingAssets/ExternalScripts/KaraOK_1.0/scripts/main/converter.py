# converter.py (Handles the conversion process)
from scripts.functional.youtube_downloader import download_audio_from_youtube
from scripts.functional.audio_processing import separate_vocals, convert_wav_to_m4a
from scripts.functional.logger import log_message
from scripts.functional.report import print_report
from files_utils import move, load_json_file  # Import the new move function
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
    The path is <home>/<user>/kara-ok_temp.
    """
    # Get the user's home directory
    home_dir = os.path.expanduser('~')

    # Define the path to <home>/<user>/kara-ok_temp
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

def convert(metadata_path, output_folder, model_int=2):
    song_metadata = load_json_file(metadata_path)
    temp_output_folder = temp_output_path()
    """
    Run the actual process (downloading, converting) with YouTube URL, output folder, and Demucs model.
    """
    # Log the start of the conversion process
    youtube_url = song_metadata["URL"]
    log_message(f"Starting conversion for YouTube URL: {youtube_url}")
    print(f"Output folder sanitized and created (if not existing): {temp_output_folder}")

    try:
        # Step 1: Download the audio from YouTube
        audio_file_path = download_audio_from_youtube(song_metadata, temp_output_folder)
        log_message(f"Downloaded file: {audio_file_path}")

        # Step 2: Parse the model integer into the Demucs model name
        demucs_model = DEMUCS_MODELS.get(model_int, "htdemucs_ft")  # Default to "htdemucs_ft" if invalid
        log_message(f"Using Demucs model: {demucs_model}")
        
        # Step 3: Separate vocals using the audio processing script
        log_message("Starting vocal separation process.")
        separate_vocals(audio_file_path, temp_output_folder, demucs_model)

        # Step 4: Move Demucs outputs to the final destination
        song_name = os.path.basename(audio_file_path).replace(".wav", "")
        demucs_outputs = os.path.join(temp_output_folder, demucs_model, song_name)
        move(audio_file_path, demucs_outputs)
        # destination_folder = os.path.join(output_folder, "outputs")
        # Move the files, overwriting duplicates but keeping the folder intact
        song_outputs_path = os.path.join(output_folder, song_name)
        move(demucs_outputs, song_outputs_path)
        #save vocals and no vocals paths
        no_vocals_path = os.path.join(song_outputs_path, "no_vocals.wav")
        vocals_file_path = os.path.join(song_outputs_path, "vocals.wav")

        # Step 5: Convert WAV files to M4A
        
        log_message(f"Starting conversion from WAV to M4A. WAV file path: {no_vocals_path}, Output folder path: {song_outputs_path}")

        # Call the convert_wav_to_m4a method
        m4a_file_path = convert_wav_to_m4a(no_vocals_path, song_outputs_path)

        # Log the final file creation
        log_message(f"Final M4A file created at: {m4a_file_path}")

        # Uncomment this if you want to process the Aeneas service for lyrics synchronization
        # process_aeneas_service(wav_file_path, vocals_file_path, lyrics)

        # Step 6: Generate and print the final report
        print_report({youtube_url}, True, True, m4a_file_path, 0)
        print (f'Return Value: {song_outputs_path}')

    except Exception as e:
        log_message(f"An error occurred: {e}")
        print_report({youtube_url}, False, False, "", 1)
        raise e