# converter.py (Handles the conversion process)
from scripts.functional.youtube_downloader import download_audio_from_youtube
from scripts.functional.audio_processing import separate_vocals, convert_wav_to_m4a
from scripts.functional.logger import log_message
from scripts.functional.report import print_report
from scripts.functional.sanitize import sanitize_output_folder
import os

# Demucs models mapping
DEMUCS_MODELS = {
    1: "htdemucs",      # Fastest
    2: "htdemucs_ft",   # Default
    3: "mdx_extra",     # Slower
    4: "mdx_extra_q"    # Slowest
}

def convert(youtube_url, output_folder, model_int=2):
    """
    Run the actual process (downloading, converting) with YouTube URL, output folder, and Demucs model.
    """
    # Log the start of the conversion process
    log_message(f"Starting conversion for YouTube URL: {youtube_url}")
    
    # Sanitize the output folder
    sanitized_output_folder = sanitize_output_folder(output_folder)
    os.makedirs(sanitized_output_folder, exist_ok=True)
    log_message(f"Output folder sanitized and created (if not existing): {sanitized_output_folder}")

    try:
        # Step 1: Download the audio from YouTube
        audio_file_path = download_audio_from_youtube(youtube_url, sanitized_output_folder)
        log_message(f"Downloaded file: {audio_file_path}")

        # Step 2: Parse the model integer into the Demucs model name
        demucs_model = DEMUCS_MODELS.get(model_int, "htdemucs_ft")  # Default to "htdemucs_ft" if invalid
        log_message(f"Using Demucs model: {demucs_model}")

        # Step 3: Separate vocals using the audio processing script
        log_message("Starting vocal separation process.")
        separate_vocals(audio_file_path, sanitized_output_folder, demucs_model)

        # Step 4: Convert WAV files to M4A
        wav_file_path = os.path.join(sanitized_output_folder, demucs_model, os.path.basename(audio_file_path).replace(".mp3", ""), "no_vocals.wav")
        m4a_output_folder_path = os.path.dirname(os.path.dirname(wav_file_path))

        log_message(f"Starting conversion from WAV to M4A. WAV file path: {wav_file_path}, Output folder path: {m4a_output_folder_path}")

        # Call the convert_wav_to_m4a method
        m4a_file_path = convert_wav_to_m4a(wav_file_path, m4a_output_folder_path)

        # Log the final file creation
        log_message(f"Final M4A file created at: {m4a_file_path}")
        
        # Step 5: Generate and print the final report
        print_report(youtube_url, True, True, m4a_file_path, 0)
        
    except Exception as e:
        log_message(f"An error occurred: {e}")
        print_report(youtube_url, False, False, "", 1)
        raise e