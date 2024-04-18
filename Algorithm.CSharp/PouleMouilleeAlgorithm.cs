using System;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Framework.Portfolio;
using QuantConnect.Algorithm.Framework.Selection;
using QuantConnect.Algorithm.Framework.Risk;
using QuantConnect.Algorithm.Framework.Execution;
using QuantConnect.Indicators;
using QLNet;
using QuantConnect.Brokerages;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Scheduling;
using System.Security.Cryptography;
using QuantConnect.Parameters;
using static QuantConnect.Messages;

namespace QuantConnect.Algorithm.CSharp
{
    public class PouleMouilleeAlgorithm : QCAlgorithm
    {
        private MovingAverageConvergenceDivergence _macd;
        private RelativeStrengthIndex _rsi;
        private Symbol _btcusd;
        private const decimal _tolerance = 0.0025m;
        public override void Initialize()
        {
            // Début de test
            SetStartDate(2013, 1, 1); 
            SetEndDate(2023, 12, 31); 

            // Capital initial
            SetCash(10000);
            SetCash("BTC", 1m);

            SetBrokerageModel(BrokerageName.Bitstamp, AccountType.Cash);

            _btcusd = AddCrypto("BTCUSD", Resolution.Daily).Symbol;
            _macd = MACD(_btcusd, 12, 26, 9, MovingAverageType.Exponential, Resolution.Daily, Field.Close);
            _rsi = RSI(_btcusd, 14, MovingAverageType.Exponential, Resolution.Daily);
        }

        public override void OnData(Slice data)
        {
            if (data == null || !_macd.IsReady || !_rsi.IsReady) return;

            var holdingsQuantity = Securities[_btcusd].Holdings.Quantity;
            var closingPrice = data.Bars[_btcusd].Close;

            if (holdingsQuantity == 0 && _rsi < 70)
            {
                if (_macd > _macd.Signal * (1 + _tolerance))
                {
                    SetHoldings(_btcusd, 1.0);
                    Debug($"Purchased BTC @{closingPrice}$/Btc");
                    Debug($"Portfolio: {Portfolio.Cash}$");
                    Debug($"Quantity: {Portfolio[_btcusd].Quantity}BTCs");
                    Debug($"Total Value: {Portfolio.TotalPortfolioValue}$");
                    Debug($"Total Fees: {Portfolio.TotalFees}");
                }
            }
            else if (_rsi >= 70 && holdingsQuantity > 0)
            {
                if (_macd < _macd.Signal)
                {
                    Liquidate(_btcusd);
                    Debug($"Sold BTC @{closingPrice}$/Btc");
                    Debug($"Portfolio: {Portfolio.Cash}$");
                    Debug($"Quantity: {Portfolio[_btcusd].Quantity}BTCs");
                    Debug($"Total Value: {Portfolio.TotalPortfolioValue}$");
                    Debug($"Total Fees: {Portfolio.TotalFees}");
                }
            }
        }
    }
}

