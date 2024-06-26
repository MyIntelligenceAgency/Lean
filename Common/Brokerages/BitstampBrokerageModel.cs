/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using QuantConnect.Orders;
using QuantConnect.Securities;
using QuantConnect.Orders.Fees;
using System.Linq;
using QuantConnect.Benchmarks;
using System.Collections.Generic;
using QuantConnect.Util;

namespace QuantConnect.Brokerages
{
    /// <summary>
    /// Provides Bitstamp specific properties
    /// </summary>
    public class BitstampBrokerageModel : DefaultBrokerageModel
    {
        private readonly BrokerageMessageEvent _message = new BrokerageMessageEvent(BrokerageMessageType.Warning, 0,
            Messages.DefaultBrokerageModel.OrderUpdateNotSupported);


        private readonly HashSet<OrderType> _supportedOrderTypes = new()
        {
            OrderType.Limit,
            OrderType.Market,
            OrderType.StopMarket,
            OrderType.StopLimit,
            OrderType.TrailingStop
            
        };


        /// <summary>
        /// Gets a map of the default markets to be used for each security type
        /// </summary>
        public override IReadOnlyDictionary<SecurityType, string> DefaultMarkets { get; } = GetDefaultMarkets(Market.Bitstamp);


        /// <summary>
        /// Initializes a new instance of the <see cref="BitstampBrokerageModel"/> class
        /// </summary>
        /// <param name="accountType">The type of account to be modelled, defaults to
        /// <see cref="AccountType.Cash"/></param>
        public BitstampBrokerageModel(AccountType accountType = AccountType.Cash)
            : base(accountType)
        {
            if (accountType == AccountType.Margin)
            {
                throw new ArgumentException(Messages.BitstampBrokerageModel.UnsupportedAccountType, nameof(accountType));
            }
        }

        /// <summary>
        /// Bitstamp global leverage rule
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override decimal GetLeverage(Security security)
        {
            // margin trading is not currently supported by Bitstamp
            return 1m;
        }

        /// <summary>
        /// Get the benchmark for this model
        /// </summary>
        /// <param name="securities">SecurityService to create the security with if needed</param>
        /// <returns>The benchmark for this brokerage</returns>
        public override IBenchmark GetBenchmark(SecurityManager securities)
        {
            var symbol = Symbol.Create("BTCUSD", SecurityType.Crypto, Market.Bitstamp);
            return SecurityBenchmark.CreateInstance(securities, symbol);
        }

        /// <summary>
        /// Provides Bitstamp fee model
        /// </summary>
        /// <param name="security"></param>
        /// <returns></returns>
        public override IFeeModel GetFeeModel(Security security)
        {
            return new BitstampFeeModel();
        }

        /// <summary>
        /// Bitstamp does not support update of orders
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be updated</param>
        /// <param name="request">The requested update to be made to the order</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be updated</param>
        /// <returns>Bitstamp does not support update of orders, so it will always return false</returns>
        public override bool CanUpdateOrder(Security security, Order order, UpdateOrderRequest request, out BrokerageMessageEvent message)
        {
            message = _message;
            return false;
        }

        /// <summary>
        /// Evaluates whether exchange will accept order. Will reject order update
        /// </summary>
        /// <param name="security">The security of the order</param>
        /// <param name="order">The order to be processed</param>
        /// <param name="message">If this function returns false, a brokerage message detailing why the order may not be submitted</param>
        /// <returns>True if the brokerage could process the order, false otherwise</returns>
        public override bool CanSubmitOrder(Security security, Order order, out BrokerageMessageEvent message)
        {
            if (order.BrokerId != null && order.BrokerId.Any())
            {
                message = _message;
                return false;
            }

            if(!IsValidOrderSize(security, order.Quantity, out message))
            {
                return false;
            }

            if (security.Type != SecurityType.Crypto)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedSecurityType(this, security));

                return false;
            }

            if (!_supportedOrderTypes.Contains(order.Type))
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedOrderType(this, order, _supportedOrderTypes));

                return false;
            }

            

            if (order.TimeInForce != TimeInForce.GoodTilCanceled)
            {
                message = new BrokerageMessageEvent(BrokerageMessageType.Warning, "NotSupported",
                    Messages.DefaultBrokerageModel.UnsupportedTimeInForce(this, order));

                return false;
            }

            return base.CanSubmitOrder(security, order, out message);
        }

        /// <summary>
        /// Gets a new buying power model for the security, returning the default model with the security's configured leverage.
        /// For cash accounts, leverage = 1 is used.
        /// </summary>
        /// <param name="security">The security to get a buying power model for</param>
        /// <returns>The buying power model for this brokerage/security</returns>
        public override IBuyingPowerModel GetBuyingPowerModel(Security security)
        {
            // margin trading is not currently supported by Bitstamp
            return new CashBuyingPowerModel();
        }

        protected static IReadOnlyDictionary<SecurityType, string> GetDefaultMarkets(string marketName)
        {
            var map = DefaultMarketMap.ToDictionary();
            map[SecurityType.Crypto] = marketName;
            return map.ToReadOnlyDictionary();
        }

        

    }
}
