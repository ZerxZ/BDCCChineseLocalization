using System.Collections.Concurrent;
using BDCCChineseLocalization.Paratranz;
using GDShrapt.Reader;

namespace BDCCChineseLocalization;

internal static class GDShraptExtensions
{
    public static bool HasStringNode(this GDNode node)
    {
        return node.AllNodes.OfType<GDStringNode>().Any();
    }
}

public class GDScriptParser
{
    public static readonly GDScriptReader Reader = new GDScriptReader();
    public static GDScriptParser Parse(string context, string? prefix = default)
    {
        var parser = new GDScriptParser(Reader.ParseFileContent(context), prefix);
        return parser;
    }
    public static GDScriptParser ParseFile(string path, string? prefix = default)
    {
        var parser = new GDScriptParser(Reader.ParseFile(path), prefix);
        return parser;
    }
    public GDScriptParser(GDClassDeclaration classDeclaration, string? prefix = default)
    {
        Prefix = prefix ?? string.Empty;
        ClassDeclaration = classDeclaration;
    }
    public ulong                                Index            { get; private set; } = 0;
    public List<TranslationToken>               Tokens           { get; private set; } = new List<TranslationToken>(512);
    public ConcurrentDictionary<string, GDNode> Nodes            { get; private set; } = new ConcurrentDictionary<string, GDNode>();
    public GDClassDeclaration?                  ClassDeclaration { get; private set; }
    public string                               Prefix           { get; private set; }
    public bool                                 HasPrefix        => !string.IsNullOrEmpty(Prefix);
    public string                               Key              => HasPrefix ? $"{Prefix}_{Index}" : $"0_{Index}";

    public bool HasTokens => Tokens.Count > 0;
    public void AddToken(string original)
    {
       
        Tokens.Add(TranslationToken.Create(Key, original));
     
        Index++;
    }
    public void AddToken(GDNode original)
    { 
        Nodes.TryAdd(Key, original);
        AddToken(original.ToString());
    }
    public void Translate(List<TranslationToken> translationTokens)
    {
        if (Tokens.Count == 0)
        {
            return;
        }
        foreach (var token in translationTokens)
        {
            if (!Nodes.TryGetValue(token.Key, out var node)) continue;
            if (string.IsNullOrWhiteSpace(token.Translation))
            {
                continue;
            }
            node.Parent.Form.AddBeforeToken(Reader.ParseExpression(token.Translation),node);
            node.RemoveFromParent();
        }
    }
    
    public void Parse()
    {
        if (ClassDeclaration is null)
        {
            return;
        }
        foreach (var node in ClassDeclaration.AllNodes)
        {
            switch (node)
            {
                case GDIfStatement gdIfStatement:
                {
                    var ifBranchCondition = gdIfStatement.IfBranch.Condition;

                    if (ifBranchCondition.HasStringNode())
                    {
                        AddToken(ifBranchCondition);
                    }

                    if (!gdIfStatement.ElifBranchesList.HasTokens)
                    {
                        continue;
                    }
                    foreach (var elifBranch in gdIfStatement.ElifBranchesList)
                    {
                        var elifBranchCondition = elifBranch.Condition;
                        if (elifBranchCondition.HasStringNode())
                        {
                            AddToken(elifBranchCondition);
                        }
                    }
                    break;
                }
                case GDExpressionStatement gdExpressionStatement:
                {
                    if (!gdExpressionStatement.HasStringNode())
                    {
                        continue;
                    }
                    AddToken(gdExpressionStatement);
                    break;
                }

            }
        }
    }
}