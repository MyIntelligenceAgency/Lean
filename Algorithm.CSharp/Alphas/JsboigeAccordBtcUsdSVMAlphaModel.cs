using System;
using System.Collections.Generic;
using MyIA.Trading.Backtester;
using MyIA.Trading.Converter;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp.Alphas;

public class JsboigeAccordBtcUsdSVMAlphaModel : AlphaModel
{

    public int _lookback;

    public int _inputSize;

    TradingTrainingConfig _trainingConfig;



    private Symbol _btcusd;

    // Choix de la résolution des données
    private Resolution _resolution;


    private TimeSpan _insightPeriod;


    private List<Trade> _HistoricalPrices = new();

    private ITradingModel _Model;

    private KnownKernel _kernel;

    private double _complexity;

    private TimeSpan _trainingTimout = TimeSpan.FromSeconds(30);

    public JsboigeAccordBtcUsdSVMAlphaModel(QCAlgorithm algorithm, Symbol btcusd, int lookback,
        int inputSize, Resolution resolution, TimeSpan insightPeriod, TimeSpan trainingTimout, KnownKernel kernel = KnownKernel.NormalizedPolynomial3, double complexity = 0.023) : base()
    {
        _btcusd = btcusd;
        _lookback = lookback;
        _inputSize = inputSize;
        _resolution = resolution;
        _insightPeriod = insightPeriod;
        _trainingTimout = trainingTimout;
        _kernel = kernel;
        _complexity = complexity;



        var resolutionSpan = _resolution.ToTimeSpan();

        var inputSpan = TimeSpan.FromTicks((_inputSize + 1) * resolutionSpan.Ticks);


        algorithm.SetWarmUp(inputSpan);


        _trainingConfig = new TradingTrainingConfig()
        {
            DataConfig = new TradingTrainingDataConfig()
            {
                // Durée de la prédiction
                OutputPrediction = TimeSpan.FromHours(48),
                // Pourcentage de variation du prix à partir duquel on considère que le prix a augmenté ou baissé
                OutputThresold = 10,
                TrainNb = 2000,
                TestNb = 500,
                TrainStartDate = new DateTime(2011, 01, 01),
                TrainEndDate = new DateTime(2016, 12, 31),
                TestStartDate = new DateTime(2017, 01, 01),
                TestEndDate = new DateTime(2017, 12, 31),
                PredictionMode = PredictionMode.ThresholdPeak,
                //Taux de prédictions classifiées minimum
                ClassifiedRate = 0,
                SampleConfig = new TradingSampleConfig()
                {
                    Filename = @"A:\TradingTests\bitstampUSD.bin.7z",
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
                    SamplingMode = SamplingMode.Constant,
                    ConstantSliceSpan = resolutionSpan,
                    // Les inputs sont échantillonés à des intervalles de temps se rapprochant exponentiellement
                    //SamplingMode = SamplingMode.Exponential,
                    //TimeCoef = 0.7m,
                }
            },
            ModelsConfig = new TradingModelsConfig()
            {
                SvmModelConfig = new TradingSvmModelConfig()
                {
                    Kernel = _kernel,
                    Complexity = _complexity,
                    TrainingTimeout = _trainingTimout,
                }
            }
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
