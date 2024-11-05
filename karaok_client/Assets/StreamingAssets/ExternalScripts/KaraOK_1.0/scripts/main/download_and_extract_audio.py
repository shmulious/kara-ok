import os
import subprocess
import sys

def activate_venv(venv_path, venv_name):
    """
    Activate the virtual environment by sourcing its activate script and save the venv_path.
    """
    global _venv_path
    print(f"Referring to virtual environment '{venv_name}' located at: {venv_path}")
    
    if sys.platform == "win32":
        activate_script = os.path.join(venv_path, "Scripts", "activate.bat")
    else:
        activate_script = os.path.join(venv_path, "bin", "activate")

    if os.path.exists(activate_script):
        print(f"Activating virtual environment: {venv_name}")
        
        # Activate the virtual environment using subprocess
        if sys.platform == "win32":
            subprocess.call([activate_script], shell=True)
        else:
            # For Linux/Mac, source the activate script
            subprocess.call(f'source "{activate_script}"', shell=True, executable="/bin/bash")
        
        # Save the virtual environment path after successful activation
        _venv_path = venv_path
    else:
        print(f"Error: Activate script not found in virtual environment '{venv_name}' at: {venv_path}")
        sys.exit(112)


def ensure_requests_installed(venv_path):
    """Ensure that the requests library is installed in the specified virtual environment"""
    try:
        import requests
    except ModuleNotFoundError:
        print("requests module not found in the virtual environment. Installing...")

        # Path to pip inside the virtual environment
        pip_executable = os.path.join(venv_path, "bin", "pip") if os.name != "nt" else os.path.join(venv_path, "Scripts", "pip.exe")

        if not os.path.exists(pip_executable):
            print(f"pip not found in virtual environment at {pip_executable}")
            sys.exit(1)

        # Run pip from the virtual environment to install requests
        subprocess.run([pip_executable, "install", "requests"], check=True)
        import requests  # Re-import after installation

    return requests

def download_media_file(url, output_path, venv_path):
    requests = ensure_requests_installed(venv_path)  # Ensure requests is installed
    file_name = os.path.basename(url)
    file_path = os.path.join(output_path, file_name)

    response = requests.get(url, stream=True)
    if response.status_code == 200:
        with open(file_path, 'wb') as f:
            f.write(response.content)
        print(f"Downloaded file to: {file_path}")
        extract_audio(file_path, output_path, venv_path)
    else:
        print("Failed to download file")


def get_unique_filename(base_path, extension):
    """Generate a unique filename by adding a suffix if the file already exists."""
    if not os.path.exists(f"{base_path}{extension}"):
        return f"{base_path}{extension}"
    
    counter = 0
    while os.path.exists(f"{base_path}_{counter}{extension}"):
        counter += 1
    return f"{base_path}_{counter}{extension}"

def extract_audio(media_file_path, output_path, venv_path):
    # Get base name from parent directory and file name
    dir_name = os.path.basename(os.path.dirname(media_file_path))
    base_output_name = os.path.join(output_path, f"{dir_name}")

    # Define paths for MP3 and M4A files, ensuring they are unique
    mp3_output_path = get_unique_filename(base_output_name, ".mp3")
    m4a_output_path = get_unique_filename(base_output_name, ".m4a")

    # Path to ffmpeg within the virtual environment
    ffmpeg_path = os.path.join(venv_path, "bin", "ffmpeg")

    # Extract to MP3
    subprocess.run([ffmpeg_path, "-i", media_file_path, "-q:a", "0", mp3_output_path], check=True)
    print(f"Audio extracted to MP3: {mp3_output_path}")

    # Extract to M4A
    subprocess.run([ffmpeg_path, "-i", media_file_path, "-c:a", "aac", "-b:a", "320k", m4a_output_path], check=True)
    print(f"Audio extracted to M4A: {m4a_output_path}")

# Usage
if __name__ == "__main__":
    if len(sys.argv) < 4:
        print("Usage: python download_and_extract_audio.py <media_file_url> <output_path> <venv_activate_path>")
        sys.exit(1)
    
    url = sys.argv[1]
    output_path = sys.argv[2]
    venv_path = sys.argv[3]

    # Activate the virtual environment
    venv_name = os.path.basename(venv_path)
    activate_venv(venv_path, venv_name)
    
    # Download and extract media file
    download_media_file(url, output_path, venv_path)