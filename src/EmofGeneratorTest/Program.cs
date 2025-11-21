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

            EmofRepository emofRepository = dotNetToEmofConverter.ConvertToEMOF(typeof(Repository));

            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

            string json = JsonConvert.SerializeObject(emofRepository, 
                                                      Formatting.Indented, 
                                                      new JsonSerializerSettings()
                                                      {
                                                          NullValueHandling = NullValueHandling.Ignore,
                                                          TypeNameHandling = TypeNameHandling.Auto
                                                      });

            File.WriteAllText("..\\..\\..\\emofModel.json", json);
        }
    }
}
