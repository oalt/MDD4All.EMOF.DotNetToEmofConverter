using CycloneDX.Models;
using KMRD.FunctionModules.DataModels;
using MDD4All.EMOF.DataModels;
using MDD4All.EMOF.DotNetToEmofConverter;
using MDD4All.Person.DataModels;
using Newtonsoft.Json;

namespace EmofGeneratorTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DotNetToEmofConverter dotNetToEmofConverter = new DotNetToEmofConverter();

            Type type = typeof(Bom);


            EmofRepository emofRepository = dotNetToEmofConverter.ConvertToEMOF(type);

            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

            string json = JsonConvert.SerializeObject(emofRepository, 
                                                      Formatting.Indented, 
                                                      new JsonSerializerSettings()
                                                      {
                                                          NullValueHandling = NullValueHandling.Ignore,
                                                          TypeNameHandling = TypeNameHandling.Auto
                                                      });

            string filename = type.FullName;

            File.WriteAllText("..\\..\\..\\" + filename + ".emof.json", json);
        }
    }
}
