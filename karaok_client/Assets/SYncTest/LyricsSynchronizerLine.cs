using UnityEngine;
using UnityEngine.UI;

namespace SYncTest
{
    public class LyricsSynchronizerLine : MonoBehaviour
    {
        [SerializeField] private RTLTMPro.RTLTextMeshPro _lyricsLine;
        [SerializeField] private RTLTMPro.RTLTextMeshPro _timeStamp;
        [SerializeField] private Image _currentLineImage;

        public void SetLyrics(string lyrics)
        {
            _lyricsLine.text = lyrics;
        }

        public void SetTimeStamp(string timeStamp)
        {
            _timeStamp.text = timeStamp;
        }

        public void SetCurrentLineImage(bool isCurrentLineImage)
        {
            _currentLineImage.gameObject.SetActive(isCurrentLineImage);
        }
    }
}