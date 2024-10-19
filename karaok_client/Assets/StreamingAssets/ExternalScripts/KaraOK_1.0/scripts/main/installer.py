import os
import subprocess
import sys
PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
sys.path.append(PROJECT_ROOT)
from scripts.main.environment import activate_venv
from scripts.main.external_installs import install_ffmpeg
from scripts.main.config_manager import get_from_config, set_to_config
import shutil

def install_package_manager():
    """
    Install the appropriate package manager based on the operating system.
    """
    if sys.platform == "darwin":  # macOS
        print("Installing Homebrew on macOS non-interactively...")
        try:
            subprocess.run(
                ["/bin/bash", "-c", "NONINTERACTIVE=1 $(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"],
                check=True
            )
        except subprocess.CalledProcessError as e:
            print(f"Error installing Homebrew: {e}")
            sys.exit(1)
    elif sys.platform.startswith("linux"):
        print("Updating package manager on Linux...")
        subprocess.run(["sudo", "apt-get", "update"], check=True)
    elif sys.platform == "win32":
        print("Installing Chocolatey on Windows...")
        subprocess.run(
            ["powershell", "Set-ExecutionPolicy", "Bypass", "-Scope", "Process", "-Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))"],
            check=True
        )
    else:
        print(f"Unsupported OS: {sys.platform}")
        sys.exit(1)

def install_git():
    """
    Install Git using the appropriate package manager.
    """
    if sys.platform == "darwin":  # macOS
        print("Installing Git on macOS...")
        subprocess.run(["brew", "install", "git"], check=True)
    elif sys.platform.startswith("linux"):
        print("Installing Git on Linux...")
        subprocess.run(["sudo", "apt-get", "install", "-y", "git"], check=True)
    elif sys.platform == "win32":
        print("Installing Git on Windows...")
        subprocess.run(["choco", "install", "git"], check=True)
    else:
        print(f"Unsupported OS: {sys.platform}")
        sys.exit(1)

def run_gentle_installer(venv_path):
    """
    Run the gentle_installer.py script located in the same folder, passing venv_path as the argument.
    """
    current_dir = os.path.dirname(os.path.abspath(__file__))
    gentle_installer_path = os.path.join(current_dir, "gentle_installer.py")

    print("Running Gentle installer...")
    try:
        subprocess.run(["python3", gentle_installer_path, venv_path], check=True)  # Pass venv_path as argument
        print("Gentle installation complete.")
    except subprocess.CalledProcessError as e:
        print(f"Error running Gentle installer: {e}")
        sys.exit(1)

def setup_environment(venv_path, venv_name):
    """
    Set up the virtual environment and install necessary packages.
    """
    # Step 1: Create the virtual environment
    print(f"Creating or updating virtual environment '{venv_name}' at '{venv_path}'...")

    if os.path.exists(venv_path):
        print(f"Removing existing virtual environment at {venv_path}")
        subprocess.run(["rm", "-rf", venv_path])

    subprocess.run(["python3", "-m", "venv", venv_path])

    # Log the virtual environment creation path
    print(f"Virtual environment created at: {venv_path}")

    # Step 2: Activate virtual environment right after creation
    activate_venv(venv_path, venv_name)
    
    # Step 3: Install the package manager based on OS
    install_package_manager(venv_path)

    # Step 4: Install Git using the package manager
    install_git()

    # Step 5: Install Gentle
    run_gentle_installer(venv_path)

    # Step 6: Install required Python urllib3 packages
    required_packages = ["demucs", "torchaudio", "yt-dlp", "diffq", "soundfile"]
    
    subprocess.run([os.path.join(venv_path, "bin", "pip3"), "install", "--upgrade"] + required_packages)

    print(f"Environment setup complete at: {venv_path}")