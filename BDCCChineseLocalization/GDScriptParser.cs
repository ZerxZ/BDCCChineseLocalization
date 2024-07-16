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

public class GDNodeInfo
{
    public GDNodeInfo(GDNode node, TranslationToken token)
    {
        Node = node;
        Token = token;
    }
    public GDNode           Node  { get; private set; }
    public TranslationToken Token { get; }
    public GDScriptReader   Reader { get; } = new GDScriptReader();
    public bool ReplaceWith(TranslationToken token)
    {
        if (string.IsNullOrWhiteSpace(token.Translation))
        {
            return false;
        }
        var nodeContext = Node.ToString();
        if (token.Translation == nodeContext)
        {
            return false;
        }
        if (token.Original != nodeContext)
        {
            return false;
        }

        return ReplaceWith(Reader.ParseExpression(token.Translation));

    }
    public bool ReplaceWith(GDNode newNode)
    {
        if (!newNode.HasTokens)
        {
            return false;
        }
        if (newNode.ToString() == Node.ToString())
        {
            return false;
        }
        Node.Parent.Form.AddBeforeToken(newNode, Node);
        Node.RemoveFromParent();
        Node = newNode;
        Token.Translation = newNode.ToString();
        return true;
    }
}

public class GDScriptParser
{
    public   GDScriptReader Reader = new GDScriptReader();
    public static GDScriptParser Parse(string content, string? prefix = default)
    {
        var parser = new GDScriptParser(prefix);
        parser.SetClassDeclarationContent(content);
        return parser;
    }
    public static GDScriptParser ParseFile(string path, string? prefix = default)
    {
        var parser = new GDScriptParser( prefix);
        parser.SetClassDeclarationFile(path);
        return parser;
    }
    public GDScriptParser(string? prefix = default)
    {
        Prefix = prefix ?? string.Empty;
    }
    public void SetClassDeclarationContent(string content)
    {
        ClassDeclaration = Reader.ParseFileContent(content);
    }
    public void SetClassDeclarationFile(string path)
    {
        ClassDeclaration = Reader.ParseFile(path);
    }
    public GDScriptParser(GDClassDeclaration classDeclaration, string? prefix = default)
    {
        Prefix = prefix ?? string.Empty;
        ClassDeclaration = classDeclaration;
    }
    public ulong                                    Index            { get; private set; } = 0;
    public List<TranslationToken>                   Tokens           { get; private set; } = new List<TranslationToken>(512);
    public ConcurrentDictionary<string, GDNodeInfo> Nodes            { get; private set; } = new ConcurrentDictionary<string, GDNodeInfo>();
    public GDClassDeclaration?                      ClassDeclaration { get; private set; }
    public string                                   Prefix           { get; private set; }
    public bool                                     HasPrefix        => !string.IsNullOrEmpty(Prefix);
    public string                                   Key              => HasPrefix ? $"{Prefix}_{Index}" : $"0_{Index}";

    public bool HasTokens => Tokens.Count > 0;
    public void AddToken(TranslationToken original)
    {

        Tokens.Add(original);

        Index++;
    }
    public void AddToken(string original)
    {

        var token = TranslationToken.Create(Key, original);
        Tokens.Add(token);

        Index++;
    }
    public void AddToken(GDNode original)
    {
        var info = new GDNodeInfo(original,        TranslationToken.Create(Key, original.ToString()));
        Nodes.TryAdd(Key, new GDNodeInfo(original, TranslationToken.Create(Key, original.ToString())));

        AddToken(info.Token);

    }
    public void Translate(TranslationToken token)
    {
        if (!Nodes.TryGetValue(token.Key, out var nodeInfo)) return;
        nodeInfo.ReplaceWith(token);
    }
    public void Translate(List<TranslationToken> translationTokens)
    {
        if (Tokens.Count == 0)
        {
            return;
        }

        foreach (var token in translationTokens)
        {
            Translate(token);
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