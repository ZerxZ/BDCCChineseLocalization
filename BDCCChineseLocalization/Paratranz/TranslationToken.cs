using Newtonsoft.Json;

namespace BDCCChineseLocalization.Paratranz
{
    
    public class TranslationToken
    {
        public static TranslationToken Create(string key, string original, string translation, string? context = null)
        {
            return new TranslationToken
            {
                Key = key,
                Original = original,
                Translation = translation,
                Context = context
            };
        }
        public static TranslationToken Create(string key, string original, string translation)
        {
            return new TranslationToken
            {
                Key = key,
                Original = original,
                Translation = translation,
            };
        }
        public static TranslationToken Create(string key, string original)
        {
            return new TranslationToken
            {
                Key = key,
                Original = original,
            };
        }
        public        string Key         { get; set; } = string.Empty;
        public        string Original    { get; set; } = string.Empty;
        public        string Translation { get; set; } = string.Empty;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Context     { get; set; } = null;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Type        { get; set; } = null;
        
    }
}