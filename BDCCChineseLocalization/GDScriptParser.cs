using System.Collections.Concurrent;
using System.Drawing;
using System.Reflection.Metadata;
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
    public static GDScriptParser Parse(string content, string? prefix = null)
    {
        var parser = new GDScriptParser();
        parser.Prefix = prefix ?? "0";
        parser.SetClassDeclarationContent(content);
        return parser;
    }
    public static GDScriptParser ParseFile(string path, string relativePath)
    {
        var parser = new GDScriptParser(relativePath);
        parser.Prefix =Path.GetFileNameWithoutExtension(path);
        parser.SetClassDeclarationFile(path);
        return parser;
    }
    public GDScriptParser()
    {
    }
    public GDScriptParser(string path)
    {
        Filepath = path;
        Prefix = Path.GetFileNameWithoutExtension(path);
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
        Prefix = prefix ?? "0";
        ClassDeclaration = classDeclaration;
    }
    public string Filepath { get; private set; } = string.Empty;
    // public ulong                                    Index            { get; private set; } = 0;
    public List<TranslationToken>                   Tokens { get; private set; } = new List<TranslationToken>(512);
    public ConcurrentDictionary<string, GDNodeInfo> Nodes  { get; private set; } = new ConcurrentDictionary<string, GDNodeInfo>();
    // public TranslationJson                          TranslationJson  { get; }
    public GDClassDeclaration? ClassDeclaration { get; private set; }
    public string              Prefix = string.Empty;
    // public bool                                     HasPrefix        => !string.IsNullOrEmpty(Prefix);
    // public string                                   Key              => HasPrefix ? $"{Prefix}_{Index}" : $"0_{Index}";

    public bool HasTokens => Tokens.Count > 0;

    public void AddToken(GDNode original)
    {
        if (original.FirstChildNode is GDReturnExpression or GDDualOperatorExpression or GDCallExpression)
        {
            GDNodeInfo gdNodeInfo;
            switch (original.FirstChildNode)
            {
                case GDReturnExpression gdReturnExpression:
                    switch (gdReturnExpression.FirstChildNode)
                    {
                        case GDCallExpression:
                        case GDStringExpression:
                        case GDDictionaryInitializerExpression:
                        case GDArrayInitializerExpression:
                            break;
                        default:
                            gdNodeInfo = new GDNodeInfo(original,Prefix , Filepath);
                            Tokens.Add(gdNodeInfo.Token);
                            Nodes.TryAdd(gdNodeInfo.Token.Key, gdNodeInfo);
                            return;
                    }
                    break;
                case GDDualOperatorExpression { RightExpression: GDStringExpression }:
                    break;
                case GDCallExpression gdCallExpression:
                    var callExpression = gdCallExpression.CallerExpression;
                    var identifier     = "";
                    switch (callExpression)
                    {
                        case GDMemberOperatorExpression gdMemberOperatorExpression:
                        {
                            identifier = gdMemberOperatorExpression.Identifier.ToString();
                            if (identifier is "get_node" or "get_node_or_null" or "has")
                            {
                                return;
                            }
                            break;
                        }
                        case GDIdentifierExpression gdIdentifierExpression:
                        {
                            identifier = gdIdentifierExpression.ToString();
                            if (identifier is "Color" or "get_node" or "get_node_or_null" or "preload" or "load")
                            {
                                return;
                            }
                            break;
                        }
                    }
                    var parameters = gdCallExpression.Parameters;
                    switch (identifier)
                    {
                        case "emit_signal":
                            if (parameters.Count <= 1)
                            {
                                return;
                            }
                            break;
                        case "connect":
                            if (parameters.Count <= 3)
                            {
                                return;
                            }
                            break;
                    }
                    for (var index = 0; index < parameters.Count; index++)
                    {
                        var parameter = parameters[index];
                        switch (parameter)
                        {
                            case GDDualOperatorExpression gdDualOperatorExpression when gdDualOperatorExpression.HasStringNode():
                                switch (identifier)
                                {
                                    case "emit_signal":
                                        if (index <= 1)
                                        {
                                            continue;
                                        }
                                        break;
                                    case "connect":
                                        if (index <= 3)
                                        {
                                            continue;
                                        }
                                        break;
                                }
                                gdNodeInfo = new GDNodeInfo(original, gdDualOperatorExpression, Prefix, Filepath);
                                Tokens.Add(gdNodeInfo.Token);
                                Nodes.TryAdd(gdNodeInfo.Token.GetHashId(), gdNodeInfo);
                                break;
                            case GDStringExpression gdStringExpression:
                                switch (identifier)
                                {
                                    case "emit_signal":
                                        if (index <= 1)
                                        {
                                            continue;
                                        }
                                        break;
                                    case "connect":
                                        if (index <= 3)
                                        {
                                            continue;
                                        }
                                        break;
                                }
                                gdNodeInfo = new GDNodeInfo(original, gdStringExpression, Prefix, Filepath);
                                Tokens.Add(gdNodeInfo.Token);
                                Nodes.TryAdd(gdNodeInfo.Token.GetHashId(), gdNodeInfo);
                                break;
                        }
                    }
                    return;
                default:
                {
                    gdNodeInfo = new GDNodeInfo(original, Prefix, Filepath);
                    Tokens.Add(gdNodeInfo.Token);
                    Nodes.TryAdd(gdNodeInfo.Token.GetHashId(), gdNodeInfo);
                    return;
                }
            }

        }

        var gdStringNodes = original.AllNodes.OfType<GDStringNode>().ToList();
        foreach (var gdStringNode in gdStringNodes)
        {

            var skip = false;
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
            var gdNodeInfo = new GDNodeInfo(original, gdStringNode, Prefix, Filepath);
            Tokens.Add(gdNodeInfo.Token);
            Nodes.TryAdd(gdNodeInfo.Token.GetHashId(), gdNodeInfo);
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
            if (Nodes.TryGetValue(token.GetHashId(), out var gdNodeInfo))
            {
                gdNodeInfo.ReplaceWith(token);
            }
        }
    }
    public void ParseNode(GDNode node)
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
                    return;
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
                    return;
                }

                AddToken(gdExpressionStatement);
                break;
            }
            case GDVariableDeclarationStatement gdVariableDeclarationStatement:
            {
                if (!gdVariableDeclarationStatement.HasStringNode())
                {
                    return;
                }
                AddToken(gdVariableDeclarationStatement);
                break;
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
            ParseNode(node);
        }
    }
}