#!/bin/bash

get_containing_folder() {
    VENV_PATH=$1
    # Get the parent of the parent directory (containing folder)
    CONTAINING_FOLDER=$(dirname "$(dirname "$VENV_PATH")")
    
    # Check if the directory exists, if not, create it
    if [ ! -d "$CONTAINING_FOLDER" ]; then
        echo "Creating directory: \"$CONTAINING_FOLDER\""
        mkdir -p "$CONTAINING_FOLDER"
    fi

    echo "$CONTAINING_FOLDER"
}
# Function to extract venv name from venv path
get_venv_name() {
    VENV_NAME=$(basename "$VENV_PATH")
    echo "Extracted virtual environment name: $VENV_NAME"
}

uninstall_python3_9() {
    if command -v python3.9 &> /dev/null; then
        echo "Uninstalling Python 3.9..."

        if [[ "$OSTYPE" == "darwin"* ]]; then
            # macOS uninstallation
            sudo rm -rf /usr/local/bin/python3.9 /usr/local/lib/python3.9 /usr/local/include/python3.9 /usr/local/share/man/man1/python3.9.1
            sudo rm -rf /Library/Frameworks/Python.framework/Versions/3.9
            sudo rm -rf ~/Library/Python/3.9
        elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
            # Linux uninstallation
            sudo rm -rf /usr/local/bin/python3.9 /usr/local/lib/python3.9
            sudo apt-get remove --purge python3.9 -y
        elif [[ "$OSTYPE" == "msys" ]]; then
            # Windows uninstallation (if using Chocolatey)
            choco uninstall python --version=3.9 -y
        else
            echo "Unsupported OS for Python uninstallation."
            exit 1
        fi
        
        echo "Python 3.9 uninstalled."
    else
        echo "Python 3.9 is not installed."
    fi
}

# Function to uninstall Homebrew (macOS only)
uninstall_homebrew() {
    if command -v brew &> /dev/null; then
        echo "Uninstalling Homebrew..."
        NONINTERACTIVE=1 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/uninstall.sh)"
        echo "Homebrew uninstalled."
    else
        echo "Homebrew is not installed."
    fi
}

# Function to uninstall Git
uninstall_git() {
    echo "To implement later"
}

# Uninstall existing Python, Homebrew, and Git if a fresh install is requested
uninstall_if_fresh() {
    if [ "$IS_FRESH_INSTALL" -eq 1 ]; then
        echo "Performing fresh installation, uninstalling existing components..."
        uninstall_python3_9
        uninstall_git
        uninstall_homebrew
    fi
}

# Function to install Python 3.9.20 from source
install_python3_9() {
    if [ "$IS_FRESH_INSTALL" -eq 1 ] || ! command -v python3.9 &> /dev/null; then
        get_containing_folder "$VENV_PATH"
        cd "$CONTAINING_FOLDER"
        echo "Python 3.9 not found. Installing Python 3.9.20 from source..."
        wget https://www.python.org/ftp/python/3.9.20/Python-3.9.20.tgz
        tar -xzf Python-3.9.20.tgz
        PYTHON_SOURCE="$CONTAINING_FOLDER/Python-3.9.20"
        cd "$PYTHON_SOURCE"
        ./configure --enable-optimizations
        make
        sudo make altinstall
        cd ..
        rm -rf Python-3.9.20.tgz
    else
        echo "Python 3.9 is already installed."
    fi
}

# Function to install OpenSSL and configure Python with SSL
install_openssl() {
    get_containing_folder "$VENV_PATH"
    PYTHON_SOURCE="$CONTAINING_FOLDER/Python-3.9.20"

    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "Installing OpenSSL on macOS..."
        brew install openssl
        OPENSSL_DIR=$(brew --prefix openssl)
        echo "Configuring Python with SSL support..."
        cd "$PYTHON_SOURCE"
        ./configure --with-openssl="$OPENSSL_DIR" --enable-optimizations
        make
        sudo make altinstall
        cd ..
    elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
        echo "Installing OpenSSL on Linux..."
        sudo apt-get install -y libssl-dev
        echo "Configuring Python with SSL support..."
        cd "$PYTHON_SOURCE"
        ./configure --with-openssl --enable-optimizations
        make
        sudo make altinstall
        cd ..
    else
        echo "OpenSSL configuration not supported on this OS."
        exit 1
    fi
}

# Function to install or validate package manager
install_package_manager() {
    if [ "$IS_FRESH_INSTALL" -eq 1 ] || ! command -v brew &> /dev/null && [[ "$OSTYPE" == "darwin"* ]]; then
        echo "Installing Homebrew on macOS non-interactively..."
        NONINTERACTIVE=1 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    elif [ "$IS_FRESH_INSTALL" -eq 1 ] || ! command -v apt-get &> /dev/null && [[ "$OSTYPE" == "linux-gnu"* ]]; then
        echo "Updating package manager on Linux..."
        sudo apt-get update
    elif [ "$IS_FRESH_INSTALL" -eq 1 ] || ! command -v choco &> /dev/null && [[ "$OSTYPE" == "msys" ]]; then
        echo "Installing Chocolatey on Windows..."
        powershell -Command 'Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString("https://chocolatey.org/install.ps1"))'
    else
        echo "Package manager is already installed."
    fi
}

