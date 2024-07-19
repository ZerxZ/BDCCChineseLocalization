using Newtonsoft.Json;

namespace BDCCChineseLocalization.Paratranz
{

    public class TranslationToken
    {
        // public static TranslationToken Create(string key, string original, string translation, string? context = null)
        // {
        //     return new TranslationToken
        //     {
        //         Key = key,
        //         Original = original,
        //         Translation = translation,
        //         Context = context
        //     };
        // }
        // public static TranslationToken Create(string key, string original, string translation)
        // {
        //     return new TranslationToken
        //     {
        //         Key = key,
        //         Original = original,
        //         Translation = translation,
        //     };
        // }
        // public static TranslationToken Create(string key, string original)
        // {
        //     return new TranslationToken
        //     {
        //         Key = key,
        //         Original = original,
        //     };
        // }
        public static TranslationToken CreateToken(string original, string content)
        {
            return new TranslationToken()
            {
                Original = original,
                Context = content
            };
        }
        public string Key         { get; set; } = string.Empty;
        public string Original    { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Context { get; set; } = null;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Type { get; set; } = null;
        public string HashId => GetHashId();
        public void SetKey(string prefix,TranslationHashIndex hashIndex)
        {
            Key = $"{prefix}_{hashIndex.GetHashIndex(HashId)}";
        }
        public string GetHashId()
        {
            var context = Context ?? string.Empty;
            return HashHelper.GetMd5(Original + context);
        }


    }
}