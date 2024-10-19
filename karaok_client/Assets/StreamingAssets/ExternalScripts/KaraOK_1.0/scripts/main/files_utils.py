# files_utils.py

import os
import shutil
import json

def load_json_file(path):
    try:
        # Step 1: Check if the file exists
        if not os.path.exists(path):
            raise FileNotFoundError(f"File not found: {path}")

        # Step 2: Open the JSON file
        with open(path, 'r') as file:
            # Step 3: Load the JSON data into a Python dictionary
            data = json.load(file)
            #print(jsonify(json))
            return data

    except FileNotFoundError as fnf_error:
        print(f"Error: {fnf_error}")
    except json.JSONDecodeError as json_error:
        print(f"Error: Failed to decode JSON. {json_error}")
    except Exception as e:
        print(f"An unexpected error occurred: {e}")
    
    return None  # Return None if an error occurs

def move(source, destination):
    """
    Move files or directories from source to destination.
    If the destination exists, overwrite only duplicate files.
    Supports moving a single file or all files under a directory.
    """
    try:
        # If the source is a file, handle moving the single file
        if os.path.isfile(source):
            # Ensure the destination directory exists
            if not os.path.exists(destination):
                os.makedirs(os.path.dirname(destination))

            # If the destination is a directory, move the file inside it
            if os.path.isdir(destination):
                destination_file_path = os.path.join(destination, os.path.basename(source))
            else:
                destination_file_path = destination

            # If the file already exists, overwrite it
            if os.path.exists(destination_file_path):
                os.remove(destination_file_path)
                print(f"Overwriting file: {destination_file_path}")
            
            # Move the single file
            shutil.move(source, destination_file_path)
            print(f"Moved {source} to {destination_file_path}")
        
        # If the source is a directory, handle moving the entire directory contents
        elif os.path.isdir(source):
            # Ensure the destination directory exists
            if not os.path.exists(destination):
                os.makedirs(destination)

            # Iterate over the source directory and move/overwrite files
            for root, dirs, files in os.walk(source):
                # Determine the relative path of the source folder
                relative_path = os.path.relpath(root, source)
                destination_dir = os.path.join(destination, relative_path)
                
                # Ensure the destination directory exists
                if not os.path.exists(destination_dir):
                    os.makedirs(destination_dir)

                # Move or overwrite files
                for file in files:
                    source_file_path = os.path.join(root, file)
                    destination_file_path = os.path.join(destination_dir, file)

                    # If the file already exists, overwrite it
                    if os.path.exists(destination_file_path):
                        os.remove(destination_file_path)
                        print(f"Overwriting file: {destination_file_path}")

                    # Move the file from source to destination
                    shutil.move(source_file_path, destination_file_path)
                    print(f"Moved {source_file_path} to {destination_file_path}")

            # Optionally, remove the source folder after moving all files
            shutil.rmtree(source)
            print(f"Removed source folder: {source}")
        
        else:
            print(f"Source path {source} does not exist or is not a file/directory.")

    except Exception as e:
        print(f"Error moving files from {source} to {destination}: {e}")
        raise