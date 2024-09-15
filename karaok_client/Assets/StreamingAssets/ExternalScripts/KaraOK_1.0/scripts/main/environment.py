import os
import subprocess
import sys

# Global variable to store the virtual environment path
_venv_path = None

def check_environment(venv_path, venv_name):
    """
    Check if the virtual environment is properly set up by checking for the activate script.
    """
    print(f"Checking if virtual environment '{venv_name}' is installed at: {venv_path}")
    
    if sys.platform == "win32":
        activate_script = os.path.join(venv_path, "Scripts", "activate.bat")
    else:
        activate_script = os.path.join(venv_path, "bin", "activate")

    if os.path.exists(activate_script):
        print(f"Virtual environment '{venv_name}' is installed and valid at: {venv_path}")
        return True
    else:
        print(f"Virtual environment '{venv_name}' is NOT installed or incomplete at: {venv_path}")
        return False

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
            subprocess.call(f"source {activate_script}", shell=True, executable="/bin/bash")
        
        # Save the virtual environment path after successful activation
        _venv_path = venv_path
    else:
        print(f"Error: Activate script not found in virtual environment '{venv_name}' at: {venv_path}")
        sys.exit(112)

def get_venv_path():
    """
    Return the path to the activated virtual environment.
    """
    if _venv_path:
        return _venv_path
    else:
        print("Error: Virtual environment is not activated. Call activate_venv first.")
        sys.exit(325)

def ensure_virtual_env(venv_path):
    """
    Ensures that the script is running inside the virtual environment.
    If not, restart the script using the virtual environment's Python interpreter.
    """
    venv_python = os.path.join(venv_path, 'bin', 'python') if sys.platform != "win32" else os.path.join(venv_path, 'Scripts', 'python.exe')

    # Check if we're running in the virtual environment
    if sys.executable != venv_python:
        print(f"Switching to virtual environment's Python interpreter at: {venv_python}")
        # Restart the script using the virtual environment's Python interpreter
        subprocess.call([venv_python] + sys.argv)
        sys.exit()