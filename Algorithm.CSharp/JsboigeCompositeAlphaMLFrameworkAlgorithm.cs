using System;
using System.Collections.Generic;
using MyIA.Trading.Backtester;
using MyIA.Trading.Converter;
using QuantConnect.Algorithm.CSharp.Alphas;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Orders;
using QuantConnect.Parameters;

namespace QuantConnect.Algorithm.CSharp;

/// <summary>
/// Show cases how to use the <see cref="EmaCrossAlphaModel"/> 
/// </summary>
public class JsboigeCompositeAlphaMLFrameworkAlgorithm : QCAlgorithm
{

    private Symbol _btcusd;

    // Choix de la résolution des données
    private Resolution _resolution = Resolution.Daily;


    //L'attribut Parameter permet de définir les paramètres dans le fichier de configuration, et d'utiliser une optimisation
    [Parameter("ema-fast")]
    public int FastPeriod = 30;

    [Parameter("ema-slow")]
    public int SlowPeriod = 40;


    //L'attribut Parameter permet de définir les paramètres dans le fichier de configuration, et d'utiliser une optimisation
    [Parameter("ml-inputSize")]
    public int InputSize = 30;

    [Parameter("ml-insight-days")]
    private int InsightNbDays = 30;

    // Pourcentage de variation du prix à partir duquel on considère que le prix a augmenté ou baissé
    [Parameter("output-thresold")]
    public int OutputThresold = 20;

    // Durée de la prédiction en jours (au choix parmi 1,6,12h ou bien 1,2,3,5,10,20,30 jours)
    private int OutputPredictionNb = 10;

    // Nombre d'échantillons pour l'ensemble d'entraînement du modèle
    [Parameter("train-nb")]
    public int TrainNb = 5000;

    // Mode de prédiction: Exact (prix exact à la fin de période de prédiction) Peak (prix du premier pic de variation du seuil s'il est dans la période de prédiction) ou ThresholdPeak (prix du prochain pic de variation du seuil par rapport au prix initial s'il est dans la période de prédiction)
    private PredictionMode _predictionMode = PredictionMode.ThresholdPeak;

    private TimeSpan _trainingTimout = TimeSpan.FromSeconds(30);

    public override void Initialize()
    {

        //Passage en debug-mode
        //this.DebugMode = true;

        // Définition des périodes de backtest (3 périodes sont proposées avec retour de la valeur du btc à l'initial)
        InitPeriod();


        // Periode de warmup pour les indicateurs
        this.SetWarmUp(TimeSpan.FromDays(365));


        //Capital initial
        SetCash(10000);


        //Definition de notre univers

        // even though we're using a framework algorithm, we can still add our securities
        // using the AddEquity/Forex/Crypto/ect methods and then pass them into a manual
        // universe selection model using Securities.Keys
        SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);
        var btcSecurity = AddCrypto("BTCUSD", Resolution.Daily);
        _btcusd = btcSecurity.Symbol;

        // define a manual universe of all the securities we manually registered
        SetUniverseSelection(new ManualUniverseSelectionModel());


        //1er alpha: modèle SVM multiclasse avec Accord.NET

        var svmConfig1 = new TradingModelsConfig()
        {
            ModelType = TradingModelType.MulticlassSvm,
            SvmModelConfig = new TradingSvmModelConfig()
            {
                Kernel = KnownKernel.NormalizedPolynomial3,
                Complexity = 0.023,
                TrainingTimeout = _trainingTimout,
            }
        };

        var svmALpha1 = new JsboigeBtcUsdMachineLearningAlphaModel(this, _btcusd, svmConfig1,
             resolution: _resolution, 
             insightPeriodDays:InsightNbDays, 
             historicalTradesFilePath: @"A:\TradingTests\bitstampUSD.bin.7z", 
             inputSize: InputSize, 
             samplingMode: SamplingMode.Constant, 
             outputThreshold: 10, 
             outputPredictionDaysNb:2, 
             predictionMode: _predictionMode, 
             trainNb: TrainNb);

        //2ème alpha: modèle multiclasse AutoML avec ML.NET

        var autoMLConfig1 = new TradingModelsConfig()
        {
            ModelType = TradingModelType.AutoML,
            AutomMlModelConfig = new TradingAutoMlModelConfig()
            {
                TrainingTimeout = _trainingTimout,
            }
        };

        var autoMLAlpha1 =  new JsboigeBtcUsdMachineLearningAlphaModel(this, _btcusd, autoMLConfig1,
            resolution: _resolution,
            insightPeriodDays: InsightNbDays,
            historicalTradesFilePath: @"A:\TradingTests\bitstampUSD.bin.7z",
            inputSize: InputSize * 3,
            samplingMode: SamplingMode.Exponential,
            outputThreshold: OutputThresold,
            outputPredictionDaysNb: OutputPredictionNb,
            predictionMode: _predictionMode,
            trainNb: 10000,
            timeCoef:0.7m);

        var emaCrossAlpha = new EmaCrossAlphaModel(FastPeriod, SlowPeriod, _resolution);
        var rsiAlpha = new RsiAlphaModel(FastPeriod, _resolution);

        
        var compositeAlphaModel = new CompositeAlphaModel(svmALpha1, autoMLAlpha1, emaCrossAlpha, rsiAlpha);

        // define alpha model as a composite of the rsi and ema cross models
        SetAlpha(compositeAlphaModel);

        // default models for the rest
        SetPortfolioConstruction(new EqualWeightingPortfolioConstructionModelWithoutExpiry(Resolution.Daily, PortfolioBias.Long));
        SetExecution(new ImmediateExecutionModel());
        SetRiskManagement(new NullRiskManagementModel());
    }

        


    private void InitPeriod()
    {
        //SetStartDate(2013, 04, 07); // début backtest 164
        //SetEndDate(2015, 01, 14); // fin backtest 172


        //SetStartDate(2014, 02, 08); // début backtest 680
        //SetEndDate(2016, 11, 07); // fin backtest 703


        //SetStartDate(2017, 08, 08); // début backtest 3412
        //SetEndDate(2019, 02, 05); // fin backtest 3432

        //SetStartDate(2018, 01, 30); // début backtest 9971
        //SetEndDate(2020, 07, 26); // fin backtest 9945


        //SetStartDate(2017, 12, 15); // début backtest 17478
        //SetEndDate(2022, 12, 12); // fin backtest 17209

        //SetStartDate(2017, 11, 25); // début backtest 8718
        //SetEndDate(2020, 05, 1); // fin backtest 8832

        SetStartDate(2021, 1, 1); // début backtest 29410
        SetEndDate(2023, 10, 20); // fin backtest 29688
    }




    public override void OnOrderEvent(OrderEvent orderEvent)
    {

        if (orderEvent.Status == OrderStatus.Filled)
        {

            string message = "";
            if (orderEvent.Quantity < 0)
            {
                message = "Sold";
            }
            else
            {
                message = "Purchased";
            }

            var endMessage =
                $"{orderEvent.UtcTime.ToShortDateString()}, Price:  @{this.CurrentSlice.Bars[_btcusd].Close:N3}$/Btc; Portfolio: {Portfolio.CashBook[Portfolio.CashBook.AccountCurrency].Amount:N3}$, {Portfolio[_btcusd].Quantity}BTCs, Total Value: {Portfolio.TotalPortfolioValue:N3}$, Total Fees: {Portfolio.TotalFees:N3}$";
            //We skip small adjusting orders
            if (orderEvent.AbsoluteFillQuantity * orderEvent.FillPrice > 100)
            {
                Log($"{message} {endMessage}");
            }


        }

    }

}

