import subprocess
import requests
import sys

def run_command(command, error_message):
    """
    Run a shell command and print a custom error message if it fails.
    """
    try:
        subprocess.run(command, check=True)
    except subprocess.CalledProcessError as e:
        print(f"{error_message}: {e}")
        sys.exit(1)

def start_gentle_server():
    """
    Start the Gentle server locally.
    """
    print("Starting the Gentle server on http://localhost:8765...")
    try:
        subprocess.Popen(["python3", "serve.py"])
        print("Gentle server started. You can access the GUI and API at http://localhost:8765.")
    except subprocess.CalledProcessError as e:
        print(f"Error starting the Gentle server: {e}")
        sys.exit(1)

def align_audio_with_text(audio_path, transcript_path):
    """
    Use the Gentle API to align an audio file with its transcript.
    Returns the JSON response from the API.
    """
    url = "http://localhost:8765/transcriptions?async=false"
    try:
        print(f"Uploading {audio_path} and {transcript_path} for alignment...")
        files = {
            'audio': open(audio_path, 'rb'),
            'transcript': open(transcript_path, 'rb')
        }
        response = requests.post(url, files=files)
        response.raise_for_status()
        print("Alignment successful.")
        return response.json()
    except requests.RequestException as e:
        print(f"Error using the Gentle API: {e}")
        sys.exit(1)