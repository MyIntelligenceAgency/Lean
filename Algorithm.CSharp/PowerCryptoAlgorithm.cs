using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;
using System;
using System.Drawing;
using QuantConnect.Parameters;
using System.Linq;

using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Portfolio;


using Accord.Math;

using Accord.MachineLearning;
using Accord.MachineLearning.DecisionTrees;
using Accord.MachineLearning.DecisionTrees.Learning;
using QuantConnect.Data.Market;

using QuantConnect.Orders;
using QLNet;

namespace QuantConnect
{
    public class PowerCryptoAlgorithm : QCAlgorithm
    {
        //rci
        ////private Symbol _btcusd;
        private RollingWindow<decimal> _priceWindow;
        private RollingWindow<decimal> _correlationWindow;
        private const int WindowSize = 14; // Ajustez la taille de la fenêtre selon vos besoins
        private const decimal CorrelationThreshold = 0.5m; // Ajustez le seuil de corrélation selon vos besoins


        //
        [Parameter("macd-fast")]
        public int FastPeriodMacd = 12;

        [Parameter("macd-slow")]
        public int SlowPeriodMacd = 26;

        [Parameter("dt-max-vars")]
        public int DecisionTreeMaxVariables = 0;

        [Parameter("dt-max-height")]
        public int DecisionTreeMaxHeight = 5;


        private MovingAverageConvergenceDivergence _macd;
        private Symbol _btcusd;
        private const decimal _tolerance = 0.0025m;
        private bool _invested;

        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfoliovalueSeriesName = "PortFolioValue";

        private DecisionTree _decisionTree;

        private BollingerBands _bollingerBands;

        private TrailingStopRiskManagementModel _trailingStopRiskManagementModel;

        // Niveaux de support et de résistance
        private decimal _supportLevel = 50000m; // Niveau de support
        private decimal _resistanceLevel = 65000m; // Niveau de résistance



        public override void Initialize()
        {
            SetStartDate(2021, 1, 1); // début backtest
            SetEndDate(2023, 3, 22); // fin backtest



            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            SetCash(10000); // capital

            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;


            _macd = MACD(_btcusd, FastPeriodMacd, SlowPeriodMacd, 9, MovingAverageType.Exponential, Resolution.Daily, Field.Close);


            // Dealing with plots
            var stockPlot = new Chart(_ChartName);
            var assetPrice = new Series(_PriceSeriesName, SeriesType.Line, "$", Color.Blue);
            var portFolioValue = new Series(_PortfoliovalueSeriesName, SeriesType.Line, "$", Color.Green);
            stockPlot.AddSeries(assetPrice);
            stockPlot.AddSeries(portFolioValue);
            AddChart(stockPlot);
            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);

            // RCI
            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;
            _priceWindow = new RollingWindow<decimal>(WindowSize);

            _correlationWindow = new RollingWindow<decimal>(WindowSize);

            // Initialisation du modèle Arbre de Décision
            _decisionTree = TrainDecisionTree();


            // Ajouter l'indicateur Bollinger Bands avec les paramètres par défaut (20 périodes, déviation standard de 2)
            _bollingerBands = BB(_btcusd, 20, 2, MovingAverageType.Simple, Resolution.Daily);

            // Ajouter l'indicateur à votre chart
             //AddChartIndicator(_ChartName, _bollingerBands);
            

            // Initialisation du modèle de gestion des risques de stop suiveur
            _trailingStopRiskManagementModel = new TrailingStopRiskManagementModel(0.05m); // 5% de drawdown

            // Ajoutez le modèle de gestion des risques à votre algorithme
            SetRiskManagement(_trailingStopRiskManagementModel);
        }

        private void DoPlots()
        {
            Plot(_ChartName, _PriceSeriesName, Securities[_btcusd].Price);
            Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
        }
        private DecisionTree TrainDecisionTree()
        {
            // Préparez vos données d'entraînement (features et labels)
            var features = PrepareTrainingFeatures();
            var labels = PrepareTrainingLabels();

            // Assurez-vous que features et labels ne sont pas null
            if (features != null && labels != null)
            {
                // Créez et entraînez le modèle Arbre de Décision avec C45Learning
                var teacher = new C45Learning
                {
                    MaxVariables = this.DecisionTreeMaxVariables, // Définissez le nombre maximum de variables à utiliser pour chaque arbre
                    MaxHeight = this.DecisionTreeMaxHeight// Set the maximum height of the decision tree
                };
                // Entraînez le modèle avec vos données d'entraînement
                _decisionTree = teacher.Learn(features, labels);
            }




            return _decisionTree;
        }

