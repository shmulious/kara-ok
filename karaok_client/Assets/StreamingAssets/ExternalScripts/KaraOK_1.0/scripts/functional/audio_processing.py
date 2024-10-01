import os
import subprocess
from scripts.main.environment import get_venv_path
from scripts.main.config_manager import get_from_config

def sanitize_output_folder(output_folder):
    """
    Sanitize the output folder path by removing trailing slashes and ensuring the path is valid.
    """
    sanitized_path = os.path.normpath(output_folder)
    if not os.path.exists(sanitized_path):
        os.makedirs(sanitized_path)
    return sanitized_path

def separate_vocals(audio_file_path, output_folder, model):
    """
    Separate vocals from the audio file using Demucs with the specified model inside the virtual environment.
    """
    # Get the virtual environment path using get_venv_path
    venv_path = get_venv_path()
    demucs_command = os.path.join(venv_path, "bin", "demucs")

    print(f"Using Demucs model: {model}")
    
    # Use the full path to the Demucs executable with the selected model
    try:
        # Adding the stem specification to extract only vocals
        subprocess.run([demucs_command, "-n", model, "--two-stems=vocals", audio_file_path, '-o', output_folder], check=True)
        print(f"Vocals successfully separated for {audio_file_path}")
    except subprocess.CalledProcessError as e:
        print(f"Error during vocal separation: {e}")
        raise

def convert_wav_to_m4a(input_wav_path, output_directory):
    """
    Convert the separated WAV files to M4A format using ffmpeg.
    The output file name will be <parent_folder_name>_<wav_file_name_without_ext>.m4a.
    """
    # Retrieve the ffmpeg path from the configuration
    ffmpeg_path = get_from_config("ffmpeg_path")
    if not ffmpeg_path:
        raise ValueError("FFmpeg path not set. Please run 'smule.py --init' to configure the environment.")

    # Ensure the output directory exists
    os.makedirs(output_directory, exist_ok=True)

    # Extract the parent folder name
    parent_folder_name = os.path.basename(os.path.dirname(input_wav_path))

    # Extract the WAV file name without extension
    wav_file_name_without_ext = os.path.splitext(os.path.basename(input_wav_path))[0]

    # Construct the output file name in the format <parent_folder_name>_<wav_file_name_without_ext>.m4a
    m4a_output_filename = f"{parent_folder_name}_{wav_file_name_without_ext}.m4a"
    m4a_output_path = os.path.join(output_directory, m4a_output_filename)

    print(f"Converting {input_wav_path} to {m4a_output_path} using ffmpeg...")

    try:
        # Use the configured ffmpeg path to convert wav to m4a, adding -y to overwrite existing files
        subprocess.run([ffmpeg_path, '-i', input_wav_path, '-c:a', 'aac', '-b:a', '320k', '-y', m4a_output_path], check=True)
        print(f"Successfully converted to {m4a_output_path}")
        return m4a_output_path
    except subprocess.CalledProcessError as e:
        print(f"Error during conversion: {e}")
        raise