using System;
using System.Text.Json;
using Newtonsoft.Json;
using NUnit.Framework;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MyJetWallet.Sdk.WalletApi.Tests
{
    
    public class Class1
    {
        [Test]
        public void Test1()
        {
            var data = new Data()
            {
                Name = "data",
                V1 = 1m/3m,
                V2 = 1.0/3.0,
                V3 = 1111111
            };

            // var json = JsonSerializer.Serialize(data, typeof(Data), new JsonSerializerOptions()
            // {
            //     Converters =
            //     {
            //         new MyDoubleConverter()
            //     }
            // });
            //
            // var d = JsonSerializer.Deserialize<Data>(json, new JsonSerializerOptions()
            // {
            //     Converters =
            //     {
            //         new MyDoubleConverter()
            //     }
            // });
            
            //Console.WriteLine(json);

            // Console.WriteLine();
            // Console.WriteLine();
            //
            // Console.WriteLine(JsonConvert.SerializeObject(d, Formatting.Indented));
            //
            // Console.WriteLine();
            // Console.WriteLine();
            //
            // Console.WriteLine((data.V2 + data.V2 + data.V2) == 1);
            // Console.WriteLine((d.V2 + d.V2 + d.V2) == 1);
        }

    }

    public class Data
    {
        public string Name { get; set; }
        public decimal V1 { get; set; }
        public double V2 { get; set; }
        public int V3 { get; set; }
    }
}
