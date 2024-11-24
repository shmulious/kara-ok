﻿import os
import json


# Always set the path to the config file inside the user's root directory
CONFIG_FILE_PATH = os.path.join(os.path.expanduser("~"), "karaok_config", "smule_config.json")

# Ensure the directory exists
os.makedirs(os.path.dirname(CONFIG_FILE_PATH), exist_ok=True)

def load_config():
    """
    Load the configuration from the default configuration file.
    If the config file doesn't exist, return an empty dictionary.
    """
    print(f"Loading config from: {CONFIG_FILE_PATH}")
    if os.path.exists(CONFIG_FILE_PATH):
        with open(CONFIG_FILE_PATH, 'r') as config_file:
            return json.load(config_file)
    return {}

def save_config(config_data):
    """
    Save the configuration data to the default configuration file.
    """
    print(f"Saving config to: {CONFIG_FILE_PATH}")
    with open(CONFIG_FILE_PATH, 'w') as config_file:
        json.dump(config_data, config_file, indent=4)

def set_to_config(key, value):
    """
    Set a key-value pair in the config file and update it.
    
    :param key: The key to set in the configuration.
    :param value: The value to associate with the key.
    """
    # Load the existing config
    config_data = load_config()

    # Set the new key-value pair
    config_data[key] = value

    # Save the updated config back to the file
    save_config(config_data)

    print(f"Updated config: Set {key} to {value}")

def sanitize_value(value):
    """
    Sanitize the value by wrapping it with double quotes if it contains spaces.
    """
    # Remove any existing quotes
    value = value.strip('\'"')

    # If the value contains spaces, wrap it in double quotes
    if " " in value:
        return f'"{value}"'
    return value

def get_from_config(key, default_value=None):
    """
    Get the value associated with the key from the config file.
    
    :param key: The key to look up in the configuration.
    :param is_sanitized: If True, the returned value will be sanitized (default: True).
    :param default_value: The value to return if the key is not found (default: None).
    :return: The value associated with the key or the default value if not found.
    """
    # Load the existing config
    config_data = load_config()

    # Get the value associated with the key, or the default value if the key is not found
    value = config_data.get(key, default_value)

    return value