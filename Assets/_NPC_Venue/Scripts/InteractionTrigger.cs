using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Univrse.Demo.NPC
{
    public class InteractionTrigger : MonoBehaviour
    {
        bool isRunning = false;
        float timeout = 0;

        [SerializeField] private bool npcState;
        [SerializeField] private bool mirror;
        [SerializeField] private bool tinyStories;

        [Header("UI Elements")]
        [SerializeField] private Button wakeNPC_BTN;
        [SerializeField] private Button sendMsg_BTN;

        private void Start()
        {
            wakeNPC_BTN.onClick.AddListener(StartExperience);

            GameManager.Instance.recorder.OnAudioSaved += AnalyzeVoiceMSG;
            GameManager.Instance.speech2Text.OnTranscriptFinished += AnalyzeTranscript;
            GameManager.Instance.text2Speech.OnClipCreated += Mirror;
            GameManager.Instance.tinyStories.OnStoryFinished += PlayStory;

            isRunning = false;
        }

        private void PlayStory(string story)
        {
            string filtered = Regex.Replace(story, "[^0-9A-Za-z _.!-]", "");
            Debug.Log(filtered);
            GameManager.Instance.text2Speech.GenerateAudioClip(filtered);
        }

        private void Mirror(string audioClipPath)
        {
            GameManager.Instance.npc.Talk(audioClipPath);
        }

        private void AnalyzeTranscript(string voiceTranscript)
        {
            // if(npcState) GameManager.Instance.npc.OnOrderGiven(voiceTranscript);
            // if(mirror) GameManager.Instance.text2Speech.GenerateAudioClip(voiceTranscript);

            if(tinyStories) GameManager.Instance.tinyStories.GenerateStory(voiceTranscript);
        }

        private void AnalyzeVoiceMSG(string audiofile_path)
        {
            GameManager.Instance.speech2Text.AudioClip2String(audiofile_path);
        }

        private void OnDisable()
        {
            wakeNPC_BTN.onClick.RemoveListener(StartExperience);
            GameManager.Instance.recorder.OnAudioSaved -= AnalyzeVoiceMSG;
            GameManager.Instance.speech2Text.OnTranscriptFinished -= AnalyzeTranscript;
            GameManager.Instance.text2Speech.OnClipCreated -= Mirror;
            GameManager.Instance.tinyStories.OnStoryFinished -= PlayStory;
        }

        private void StartExperience()
        {
            if (isRunning) return;

            isRunning = true;
            timeout = 0;

            // Run initial NPC animation

            // Wait for recording -> check for recording interaction to be holded and stop recording when released
            GameManager.Instance.recorder.RecordAudio();

            sendMsg_BTN.onClick.AddListener(SendVoiceMessage);

        }

        private void CancelExperience()
        {
            isRunning = false;
        }

        private void Update()
        {
            if (!isRunning) return;

            timeout += Time.deltaTime;

            if (timeout >= GameManager.Instance.recorder.recordTime) SendVoiceMessage();

        }

        private void SendVoiceMessage()
        {
            isRunning = false;
            sendMsg_BTN.onClick.RemoveListener(SendVoiceMessage);

            GameManager.Instance.recorder.SaveWavFile();
        }
    }
}