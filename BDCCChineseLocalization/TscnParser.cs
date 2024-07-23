using System.Text.RegularExpressions;
using BDCCChineseLocalization.Paratranz;

namespace BDCCChineseLocalization;

public partial class TscnParser
{
    public TscnParser(string content, string filepath)
    {
        Content = content;
        Filepath = filepath;
        TranslationHashIndex = TranslationHashIndexFile.GetTranslationHashIndex(filepath);
    }
    public         string                 Content  { get; set; }
    public         string                 Filepath { get; set; }
    public         List<TranslationToken> Tokens   { get; set; } = new List<TranslationToken>();
    [GeneratedRegex(pattern: """
                             \s*(text|hint_tooltip)\s*=\s*"(.*)"
                             """,RegexOptions.IgnoreCase |RegexOptions.Multiline)]
    public partial Regex                  GetTextRegex();
    public TranslationHashIndex                           TranslationHashIndex { get; private set; }
    public bool HasTokens => Tokens.Count > 0;
    public void Parse()
    {
        var lines = Content.Replace("\r\n","\n").Split('\n');
        var regex = GetTextRegex();
        for (int i = 0; i < lines.Length; i++)
        {
            var line  = lines[i];
            var match = regex.Match(line);
            if (match.Success)
            {
                // Console.WriteLine(match.Groups[2].Value);
                var text  = match.Groups[2].Value;
                var token = TranslationToken.CreateToken(text, line);
                token.Nodes.Add(new TokenPosition
                {
                    StartLine   = i,
                    EndLine     = i,
                    StartColumn = match.Groups[2].Index,
                    EndColumn   = match.Groups[2].Index + match.Groups[2].Length
                });
                token.SetKey(Filepath, TranslationHashIndex);
              
                Tokens.Add(token);
            }
        }
    }
}