using System;
using System.Collections.Generic;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Orders;
using QuantConnect.Interfaces;
using QuantConnect.Indicators;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    // Déclaration de la classe de l'algorithme
    public class AlphaStratMultiStrategyAlgorithm : QCAlgorithm
    {
        // Déclaration des indicateurs pour les stratégies
        public ExponentialMovingAverage _ema;
        public RelativeStrengthIndex _rsi;
        public SimpleMovingAverage _sma;

        // Paramètres de stratégie
        private int emaPeriod = 30;
        private int rsiPeriod = 14;
        private int smaPeriod = 50;
        private decimal emaSmaThreshold = 0.01m;
        private decimal rsiBuyThreshold = 40;
        private decimal rsiSellThreshold = 60;
        private decimal smaBuyThreshold = 0.01m;
        private decimal smaSellThreshold = -0.01m;

        // Taille des Ordres
        private int buyQuantityStrategy1 = 10;
        private int sellQuantityStrategy2 = 10;
        private int buyQuantityStrategy3 = 5;
        private int sellQuantityStrategy4 = 5;

        // Fenêtre Temporelle
        private DateTime startDate = new DateTime(2018, 4, 4);
        private DateTime endDate = new DateTime(2018, 4, 4);

        // Capital Initial
        private decimal initialCash = 200000;

        // Fréquence des Données
        private Resolution resolution = Resolution.Daily;

        // Méthode d'initialisation de l'algorithme
        public override void Initialize()
        {
            // Configuration de la période de démarrage et de fin
            SetStartDate(startDate);
            SetEndDate(endDate);

            // Configuration du capital initial
            SetCash(initialCash);

            // Ajout du symbole (dans cet exemple, AAPL) et initialisation des indicateurs
            AddEquity("AAPL");
            _ema = EMA("AAPL", emaPeriod, resolution);
            _rsi = RSI("AAPL", rsiPeriod, MovingAverageType.Simple, resolution);
            _sma = SMA("AAPL", smaPeriod, resolution);
        }

        // Méthode principale appelée à chaque nouvelle donnée (c'est l'événement principal)
        public override void OnData(Slice data)
        {
            // Récupérer le prix actuel
            var currentPrice = Securities["AAPL"].Price;

            // Implémentation de la logique de trading multi-stratégies
            if (_ema > _sma * (1 + emaSmaThreshold) && _rsi < rsiBuyThreshold)
            {
                // Stratégie 1 : Achat si EMA est au-dessus de SMA et RSI est en dessous de la borne inférieure
                Buy("AAPL", buyQuantityStrategy1);
            }
            else if (_ema < _sma * (1 - emaSmaThreshold) && _rsi > rsiSellThreshold)
            {
                // Stratégie 2 : Vente si EMA est en dessous de SMA et RSI est au-dessus de la borne supérieure
                Sell("AAPL", sellQuantityStrategy2);
            }
            else if (currentPrice > _sma * (1 + smaBuyThreshold))
            {
                // Stratégie 3 : Achat si le cours est au-dessus de la moyenne mobile simple (SMA)
                Buy("AAPL", buyQuantityStrategy3);
            }
            else if (currentPrice < _sma * (1 - smaSellThreshold))
            {
                // Stratégie 4 : Vente si le cours est en dessous de la moyenne mobile simple (SMA)
                Sell("AAPL", sellQuantityStrategy4);
            }
        }
    }
}
