using System;
using System.Collections;
using System.Collections.Generic;
using Meta.WitAi.TTS.Utilities;
using UnityEngine;

namespace Univrse.Demo.NPC
{
    public class MetaTTS : MonoBehaviour
    {
        [SerializeField] private TTSSpeaker _speaker;
        [SerializeField] private AudioClip _asyncClip;
        [SerializeField] private string _dateId = "[DATE]";
        public string message;

        public Action OnSpeakEnd;

        // Speak phrase click
        public void SpeakClick(string _msg = null)
        {
            // Speak phrase
            string phrase = FormatText(_msg != null ? _msg : message);

            StartCoroutine(SpeakAsync(phrase));
        }

        public void PlayAsyncClip()
        {
            if (_asyncClip != null) _speaker.AudioSource.PlayOneShot(_asyncClip);
        }

        private IEnumerator SpeakAsync(string phrase)
        {

            yield return _speaker.SpeakAsync(phrase);

            // Play complete clip
            if (_asyncClip != null)
            {
                _speaker.AudioSource.PlayOneShot(_asyncClip);
            }

            OnSpeakEnd?.Invoke();
        }

        // Format text with current datetime
        private string FormatText(string text)
        {
            string result = text;
            if (result.Contains(_dateId))
            {
                DateTime now = DateTime.Now;
                string dateString = $"{now.ToLongDateString()} at {now.ToLongTimeString()}";
                result = text.Replace(_dateId, dateString);
            }
            return result;
        }
    }
}