using UnityEngine;
using UnityEngine.Audio;
using System.IO;
using System;

namespace Univrse.Demo.NPC
{
    public class AudioRecorder : MonoBehaviour
    {
        private AudioClip audioClip;
        public int recordTime = 2; // no. of seconds can be set in Unity Editor inspector
        private const int sampleRate = 16000; // sample rate for recording speech

        public bool isRecording = false;

        public Action<string> OnAudioSaved;
        public void RecordAudio()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("No microphone found to record audio clip sample with.");
                return;
            }
            string mic = Microphone.devices[0];
            audioClip = Microphone.Start(mic, false, recordTime, sampleRate);

            isRecording = true;
            Debug.Log("Recording...");
        }

        public void SaveWavFile()
        {
            isRecording = false;

            string filepath;
            byte[] bytes = WavUtility.FromAudioClip(audioClip, out filepath, true);

            Debug.Log("Saving audioclip: " + filepath);
            OnAudioSaved?.Invoke(filepath);

        }
    }
}