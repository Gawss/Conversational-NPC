using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System;

namespace Univrse.Demo.NPC
{
    public class AsyncTinyStories : MonoBehaviour
    {
        #region Sentis Variables
        const BackendType backend = BackendType.GPUCompute;

        //string outputString = "Once upon a time, there were three bears";
        string outputString = "One day an alien came down from Mars. It saw a chicken";

        // This is how many tokens you want. It can be adjusted.
        const int maxTokens = 100;

        //Make this smaller for more randomness
        const float predictability = 5f;

        //Special tokens
        const int END_OF_TEXT = 50256;

        Ops ops;
        ITensorAllocator allocator;

        //Store the vocabulary
        string[] tokens;

        IWorker engine;

        int currentToken = 0;
        int[] outputTokens = new int[maxTokens];

        // Used for special character decoding
        int[] whiteSpaceCharacters = new int[256];
        int[] encodedCharacters = new int[256];

        bool runInference = false;


        //stop after this many tokens
        const int stopAfter = 100;

        int totalTokens = 0;

        string[] merges;
        Dictionary<string, int> vocab;

        public Action<string> OnStoryFinished;
        TensorFloat probs;
        #endregion

        bool waitingReadback;
        private void Start()
        {

            allocator = new TensorCachingAllocator();
            ops = WorkerFactory.CreateOps(backend, allocator);
            SetupWhiteSpaceShifts();
            LoadVocabulary();
            Model model = ModelLoader.Load(Application.streamingAssetsPath + "/tinystories.sentis");
            engine = WorkerFactory.CreateWorker(backend, model);

        }

        private void Update()
        {
            if (runInference)
            {
                RunInference();
            }
        }

        public void GenerateStory(string prompt)
        {
            totalTokens = 0;
            currentToken = 0;
            outputString = prompt;
            waitingReadback = false;
            DecodePrompt(outputString);

            runInference = true;
        }

        private void RunInference()
        {
            Debug.Log("Running inference...");
            if (waitingReadback) return;

            using var tokensSoFar = new TensorInt(new TensorShape(1, maxTokens), outputTokens);
            engine.Execute(tokensSoFar);

            var tokensOut = engine.PeekOutput() as TensorFloat;

            using var row = ops.Slice(tokensOut, new[] { currentToken }, new[] { currentToken + 1 }, new[] { 1 }, new[] { 1 });
            using var rowB = ops.Mul(predictability, row as TensorFloat);
            probs = ops.Softmax(rowB, 2);

            probs.AsyncReadbackRequest(ReadbackCallback);
            waitingReadback = true;
        }

        private void ReadbackCallback(bool completed)
        {
            if (!completed) return;

            // The call to `MakeReadable` will no longer block with a readback as the data is already on the CPU
            probs.MakeReadable();
            // The output tensor is now in a readable state on the CPU

            int ID = SelectRandomToken(probs.ToReadOnlyArray());

            if (currentToken >= maxTokens - 1)
            {
                for (int i = 0; i < maxTokens - 1; i++) outputTokens[i] = outputTokens[i + 1];
                currentToken--;
            }

            outputTokens[++currentToken] = ID;
            totalTokens++;

            if (ID == END_OF_TEXT || totalTokens >= stopAfter)
            {
                runInference = false;
                Debug.Log("End of the story");
                Debug.Log(outputString);
                OnStoryFinished?.Invoke(outputString);
            }
            else outputString += GetUnicodeText(tokens[ID]);

            waitingReadback = false;

        }


        private int SelectRandomToken(float[] probs)
        {
            float p = UnityEngine.Random.Range(0, 1f);
            float t = 0;
            for (int i = 0; i < probs.Length; i++)
            {
                t += probs[i];
                if (p < t)
                {
                    return i;
                }
            }
            return probs.Length - 1;
        }

        private async void LoadVocabulary()
        {
            var jsonText = await File.ReadAllTextAsync(Application.streamingAssetsPath + "/vocab.json");
            vocab = JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonText);
            tokens = new string[vocab.Count];
            foreach (var item in vocab)
            {
                tokens[item.Value] = item.Key;
            }

            merges = await File.ReadAllLinesAsync(Application.streamingAssetsPath + "/merges.txt");
        }

        private void DecodePrompt(string text)
        {
            var inputTokens = GetTokens(text);

            for (int i = 0; i < inputTokens.Count; i++)
            {
                outputTokens[i] = inputTokens[i];
            }
            currentToken = inputTokens.Count - 1;
        }

        private List<int> GetTokens(string text)
        {
            text = GetASCIIText(text);

            // Start with a list of single characters
            var inputTokens = new List<string>();
            foreach (var letter in text)
            {
                inputTokens.Add(letter.ToString());
            }

            ApplyMerges(inputTokens);


            //Find the ids of the words in the vocab
            var ids = new List<int>();
            foreach (var token in inputTokens)
            {
                if (vocab.TryGetValue(token, out int id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private void ApplyMerges(List<string> inputTokens)
        {
            foreach (var merge in merges)
            {
                string[] pair = merge.Split(' ');
                int n = 0;
                while (n >= 0)
                {
                    n = inputTokens.IndexOf(pair[0], n);
                    if (n != -1 && n < inputTokens.Count - 1 && inputTokens[n + 1] == pair[1])
                    {
                        inputTokens[n] += inputTokens[n + 1];
                        inputTokens.RemoveAt(n + 1);
                    }
                    if (n != -1) n++;
                }
            }
        }

        private void OnDestroy()
        {
            engine?.Dispose();
            ops?.Dispose();
            allocator?.Dispose();
        }


        #region Utilities
        // Translates encoded special characters to Unicode
        string GetUnicodeText(string text)
        {
            var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
            return Encoding.UTF8.GetString(bytes);
        }
        string GetASCIIText(string newText)
        {
            var bytes = Encoding.UTF8.GetBytes(newText);
            return ShiftCharacterUp(Encoding.GetEncoding("ISO-8859-1").GetString(bytes));
        }

        string ShiftCharacterDown(string text)
        {
            string outText = "";
            foreach (char letter in text)
            {
                outText += ((int)letter <= 256) ? letter :
                    (char)whiteSpaceCharacters[(int)(letter - 256)];
            }
            return outText;
        }

        string ShiftCharacterUp(string text)
        {
            string outText = "";
            foreach (char letter in text)
            {
                outText += (char)encodedCharacters[(int)letter];
            }
            return outText;
        }

        void SetupWhiteSpaceShifts()
        {
            for (int i = 0, n = 0; i < 256; i++)
            {
                encodedCharacters[i] = i;
                if (IsWhiteSpace((char)i))
                {
                    encodedCharacters[i] = n + 256;
                    whiteSpaceCharacters[n++] = i;
                }
            }
        }

        bool IsWhiteSpace(char c)
        {
            return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
        }
        #endregion
    }
}