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

using Newtonsoft.Json.Linq;
using NUnit.Framework;
using QuantConnect.Lean.Engine.DataFeeds.Enumerators;
using QuantConnect.Optimizer;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using Math = System.Math;
using OptimizationParameter = QuantConnect.Optimizer.OptimizationParameter;

namespace QuantConnect.Tests.Optimizer.Strategies
{
    [TestFixture, Parallelizable(ParallelScope.Fixtures)]
    public class GridSearchOptimizationStrategyTests
    {
        private GridSearchOptimizationStrategy _strategy;
        private Func<ParameterSet, decimal> _profit = parameterSet => parameterSet.Value.Sum(arg => arg.Value.ToDecimal());
        private Func<ParameterSet, decimal> _drawdown = parameterSet => parameterSet.Value.Sum(arg => arg.Value.ToDecimal()) / 100.0m;
        private Func<string, string, decimal> _parse = (dump, parameter) => JObject.Parse(dump).SelectToken($"Statistics.{parameter}").Value<decimal>();
        private Func<decimal, decimal, string> _stringify = (profit, drawdown) => BacktestResult.Create(profit, drawdown).ToJson();
        private HashSet<OptimizationParameter> _optimizationParameters = new HashSet<OptimizationParameter>
        {
            new OptimizationStepParameter("ema-slow", 1, 5, 1),
            new OptimizationStepParameter("ema-fast", 3, 6, 2)
        };

        [SetUp]
        public void Init()
        {
            _strategy = new GridSearchOptimizationStrategy();
            _strategy.NewParameterSet += (s, e) =>
            {
                var parameterSet = (e as OptimizationEventArgs)?.ParameterSet;
                _strategy.PushNewResults(new OptimizationResult(_stringify(_profit(parameterSet), _drawdown(parameterSet)), parameterSet, ""));
            };
        }

        private static TestCaseData[] StrategySettings => new[]
        {
            new TestCaseData(new Maximization(), 10),
            new TestCaseData(new Minimization(), 1)
        };

        [Test, TestCaseSource(nameof(StrategySettings))]
        public void StepInsideNoTargetNoConstraints(Extremum extremum, int bestSet)
        {
            ParameterSet solution = null;
            _strategy.Initialize(
                new Target("Profit", extremum, null),
                null,
                _optimizationParameters,
                new OptimizationStrategySettings());
            _strategy.NewParameterSet += (s, e) =>
            {
                var parameterSet = (e as OptimizationEventArgs)?.ParameterSet;
                if (parameterSet.Id == bestSet)
                {
                    solution = parameterSet;
                }
            };

            _strategy.PushNewResults(OptimizationResult.Initial);

            Assert.AreEqual(_profit(solution), _parse(_strategy.Solution.JsonBacktestResult, "Profit"));
            foreach (var arg in _strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution.Value[arg.Key], arg.Value);
            }
        }

