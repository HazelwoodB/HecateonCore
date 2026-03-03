using Microsoft.ML;

namespace Hecateon.Services;

public record SentimentResult(string Label, float Score);

public class SimpleSentimentModel
{
    private readonly PredictionEngine<SentimentData, SentimentPrediction> _predictor;

    public SimpleSentimentModel()
    {
        var mlContext = new MLContext(seed: 0);

        // Very small in-memory training data for demo purposes
        var data = new[]
        {
            new SentimentData { Text = "I love this", Label = true },
            new SentimentData { Text = "This is fantastic", Label = true },
            new SentimentData { Text = "I am very happy", Label = true },
            new SentimentData { Text = "I hate this", Label = false },
            new SentimentData { Text = "This is terrible", Label = false },
            new SentimentData { Text = "I am sad", Label = false },
        };

        var trainingData = mlContext.Data.LoadFromEnumerable(data);

        var pipeline = mlContext.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.Text))
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: nameof(SentimentData.Label), featureColumnName: "Features"));

        var model = pipeline.Fit(trainingData);

        _predictor = mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);
    }

    public SentimentResult Predict(string text)
    {
        var pred = _predictor.Predict(new SentimentData { Text = text });
        var label = pred.PredictedLabel ? "Positive" : "Negative";
        return new SentimentResult(label, pred.Probability);
    }

    private class SentimentData
    {
        public string Text { get; set; } = string.Empty;
        public bool Label { get; set; }
    }

    private class SentimentPrediction
    {
        public bool PredictedLabel { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
    }
}
