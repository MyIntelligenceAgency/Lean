using System;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Brokerages;
using QuantConnect.Interfaces;
// Pas d'espace de noms 'QuantConnect.Charting' nécessaire ici.
namespace QuantConnect.Algorithm.CSharp
{
    public class mimi : QCAlgorithm
    {
        private Symbol _btcusdSymbol;
        private ExponentialMovingAverage _fastEMA;
        private ExponentialMovingAverage _slowEMA;
        private decimal _lastPurchasePrice;
        private const decimal StopLossPercentage = 0.75m; // 75% stop loss
                                                          // Ajoutez les membres pour le graphique et les séries
        private string _ChartName = "Trade Plot";
        private string _PriceSeriesName = "Price";
        private string _PortfolioValueSeriesName = "Portfolio Value";
        public override void Initialize()
        {
            SetStartDate(2018, 1, 1); // Set Start Date
            SetEndDate(2020, 12, 31); // Set End Date
            SetCash(10000); // Set Strategy Cash
            SetCash("BTC", 1m); // Set initial BTC holdings
            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);
            _btcusdSymbol = AddCrypto("BTCUSD").Symbol;
            // Initialize EMAs
            _fastEMA = EMA(_btcusdSymbol, 30, Resolution.Minute);
            _slowEMA = EMA(_btcusdSymbol, 70, Resolution.Minute);
            _lastPurchasePrice = 0m;
            // Créez le graphique
            var tradePlot = new Chart(_ChartName);
            tradePlot.AddSeries(new Series(_PriceSeriesName, SeriesType.Line));
            tradePlot.AddSeries(new Series(_PortfolioValueSeriesName, SeriesType.Line));
            AddChart(tradePlot);
        }
        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey(_btcusdSymbol)) return;
            if (Portfolio.CashBook["BTC"].ConversionRate == 0)
            {
                Log($"BTC conversion rate: {Portfolio.CashBook["BTC"].ConversionRate}");
                throw new Exception("Conversion rate is 0");
            }
            // EMA Crossover Strategy for Buying
            if (_fastEMA > _slowEMA && !Portfolio.Invested)
            {
                var buyQuantity = CalculateFibonacciBuyQuantity();
                if (buyQuantity > 0)
                {
                    SetHoldings(_btcusdSymbol, 0.10); // Invest only 10% of total portfolio value
                    _lastPurchasePrice = Securities[_btcusdSymbol].Price;
                }
            }
            // EMA Crossover Strategy for Selling
            else if (_fastEMA < _slowEMA && Portfolio.Invested && Portfolio[_btcusdSymbol].Quantity > 0.0001m)
            {
                var sellQuantity = CalculateFibonacciSellQuantity();
                MarketOrder(_btcusdSymbol, -sellQuantity); // Sell a quantity based on Fibonacci
            }
            // Stop Loss Logic
            if (Portfolio[_btcusdSymbol].Invested && Securities[_btcusdSymbol].Price < _lastPurchasePrice * StopLossPercentage)
            {
                Liquidate(_btcusdSymbol); // Sell BTC
            }
            // Mise à jour des séries de graphiques
            Plot(_ChartName, _PriceSeriesName, Securities[_btcusdSymbol].Price);
            Plot(_ChartName, _PortfolioValueSeriesName, Portfolio.TotalPortfolioValue);
        }
        private decimal CalculateFibonacciBuyQuantity()
        {
            var fibonacciLevelForBuying = 0.618m; // Niveau de Fibonacci ajusté pour l'achat
            var usdAvailable = Portfolio.TotalPortfolioValue * 0.10m; // 10% du total du portefeuille
            var currentPrice = Securities[_btcusdSymbol].Price;
            var maxBtcQuantity = usdAvailable / currentPrice;
            return maxBtcQuantity;
        }
        private decimal CalculateFibonacciSellQuantity()
        {
            var fibonacciLevelForSelling = 0.5m; // Niveau de Fibonacci ajusté pour la vente
            var quantity = Portfolio[_btcusdSymbol].Quantity * fibonacciLevelForSelling;
            return quantity;
        }
        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Debug($"{Time} {orderEvent}");
        }
        public override void OnEndOfAlgorithm()
        {
            Log($"{Time} - TotalPortfolioValue: {Portfolio.TotalPortfolioValue}");
            Log($"{Time} - CashBook: {Portfolio.CashBook}");
        }
    }
}
