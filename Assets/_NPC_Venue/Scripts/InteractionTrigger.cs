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
        [SerializeField] private Image recordProgress_IMG;

        private void Start()
        {
            wakeNPC_BTN.onClick.AddListener(StartExperience);

            GameManager.Instance.recorder.OnAudioSaved += AnalyzeVoiceMSG;
            GameManager.Instance.speech2Text.OnTranscriptFinished += AnalyzeTranscript;
            GameManager.Instance.tinyStories.OnStoryFinished += PlayStory;

            GameManager.Instance.metaTTS.OnSpeakEnd += SetNPCAnimation;

            isRunning = false;
        }

        private void SetNPCAnimation()
        {
            GameManager.Instance.npc.Talk(false);
        }

        private void PlayStory(string story)
        {
            // string filtered = Regex.Replace(story, "[^0-9A-Za-z _.!-]", "");
            // Debug.Log(filtered);         
            story += " And that's all that I can tell.";
            GameManager.Instance.metaTTS.SpeakClick(story);
        }

        private void AnalyzeTranscript(string voiceTranscript)
        {
            if (npcState) GameManager.Instance.npc.OnOrderGiven(voiceTranscript);
            if (mirror) PlayStory(voiceTranscript);
            if (tinyStories) GameManager.Instance.tinyStories.GenerateStory(voiceTranscript);
        }

        private void AnalyzeVoiceMSG(string audiofile_path)
        {
            GameManager.Instance.npc.Talk(true);
            GameManager.Instance.speech2Text.RunModel(audiofile_path);
        }

        private void OnDisable()
        {
            wakeNPC_BTN.onClick.RemoveListener(StartExperience);
            GameManager.Instance.recorder.OnAudioSaved -= AnalyzeVoiceMSG;
            GameManager.Instance.speech2Text.OnTranscriptFinished -= AnalyzeTranscript;
            GameManager.Instance.tinyStories.OnStoryFinished -= PlayStory;
            GameManager.Instance.metaTTS.OnSpeakEnd -= SetNPCAnimation;
        }

        private void StartExperience()
        {
            if (isRunning) return;

            isRunning = true;
            timeout = 0;

            GameManager.Instance.recorder.RecordAudio();
            GameManager.Instance.metaTTS.PlayAsyncClip();

            sendMsg_BTN.onClick.AddListener(SendVoiceMessage);

        }

        float startRecordingTimer;
        float cancelExperienceTimer;

        private void CancelExperience()
        {
            isRunning = false;
            startRecordingTimer = 0;
            cancelExperienceTimer = 0;
            recordProgress_IMG.fillAmount = 0;

            if (GameManager.Instance.npc.audioSource.isPlaying) GameManager.Instance.npc.audioSource.Stop();
            if (GameManager.Instance.recorder.isRecording)
            {
                GameManager.Instance.metaTTS.PlayAsyncClip();
                GameManager.Instance.recorder.StopRecording();
            }
        }

        private void Update()
        {
            if (playerIsLooking)
            {
                if (GameManager.Instance.npc.isTalking) return;

                if (isIntro)
                {
                    GameManager.Instance.npc.audioSource.clip = GameManager.Instance.npc.intro_audioclip;
                    GameManager.Instance.npc.audioSource.Play();
                    GameManager.Instance.npc.Talk(true, true);
                    isIntro = false;
                }
                else if (isGreetings)
                {
                    GameManager.Instance.npc.audioSource.clip = GameManager.Instance.npc.hello_audioclips[Random.Range(0, GameManager.Instance.npc.hello_audioclips.Length)];
                    GameManager.Instance.npc.audioSource.Play();
                    GameManager.Instance.npc.Talk(true, true);
                    isGreetings = false;
                    startRecordingTimer = 0;
                }
                else
                {
                    if (!isRunning)
                    {
                        startRecordingTimer += Time.deltaTime;

                        if (startRecordingTimer > 5f) StartExperience();
                    }
                }

            }
            else
            {
                if (!isIntro) isGreetings = true;
                cancelExperienceTimer += Time.deltaTime;

                if (cancelExperienceTimer > 5f) CancelExperience();
            }

            if (!isRunning) return;

            timeout += Time.deltaTime;

            recordProgress_IMG.fillAmount = timeout / GameManager.Instance.recorder.defaultRecordTime;

            if (timeout >= GameManager.Instance.recorder.defaultRecordTime) SendVoiceMessage();

        }
        [SerializeField] private LayerMask layerMask;
        bool isIntro = true;
        bool isGreetings = false;
        bool playerIsLooking;

        private void FixedUpdate()
        {
            RaycastHit hit;
            // Does the ray intersect any objects excluding the player layer
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, layerMask))
            {
                Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.TransformDirection(Vector3.forward) * hit.distance, Color.yellow);

                playerIsLooking = true;
            }
            else
            {
                Debug.DrawRay(Camera.main.transform.position, Camera.main.transform.TransformDirection(Vector3.forward) * 1000, Color.white);
                playerIsLooking = false;
            }
        }


        private void SendVoiceMessage()
        {
            isRunning = false;
            sendMsg_BTN.onClick.RemoveListener(SendVoiceMessage);

            GameManager.Instance.recorder.recordTime = Mathf.CeilToInt(timeout);
            GameManager.Instance.recorder.SaveWavFile();
        }
    }
}