        public override void OnData(Slice data)
        {
            if (!_macd.IsReady) return;

            var closePrice = Securities[_btcusd].Close;
            var holdings = Portfolio[_btcusd].Quantity;

            // Condition d'achat : acheter seulement si non investi et MACD croise au-dessus du signal
            if (holdings <= 0 && _macd > _macd.Signal && _priceWindow.IsReady && closePrice <= _supportLevel)
            {
                // Vérifiez si la corrélation est également dans une plage acceptable
                decimal _correlation = Correlation(_priceWindow, WindowSize);

                if (_correlation > CorrelationThreshold)
                {
                    SetHoldings(_btcusd, 1.0);
                    Debug($"Purchased BTC @{data.Bars[_btcusd].Close}$/Btc; Portfolio: {Portfolio.Cash}$, {Portfolio[_btcusd].Quantity}BTCs, Total Value: {Portfolio.TotalPortfolioValue}$, Total Fees: {Portfolio.TotalFees}$");
                    _invested = true;
                }
                else
                {
                    Debug($"Did not buy BTC due to low correlation: {_correlation}");
                }


            }

            // Condition de vente : vendre seulement si investi et MACD croise en dessous du signal
            else if (_invested && _macd < _macd.Signal && closePrice >= _resistanceLevel)
            {
                Liquidate(_btcusd);
                _invested = false;
                Debug($"Sold BTC @{data.Bars[_btcusd].Close}$/Btc; Portfolio: {Portfolio.Cash}$, {Portfolio[_btcusd].Quantity}BTCs, Total Value: {Portfolio.TotalPortfolioValue}$, Total Fees: {Portfolio.TotalFees}$");
            }

            if (!data.ContainsKey(_btcusd)) return;

            _priceWindow.Add(data[_btcusd].Close);
            decimal correlation = Correlation(_priceWindow, WindowSize);
            _correlationWindow.Add(correlation);

            Debug($"Current Correlation: {correlation}");
            // Récupérer les données et les préparer pour le modèle
            var inputFeatures = PrepareInputFeatures();

            // Faire des prédictions avec le modèle Arbre de Décision
            var prediction = _decisionTree.Decide(inputFeatures);

            // Traiter la sortie du modèle et effectuer des actions en conséquence
            //  ExecuteActions(prediction);

            //  var upperBand = _bollingerBands.UpperBand.Current.Value;
            //  var lowerBand = _bollingerBands.LowerBand.Current.Value;

            var upperBand = 50000m;
            var lowerBand = 50000m;

            Debug($" Upper Band : {upperBand}");
            Debug($" lower Band : {lowerBand}");


            // Utilisez les Bollinger Bands comme déclencheurs pour vos décisions de trading
            if (data.Bars[_btcusd].Close > upperBand && !_invested)
            {
                // Condition de vente basée sur le franchissement de la bande supérieure
                Liquidate(_btcusd);
                _invested = false;
                Debug($"Sold BTC due to upper Bollinger Band crossing. Close Price: {data.Bars[_btcusd].Close}, Upper Band: {upperBand}");
            }
            else if (data.Bars[_btcusd].Close < lowerBand && !_invested)
            {
                // Condition d'achat basée sur le franchissement de la bande inférieure
                SetHoldings(_btcusd, 1.0);
                _invested = true;
                Debug($"Purchased BTC due to lower Bollinger Band crossing. Close Price: {data.Bars[_btcusd].Close}, Lower Band: {lowerBand}");
            }

            // Application du modèle de gestion des risques de stop suiveur
            var riskAdjustedTargets = _trailingStopRiskManagementModel.ManageRisk(this, new[] { new PortfolioTarget(_btcusd, holdings) });
            foreach (var target in riskAdjustedTargets)
            {
                SetHoldings(target.Symbol, 5);
            }
        }
        private decimal Correlation(RollingWindow<decimal> x, int period)
        {
            // Calculez la corrélation entre les prix actuels et les prix passés
            // Vous pouvez utiliser une formule de corrélation appropriée ici, par exemple, la corrélation de Pearson
            // Notez que ceci est une implémentation simple et peut nécessiter des ajustements en fonction de vos besoins.

            if (x.IsReady)
            {
                var meanX = x.Average();
                var meanY = x.Skip(1).Take(period).Average(); // décalage d'une position pour calculer la corrélation avec les prix passés
                var cov = x.Zip(x.Skip(1).Take(period), (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
                var stdDevX = (decimal)Math.Sqrt(x.Sum(xi => (double)((xi - meanX) * (xi - meanX))));
                var stdDevY = (decimal)Math.Sqrt(x.Skip(1).Take(period).Sum(yi => (double)((yi - meanY) * (yi - meanY))));

                if (stdDevX > 0 && stdDevY > 0)
                {
                    return cov / (stdDevX * stdDevY);
                }
            }

            return 0m;
        }

        private double[] PrepareInputFeatures()
        {
            // Récupérer les données les plus récentes
            var latestData = History<TradeBar>(_btcusd, 1, Resolution.Daily);

            if (latestData.Count() == 0)
            {
                // Handle the case where there is no data available
                return new double[0];
            }

            // Exemple de construction du vecteur de caractéristiques pour la prédiction
            var latestBar = latestData.Single();
            var inputFeatures = new double[]
            {
        (double)latestBar.Open,
        (double)latestBar.High,
        (double)latestBar.Low,
        (double)latestBar.Close,
        (double)latestBar.Volume,
                // Ajouter d'autres caractéristiques nécessaires pour la prédiction
                // par exemple :
                // (double)latestBar.OtherFeature1,
                // (double)latestBar.OtherFeature2,
                // ...
            };

            return inputFeatures;
        }

        private double[][] PrepareTrainingFeatures()
        {
            // Remplacez cela par la récupération réelle des données de prix historiques depuis Bitstamp
            var bitstampData = History<TradeBar>(_btcusd, 3300, Resolution.Daily);

            // Exemple de remplissage fictif des données d'entraînement (features)
            double[][] features = bitstampData
                .Take(3300) // Prendre les 10 premiers éléments de la séquence
                .Select(bar => new double[] {
            (double)bar.Open,
            (double)bar.High,
            (double)bar.Low,
            (double)bar.Close,
            (double)bar.Volume
                })
                .ToArray();

            return features;
        }


        private int[] PrepareTrainingLabels()
        {
            // Remplacez cela par la récupération réelle des données de prix historiques depuis Bitstamp
            var bitstampData = History<TradeBar>(_btcusd, 3301, Resolution.Daily).ToList();

            // Exemple de remplissage fictif des données d'entraînement (labels)
            int[] labels = new int[3300]; // Nous avons besoin d'un label pour chaque échantillon

            // Assigner les labels en fonction de la comparaison entre le prix actuel et le prix du jour suivant
            for (int i = 0; i < 3300; i++)
            {
                // Assurez-vous que l'indice i + 1 ne dépasse pas la longueur de la liste
                if (i + 1 < bitstampData.Count)
                {
                    // Si le prix du jour suivant est supérieur au prix actuel, attribuer 1 (achat), sinon attribuer 0 (vente)
                    labels[i] = bitstampData[i + 1].Close > bitstampData[i].Close ? 1 : 0;
                }
                else
                {
                    // Gérer le cas où nous sommes à la fin de la liste
                    labels[i] = 0; // Ou toute autre logique appropriée dans votre cas
                }
            }

            return labels;
        }


        private void ExecuteActions(int prediction)
        {
            // Placer des ordres, ajuster le portefeuille, etc. en fonction de la prédiction
            // Vous pouvez personnaliser cette fonction en fonction de votre stratégie

            // Montant à investir dans chaque transaction
            decimal investmentAmount = Portfolio.Cash / 3; // Vous pouvez ajuster cela en fonction de votre stratégie

            if (prediction == 1)
            {
                // Acheter
                var quantityToBuy = investmentAmount / Securities[_btcusd].Price;
                SetHoldings(_btcusd, 1);
                Debug($"Buy order placed. Quantity: {quantityToBuy}, Portfolio Value: {Portfolio.TotalPortfolioValue}");
            }
            else
            {
                // Vendre
                Liquidate(_btcusd);
                Debug($"Sell order placed. Portfolio Value: {Portfolio.TotalPortfolioValue}");
            }
        }


    }
}
