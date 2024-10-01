import os
import shutil

def move(origin_path, destination_path):
    """
    Move a file or directory to the destination folder.
    
    :param origin_path: Path to the file or directory to move.
    :param destination_path: Path to the folder where the file or directory should be moved.
    """
    # Create the destination folder if it doesn't exist
    os.makedirs(destination_path, exist_ok=True)

    # Move the file or directory to the destination folder
    shutil.move(origin_path, destination_path)
    print(f"Moved {origin_path} to {destination_path}")
def delete(path):
    """
    Delete a file or folder.
    
    :param path: Path to the file or directory to delete.
    """
    if os.path.isdir(path):
        # Delete a directory and all its contents
        shutil.rmtree(path)
        print(f"Deleted directory: {path}")
    elif os.path.isfile(path):
        # Delete a single file
        os.remove(path)
        print(f"Deleted file: {path}")
    else:
        print(f"Path does not exist: {path}")