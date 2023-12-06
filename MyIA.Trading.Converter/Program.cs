using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using Utf8Json;
using Utf8Json.Formatters;
using Utf8Json.Resolvers;
//using System.Text.Json;

namespace MyIA.Trading.Converter
{
    class Program
    {
        //static async Task Main(string[] args)
        static void Main(string[] args)
        {
            RunningTime.Start();


            var config = GetConfig();

            Log("Config Loaded");

            config.Process(Log);
            //Benchmark(config);


            Console.WriteLine($"End > {RunningTime.Elapsed.TotalSeconds:##.#####}s");
            Console.WriteLine("Appuyez sur une touche pour fermer la fenêtre");
            Console.ReadKey();
        }


        static TradeConverter GetConfig()
        {

            var configFileName = Path.Combine(Environment.CurrentDirectory, "ConverterConfig.daily.json");
            CompositeResolver.RegisterAndSetAsDefault(new IJsonFormatter[] { new TimeSpanFormatter() }, new IJsonFormatterResolver[] { StandardResolver.Default });
            if (!File.Exists(configFileName))
            {
                var newConfig = new TradeConverter();
                var strNewConfig = JsonSerializer.PrettyPrint(JsonSerializer.ToJsonString(newConfig));
                File.WriteAllText(configFileName, strNewConfig);
            }

            using var configStream = File.OpenRead(configFileName);
            var config = JsonSerializer.Deserialize<TradeConverter>(configStream);
            return config;
        }


        static System.Diagnostics.Stopwatch RunningTime = new Stopwatch();

        private static TimeSpan _currentElpased = TimeSpan.Zero;

        public static void Log(string message)
        {
            var totalElapsed = RunningTime.Elapsed;
            var stepElapsed = totalElapsed - _currentElpased;
            _currentElpased = totalElapsed;
            Console.WriteLine($"+{stepElapsed.TotalSeconds:##.#####}s> {message}");

        }


    }


}
