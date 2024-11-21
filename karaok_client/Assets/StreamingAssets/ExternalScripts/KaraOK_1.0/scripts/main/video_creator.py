import moviepy.editor as mpe
from moviepy.video.VideoClip import TextClip, ColorClip
from pysrt import open_srt as open_srt
import argparse

def create_subtitle_video(audio_file, srt_file, output_file, resolution=(900, 1600)):
    # Load and parse subtitles
    subtitles = open_srt(srt_file, encoding='utf-8')

    # Function to create a scrolling subtitle for each line
    def create_text_clip(sub, video_duration):
        start_time = sub.start.ordinal / 1000  # Convert to seconds
        end_time = sub.end.ordinal / 1000  # Convert to seconds
        duration = end_time - start_time
        text = sub.text.replace("\n", " ")

        # Create a TextClip
        text_clip = TextClip(
            text, fontsize=40, color='white', bg_color='black', size=(resolution[0], None)
        )

        # Set the position to scroll from bottom to top
        text_clip = text_clip.set_position(("center", lambda t: resolution[1] - (t * resolution[1] / duration)))
        text_clip = text_clip.set_start(start_time).set_end(end_time)
        return text_clip

    # Create background
    background = ColorClip(size=resolution, color=(0, 0, 0), duration=None)

    # Combine all text clips
    video_clips = [create_text_clip(sub, background.duration) for sub in subtitles]
    final_subtitle = mpe.CompositeVideoClip([background] + video_clips, size=resolution)

    # Load audio
    audio = mpe.AudioFileClip(audio_file)

    # Set audio to the video
    final_video = final_subtitle.set_audio(audio)

    # Set duration to match the audio file
    final_video = final_video.set_duration(audio.duration)

    # Export final video
    final_video.write_videofile(output_file, fps=24, codec="libx264", audio_codec="aac")
    print(f"Video saved to {output_file}")

if __name__ == "__main__":
    # Parse command-line arguments
    parser = argparse.ArgumentParser(description="Generate a video with scrolling subtitles.")
    parser.add_argument("audio_file", help="Path to the audio file")
    parser.add_argument("srt_file", help="Path to the subtitle (.srt) file")
    parser.add_argument("output_file", help="Path for the output video file")
    parser.add_argument(
        "--resolution", type=str, default="900x1600",
        help="Resolution of the output video in WIDTHxHEIGHT format (default: 900x1600)"
    )

    args = parser.parse_args()

    # Parse resolution argument
    try:
        width, height = map(int, args.resolution.split('x'))
    except ValueError:
        print("Invalid resolution format. Use WIDTHxHEIGHT (e.g., 900x1600).")
        exit(1)

    # Call the function
    create_subtitle_video(args.audio_file, args.srt_file, args.output_file, resolution=(width, height))