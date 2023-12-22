using System;
using System.Collections.Generic;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Interfaces;
using QuantConnect.Brokerages;
using QuantConnect.Orders.Fees;
using QuantConnect.Securities;
using QuantConnect.Orders;
using QuantConnect.Data;
using System.Security.Cryptography;
using QLNet;

namespace QuantConnect.Algorithm.CSharp
{
    public class BAMSAlgorithm : QCAlgorithm
    {
        // Déclaration des variables pour les périodes des moyennes mobiles exponentielles (EMA)
        public int FastPeriod = 30; // Période rapide pour l'EMA

        public int SlowPeriod = 40; // Période lente pour l'EMA

        // Déclaration des marges pour la liquidation
        public decimal UpCrossMargin = 1.001m; // Marge supérieure

        public decimal DownCrossMargin = 0.999m; // Marge inférieure

        // Déclaration des indicateurs techniques
        public ExponentialMovingAverage Fast; // EMA rapide
        public ExponentialMovingAverage Slow; // EMA lent
        public MovingAverageConvergenceDivergence macd; // MACD
        public BollingerBands bb; // Bandes de Bollinger

        private Symbol _btcusd;

        // Méthode d'initialisation de l'algorithme
        public override void Initialize()
        {
            this.InitPeriod(); // Configuration de la période de backtesting
            this.SetWarmUp(TimeSpan.FromDays(365)); // Période de préchauffage pour les indicateurs
            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash); // Configuration du modèle de courtage
            SetCash(100000); // Montant initial en cash
            // Configuration de la paire de trading et des indicateurs
            var btcSecurity = AddCrypto("BTCUSD", Resolution.Daily);
            _btcusd = btcSecurity.Symbol;
            Fast = EMA(_btcusd, FastPeriod, Resolution.Daily); // Initialisation de l'EMA rapide
            Slow = EMA(_btcusd, SlowPeriod, Resolution.Daily); // Initialisation de l'EMA lent
            macd = MACD(_btcusd, 12, 26, 9, MovingAverageType.Exponential,
                Resolution.Daily); // Initialisation du MACD
            bb = BB(_btcusd, 20, 2, MovingAverageType.Simple,
                Resolution.Daily); //Initialisation des Bandes de Bollinger
        }

        public override void OnData(Slice data)
        {
            // Vérification si les indicateurs sont prêts et si la période de chauffe est terminée
            if (this.IsWarmingUp || !macd.IsReady || !bb.IsReady) return;
            var currentPrice = data[_btcusd].Close; // Récupération du prix de clôture actuel de BTCUSD
            // Conditions pour prendre une position longue
            if (!Portfolio.Invested && macd > 0 && currentPrice > bb.UpperBand)
            {
                SetHoldings(_btcusd, 1); // Prendre une position longue à 100%
            }
            // Conditions pour liquider la position longue
            else if (Portfolio.Invested && macd < 0)
            {
                Liquidate(_btcusd); // Liquider la position
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            if (orderEvent.Status == OrderStatus.Filled)
            {
                // Création d'un message pour le journal
                string message = orderEvent.Quantity < 0 ? "Sold" : "Purchased";
                var endMessage =
                    $"{orderEvent.UtcTime.ToShortDateString()}, Price:@{orderEvent.FillPrice:N3}$/ Btc; Portfolio:{Portfolio.CashBook[Portfolio.CashBook.AccountCurrency].Amount:N3}$,{Portfolio[_btcusd].Quantity}BTCs, Total Value: {Portfolio.TotalPortfolioValue:N3}$, TotalFees: {Portfolio.TotalFees:N3}$";
                if (orderEvent.AbsoluteFillQuantity * orderEvent.FillPrice > 100)
                {
                    Log($"{message} {endMessage}");
                }
            }
        }

        // Méthode pour définir la période du backtest
        private void InitPeriod()
        {
            SetStartDate(2021, 01, 1); // Date de début
            SetEndDate(2023, 10, 20); // Date de fin
        }
    }
}
