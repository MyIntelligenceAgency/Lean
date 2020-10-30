﻿/*
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
 *
*/

using System;
using Newtonsoft.Json;
using NUnit.Framework;
using QuantConnect.Optimizer;
using QuantConnect.Util;
using System.Collections.Generic;
using System.Threading;
using QuantConnect.Configuration;

namespace QuantConnect.Tests.Optimizer
{
    [TestFixture]
    public class LeanOptimizerTests
    {
        [Test]
        public void MaximizeNoTarget()
        {
            var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                Criterion = new Target("Profit", new Maximization(), null),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1),
                    new OptimizationStepParameter("ema-fast", 10, 100, 3)
                },
                MaximumConcurrentBacktests = 20
            };
            var optimizer = new FakeLeanOptimizer(packet);

            OptimizationResult result = null;
            optimizer.Ended += (s, solution) =>
            {
                result = solution;
                optimizer.DisposeSafely();
                resetEvent.Set();
            };

            optimizer.Start();

            resetEvent.WaitOne();
            Assert.NotNull(result);
            Assert.AreEqual(
                110,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Profit);

            Assert.AreEqual(10, result.ParameterSet.Value["ema-slow"].ToDecimal());
            Assert.AreEqual(100, result.ParameterSet.Value["ema-fast"].ToDecimal());
        }

        [Test]
        public void MinimizeWithTarget()
        {
            var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                Criterion = new Target("Profit", new Minimization(), 20),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1),
                    new OptimizationStepParameter("ema-fast", 10, 100, 3)
                },
                MaximumConcurrentBacktests = 20
            };
            var optimizer = new FakeLeanOptimizer(packet);

            OptimizationResult result = null;
            optimizer.Ended += (s, solution) =>
            {
                result = solution;
                optimizer.DisposeSafely();
                resetEvent.Set();
            };

            optimizer.Start();

            resetEvent.WaitOne();
            Assert.NotNull(result);
            Assert.GreaterOrEqual(
                20,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Profit);
        }

        [Test]
        public void MaximizeWithConstraints()
        {
            var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                Criterion = new Target("Profit", new Maximization(), null),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1m),
                    new OptimizationStepParameter("ema-fast", 10, 100, 3m)
                },
                Constraints = new List<Constraint>
                {
                    new Constraint("Drawdown", ComparisonOperatorTypes.LessOrEqual, 0.15m)
                },
                MaximumConcurrentBacktests = 20
            };
            var optimizer = new FakeLeanOptimizer(packet);

            OptimizationResult result = null;
            optimizer.Ended += (s, solution) =>
            {
                result = solution;
                optimizer.DisposeSafely();
                resetEvent.Set();
            };

            optimizer.Start();

            resetEvent.WaitOne();
            Assert.NotNull(result);
            Assert.AreEqual(
                15,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Profit);
            Assert.AreEqual(
                0.15m,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Drawdown);

        }

        [Test]
        public void MinimizeWithTargetAndConstraints()
        {
            var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                Criterion = new Target("Profit", new Minimization(), 20),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1),
                    new OptimizationStepParameter("ema-fast", 10, 100, 3)
                },
                Constraints = new List<Constraint>
                {
                    new Constraint("Drawdown", ComparisonOperatorTypes.LessOrEqual, 0.15m)
                },
                MaximumConcurrentBacktests = 20
            };
            var optimizer = new FakeLeanOptimizer(packet);

            OptimizationResult result = null;
            optimizer.Ended += (s, solution) =>
            {
                result = solution;
                optimizer.DisposeSafely();
                resetEvent.Set();
            };

            optimizer.Start();

            resetEvent.WaitOne();
            Assert.NotNull(result);
            Assert.GreaterOrEqual(
                20,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Profit);
            Assert.GreaterOrEqual(
                0.15m,
                JsonConvert.DeserializeObject<BacktestResult>(result.JsonBacktestResult).Statistics.Drawdown);

        }

        [Test]
        public void TrackEstimation()
        {
            Config.Set("optimization-update-interval", 1);
            OptimizationEstimate estimate = null;
            OptimizationResult result = null;
            var resetEvent = new ManualResetEvent(false);
            var packet = new OptimizationNodePacket
            {
                Criterion = new Target("Profit", new Minimization(), null),
                OptimizationParameters = new HashSet<OptimizationParameter>
                {
                    new OptimizationStepParameter("ema-slow", 1, 10, 1),
                    new OptimizationStepParameter("ema-fast", 10, 100, 3)
                },
                Constraints = new List<Constraint>
                {
                    new Constraint("Drawdown", ComparisonOperatorTypes.LessOrEqual, 0.15m)
                },
                MaximumConcurrentBacktests = 5
            };
            var optimizer = new FakeLeanOptimizer(packet);
            // keep stats up-to-date
            int totalBacktest = optimizer.GetCurrentEstimate().TotalBacktest;
            int totalUpdates = 0;
            int completedTests = 0;
            int failed = 0;
            optimizer.Update += (s, e) =>
            {
                estimate = optimizer.GetCurrentEstimate();
                Assert.LessOrEqual(estimate.RunningBacktest, packet.MaximumConcurrentBacktests);
                Assert.LessOrEqual(completedTests, estimate.CompletedBacktest);
                Assert.LessOrEqual(failed, estimate.FailedBacktest);

                Assert.AreEqual(totalBacktest, estimate.TotalBacktest);

                completedTests = estimate.CompletedBacktest;
                failed = estimate.FailedBacktest;

                if (completedTests > 0)
                {
                    Assert.Greater(estimate.AverageBacktest, TimeSpan.Zero);
                }

                totalUpdates++;
            };
            optimizer.Ended += (s, solution) =>
            {
                result = solution;
                estimate = optimizer.GetCurrentEstimate();
                optimizer.DisposeSafely();
                resetEvent.Set();
            };

            optimizer.Start();

            resetEvent.WaitOne();

            Assert.NotZero(estimate.CompletedBacktest);
            Assert.NotZero(estimate.FailedBacktest);
            // we have 2 force updates at least, expect a few more over it.
            Assert.Greater(totalUpdates, 2);
            Assert.AreEqual(estimate.CompletedBacktest + estimate.FailedBacktest + estimate.RunningBacktest, totalBacktest);
        }
    }
}