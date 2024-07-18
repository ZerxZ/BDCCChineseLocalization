using System.Collections.Concurrent;
using System.Drawing;
using BDCCChineseLocalization.Paratranz;
using CommandDotNet.Tokens;
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
    public GDScriptReader Reader = new GDScriptReader();
    public static GDScriptParser Parse(string content, string? prefix = default)
    {
        var parser = new GDScriptParser(prefix);
        parser.SetClassDeclarationContent(content);
        return parser;
    }
    public static GDScriptParser ParseFile(string path, string? prefix = default)
    {
        var parser = new GDScriptParser(prefix);
        parser.SetClassDeclarationFile(path);
        return parser;
    }
    public GDScriptParser(string? prefix = default)
    {
        Prefix = prefix;
    }
    public void SetClassDeclarationContent(string content)
    {
        ClassDeclaration = Reader.ParseFileContent(content);
    }
    public void SetClassDeclarationFile(string path)
    {
        ClassDeclaration = Reader.ParseFile(path);
    }
    public GDScriptParser(GDClassDeclaration classDeclaration, string? prefix = default) : this(prefix)
    {
        ClassDeclaration = classDeclaration;
    }
    // public ulong                                    Index            { get; private set; } = 0;
    public List<TranslationToken>                   Tokens { get; private set; } = new List<TranslationToken>(512);
    public ConcurrentDictionary<string, GDNodeInfo> Nodes  { get; private set; } = new ConcurrentDictionary<string, GDNodeInfo>();
    // public TranslationJson                          TranslationJson  { get; }
    public GDClassDeclaration? ClassDeclaration { get; private set; }
    public string?             Prefix           { get; private set; } = null;
    // public bool                                     HasPrefix        => !string.IsNullOrEmpty(Prefix);
    // public string                                   Key              => HasPrefix ? $"{Prefix}_{Index}" : $"0_{Index}";

    public bool HasTokens => Tokens.Count > 0;


    public void AddToken(GDNode original)
    {

        if (original.FirstChildNode is GDReturnExpression or GDDualOperatorExpression)
        {
            switch (original.FirstChildNode)
            {
                case GDReturnExpression gdReturnExpression:
                    switch (gdReturnExpression.FirstChildNode)
                    {
                        case GDCallExpression:
                            break;
                        case GDStringExpression:
                            break;
                        case GDDictionaryInitializerExpression:
                            break;
                        default:
                            var gdNodeInfo = new GDNodeInfo(original, Prefix);
                            Tokens.Add(gdNodeInfo.Token);
                            Nodes.TryAdd(gdNodeInfo.Token.Key, gdNodeInfo);
                            return;
                    }
                    break;
                case GDDualOperatorExpression { RightExpression: not GDStringExpression and not GDDualOperatorExpression }:
                    break;
                default:
                {
                    var gdNodeInfo = new GDNodeInfo(original, Prefix);
                    Tokens.Add(gdNodeInfo.Token);
                    Nodes.TryAdd(gdNodeInfo.Token.Key, gdNodeInfo);
                    return;
                }
            }

        }

        var gdStringNodes = original.AllNodes.OfType<GDStringNode>().ToList();
        var gdNodeInfos   = new List<GDNodeInfo>();
        foreach (var gdStringNode in gdStringNodes)
        {

            var skip   = false;
            var parent = gdStringNode.Parent as GDStringExpression;
            foreach (var gdNode in gdStringNode.Parents)
            {
                if (gdNode is GDIndexerExpression)
                {
                    skip = true;
                    break;
                }
                if (gdNode is GDDictionaryKeyValueDeclaration gdDictionaryKeyValueDeclaration)
                {
                    if (gdDictionaryKeyValueDeclaration.Key == gdStringNode.Parent)
                    {
                        skip = true;
                        break;
                    }
                }
                if (gdNode is GDExpressionsList gdExpressionsList && gdExpressionsList.Parent is GDCallExpression gdCallExpression)
                {

                    var callExpression = gdCallExpression.CallerExpression;
                    if (callExpression is GDMemberOperatorExpression gdMemberOperatorExpression)
                    {
                        var identifier = gdMemberOperatorExpression.Identifier.ToString();
                        if (identifier == "connect")
                        {
                            var index = gdExpressionsList.IndexOf(parent);
                            if (index <= 2)
                            {
                                skip = true;
                                break;
                            }
                        }
                        else if (identifier is "get_node" or "get_node_or_null" or "has")
                        {
                            skip = true;
                            break;
                        }
                        else if (identifier is "replace")
                        {
                            var index = gdExpressionsList.IndexOf(parent);
                            if (index <= 2 && identifier.Length <= 2)
                            {
                                skip = true;
                                break;
                            }
                        }

                    }
                    if (callExpression is GDIdentifierExpression gdIdentifierExpression)
                    {
                        var identifier = gdIdentifierExpression.ToString();
                        if (identifier == "emit_signal")
                        {
                            var index = gdExpressionsList.IndexOf(parent);
                            if (index <= 0)
                            {
                                skip = true;
                                break;
                            }
                        }
                        else if (identifier is "Color" or "get_node" or "get_node_or_null" or "preload" or "load")
                        {
                            skip = true;
                            break;
                        }

                    }

                }
            }

            var parts = gdStringNode.Parts.ToString();

            if (parts.StartsWith("res://") || parts.StartsWith("user://") || string.IsNullOrWhiteSpace(parts))
            {
                continue;
            }
            if (skip)
            {
                continue;
            }
            var gdNodeInfo = new GDNodeInfo(original, gdStringNode, Prefix);
            Tokens.Add(gdNodeInfo.Token);
            Nodes.TryAdd(gdNodeInfo.Token.Key, gdNodeInfo);
        }

    }
    public void Translate(List<TranslationToken>? translationTokens)
    {

        if (Tokens.Count == 0 || translationTokens is null or { Count: <= 0 })
        {
            return;
        }
        var count = translationTokens.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            var token = translationTokens[i];
            if (Nodes.TryGetValue(token.Key, out var gdNodeInfo))
            {
                gdNodeInfo.ReplaceWith(token);
            }
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
                case GDVariableDeclarationStatement gdVariableDeclarationStatement:
                {
                    if (!gdVariableDeclarationStatement.HasStringNode())
                    {
                        continue;
                    }
                    AddToken(gdVariableDeclarationStatement);
                    break;
                }

            }
        }
    }
}