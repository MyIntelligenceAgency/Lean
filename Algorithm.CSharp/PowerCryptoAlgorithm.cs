using QuantConnect.Algorithm;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Data.Market;
using System;
using System.Drawing;
using QuantConnect.Parameters;
using System.Linq;

namespace QuantConnect
{
    public class PowerCryptoAlgorithm : QCAlgorithm
    {
  

        //
        [Parameter("macd-fast")]
        public int FastPeriodMacd = 12;

        [Parameter("macd-slow")]
        public int SlowPeriodMacd = 26;

        private MovingAverageConvergenceDivergence _macd;
        private Symbol _btcusd;
        private const decimal _tolerance = 0.0025m;
        private bool _invested;

        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfoliovalueSeriesName = "PortFolioValue";

        //rci

        private RollingWindow<decimal> _priceWindow;
        private RollingWindow<decimal> _correlationWindow;
        private const int WindowSize = 14; // Ajustez la taille de la fenêtre selon vos besoins
        private const decimal CorrelationThreshold = 0.5m; // Ajustez le seuil de corrélation selon vos besoins

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
        }

        private void DoPlots()
        {
            Plot(_ChartName, _PriceSeriesName, Securities[_btcusd].Price);
            Plot(_ChartName, _PortfoliovalueSeriesName, Portfolio.TotalPortfolioValue);
        }

        public override void OnData(Slice data)
        {
            if (!_macd.IsReady) return;

            var holdings = Portfolio[_btcusd].Quantity;

            if (holdings <= 0 && _macd > _macd.Signal * (1 + _tolerance)) // condition d'achat
            {
                SetHoldings(_btcusd, 1.0);

                Debug($"Purchased BTC @{data.Bars[_btcusd].Close}$/Btc; Portfolio: {Portfolio.Cash}$, {Portfolio[_btcusd].Quantity}BTCs, Total Value: {Portfolio.TotalPortfolioValue}$, Total Fees: {Portfolio.TotalFees}$");
                _invested = true;
            }
            else if (_invested && _macd < _macd.Signal) // condition de vente
            {
                Liquidate(_btcusd);
                _invested = false;
                Debug($"Sold BTC @{data.Bars[_btcusd].Close}$/Btc; Portfolio: {Portfolio.Cash}$, {Portfolio[_btcusd].Quantity}BTCs, Total Value: {Portfolio.TotalPortfolioValue}$, Total Fees: {Portfolio.TotalFees}$");
            }
            if (!data.ContainsKey(_btcusd)) return;

            // Ajouter le prix actuel à la fenêtre des prix
            _priceWindow.Add(data[_btcusd].Close);

            // Calculer la corrélation entre les prix actuels et les prix passés
            decimal correlation = Correlation(_priceWindow, WindowSize);

            // Ajouter la corrélation à la fenêtre des corrélations
            _correlationWindow.Add(correlation);

            // Ajoutez votre logique basée sur l'indicateur RCI ici
            // Par exemple, si la corrélation est inférieure à un certain seuil, cela pourrait être une condition de vente

            // Exemple :
            if (_invested && _priceWindow.IsReady && correlation < CorrelationThreshold)
            {
                Liquidate(_btcusd);
                _invested = false;
                Debug($"Sold BTC due to low RCI correlation: {correlation}");
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
    }
}
