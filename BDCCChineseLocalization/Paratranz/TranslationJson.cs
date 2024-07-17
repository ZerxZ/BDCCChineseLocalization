// using System.Collections.Concurrent;
// using GDShrapt.Reader;
// using Newtonsoft.Json;
//
// namespace BDCCChineseLocalization.Paratranz;
//
// public class TranslationJson
// {
//     public TranslationJson(List<TranslationToken>? translationTokens, string? prefix = default)
//     {
//     }
//     public TranslationJson(string? prefix = default)
//     {
//         Prefix = prefix ?? "0";
//     }
//     public string Prefix { get; }
//
//     public List<List<GDNodeInfo>> Tokens { get; set; } = new List<List<GDNodeInfo>>();
//     [JsonIgnore]
//     public Dictionary<GDNode, List<GDNodeInfo>> GDNodeData { get; set; } = new Dictionary<GDNode, List<GDNodeInfo>>();
//     [JsonIgnore]
//     public bool IsEmpty => Tokens.Count == 0;
//     [JsonIgnore]
//     public int Count => Tokens.Count;
//     [JsonIgnore]
//     public int Index => Tokens.Count - 1;
//
//     public List<TranslationToken> GetTokens()
//     {
//       
//         return Tokens.SelectMany(t => t.Select(x => x.Token)).ToList();
//     }
//     public void AddToken(GDNodeInfo token, bool isMultiple = true)
//     {
//        
//         var key = Prefix + "_" + Count;
//
//
//         if (GDNodeData.TryGetValue(token.Original, out var list))
//         {
//             list.Add(token);
//             token.SetToken(key + "_" + (list.Count - 1));
//             return;
//         }
//         if (isMultiple)
//         {
//             key += "_0";
//         }
//         var gdNodeInfos = new List<GDNodeInfo>
//         {
//             token
//         };
//         Tokens.Add(gdNodeInfos);
//         GDNodeData.TryAdd(token.Original, gdNodeInfos);
//         token.SetToken(key);
//     }
// }