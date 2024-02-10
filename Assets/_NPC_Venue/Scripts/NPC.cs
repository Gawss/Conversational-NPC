using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Univrse.Demo.NPC
{
    public class NPC : MonoBehaviour
    {
        /// <summary>
        /// The Action List
        /// </summary>
        [Serializable]
        public struct Actions
        {
            public string sentence;
            public string verb;
            public string noun;
        }

        /// <summary>
        /// Enum of the different possible states of our Robot
        /// </summary>
        private enum State
        {
            Bye,
            Idle,
            Jump, // Say hello
            Slide
        }

        [Header("List of actions")]
        public List<Actions> actionsList;

        private State state;

        [HideInInspector]
        public List<string> sentences; // List of sentences (actions)
        public string[] sentencesArray;

        [HideInInspector]
        public float maxScore;
        public int maxScoreIndex;


        private void Awake()
        {
            // Set the State to Idle
            state = State.Idle;

            // Take all the possible actions in actionsList
            foreach (Actions actions in actionsList)
            {
                sentences.Add(actions.sentence);
            }
            sentencesArray = sentences.ToArray();
        }


        /// <summary>
        /// Utility function: Given the results of HuggingFaceAPI, select the State with the highest score
        /// </summary>
        /// <param name="maxValue">Value of the option with the highest score</param>
        /// <param name="maxIndex">Index of the option with the highest score</param>
        public void Utility(float maxScore, int maxScoreIndex)
        {
            // First we check that the score is > of 0.2, otherwise we let our agent perplexed;
            // This way we can handle strange input text (for instance if we write "Go see the dog!" the agent will be puzzled).
            if (maxScore < 0.20f)
            {
                state = State.Bye;
            }
            else
            {
                // // Get the verb and noun (if there is one)
                // goalObject = GameObject.Find(actionsList[maxScoreIndex].noun);

                string verb = actionsList[maxScoreIndex].verb;

                // Set the Robot State == verb
                state = (State)Enum.Parse(typeof(State), verb, true);
            }
        }


        /// <summary>
        /// When the user finished to type the order
        /// </summary>
        /// <param name="prompt"></param>
        public void OnOrderGiven(string prompt)
        {
            Tuple<int, float> tuple_ = GameManager.Instance.sentenceSimilarity.RankSimilarityScores(prompt, sentencesArray);
            Utility(tuple_.Item2, tuple_.Item1);
        }

        AudioClip audioClip;
        [SerializeField] private AudioSource audioSource;

        public IEnumerator RequestAudiofile(string audioclipPath)
        {
            Debug.Log("Loading the audioclip...");
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(audioclipPath, AudioType.WAV);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning(request.error + "\n" + audioclipPath);
            }
            else
            {
                Debug.Log("audioclip...");
                audioClip = DownloadHandlerAudioClip.GetContent(request);
                audioClip.name = "audio_command";

                audioSource.clip = audioClip;
                audioSource.Play();
            }
        }
    }
}