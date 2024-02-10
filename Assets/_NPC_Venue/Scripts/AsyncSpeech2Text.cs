using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;

namespace Univrse.Demo.NPC
{
    public class AsyncSpeech2Text : MonoBehaviour
    {
        #region Sentis Variables
        IWorker decoderEngine, encoderEngine, spectroEngine;

        [SerializeField] private BackendType backend = BackendType.GPUCompute;

        // Link your audioclip here. Format must be 16Hz mono non-compressed.
        public AudioClip audioClip;

        // This is how many tokens you want. It can be adjusted.
        const int maxTokens = 100;

        //Special tokens
        const int END_OF_TEXT = 50257;
        const int START_OF_TRANSCRIPT = 50258;
        const int ENGLISH = 50259;
        const int TRANSCRIBE = 50359;
        const int START_TIME = 50364;

        Ops ops;
        ITensorAllocator allocator;

        int numSamples;
        float[] data;
        string[] tokens;

        int currentToken = 0;
        int[] outputTokens = new int[maxTokens];

        // Used for special character decoding
        int[] whiteSpaceCharacters = new int[256];

        TensorFloat encodedAudio;

        bool transcribe = false;
        string outputString = "";

        // Maximum size of audioClip (30s at 16kHz)
        const int maxSamples = 30 * 16000;

        public Action<string> OnTranscriptFinished;


        TensorInt tokensPredictions;
        #endregion
        private Model decoder;
        private Model encoder;
        private Model spectro;

        [SerializeField] private ModelAsset decoderAsset;
        [SerializeField] private ModelAsset encoderAsset;
        [SerializeField] private ModelAsset spectroAsset;

        private void Start()
        {
            decoder = ModelLoader.Load(decoderAsset);
            encoder = ModelLoader.Load(encoderAsset);
            spectro = ModelLoader.Load(spectroAsset);

            InitializeModel();
        }

        private async void InitializeModel()
        {
            allocator = new TensorCachingAllocator();
            ops = WorkerFactory.CreateOps(backend, allocator);

            SetupWhiteSpaceShifts();

            await GetTokens();

            decoderEngine = WorkerFactory.CreateWorker(backend, decoder);
            encoderEngine = WorkerFactory.CreateWorker(backend, encoder);
            spectroEngine = WorkerFactory.CreateWorker(backend, spectro);

            outputTokens[0] = START_OF_TRANSCRIPT;
            outputTokens[1] = ENGLISH;
            outputTokens[2] = TRANSCRIBE;
            outputTokens[3] = START_TIME;
            currentToken = 3;
        }

        // Update is called once per frame
        private void Update()
        {
            if (transcribe && currentToken < outputTokens.Length - 1)
            {
                using var tokensSoFar = new TensorInt(new TensorShape(1, outputTokens.Length), outputTokens);

                var inputs = new Dictionary<string, Tensor>
            {
                {"encoded_audio", encodedAudio},
                {"tokens" , tokensSoFar}
            };

                decoderEngine.Execute(inputs);
                var tokensOut = decoderEngine.PeekOutput() as TensorFloat;

                tokensPredictions = ops.ArgMax(tokensOut, 2, false);
                // tokensPredictions.MakeReadable();
                transcribe = false;
                tokensPredictions.AsyncReadbackRequest(ReadbackCallback);
            }
        }

        private void ReadbackCallback(bool completed)
        {
            Debug.Log("S2T: Readback callback");
            // The call to `MakeReadable` will no longer block with a readback as the data is already on the CPU
            tokensPredictions.MakeReadable();
            // The output tensor is now in a readable state on the CPU

            int ID = tokensPredictions[currentToken];

            outputTokens[++currentToken] = ID;

            if (ID == END_OF_TEXT)
            {
                transcribe = false;
                Debug.Log($"Transcript Done -> (time={(ID - START_TIME) * 0.02f})");
                return;
            }
            else if (ID >= tokens.Length)
            {
                Debug.Log("Whisper finished: " + outputString);
                OnTranscriptFinished?.Invoke(outputString);

                outputString += $"(time={(ID - START_TIME) * 0.02f})";

            }
            else outputString += GetUnicodeText(tokens[ID]);

            transcribe = true;
        }

        public void RunModel(string audioclipPath)
        {
            currentToken = 3;
            StartCoroutine(RequestAudiofile(audioclipPath));
        }

        private IEnumerator RequestAudiofile(string audioclipPath)
        {
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(audioclipPath, AudioType.WAV);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogWarning(request.error + "\n" + audioclipPath);
            }
            else
            {
                audioClip = DownloadHandlerAudioClip.GetContent(request);
                audioClip.name = "audio_command";

                outputString = "";

                LoadAudio();
                EncodeAudio();
                transcribe = true;
            }
        }


        private void LoadAudio()
        {
            if (audioClip.frequency != 16000)
            {
                Debug.Log($"The audio clip should have frequency 16kHz. It has frequency {audioClip.frequency / 1000f}kHz");
                return;
            }

            numSamples = audioClip.samples;

            if (numSamples > maxSamples)
            {
                Debug.Log($"The AudioClip is too long. It must be less than 30 seconds. This clip is {numSamples / audioClip.frequency} seconds.");
                return;
            }

            data = new float[numSamples];
            audioClip.GetData(data, 0);
        }

        private void EncodeAudio()
        {
            using var input = new TensorFloat(new TensorShape(1, numSamples), data);

            // Pad out to 30 seconds at 16khz if necessary
            using var input30seconds = ops.Pad(input, new int[] { 0, 0, 0, maxSamples - numSamples });

            spectroEngine.Execute(input30seconds);
            var spectroOutput = spectroEngine.PeekOutput() as TensorFloat;

            encoderEngine.Execute(spectroOutput);
            encodedAudio = encoderEngine.PeekOutput() as TensorFloat;

        }

        private async Task GetTokens()
        {
            var jsonText = await File.ReadAllTextAsync(Application.streamingAssetsPath + "/Whisper/vocab.json");
            var vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonText);
            tokens = new string[vocab.Count];
            foreach (var item in vocab)
            {
                tokens[item.Value] = item.Key;
            }
        }

        private void OnDestroy()
        {
            decoderEngine?.Dispose();
            encoderEngine?.Dispose();
            spectroEngine?.Dispose();
            ops?.Dispose();
            allocator?.Dispose();
        }

        #region Utilities
        private void SetupWhiteSpaceShifts()
        {
            for (int i = 0, n = 0; i < 256; i++)
            {
                if (IsWhiteSpace((char)i)) whiteSpaceCharacters[n++] = i;
            }
        }

        private bool IsWhiteSpace(char c)
        {
            return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
        }

        // Translates encoded special characters to Unicode
        private string GetUnicodeText(string text)
        {
            var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
            return Encoding.UTF8.GetString(bytes);
        }

        private string ShiftCharacterDown(string text)
        {
            string outText = "";
            foreach (char letter in text)
            {
                outText += ((int)letter <= 256) ? letter :
                    (char)whiteSpaceCharacters[(int)(letter - 256)];
            }
            return outText;
        }

        #endregion


    }
}