using System.Collections.Concurrent;
using GDShrapt.Reader;

namespace BDCCChineseLocalization.Paratranz;

public class TranslationHashIndex
{
    public ulong                               Index                                { get; set; } = 0;
    public ConcurrentDictionary<string, ulong> Indexes                              { get; set; } = new ConcurrentDictionary<string, ulong>();
    public ulong                               GetHashIndex(TranslationToken token) => GetHashIndex(token.HashId);
    public ulong GetHashIndex(string hashId)
    {
        if (Indexes.TryGetValue(hashId, out var index))
        {
            return index;
        }
        index = Index++;
        Indexes.TryAdd(hashId, index);
        return index;
    }
    // public string GetPrefix(string prefix, GDNode original, GDNode context)
    // {
    //     return prefix + "_" + GetHashIndex(HashHelper.GetMd5(original.ToString() + context.ToString()));
    // }
    // public string GetPrefix(string prefix, GDNode original)
    // {
    //     return prefix + "_" + GetHashIndex(HashHelper.GetMd5( original.ToString()));
    // }
}