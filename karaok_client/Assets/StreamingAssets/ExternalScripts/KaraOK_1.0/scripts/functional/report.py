# report.py
from .logger import log_message

def print_report(youtube_url, download_success, separation_success, final_file_path, exit_code):
    """Print and log the final report of the process."""
    log_message("Process Report:")
    log_message(f"YouTube URL: {youtube_url}")
    log_message(f"Download Successful: {download_success}")
    log_message(f"Separation Successful: {separation_success}")
    log_message(f"Final File Path: {final_file_path}")
    log_message(f"Exit Code: {exit_code}")

    if exit_code != 0:
        log_message(f"Error: The process encountered an issue. Exit code {exit_code}.")