using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace BDCCChineseLocalization.Paratranz;

public class TranslationHashIndexFile
{
    public static TranslationHashIndexFile Instance                                                     { get; } = new TranslationHashIndexFile();
    public static void                     SetDir(string                  dir)                          => Instance.Dir = dir;
    public static ulong                    GetHashIndex(string            path, TranslationToken token) => Instance.GetFileHashIndex(path, token);
    public static TranslationHashIndex     GetTranslationHashIndex(string path) => Instance.GetFileTranslationHashIndex(path);
    public TranslationHashIndex GetFileTranslationHashIndex(string path)
    {
        if (Files.TryGetValue(path, out var file))
        {
            return file;
        }
        file = new TranslationHashIndex();
        Files.TryAdd(path, file);
        return file;
    }
    public string                                             Dir      { get; set; } = string.Empty;
    public string                                             FileName { get; }      = "hash_index.json";
    public string                                             FilePath => Path.Combine(Dir, FileName);
    public ConcurrentDictionary<string, TranslationHashIndex> Files    { get; set; } = new ConcurrentDictionary<string, TranslationHashIndex>();
    public void Save()
    {
        var path = Path.Combine(Dir, FileName);
        var json = JsonConvert.SerializeObject(Files, Formatting.None);
        File.WriteAllText(path, json);
    }
    public void Load()
    {
        var path = Path.Combine(Dir, FileName);
        if (!File.Exists(path))
        {
            Save();
            return;
        }
        var json = File.ReadAllText(path);
        Files = JsonConvert.DeserializeObject<ConcurrentDictionary<string, TranslationHashIndex>>(json) ?? new ConcurrentDictionary<string, TranslationHashIndex>();

        foreach (var (key, translationHashIndex) in Files)
        {
            if (key.StartsWith(Dir) || Path.GetExtension(key) is not ".tscn" and not ".gd")
            {
                Files.TryRemove(key, out _);
                continue;
            }
            translationHashIndex.ClearPosition();
        }
    }

    public ulong GetFileHashIndex(string path, TranslationToken token)
    {

        return GetTranslationHashIndex(path).GetHashIndex(token);
    }

}