# Function to install wget if it's not available
install_wget() {
    if ! command -v wget &> /dev/null; then
        echo "wget not found. Installing wget..."
        if [[ "$OSTYPE" == "darwin"* ]]; then
            brew install wget
        elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
            sudo apt-get install -y wget
        elif [[ "$OSTYPE" == "msys" ]]; then
            choco install wget
        else
            echo "Unsupported OS for wget installation."
            exit 1
        fi
    else
        echo "wget is already installed."
    fi
}

# Function to install or validate Git
install_git() {
    if [ "$IS_FRESH_INSTALL" -eq 1 ] || ! command -v git &> /dev/null; then
        if [[ "$OSTYPE" == "darwin"* ]]; then
            echo "Installing Git on macOS..."
            brew install git
        elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
            echo "Installing Git on Linux..."
            sudo apt-get install -y git
        elif [[ "$OSTYPE" == "msys" ]]; then
            echo "Installing Git on Windows..."
            choco install git
        fi
    else
        echo "Git is already installed."
    fi
}

# Function to install or validate ffmpeg
install_ffmpeg() {
    if [ "$IS_FRESH_INSTALL" -eq 1 ] || ! command -v ffmpeg &> /dev/null; then
        if [[ "$OSTYPE" == "darwin"* ]]; then
            echo "Installing ffmpeg on macOS..."
            brew install ffmpeg
        elif [[ "$OSTYPE" == "linux-gnu"* ]]; then
            echo "Installing ffmpeg on Linux..."
            sudo apt-get install -y ffmpeg
        elif [[ "$OSTYPE" == "msys" ]]; then
            echo "Installing ffmpeg on Windows..."
            choco install ffmpeg
        fi
    else
        echo "ffmpeg is already installed."
    fi
}

# Function to install Gentle
install_gentle() {
    gentle_path="$VENV_PATH/gentle"

    echo "Step 1: Cloning the Gentle repository into $gentle_path..."
    if [ "$IS_FRESH_INSTALL" -eq 1 ] || [ -d "$gentle_path" ]; then
        echo "Removing existing Gentle installation at $gentle_path..."
        rm -rf "$gentle_path"
        echo "Removed $gentle_path. Cloning a fresh repository."
    fi

    git clone https://github.com/lowerquality/gentle.git "$gentle_path" || { echo "Error cloning Gentle repository"; exit 1; }

    cd "$gentle_path" || { echo "Failed to navigate to $gentle_path"; exit 1; }

    echo "Step 2: Running the install.sh script to set up dependencies..."
    zsh install.sh || { echo "Error running install.sh"; exit 1; }

    echo "Step 3: Installing necessary models with install_models.sh..."
    zsh install_models.sh || { echo "Error running install_models.sh"; exit 1; }

    echo "Gentle installation complete. Installed at $gentle_path"
}

# Function to write the smule_config.json file
write_config_file() {
    containing_folder_path=$(get_containing_folder "$VENV_PATH")
    config_file_path="$containing_folder_path/smule_config.json"
    
    cat <<EOF > "$config_file_path"
{
    "venv_path": "$VENV_PATH",
    "python3.9_path": "$(command -v python3.9)",
    "package_manager_path": "$(command -v brew || command -v apt-get || command -v choco)",
    "ffmpeg_path": "$(command -v ffmpeg)",
    "gentle_path": "$gentle_path",
    "containing_folder_path": "$containing_folder_path"
}
EOF

    echo "Configuration saved to $config_file_path"
}

# Function to install virtual environment and required packages
setup_environment() {
    VENV_PATH=$1
    IS_FRESH_INSTALL=$2

    # Extract venv name from path
    get_venv_name "$VENV_PATH"

    uninstall_if_fresh

    # Step 1: Install or validate package manager
    install_package_manager

    # Step 1.1: Install wget
    install_wget

    # Step 2: Install Python 3.9.20
    install_python3_9

    echo "Creating virtual environment $VENV_NAME at $VENV_PATH using Python 3.9..."

    if [ "$IS_FRESH_INSTALL" -eq 1 ] || [ -d "$VENV_PATH" ]; then
        echo "Removing existing virtual environment..."
        rm -rf "$VENV_PATH"
    fi

    # Step 4: Install OpenSSL and configure Python with SSL support
    install_openssl

    pip3 install --upgrade  wheel Cython
    # Install global Python packages before venv creation
    brew install python-setuptools

    export PATH=$PATH:~/.local/bin

    # Step 5: Install or validate Git
    install_git

    # Step 6: Install or validate ffmpeg
    install_ffmpeg

    # Step 8: Install Gentle
    install_gentle

    # Create the virtual environment with Python 3.9
    python3.9 -m venv "$VENV_PATH"
    echo "Virtual environment created at: $VENV_PATH"

    # Step3: Activate virtual environment
    source "$VENV_PATH/bin/activate"

    # Step 7: Install required Python packages inside the virtual environment
    "$VENV_PATH/bin/pip3" install --upgrade demucs torchaudio yt-dlp diffq soundfile

    # Step 9: Write smule_config.json
    write_config_file

    echo "Environment setup complete!"
}

# Main logic
if [ "$#" -ne 2 ]; then
    echo "Usage: ./setup_env.sh <path_to_virtual_environment> <is_fresh_install(0 or 1)>"
    exit 1
fi

setup_environment "$1" "$2"