import os
import subprocess
import shutil
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

def install_gentle(venv_path):
    gentle_path = os.path.join(venv_path, "gentle")

    # Modify the PATH to include the Homebrew path inside the virtual environment
    brew_bin_path = os.path.join(venv_path, "bin")
    os.environ["PATH"] = f"{brew_bin_path}:{os.environ['PATH']}"

    print(f"Step 1: Cloning the Gentle repository into {gentle_path}...")
    if os.path.exists(gentle_path):
        print(f"Removing existing Gentle installation at {gentle_path}...")
        shutil.rmtree(gentle_path)
        print(f"Removed {gentle_path}. Cloning a fresh repository.")

    run_command(["git", "clone", "https://github.com/lowerquality/gentle.git", gentle_path],
                "Error cloning the Gentle repository")

    os.chdir(gentle_path)

    # Make the entire path executable
    run_command(["chmod", "-R", "+x", gentle_path], f"Error making {gentle_path} executable")

    print("Step 2: Running the install.sh script to set up dependencies...")
    run_command(["zsh", "install.sh"], "Error running install.sh")

    print("Step 3: Installing necessary models with install_models.sh...")
    run_command(["zsh", "install_models.sh"], "Error running install_models.sh")

    print(f"Gentle installation complete. Installed at {gentle_path}")

if __name__ == "__main__":
    if len(sys.argv) != 2:
        print("Usage: python3 gentle_installer.py <path_to_virtual_environment>")
        sys.exit(1)

    install_path = sys.argv[1]
    install_gentle(install_path)