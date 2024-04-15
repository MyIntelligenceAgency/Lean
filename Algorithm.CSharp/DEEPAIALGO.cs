using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;
using System;
using System.Drawing;
using QuantConnect.Parameters;

namespace QuantConnect
{
    public class DEEPAIALGO : QCAlgorithm
    {
        [Parameter("macd-fast")]
        public int FastPeriodMacd = 5;

        [Parameter("macd-slow")]
        public int SlowPeriodMacd = 80;

        private MovingAverageConvergenceDivergence _macd;
        private Symbol _btcusd;
        private VolumeWeightedAveragePriceIndicator _vwap;
        private const decimal _tolerance = 0.0025m;
        private bool _invested;

        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfoliovalueSeriesName = "PortFolioValue";
        private decimal _stopLossAmount = 300; // Montant de la perte maximale autorisée (valant 10 000$, le btc peut facilement perdre 500$ de sa valeur initiale)
        private decimal _takeProfitAmount = 100; // Montant du gain maximal autorisé


        public override void Initialize()
        {
            SetStartDate(2022, 1, 1); // début backtest
            SetEndDate(2023, 3, 22); // fin backtest

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            SetCash(60000); // capital de départ

            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;

            _macd = MACD(_btcusd, FastPeriodMacd, SlowPeriodMacd, 9, MovingAverageType.Exponential, Resolution.Daily, Field.Close);
            _vwap = VWAP(_btcusd, 20, Resolution.Daily);

            // Initialisation des plots pour pouvoir les analyser
            var stockPlot = new Chart(_ChartName);
            var assetPrice = new Series(_PriceSeriesName, SeriesType.Line, "$", Color.Blue);
            var portFolioValue = new Series(_PortfoliovalueSeriesName, SeriesType.Line, "$", Color.Green);
            stockPlot.AddSeries(assetPrice);
            stockPlot.AddSeries(portFolioValue);
            AddChart(stockPlot);
            Schedule.On(DateRules.EveryDay(), TimeRules.Every(TimeSpan.FromDays(1)), DoPlots);
        }

        private void DoPlots()
        {
            Plot(_ChartName, _PriceSeriesName, Securities[_btcusd].Price);
            Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
        }

        //Fonction principale de prise des décisions d'investissement
        public override void OnData(Slice data)
        {
            if (!_macd.IsReady || !_vwap.IsReady) return;

            var portfolio_value = Portfolio[_btcusd].Quantity;

            // Condition d'achat :  vérifier si pas de position ouverte puis vérifier si la ligne MACD est supérieure à (la ligne de signal multipliée par (1 + _tolerance)). On vérifie une condition de croisement haussier entre la ligne MACD et la ligne de signal, avec une tolérance.
            // Enfin, on vérifie si le prix actuel du BTC est inférieur à la moyenne pondérée par le volume (VWAP): on cherche ici une opportunité d'achat à un prix relativement bas.
            if (portfolio_value <= 1 && _macd > _macd.Signal * (1 + _tolerance) && Securities[_btcusd].Price < _vwap)
            {
                SetHoldings(_btcusd, 1.0); // Achat d'un bitcoin
                Debug($"Achats de BTC @{data.Bars[_btcusd].Close}$/Btc; Portefeuille : {Portfolio.Cash}$, {Portfolio[_btcusd].Quantity}BTCs, Valeur totale : {Portfolio.TotalPortfolioValue}$, Frais totaux : {Portfolio.TotalFees}$");
                _invested = true;
            }
            // Condition de vente avec stop-loss et take-profit
            else if (_invested)
            {
                // Calcul de la perte latente et du gain réalisé
                var unrealizedLoss = (Securities[_btcusd].Price - Portfolio[_btcusd].AveragePrice) * Portfolio[_btcusd].Quantity;
                var unrealizedGain = (Securities[_btcusd].Price - Portfolio[_btcusd].AveragePrice) * Portfolio[_btcusd].Quantity;

                // Niveau de stop-loss
                if (unrealizedLoss <= -_stopLossAmount)
                {
                    Liquidate(_btcusd);
                    _invested = false;
                    Debug($"Vente avec stop-loss de BTC @{data.Bars[_btcusd].Close}$/Btc; Portefeuille : {Portfolio.Cash}$, {Portfolio[_btcusd].Quantity}BTCs, Valeur totale : {Portfolio.TotalPortfolioValue}$, Frais totaux : {Portfolio.TotalFees}$");
                }
                // Niveau de take-profit
                else if (unrealizedGain >= _takeProfitAmount)
                {
                    Liquidate(_btcusd);
                    _invested = false;
                    Debug($"Vente avec take-profit de BTC @{data.Bars[_btcusd].Close}$/Btc; Portefeuille : {Portfolio.Cash}$, {Portfolio[_btcusd].Quantity}BTCs, Valeur totale : {Portfolio.TotalPortfolioValue}$, Frais totaux : {Portfolio.TotalFees}$");
                }
            }
        }
    }
}
