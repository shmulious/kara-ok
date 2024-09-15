import os
import subprocess
import platform
import urllib.request
import zipfile
import tarfile

def install_ffmpeg(containing_folder_path):
    """
    Check if FFmpeg exists; if not, download and install it under the provided containing_folder_path.
    
    Returns the path to the installed FFmpeg.
    """
    ffmpeg_dir = os.path.join(containing_folder_path, 'ffmpeg')
    ffmpeg_path = os.path.join(ffmpeg_dir, 'ffmpeg')

    # Check if FFmpeg is already installed
    if os.path.exists(ffmpeg_path):
        print(f"[FFmpeg] FFmpeg is already installed at: {ffmpeg_path}")
        return ffmpeg_path

    print("[FFmpeg] FFmpeg not found, starting installation...")

    # Download FFmpeg based on the OS
    system = platform.system()
    
    if system == "Darwin":  # macOS
        ffmpeg_url = "https://evermeet.cx/ffmpeg/ffmpeg-7.0.2.zip"
        ffmpeg_zip_path = os.path.join(containing_folder_path, 'ffmpeg.zip')
        
        print(f"[FFmpeg] Downloading FFmpeg from {ffmpeg_url}...")
        urllib.request.urlretrieve(ffmpeg_url, ffmpeg_zip_path)
        print(f"[FFmpeg] Downloaded FFmpeg zip file to {ffmpeg_zip_path}")

        print("[FFmpeg] Extracting FFmpeg zip file...")
        with zipfile.ZipFile(ffmpeg_zip_path, 'r') as zip_ref:
            zip_ref.extractall(ffmpeg_dir)
        print(f"[FFmpeg] Extracted FFmpeg to {ffmpeg_dir}")

        # Clean up the downloaded zip file
        os.remove(ffmpeg_zip_path)
        print(f"[FFmpeg] Deleted zip file: {ffmpeg_zip_path}")

    elif system == "Linux":
        ffmpeg_url = "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-i686-static.tar.xz"
        ffmpeg_tar_path = os.path.join(containing_folder_path, 'ffmpeg.tar.xz')

        print(f"[FFmpeg] Downloading FFmpeg from {ffmpeg_url}...")
        urllib.request.urlretrieve(ffmpeg_url, ffmpeg_tar_path)
        print(f"[FFmpeg] Downloaded FFmpeg tar file to {ffmpeg_tar_path}")

        print("[FFmpeg] Extracting FFmpeg tar file...")
        with tarfile.open(ffmpeg_tar_path, 'r:xz') as tar_ref:
            tar_ref.extractall(ffmpeg_dir)
        print(f"[FFmpeg] Extracted FFmpeg to {ffmpeg_dir}")

        # Clean up the downloaded tar file
        os.remove(ffmpeg_tar_path)
        print(f"[FFmpeg] Deleted tar file: {ffmpeg_tar_path}")
        
        # Move the extracted ffmpeg binary
        extracted_ffmpeg_dir = os.path.join(ffmpeg_dir, os.listdir(ffmpeg_dir)[0])
        ffmpeg_binary = os.path.join(extracted_ffmpeg_dir, 'ffmpeg')
        os.rename(ffmpeg_binary, ffmpeg_path)
        print(f"[FFmpeg] Moved FFmpeg binary to {ffmpeg_path}")

    elif system == "Windows":
        ffmpeg_url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
        ffmpeg_zip_path = os.path.join(containing_folder_path, 'ffmpeg.zip')

        print(f"[FFmpeg] Downloading FFmpeg from {ffmpeg_url}...")
        urllib.request.urlretrieve(ffmpeg_url, ffmpeg_zip_path)
        print(f"[FFmpeg] Downloaded FFmpeg zip file to {ffmpeg_zip_path}")

        print("[FFmpeg] Extracting FFmpeg zip file...")
        with zipfile.ZipFile(ffmpeg_zip_path, 'r') as zip_ref:
            zip_ref.extractall(ffmpeg_dir)
        print(f"[FFmpeg] Extracted FFmpeg to {ffmpeg_dir}")

        # Clean up the downloaded zip file
        os.remove(ffmpeg_zip_path)
        print(f"[FFmpeg] Deleted zip file: {ffmpeg_zip_path}")
        
        # Move the ffmpeg binary
        extracted_ffmpeg_dir = os.path.join(ffmpeg_dir, os.listdir(ffmpeg_dir)[0], 'bin')
        ffmpeg_binary = os.path.join(extracted_ffmpeg_dir, 'ffmpeg.exe')
        os.rename(ffmpeg_binary, os.path.join(ffmpeg_dir, 'ffmpeg.exe'))
        ffmpeg_path = os.path.join(ffmpeg_dir, 'ffmpeg.exe')
        print(f"[FFmpeg] Moved FFmpeg binary to {ffmpeg_path}")

    else:
        print("[FFmpeg] Error: Unsupported operating system")
        raise OSError("[FFmpeg] Unsupported operating system")

    # Grant execute permissions for macOS and Linux
    if system in ["Darwin", "Linux"]:
        print(f"[FFmpeg] Granting execute permissions to FFmpeg binary at {ffmpeg_path}...")
        try:
            subprocess.run(["chmod", "+x", ffmpeg_path], check=True)
            print(f"[FFmpeg] Execute permissions granted for FFmpeg at {ffmpeg_path}")
        except subprocess.CalledProcessError as e:
            print(f"[FFmpeg] Error granting permissions: {e}")
            raise

    # Verify FFmpeg installation
    try:
        print(f"[FFmpeg] Verifying FFmpeg installation at {ffmpeg_path}...")
        subprocess.run([ffmpeg_path, '-version'], check=True)
        print(f"[FFmpeg] FFmpeg successfully installed at: {ffmpeg_path}")
    except subprocess.CalledProcessError as e:
        print(f"[FFmpeg] FFmpeg installation verification failed: {e}")
        raise

    return ffmpeg_path