using System;
using System.Collections.Generic;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    public class mimi : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private Symbol _btcEur;
        private List<decimal> _fibonacciLevels;
        private RelativeStrengthIndex _rsi;

        public override void Initialize()
        {
            // Définir la période de démarrage et de fin
            SetStartDate(2015, 04, 04);
            SetEndDate(2021, 12, 04);

            // Modèle de courtage et devise du compte
            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            // Avant d'ajouter une sécurité, appeler SetAccountCurrency
            SetAccountCurrency("USD");

            // Ajouter le symbole crypto
            _btcEur = AddCrypto("BTCUSD", Resolution.Daily).Symbol;

            // Ajouter les niveaux de retracement de Fibonacci
            _fibonacciLevels = new List<decimal> { 0, 23.6m, 38.2m, 50, 61.8m, 100 };

            // Ajouter l'indicateur RSI
            _rsi = RSI(_btcEur, 14, MovingAverageType.Exponential, Resolution.Daily);

            // Définir l'argent de la stratégie
            SetCash(100000);
        }

        public override void OnData(Slice data)
        {
            // Vérifier si les données pour le symbole sont disponibles
            if (data.ContainsKey(_btcEur))
            {
                var currentPrice = data[_btcEur].Close;

                // Calculer les retracements de Fibonacci
                var fibonacciRetracements = new List<decimal>();
                foreach (var level in _fibonacciLevels)
                {
                    fibonacciRetracements.Add((level / 100) * currentPrice);
                }

                //// Mettre à jour la valeur de l'indicateur RSI
                //_rsi.Update(data.Bars[_btcEur]);

                // Conditions d'achat
                if (!Portfolio.Invested)
                {
                    // Ajouter ici la condition basée sur l'indicateur RSI pour l'achat
                    if (_rsi < 30)  // Exemple : acheter si RSI est inférieur à 30
                    {
                        // Ajouter ici la condition basée sur la stop-loss pour l'achat
                        var stopLossPercentage = 5;
                        var stopLossPrice = currentPrice * (1 - stopLossPercentage / 100);

                        // Acheter si la stop-loss condition est vérifiée
                        if (currentPrice < stopLossPrice)
                        {
                            // Calculer la quantité à acheter (50% du capital)
                            var quantityToBuy = Portfolio.Cash * 0.5m / currentPrice;

                            // Définir l'ordre d'achat avec un stop-loss
                            SetHoldings(_btcEur, quantityToBuy, stopLossPrice);
                            Debug($"Achat de {_btcEur.Value} au prix de {currentPrice} avec un retracement de {stopLossPrice} (stop-loss)");
                        }
                    }
                }
                // Conditions de vente
                else
                {
                    // Ajouter ici la condition basée sur l'indicateur RSI pour la vente
                    if (_rsi > 70)  // Exemple : vendre si RSI est supérieur à 70
                    {
                        Liquidate(_btcEur);
                        Debug($"Vente de {_btcEur.Value} au prix de {currentPrice} en raison du RSI élevé");
                    }
                }
            }
        }

        // Statistiques attendues pour les tests de régression
        public bool CanRunLocally { get; } = true;

        public Language[] Languages { get; } = { Language.CSharp, Language.Python };

        public long DataPoints => 4319;

        public int AlgorithmHistoryDataPoints => 120;

        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "0"},
            {"Tracking Error", "0"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "€0.00"},
            {"Estimated Strategy Capacity", "0.00"},
            {"Lowest Capacity Asset", ""},
            {"Portfolio Turnover", "0.00%"},
            {"OrderListHash", ""}
        };
    }
}
