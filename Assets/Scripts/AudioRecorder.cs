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
        public int defaultRecordTime = 25;
        private const int sampleRate = 16000; // sample rate for recording speech

        public bool isRecording = false;

        public Action<string> OnAudioSaved;
        string mic;
        public void RecordAudio()
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("No microphone found to record audio clip sample with.");
                return;
            }
            mic = Microphone.devices[0];
            audioClip = Microphone.Start(mic, false, recordTime, sampleRate);

            isRecording = true;
            Debug.Log("Recording...");
        }

        public void StopRecording()
        {
            isRecording = false;
            if (Microphone.IsRecording(mic)) Microphone.End(mic);
        }

        public async void SaveWavFile()
        {
            if (!isRecording) return;

            isRecording = false;
            if (Microphone.IsRecording(mic))
            {
                Microphone.End(mic);
                audioClip = CutAudio(audioClip, recordTime);
            }

            string filepath = Application.streamingAssetsPath + "/recordings/recorded.wav";
            Debug.Log("Starting Save Wav");
            byte[] bytes = await WavUtility.FromAudioClip(audioClip, filepath, true);

            if (Microphone.IsRecording(mic)) Microphone.End(mic);
            Debug.Log("Audioclip Saved: " + filepath);
            OnAudioSaved?.Invoke(filepath);

        }

        AudioClip CutAudio(AudioClip originalClip, float desiredLength)
        {
            float originalLength = originalClip.length;
            int samplesToCopy = Mathf.FloorToInt(desiredLength * originalClip.frequency);

            float[] data = new float[samplesToCopy * originalClip.channels];
            originalClip.GetData(data, 0);

            AudioClip cutAudioClip = AudioClip.Create("CutAudioClip", samplesToCopy, originalClip.channels, originalClip.frequency, false);
            cutAudioClip.SetData(data, 0);

            return cutAudioClip;
        }
    }
}