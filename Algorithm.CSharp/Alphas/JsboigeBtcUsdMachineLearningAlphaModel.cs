using System;
using System.Collections.Generic;
using MyIA.Trading.Backtester;
using MyIA.Trading.Converter;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp.Alphas;

public class JsboigeBtcUsdMachineLearningAlphaModel : AlphaModel
{

    private Symbol _btcusd;

    private TimeSpan _insightPeriod;


    TradingTrainingConfig _trainingConfig;

    private List<Trade> _HistoricalPrices = new();

    private ITradingModel _Model;

    private TradingModelsConfig _modelsConfig;


    /// <summary>
    ///  Constructeur de l'alpha
    /// </summary>
    /// <param name="algorithm">algorithme utilisant l'alpha</param>
    /// <param name="btcusd">symbole btcusd</param>
    /// <param name="resolution">résolution des données de l'alpha</param>
    /// <param name="insightPeriodDays">Période de validité en jours des insights générés</param>
    /// <param name="modelsConfig"></param>
    /// <param name="inputSize">Taille de la fenêtre d'inputs en nombre de résolutions</param>
    /// <param name="samplingMode"> Mode d'échantillonnage des inputs: Constant (intervalles de temps réguliers) ou Exponential (intervalles de temps se rapprochant exponentiellement)</param>
    /// <param name="outputThreshold"> Pourcentage de variation du prix à partir duquel on considère que le prix a augmenté ou baissé</param>
    /// <param name="outputPredictionDaysNb">Durée de la prédiction en jours (au choix parmi 1,6,12h ou bien 1,2,3,5,10,20,30 jours)</param>
    /// <param name="predictionMode">Mode de prédiction: Exact (prix exact à la fin de période de prédiction) Peak (prix du premier pic de variation du seuil s'il est dans la période de prédiction) ou ThresholdPeak (prix du prochain pic de variation du seuil par rapport au prix initial s'il est dans la période de prédiction)</param>
    /// <param name="trainNb">Nombre d'échantillons pour l'ensemble d'entraînement du modèle</param>
    /// <param name="timeCoef">coefficient de raccourcissement des intervals d'échantillonage en mode Exponential</param>
    public JsboigeBtcUsdMachineLearningAlphaModel(QCAlgorithm algorithm, Symbol btcusd,
        TradingModelsConfig modelsConfig,
        Resolution resolution = Resolution.Daily,
        int insightPeriodDays = 30,
        string historicalTradesFilePath = @"A:\TradingTests\bitstampUSD.bin.7z",
        int inputSize = 30,  
        SamplingMode samplingMode = SamplingMode.Constant,
        int outputThreshold = 20,
        int outputPredictionDaysNb = 10, 
        PredictionMode predictionMode = PredictionMode.ThresholdPeak,
        int trainNb = 10000,
        decimal timeCoef = 0.7m
        ) : base()
    {
        _btcusd = btcusd;
        _modelsConfig = modelsConfig;

        _insightPeriod = TimeSpan.FromDays(insightPeriodDays);

        var resolutionSpan = resolution.ToTimeSpan();

        var inputSpan = TimeSpan.FromTicks((inputSize + 1) * resolutionSpan.Ticks);


        algorithm.SetWarmUp(inputSpan);

        var outputPredictionSpan = TimeSpan.FromDays(outputPredictionDaysNb);


        _trainingConfig = new TradingTrainingConfig()
        {
            DataConfig = new TradingTrainingDataConfig()
            {
                // Durée de la prédiction
                OutputPrediction = outputPredictionSpan,
                // Pourcentage de variation du prix à partir duquel on considère que le prix a augmenté ou baissé
                OutputThresold = outputThreshold,
                TrainNb = trainNb,
                TestNb = 500,
                TrainStartDate = new DateTime(2011, 01, 01),
                TrainEndDate = new DateTime(2016, 12, 31),
                TestStartDate = new DateTime(2017, 01, 01),
                TestEndDate = new DateTime(2017, 12, 31),
                PredictionMode = predictionMode,
                //Taux de prédictions classifiées minimum
                ClassifiedRate = 0,
                SampleConfig = new TradingSampleConfig()
                {
                    Filename = historicalTradesFilePath,
                    //Fenêtre d'échantillonnage
                    StartDate = new DateTime(2011, 01, 01),
                    EndDate = new DateTime(2022, 12, 31),
                    // Nombre d'échantillons à générer
                    NbSamples = 400000,
                    // Résolution minimale des inputs
                    MinSlice = resolutionSpan,
                    // largeur de la fenêtre d'inputs
                    LeftWindow = inputSpan,
                    // Les inputs sont échantillonés à des intervalles de temps réguliers
                    SamplingMode = samplingMode,
                    ConstantSliceSpan = resolutionSpan,
                    // Les inputs sont échantillonés à des intervalles de temps se rapprochant exponentiellement
                    //SamplingMode = SamplingMode.Exponential,
                    TimeCoef = timeCoef,
                }
            },
            ModelsConfig = _modelsConfig
        };


    }



    public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
    {
        var insights = new List<Insight>();
        var currentBar = data.Bars[_btcusd];
        var currentTrade = new Trade()
        {
            UnixTime = currentBar.Time.ToUnixTime(),
            Price = currentBar.Close,
            Amount = currentBar.Volume
        };
        _HistoricalPrices.Add(currentTrade);

        if (!algorithm.IsWarmingUp)
        {
            var result = TrainAndTrade(algorithm);

            switch (result)
            {
                case 1:
                    insights.Add(Insight.Price(_btcusd, _insightPeriod, InsightDirection.Up));
                    break;
                case 2:
                    insights.Add(Insight.Price(_btcusd, _insightPeriod, InsightDirection.Down));
                    break;
            }
        }

        return insights;
    }


    private int TrainAndTrade(QCAlgorithm algorithm)
    {

        var objSample = _trainingConfig.DataConfig.SampleConfig.CreateInput(_HistoricalPrices, _HistoricalPrices.Count - 1);

        if (objSample != null)
        {
            var objInputs = _trainingConfig.DataConfig.GetTrainingData(objSample);

            var objData = new List<TradingTrainingSample>();
            objData.Add(objInputs);
            if (_Model == null)
            {
                double testError = 0;
                _Model = _trainingConfig.TrainModel(algorithm.Log, ref testError);
            }
            var result = _Model.Predict(objData);
            return (int)result[0].Output;

        }

        return 0;
    }



}
