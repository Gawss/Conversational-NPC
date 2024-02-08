using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Sentis;
using UnityEngine;

namespace Univrse.Demo.NPC
{
    public class Text2Speech : MonoBehaviour
    {

        [SerializeField]
        private ModelAsset modelAsset;

        [SerializeField]
        private BackendType backendType;

        [SerializeField]
        private string inputText = "Hello World! I wish I could speak.";

        private TokenizerRunner tokenizerRunner;

        Model model;

        public Action<string> OnClipCreated;

        public bool saveAudio = false;
        public AudioClip generatedAudioClip;
        IWorker engine;

        private void Awake()
        {
            tokenizerRunner = new TokenizerRunner();
        }

        private void Start()
        {
            // Load model for inference.
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

            // Setup engine of given worker type and model.
            engine = WorkerFactory.CreateWorker(backendType, model);
            engine.SetInput("text", input);
            engine.Execute();

            // Get output and cast to the appropriate tensor type (e.g. TensorFloat).
            var output = engine.PeekOutput() as TensorFloat;

            generatedAudioClip = CovertToAudioClip(output);
            if (saveAudio) SaveToStreamingAssets(generatedAudioClip);
            else OnClipCreated?.Invoke(Path.Combine(Application.streamingAssetsPath, "output.wav"));

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

        private AudioClip CovertToAudioClip(TensorFloat output)
        {
            output.MakeReadable();

            // Convert TensorFloat to AudioClip and save as WAV file.
            float[] audioData = output.ToReadOnlyArray();
            // Set the sample rate according to your Text To Speech model.
            int sampleRate = 22050;
            AudioClip audioClip = AudioClip.Create("TTSOutput", audioData.Length, 1, sampleRate, false);
            audioClip.SetData(audioData, 0);
            return audioClip;
        }

        private async void SaveToStreamingAssets(AudioClip audioClip)
        {
            // Ensure the StreamingAssets folder exists.
            string outputFolder = Application.streamingAssetsPath;

            await Task.Run(() =>
            {
                if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);
            });

            await SaveAudioClipToWavAsync(audioClip, Path.Combine(Application.streamingAssetsPath, "output.wav"));

            // Invoke the callback on the main thread
            OnClipCreated?.Invoke(Path.Combine(Application.streamingAssetsPath, "output.wav"));
        }

        // private void SaveAudioClipToWav(AudioClip clip, string filePath)
        // {
        //     using var fileStream = new FileStream(filePath, FileMode.Create);
        //     using var writer = new BinaryWriter(fileStream);

        //     // Write WAV header.
        //     writer.Write("RIFF".ToCharArray());
        //     writer.Write(36 + clip.samples * 2);
        //     writer.Write("WAVE".ToCharArray());
        //     writer.Write("fmt ".ToCharArray());
        //     writer.Write(16);
        //     writer.Write((short)1);
        //     writer.Write((short)clip.channels);
        //     writer.Write(clip.frequency);
        //     writer.Write(clip.frequency * clip.channels * 2);
        //     writer.Write((short)(clip.channels * 2));
        //     writer.Write((short)16);
        //     writer.Write("data".ToCharArray());
        //     writer.Write(clip.samples * clip.channels * 2);

        //     // Write audio data.
        //     float[] samples = new float[clip.samples * clip.channels];
        //     clip.GetData(samples, 0);
        //     for (int i = 0; i < samples.Length; i++)
        //     {
        //         writer.Write((short)(samples[i] * short.MaxValue));
        //     }

        //     OnClipCreated?.Invoke(filePath);
        // }

        private async Task SaveAudioClipToWavAsync(AudioClip clip, string filePath)
        {
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            using (var writer = new BinaryWriter(fileStream))
            {
                // Write WAV header.
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + clip.samples * 2);
                writer.Write("WAVE".ToCharArray());
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2);
                writer.Write((short)(clip.channels * 2));
                writer.Write((short)16);
                writer.Write("data".ToCharArray());
                writer.Write(clip.samples * clip.channels * 2);

                // Write audio data.
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);

                for (int i = 0; i < samples.Length; i++)
                {
                    // Write asynchronously using FileStream.WriteAsync
                    byte[] buffer = BitConverter.GetBytes((short)(samples[i] * short.MaxValue));
                    await fileStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }
    }
}