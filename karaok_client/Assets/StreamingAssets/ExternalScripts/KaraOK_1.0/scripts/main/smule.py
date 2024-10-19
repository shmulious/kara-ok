import sys
import os
import argparse

# Add the root of the project (KaraOK_1.0) to sys.path
PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
sys.path.append(PROJECT_ROOT)

from scripts.main.installer import setup_environment
from scripts.main.config_manager import set_to_config, get_from_config, load_config
from scripts.main.environment import activate_venv, check_environment, ensure_virtual_env

VENV_NAME = "smule-env"

def log_environment_details(venv_path):
    """
    Log virtual environment details, including existence check with color.
    """
    venv_exists = os.path.exists(venv_path)
    if venv_exists:
        print(f"\033[92mReferring to virtual environment '{VENV_NAME}' at: {venv_path} (Exists: True)\033[0m")
    else:
        print(f"\033[91mReferring to virtual environment '{VENV_NAME}' at: {venv_path} (Exists: False)\033[0m")

def init_action(containing_folder_path):
    """
    Initialize the environment, install ffmpeg, and save paths in config.
    """
    # Load the current configuration
    config_data = load_config()

    # Print the current configuration loaded from the file
    print("Loaded configuration:", config_data)

def environment_exists():
    """
    Check if the virtual environment exists by verifying the existence of the activate script.
    Returns True if it exists, False otherwise.
    """
    venv_path = get_from_config('venv_path')
    
    if not venv_path:
        return False

    if sys.platform == "win32":
        activate_script = os.path.join(venv_path, "Scripts", "activate.bat")
    else:
        activate_script = os.path.join(venv_path, "bin", "activate")

    return os.path.exists(activate_script)

def main():
    parser = argparse.ArgumentParser(description="Smule CLI for YouTube conversion and processing")
     
    # Command options
    parser.add_argument('--init', nargs=1, metavar=('containing_folder_path'), help="Initialize and set up environment with the given folder path")
    parser.add_argument('--install', action='store_true', help="Create or overwrite the virtual environment and install necessary packages")
    parser.add_argument('--convert', nargs=3, metavar=('metadata_path', 'output_folder', 'model'), help="Run the process for downloading and converting the YouTube video. Model is an integer between 1 (fastest) and 4 (slowest), default is 2.")
    parser.add_argument('--wav2m4a', nargs=2, metavar=('input_file_path', 'output_directory_path'), help="Convert a WAV file to M4A format")
    parser.add_argument('--version', action='store_true', help="Check if the environment is installed")
    parser.add_argument('--demo', action='store_true', help="Run the process with hardcoded demo arguments")
    parser.add_argument('--environmentExists', action='store_true', help="Check if the environment exists")
    parser.add_argument('--getmetadata', nargs=1, metavar=('youtube_url'), help="Retrieve metadata (artist and song name) from the given YouTube URL")

    args = parser.parse_args()

    # Handle the --init action
    if args.init:
        from error_handler import setup_global_error_handling
        setup_global_error_handling()
        init_action(args.init[0])
        sys.exit(0)

    # Handle the --environmentExists action
    if args.environmentExists:
        env_exists = environment_exists()
        sys.exit(0 if env_exists else 1)  # Exit with 0 if True, 1 if False

    # Load venv_path from config
    venv_path = get_from_config('venv_path')

    if not venv_path:
        print("Error: No virtual environment path set. Please run 'smule.py --init <containing_folder_path>' first.")
        exit(1)

    if args.install:
        #log_environment_details(venv_path)
        setup_environment(venv_path, VENV_NAME)
        exit(0)

    else:
        # Activate and ensure the environment is being used
        activate_venv(venv_path, VENV_NAME)
        ensure_virtual_env(venv_path)
        
        if args.getmetadata:
            from scripts.functional.metadata_provider import get_song_metadata
            youtube_url = args.getmetadata[0]
            artist, title, thumbnail, id = get_song_metadata(youtube_url)
            print(f"Artist: {artist}, Title: {title}, Thumbnail: {thumbnail}")
            sys.exit(0)

        if args.convert:
            log_environment_details(venv_path)
            if check_environment(venv_path, VENV_NAME):
                metadata_path, output_folder, model = args.convert
                model = int(model) if model else 2  # Default to 2 if model is not provided

                from scripts.main.converter import convert
                convert(metadata_path, output_folder, model)
                exit(0)
            else:
                print(f"Error: Environment '{VENV_NAME}' is not installed. Run 'smule.py --install' first.")
                exit(1)

        elif args.wav2m4a:
            input_file_path, output_directory_path = args.wav2m4a
            print(f"Converting WAV to M4A: {input_file_path} to {output_directory_path}")
            from scripts.functional.audio_processing import convert_wav_to_m4a
            convert_wav_to_m4a(input_file_path, output_directory_path)

        elif args.demo:
            log_environment_details(venv_path)
            if check_environment(venv_path, VENV_NAME):
                from scripts.main.demo import demo
                demo()
            else:
                print(f"Error: Environment '{VENV_NAME}' is not installed. Run 'smule.py --install' first.")
                exit(1)

        elif args.version:
            log_environment_details(venv_path)
            check_environment(venv_path, VENV_NAME)

if __name__ == "__main__":
    main()