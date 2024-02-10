using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;

namespace Univrse.Demo.NPC
{
    public class AsyncText2Speech : MonoBehaviour
    {
        [SerializeField] private ModelAsset modelAsset;
        [SerializeField] private BackendType backendType;
        [SerializeField] private string inputText = "Hello World! I wish I could speak.";
        private TokenizerRunner tokenizerRunner;
        public Action<string> OnClipCreated;

        public bool saveAudio = false;
        public AudioClip generatedAudioClip;
        Model model;
        IWorker engine;
        TensorFloat output;

        private void Start()
        {
            tokenizerRunner = new TokenizerRunner();
            model = ModelLoader.Load(modelAsset);

            RemoveLayersAfterLayer(model, "/generator/generator/output_conv/output_conv.2/Tanh_output_0");
        }

        private void OnDestroy()
        {
            engine?.Dispose();
        }

        public void GenerateAudioClip(string voiceMSG)
        {
            Debug.Log("Generating Audio Clip...");
            // Convert input text to tensor.
            var tokenizedOutput = tokenizerRunner.ExecuteTokenizer(voiceMSG);
            var tokenList = tokenizedOutput.Split(' ').ToList();
            for (int i = tokenList.Count - 1; i >= 0; i--)
            {
                if (tokenList[i] == "")
                {
                    tokenList.RemoveAt(i);
                };
            }
            int[] inputValues = tokenList.ToArray().Select(int.Parse).ToArray();
            var inputShape = new TensorShape(inputValues.Length);
            using var input = new TensorInt(inputShape, inputValues);

            // Setup engine of given worker type and model
            engine = WorkerFactory.CreateWorker(backendType, model);
            // engine.SetInput("text", input);
            engine.Execute(input);

            // Get output and cast to the appropriate tensor type (e.g. TensorFloat).
            output = engine.PeekOutput() as TensorFloat;

            // generatedAudioClip = CovertToAudioClip(output);
            output.AsyncReadbackRequest(ReadbackCallback);
        }


        void ReadbackCallback(bool completed)
        {
            Debug.Log("Read is completed. Data is in CPU already.");
            // The call to `MakeReadable` will no longer block with a readback as the data is already on the CPU
            output.MakeReadable();
            // The output tensor is now in a readable state on the CPU

            // Convert TensorFloat to AudioClip and save as WAV file.
            float[] audioData = output.ToReadOnlyArray();

            // Set the sample rate according to your Text To Speech model.
            int sampleRate = 22050;
            generatedAudioClip = AudioClip.Create("TTSOutput", audioData.Length, 1, sampleRate, false);
            generatedAudioClip.SetData(audioData, 0);

            // if (saveAudio) SaveToStreamingAssets(generatedAudioClip);
            // else OnClipCreated?.Invoke(Path.Combine(Application.streamingAssetsPath, "output.wav"));

            Debug.Log("Success!");
        }

        private void RemoveLayersAfterLayer(Model model, string layerName)
        {
            int index = model.layers.FindIndex(layer => layer.name == layerName);
            if (index != -1)
            {
                var newLayers = model.layers.GetRange(0, index + 1);
                model.layers = newLayers;

                // Set the output of the model to the output of the last layer we want to keep.
                model.outputs = new List<string> { layerName };

                Debug.Log("Layers Removed!");
            }
            else
            {
                Debug.LogError("Layer not found.");
            }
        }
    }
}