        [TestCase(1, 0.05)]
        [TestCase(3, 0.06)]
        public void StepInsideWithConstraints(int bestSet, double drawdown)
        {
            ParameterSet solution = null;
            _strategy.Initialize(
                new Target("Profit", new Maximization(), null),
                new List<Constraint> { new Constraint("Drawdown", ComparisonOperatorTypes.Less, new decimal(drawdown)) },
                _optimizationParameters,
                new OptimizationStrategySettings());
            _strategy.NewParameterSet += (s, e) =>
            {
                var parameterSet = (e as OptimizationEventArgs)?.ParameterSet;
                if (parameterSet.Id == bestSet)
                {
                    solution = parameterSet;
                }
            };

            _strategy.PushNewResults(OptimizationResult.Initial);

            Assert.AreEqual(_profit(solution), _parse(_strategy.Solution.JsonBacktestResult, "Profit"));
            Assert.AreEqual(_drawdown(solution), _parse(_strategy.Solution.JsonBacktestResult, "Drawdown"));
            foreach (var arg in _strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution.Value[arg.Key], arg.Value);
            }
        }

        [TestCase(1, 0)]
        [TestCase(1, 4)]
        [TestCase(2, 5)]
        [TestCase(6, 8)]
        public void StepInsideWithTarget(int bestSet, double targetValue)
        {
            bool reached = false;
            ParameterSet parameterSet = null;
            var target = new Target("Profit", new Maximization(), new decimal(targetValue));
            target.Reached += (s, e) =>
            {
                reached = true;
            };

            _strategy = new GridSearchOptimizationStrategy();

            _strategy.Initialize(
                target,
                null,
                _optimizationParameters,
                new OptimizationStrategySettings());

            var queue = new Queue<ParameterSet>();
            _strategy.NewParameterSet += (s, e) =>
            {
                queue.Enqueue((e as OptimizationEventArgs)?.ParameterSet);
            };

            _strategy.PushNewResults(OptimizationResult.Initial);

            while (queue.Any())
            {
                parameterSet = queue.Dequeue();
                var newResult = new OptimizationResult(_stringify(_profit(parameterSet), _drawdown(parameterSet)), parameterSet, "");
                _strategy.PushNewResults(newResult);
                if (reached)
                {
                    break;
                }
            }

            Assert.IsTrue(reached);
            Assert.AreEqual(bestSet, parameterSet.Id);
            Assert.AreEqual(_profit(parameterSet), _parse(_strategy.Solution.JsonBacktestResult, "Profit"));
            foreach (var arg in _strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(parameterSet.Value[arg.Key], arg.Value);
            }
        }

        [Test, TestCaseSource(nameof(StrategySettings))]
        public void FindBestNoConstraints(Extremum extremum, int bestSet)
        {
            var emaSlow = _optimizationParameters.First(s => s.Name == "ema-slow") as OptimizationStepParameter;
            var emaFast = _optimizationParameters.First(s => s.Name == "ema-fast") as OptimizationStepParameter;

            _strategy.Initialize(
                new Target("Profit", extremum, null),
                null,
                _optimizationParameters,
                new OptimizationStrategySettings());
            ParameterSet solution = null;
            int i = 0;
            for (var slow = emaSlow.MinValue; slow <= emaSlow.MaxValue; slow += emaSlow.Step)
            {
                for (var fast = emaFast.MinValue; fast <= emaFast.MaxValue; fast += emaFast.Step)
                {
                    var parameterSet = new ParameterSet(++i, new Dictionary<string, string>
                    {
                        {emaSlow.Name, slow.ToStringInvariant()},
                        {emaFast.Name, fast.ToStringInvariant()}
                    });
                    _strategy.PushNewResults(new OptimizationResult(_stringify(_profit(parameterSet), _drawdown(parameterSet)), parameterSet, ""));
                    if (parameterSet.Id == bestSet)
                    {
                        solution = parameterSet;
                    }
                }
            }

            Assert.AreEqual(_profit(solution), _parse(_strategy.Solution.JsonBacktestResult, "Profit"));
            foreach (var arg in _strategy.Solution.ParameterSet.Value)
            {
                Assert.AreEqual(solution?.Value[arg.Key], arg.Value);
            }
        }

        [Test]
        public void ThrowOnReinitialization()
        {
            int nextId = 1;
            _strategy.NewParameterSet += (s, e) =>
            {
                Assert.AreEqual(nextId++, (e as OptimizationEventArgs).ParameterSet.Id);
            };

            var set1 = new HashSet<OptimizationParameter>()
            {
                new OptimizationStepParameter("ema-fast", 10, 100, 1)
            };
            _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), set1, new OptimizationStrategySettings());

            _strategy.PushNewResults(OptimizationResult.Initial);
            Assert.Greater(nextId, 1);

            var set2 = new HashSet<OptimizationParameter>()
            {
                new OptimizationStepParameter("ema-fast", 10, 100, 1),
                new OptimizationStepParameter("ema-slow", 10, 100, 2)
            };
            Assert.Throws<InvalidOperationException>(() =>
            {
                _strategy.Initialize(new Target("Profit", new Minimization(), null), null, set2, new OptimizationStrategySettings());
            });
        }

        [Test]
        public void ThrowIfNotInitialized()
        {
            var strategy = new GridSearchOptimizationStrategy();
            Assert.Throws<InvalidOperationException>(() =>
            {
                strategy.PushNewResults(OptimizationResult.Initial);
            });
        }

        [TestFixture]
        public class GridSearchTests
        {
            private GridSearchOptimizationStrategy _strategy;

            [SetUp]
            public void Init()
            {
                this._strategy = new GridSearchOptimizationStrategy();
            }

            [TestCase(0)]
            [TestCase(1)]
            [TestCase(-2.5)]
            public void SinglePoint(decimal step)
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", 0, 0, step),
                    new OptimizationStepParameter("ema-slow", 0, 0, step),
                    new OptimizationStepParameter("ema-custom", 1, 1, step)
                };

                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), args, new OptimizationStrategySettings());

                _strategy.NewParameterSet += (s, e) =>
                {
                    var parameterSet = (e as OptimizationEventArgs).ParameterSet;
                    Assert.AreEqual("0", parameterSet.Value["ema-fast"]);
                    Assert.AreEqual("0", parameterSet.Value["ema-slow"]);
                    Assert.AreEqual("1", parameterSet.Value["ema-custom"]);
                };

                _strategy.PushNewResults(OptimizationResult.Initial);
            }

            [TestCase(-10, 0, -1)]
            [TestCase(-10, 10.5, -0.5)]
            [TestCase(10, 100, 1)]
            [TestCase(10, 100, 500)]
            public void Step1D(decimal min, decimal max, decimal step)
            {
                var param = new OptimizationStepParameter("ema-fast", min, max, step);
                var set = new HashSet<OptimizationParameter>() { param };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), set, new OptimizationStrategySettings());
                var counter = 0;

                using (var enumerator = new EnqueueableEnumerator<ParameterSet>())
                {
                    _strategy.NewParameterSet += (s, e) =>
                    {
                        enumerator.Enqueue((e as OptimizationEventArgs)?.ParameterSet);
                    };

                    _strategy.PushNewResults(OptimizationResult.Initial);

                    for (var v = param.MinValue; v <= param.MaxValue; v += param.Step)
                    {
                        counter++;
                        Assert.IsTrue(enumerator.MoveNext());

                        var suggestion = enumerator.Current;

                        Assert.IsNotNull(suggestion);
                        Assert.IsTrue(suggestion.Value.All(s => set.Any(arg => arg.Name == s.Key)));
                        Assert.AreEqual(1, suggestion.Value.Count);
                        Assert.AreEqual(v.ToStringInvariant(), suggestion.Value["ema-fast"]);
                    }

                    Assert.AreEqual(0, enumerator.Count);
                }

                Assert.Greater(counter, 0);
                Assert.AreEqual(Math.Floor((param.MaxValue - param.MinValue) / param.Step) + 1, counter);
            }

            [TestCase(1, 1, 1)]
            [TestCase(-10, 0, -1)]
            [TestCase( 0, -10, -1)]
            [TestCase(-10, 10.5, -0.5)]
            [TestCase(10, 100, 1)]
            [TestCase(10, 100, 500)]
            public void Estimate1D(decimal min, decimal max, decimal step)
            {
                var param = new OptimizationStepParameter("ema-fast", min, max, step);
                var set = new HashSet<OptimizationParameter>() { param };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), set, new OptimizationStrategySettings());
                
                Assert.AreEqual(Math.Floor(Math.Abs(max - min) / Math.Abs(step)) + 1, _strategy.GetTotalBacktestEstimate());
            }

            private static TestCaseData[] OptimizationStepParameter2D => new[]{
                new TestCaseData(new decimal[,] {{10, 100, 1}, {20, 200, 1}}),
                new TestCaseData(new decimal[,] {{10.5m, 100.5m, 1.5m}, { 20m, 209.9m, 3.5m}}),
                new TestCaseData(new decimal[,] {{ -10.5m, 0m, -1.5m }, { -209.9m, -20m, -3.5m } }),
                new TestCaseData(new decimal[,] {{ 10.5m, 0m, 1.5m }, { 209.9m, -20m, -3.5m } })
            };

            [Test, TestCaseSource(nameof(OptimizationStepParameter2D))]
            public void Step2D(decimal[,] data)
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", data[0,0], data[0,1], data[0,2]),
                    new OptimizationStepParameter("ema-slow", data[1,0], data[1,1], data[1,2])
                };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), args, new OptimizationStrategySettings());
                var counter = 0;
                using (var enumerator = new EnqueueableEnumerator<ParameterSet>())
                {
                    _strategy.NewParameterSet += (s, e) =>
                    {
                        enumerator.Enqueue((e as OptimizationEventArgs)?.ParameterSet);
                    };

                    _strategy.PushNewResults(OptimizationResult.Initial);

                    var fastParam = args.First(arg => arg.Name == "ema-fast") as OptimizationStepParameter;
                    var slowParam = args.First(arg => arg.Name == "ema-slow") as OptimizationStepParameter;
                    for (var fast = fastParam.MinValue; fast <= fastParam.MaxValue; fast += fastParam.Step)
                    {
                        for (var slow = slowParam.MinValue; slow <= slowParam.MaxValue; slow += slowParam.Step)
                        {
                            counter++;
                            Assert.IsTrue(enumerator.MoveNext());

                            var suggestion = enumerator.Current;

                            Assert.IsNotNull(suggestion);
                            Assert.IsTrue(suggestion.Value.All(s => args.Any(arg => arg.Name == s.Key)));
                            Assert.AreEqual(2, suggestion.Value.Count);
                            Assert.AreEqual(fast.ToStringInvariant(), suggestion.Value["ema-fast"]);
                            Assert.AreEqual(slow.ToStringInvariant(), suggestion.Value["ema-slow"]);
                        }
                    }

                    Assert.AreEqual(0, enumerator.Count);
                }

                Assert.Greater(counter, 0);

                var total = 1m;
                foreach (var arg in args.Cast<OptimizationStepParameter>())
                {
                    total *= Math.Floor((arg.MaxValue - arg.MinValue) / arg.Step) + 1;
                }

                Assert.AreEqual(total, counter);
            }

            [Test, TestCaseSource(nameof(OptimizationStepParameter2D))]
            public void Estimate2D(decimal[,] data)
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", data[0,0], data[0,1], data[0,2]),
                    new OptimizationStepParameter("ema-slow", data[1,0], data[1,1], data[1,2])
                };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), args, new OptimizationStrategySettings());

                var total = 1m;
                foreach (var arg in args.Cast<OptimizationStepParameter>())
                {
                    total *= Math.Floor((arg.MaxValue - arg.MinValue) / arg.Step) + 1;
                }

                Assert.AreEqual(total, _strategy.GetTotalBacktestEstimate());
            }

            [Test]
            public void Step3D()
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", 10, 100, 1),
                    new OptimizationStepParameter("ema-slow", 20, 200, 4),
                    new OptimizationStepParameter("ema-custom", 30, 300, 2)
                };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), null, args, new OptimizationStrategySettings());
                var counter = 0;

                using (var enumerator = new EnqueueableEnumerator<ParameterSet>())
                {
                    _strategy.NewParameterSet += (s, e) =>
                    {
                        enumerator.Enqueue((e as OptimizationEventArgs)?.ParameterSet);
                    };

                    _strategy.PushNewResults(OptimizationResult.Initial);

                    var fastParam = args.First(arg => arg.Name == "ema-fast") as OptimizationStepParameter;
                    var slowParam = args.First(arg => arg.Name == "ema-slow") as OptimizationStepParameter;
                    var customParam = args.First(arg => arg.Name == "ema-custom") as OptimizationStepParameter;
                    for (var fast = fastParam.MinValue; fast <= fastParam.MaxValue; fast += fastParam.Step)
                    {
                        for (var slow = slowParam.MinValue; slow <= slowParam.MaxValue; slow += slowParam.Step)
                        {
                            for (var custom = customParam.MinValue; custom <= customParam.MaxValue; custom += customParam.Step)
                            {
                                counter++;
                                Assert.IsTrue(enumerator.MoveNext());

                                var suggestion = enumerator.Current;

                                Assert.IsNotNull(suggestion);
                                Assert.IsTrue(suggestion.Value.All(s => args.Any(arg => arg.Name == s.Key)));
                                Assert.AreEqual(3, suggestion.Value.Count());
                                Assert.AreEqual(fast.ToStringInvariant(), suggestion.Value["ema-fast"]);
                                Assert.AreEqual(slow.ToStringInvariant(), suggestion.Value["ema-slow"]);
                                Assert.AreEqual(custom.ToStringInvariant(), suggestion.Value["ema-custom"]);
                            }
                        }
                    }

                    Assert.AreEqual(0, enumerator.Count);
                }

                Assert.Greater(counter, 0);

                var total = 1m;
                foreach (var arg in args.Cast<OptimizationStepParameter>())
                {
                    total *= (arg.MaxValue - arg.MinValue) / arg.Step + 1;
                }

                Assert.AreEqual(total, counter);
            }

            [Test]
            public void Estimate3D()
            {
                var args = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", 10, 100, 1),
                    new OptimizationStepParameter("ema-slow", 20, 200, 4),
                    new OptimizationStepParameter("ema-custom", 30, 300, 2)
                };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), null, args, new OptimizationStrategySettings());
                
                var total = 1m;
                foreach (var arg in args.Cast<OptimizationStepParameter>())
                {
                    total *= (arg.MaxValue - arg.MinValue) / arg.Step + 1;
                }

                Assert.AreEqual(total, _strategy.GetTotalBacktestEstimate());
            }

            [Test]
            public void NoStackOverflowException()
            {
                var depth = 100;
                var args = new HashSet<OptimizationParameter>();

                for (int i = 0; i < depth; i++)
                {
                    args.Add(new OptimizationStepParameter($"ema-{i}", 10, 100, 1));
                }
                _strategy.Initialize(new Target("Profit", new Maximization(), null), new List<Constraint>(), args, new OptimizationStrategySettings());

                var counter = 0;
                _strategy.NewParameterSet += (s, e) =>
                {
                    counter++;
                    Assert.AreEqual(depth, (e as OptimizationEventArgs).ParameterSet.Value.Count);
                    if (counter == 10000)
                    {
                        throw new Exception("Break loop due to large amount of data");
                    }
                };

                Assert.Throws<Exception>(() =>
                {
                    _strategy.PushNewResults(OptimizationResult.Initial);
                });

                Assert.AreEqual(10000, counter);
            }

            [Test]
            public void IncrementParameterSetId()
            {
                int nextId = 1,
                    last = 1;

                var set = new HashSet<OptimizationParameter>()
                {
                    new OptimizationStepParameter("ema-fast", 10, 100, 1)
                };
                _strategy.Initialize(new Target("Profit", new Maximization(), null), null, set, new OptimizationStrategySettings());

                _strategy.NewParameterSet += (s, e) =>
                {
                    Assert.AreEqual(nextId++, (e as OptimizationEventArgs).ParameterSet.Id);
                };

                last = nextId;
                _strategy.PushNewResults(OptimizationResult.Initial);
                Assert.Greater(nextId, last);

                last = nextId;
                _strategy.PushNewResults(OptimizationResult.Initial);
                Assert.Greater(nextId, last);
            }
        }
    }
}