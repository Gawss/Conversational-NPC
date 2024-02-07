using UnityEngine;
using Unity.Sentis;
using Unity.Sentis.Layers;

public class ClassifyHandwrittenDigit : MonoBehaviour
{
    public Texture2D inputTexture;
    public ModelAsset modelAsset;
    Model runtimeModel;
    IWorker worker;
    public float[] results;

    void Start()
    {
        // Create the runtime model
        runtimeModel = ModelLoader.Load(modelAsset);

        // Add softmax layer to end of model instead of non-softmaxed output
        string softmaxOutputName = "Softmax_Output";
        runtimeModel.AddLayer(new Softmax(softmaxOutputName, runtimeModel.outputs[0]));
        runtimeModel.outputs[0] = softmaxOutputName;

        // Create input data as a tensor
        using Tensor inputTensor = TextureConverter.ToTensor(inputTexture, width: 28, height: 28, channels: 1);

        // Create an engine
        worker = WorkerFactory.CreateWorker(BackendType.GPUCompute, runtimeModel);

        // Run the model with the input data
        worker.Execute(inputTensor);

        // Get the result
        using TensorFloat outputTensor = worker.PeekOutput() as TensorFloat;

        // Move the tensor data to the CPU before reading it
        outputTensor.MakeReadable();

        results = outputTensor.ToReadOnlyArray();
    }

    void OnDisable()
    {
        // Tell the GPU we're finished with the memory the engine used
        worker.Dispose();
    }
}