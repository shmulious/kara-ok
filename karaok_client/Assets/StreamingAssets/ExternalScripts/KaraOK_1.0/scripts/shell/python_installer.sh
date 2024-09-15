#!/bin/bash

# Function to check if Python 3 is installed
check_python3_installed() {
    if command -v python3 &> /dev/null; then
        echo "Python 3 is already installed."
        return 0
    else
        return 1
    fi
}

# Function to install Homebrew on macOS
install_brew() {
    echo "Installing Homebrew..."
    /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)" || { echo "Failed to install Homebrew. Exiting with code 2."; exit 2; }
    echo "Homebrew installed successfully."
}

# Function to install Chocolatey on Windows
install_choco() {
    echo "Installing Chocolatey..."
    powershell -NoProfile -ExecutionPolicy Bypass -Command \
    "Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" || { echo "Failed to install Chocolatey. Exiting with code 3."; exit 3; }
    echo "Chocolatey installed successfully."
}

# Function to install Python 3 using the appropriate package manager
install_python3() {
    if [[ "$OSTYPE" == "darwin"* ]]; then
        echo "Installing Python 3 on macOS..."
        brew install python || { echo "Failed to install Python 3 using Homebrew. Exiting with code 4."; exit 4; }
        echo "Python 3 installed successfully."
    elif [[ "$OS" == "Windows_NT" ]]; then
        echo "Installing Python 3 on Windows..."
        choco install python3 -y || { echo "Failed to install Python 3 using Chocolatey. Exiting with code 5."; exit 5; }
        echo "Python 3 installed successfully."
    else
        echo "Unsupported operating system. Exiting with code 6."
        exit 6
    fi
}

# Detecting the operating system
if [[ "$OSTYPE" == "darwin"* ]]; then
    echo "Detected macOS."
    
    # Check if brew is installed
    if command -v brew &> /dev/null; then
        echo "Homebrew is already installed."
    else
        install_brew
    fi

elif [[ "$OS" == "Windows_NT" ]]; then
    echo "Detected Windows."

    # Check if choco is installed
    if command -v choco &> /dev/null; then
        echo "Chocolatey is already installed."
    else
        install_choco
    fi

else
    echo "Unsupported OS detected. Exiting with code 1."
    exit 1
fi

# Check and install Python 3
if check_python3_installed; then
    echo "Python 3 is already installed. Exiting with code 0."
    exit 0
else
    install_python3
fi

# Exit successfully
echo "Script executed successfully."
exit 0