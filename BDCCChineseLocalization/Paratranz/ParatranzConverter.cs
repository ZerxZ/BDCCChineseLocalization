using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BDCCChineseLocalization.Paratranz;

public static class ParatranzConverter
{
    public static readonly JsonSerializerSettings  JsonSerializerSettings = new JsonSerializerSettings()
    {
        Formatting = Formatting.Indented,
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };
    public static List<TranslationToken>? Deserialize(string json)
    {
        return JsonConvert.DeserializeObject<List<TranslationToken>>(json);
    }
    public static string Serialize(List<TranslationToken> tokens)
    {
        return JsonConvert.SerializeObject(tokens, JsonSerializerSettings);
    }
    public static List<TranslationToken>? LoadFile(string path)
    {
        return Deserialize(File.ReadAllText(path));
    }
 
    public static void WriteFile(string path, List<TranslationToken> tokens)
    {
        File.WriteAllText(path, Serialize(tokens));
    }
    
}