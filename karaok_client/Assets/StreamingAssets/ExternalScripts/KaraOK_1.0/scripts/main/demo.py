# demo.py (Handles the demo mode)
import os
from scripts.main.converter import convert

def demo():
    """
    Run the process with hardcoded demo arguments.
    """
    youtube_url = "https://www.youtube.com/watch?v=mxXvV2ww9Xs&t=11s"
    output_folder = os.path.join(os.path.expanduser("~"), "Desktop", "smule", "output")
    
    print("Running demo with the following arguments:")
    print(f"URL: {youtube_url}")
    print(f"Output folder: {output_folder}")

    convert(youtube_url, output_folder)