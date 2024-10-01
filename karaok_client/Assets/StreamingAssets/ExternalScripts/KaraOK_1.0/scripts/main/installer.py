# installer.py (Handles the environment setup)
import os
import subprocess
import sys
from scripts.main.environment import activate_venv
from scripts.main.external_installs import install_ffmpeg
from scripts.main.config_manager import get_from_config, set_to_config


def setup_environment(venv_path, venv_name):
    """
    Set up the virtual environment and install necessary packages.
    """
    print(f"Creating or updating virtual environment '{venv_name}' at '{venv_path}'...")

    # Step 1: Remove existing virtual environment if it exists
    if os.path.exists(venv_path):
        print(f"Removing existing virtual environment at {venv_path}")
        subprocess.run(["rm", "-rf", venv_path])

    # Step 2: Create the virtual environment
    subprocess.run(["python3", "-m", "venv", venv_path])

    # Log the virtual environment creation path
    print(f"Virtual environment created at: {venv_path}")

    # Step 3: Fix permissions for the entire venv folder
    try:
        subprocess.run(["chmod", "-R", "+x", venv_path], check=True)
    except subprocess.CalledProcessError as e:
        print(f"Error: Permission denied while setting permissions for '{venv_path}'. Exiting with code 112.")
        sys.exit(112)

    # Step 4: Install ffmpeg using external_installs.py
    containing_folder_path = get_from_config("containing_folder_path")
    ffmpeg_path = install_ffmpeg(containing_folder_path)
    set_to_config("ffmpeg_path", ffmpeg_path)

    # Step 5: Activate virtual environment and install required packages
    activate_venv(venv_path, venv_name)
    
    # List of required packages, ensuring urllib3 is downgraded to version 1.26.15
    required_packages = ["demucs", "torchaudio", "yt-dlp", "diffq", "soundfile"]
    
    # Install all required packages, ensuring urllib3 is installed with a specific version
    subprocess.run([os.path.join(venv_path, "bin", "pip"), "install", "--upgrade"] + required_packages)

    print(f"Environment setup complete at: {venv_path}")