using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Univrse.Demo.NPC
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        public static GameManager Instance { get { return _instance; } }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                _instance = this;
            }
        }

        public NPC npc;
        public AudioRecorder recorder;
        public AsyncSpeech2Text speech2Text;
        public AsyncTinyStories tinyStories;
        public SentenceSimilarity sentenceSimilarity;
        public MetaTTS metaTTS;
    }
}