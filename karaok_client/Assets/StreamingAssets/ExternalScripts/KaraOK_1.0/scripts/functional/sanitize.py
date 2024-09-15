# sanitize.py
import re

def sanitize_output_folder(folder_path):
    """Sanitize folder path by removing illegal characters and trailing slashes."""
    sanitized_path = re.sub(r'[^\w\-./]', '', folder_path.rstrip('/\\'))
    return sanitized_path