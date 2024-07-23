using System.Collections.Concurrent;
using System.Drawing;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using BDCCChineseLocalization.Paratranz;
using CommandDotNet.Tokens;
using GDShrapt.Reader;

namespace BDCCChineseLocalization;

internal static class GdShraptExtensions
{
    public static readonly HashSet<string> BanPath = new HashSet<string>
    {
        "LoadGameScreen",
        "ModsMenu",
        "GameUI",
        "HK_DatapadHackComputer"

    };
    public static bool HasStringNode(this GDNode node)
    {
        return node.AllNodes.OfType<GDStringNode>().Any();
    }
    public static void InsertBeforeLine(this StringBuilder sb, string line)
    {
        sb.Insert(0, line + "\n");
    }
}

public partial class GdScriptParser
{
    public GDScriptReader Reader = new GDScriptReader();
    public static GdScriptParser Parse(string content, string relativePath)
    {
        var parser = new GdScriptParser(relativePath, content);
        return parser;
    }
    public GdScriptParser(string path, string content)
    {
        Filepath = path;
        Content = content;

        TranslationHashIndex = TranslationHashIndexFile.GetTranslationHashIndex(Filepath);
        Prefix = Path.GetFileNameWithoutExtension(path);
    }
    public string Filepath { get; private set; }
    public string Prefix   { get; private set; }

    public string                                         Content              { get; private set; }
    public List<TranslationToken>                         Tokens               { get; private set; } = new List<TranslationToken>(512);
    public ConcurrentDictionary<string, List<GDNodeInfo>> Nodes                { get; private set; } = new ConcurrentDictionary<string, List<GDNodeInfo>>();
    public ConcurrentDictionary<string, TranslationToken> TokensDictionary     { get; private set; } = new ConcurrentDictionary<string, TranslationToken>();
    public TranslationHashIndex                           TranslationHashIndex { get; private set; }

    public bool HasTokens => Tokens.Count > 0;

    private static readonly char[] separator = new[] { '\n' };

    public void AddGdNode(GDNodeInfo node)
    {
        if (Nodes.TryGetValue(node.Token.HashId, out var list))
        {
            list.Add(node);
            if (TokensDictionary.TryGetValue(node.Token.HashId , out var token))
            {
                token.Nodes.Add(new TokenPosition()
                {
                    StartLine =  node.StartLine,
                    EndLine = node.EndLine,
                    StartColumn = node.StartColumn,
                    EndColumn = node.EndColumn
                });
            }
            return;
        }
        list = new List<GDNodeInfo>
        {
            node
        };
        node.Token.Nodes.Add(new TokenPosition()
        {
            StartLine =  node.StartLine,
            EndLine = node.EndLine,
            StartColumn = node.StartColumn,
            EndColumn = node.EndColumn
        });
        TokensDictionary.TryAdd(node.Token.HashId, node.Token);
        Nodes.TryAdd(node.Token.HashId, list);
        Tokens.Add(node.Token);

    }

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
                            gdNodeInfo = new GDNodeInfo(original, Prefix, TranslationHashIndex);
                            AddGdNode(gdNodeInfo);
                            ;
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
                                gdNodeInfo = new GDNodeInfo(original, gdDualOperatorExpression, Prefix, TranslationHashIndex);
                                AddGdNode(gdNodeInfo);
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
                                gdNodeInfo = new GDNodeInfo(original, gdStringExpression, Prefix, TranslationHashIndex);
                                AddGdNode(gdNodeInfo);
                                break;
                        }
                    }
                    return;
                default:
                {
                    gdNodeInfo = new GDNodeInfo(original, Prefix, TranslationHashIndex);
                    AddGdNode(gdNodeInfo);
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
            var gdNodeInfo = new GDNodeInfo(original, gdStringNode, Prefix, TranslationHashIndex);
            AddGdNode(gdNodeInfo);
        }

    }
    public bool Translate(List<TranslationToken>? translationTokens)
    {
        // var missingTranslationTokens  = new List<TranslationToken>(512);
        // var completeTranslationTokens = new List<TranslationToken>(512);
        translationTokens ??= new List<TranslationToken>();
        translationTokens = translationTokens.Where(x => !string.IsNullOrWhiteSpace(x.Translation)).ToList();
        if (Tokens.Count == 0 || translationTokens is null or { Count: <= 0 })
        {
            return false;
        }
        var complete = false;
        var sb        = new StringBuilder(Content);
        var hashIdSet = translationTokens.ToDictionary(x => x.HashId, x => x);
        foreach (var (hashId, gdNodeInfos) in Nodes)
        {
            if (!hashIdSet.TryGetValue(hashId, out var translationToken))
            {
                var first = gdNodeInfos.First();
                // missingTranslationTokens.Add(first.Token);
                continue;
            }
            if (string.IsNullOrWhiteSpace(translationToken.Translation) || translationToken.Translation == translationToken.Original)
            {
                continue;
            }
            // completeTranslationTokens.Add(translationToken);
            var gdNodeInfo = gdNodeInfos.First();
            sb = gdNodeInfo.Node is GDStringPartsList ? sb.Replace($"\"{translationToken.Original}\"", $"\"{translationToken.Translation}\"") : sb.Replace(translationToken.Original, translationToken.Translation);
            complete = true;

        }
        if (complete)
        {
            Content = sb.ToString();
          
            sb.Clear();
            // Console.WriteLine(Content);

        }
        return complete;
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
    public void Parse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }
        var statementList = new List<string>();
        var sb            = new StringBuilder();
        // var isFilePath    = Prefix == "HK_DatapadHackComputer";
        // var skipLineStr   = "";
        foreach (var line in content.Split(separator).Reverse())
        {
            // if (string.IsNullOrWhiteSpace(line))
            // {
            //     continue;
            // }
            // var addLine  = line;
            // var lineTrim = line.Trim();
            // if (isFilePath )
            // {
            //     if (!string.IsNullOrWhiteSpace(skipLineStr))
            //     {
            //         if (skipLineStr  == lineTrim)
            //         {
            //             skipLineStr = "";
            //         }
            //         continue;
            //     }
            // }
            if (line.StartsWith("func "))
            {
                if (line.StartsWith("func SetFlipLegPos("))
                {

                    sb.Clear();
                    continue;
                }
                if (sb.Length > 0)
                {
                    sb.InsertBeforeLine(line);
                    statementList.Add(sb.ToString());
                    sb.Clear();
                }
                continue;
            }
            // if (isFilePath && lineTrim == "elif(_command == \"unlock\"):")
            // {
            //     skipLineStr = "elif(_command == \"monitor\"):";
            // }
            sb.InsertBeforeLine(line);
        }
        if (sb.Length > 0)
        {
            statementList.Add(sb.ToString());
            sb.Clear();
        }
        statementList.Reverse();
        for (var i = 0; i < statementList.Count; i++)
        {
            var statement = statementList[i];

            if (statement.Contains("JavaScript.eval("))
            {
                continue;
            }
            // Console.WriteLine(new string('=', 50));
            // Console.WriteLine(statement);

            if (i == 0 && !statement.Contains("func "))
            {
                var statements = Reader.ParseFileContent(statement);
                foreach (var gdStatement in statements.AllNodes)
                {
                    ParseNode(gdStatement);
                }
            }
            else
            {
                var gdStatement = Reader.ParseStatementsList(statement);
                if (gdStatement is null)
                {
                    continue;
                }
                foreach (var gdNode in gdStatement.AllNodes)
                {

                    try
                    {
                        ParseNode(gdNode);
                    }
                    catch (StackOverflowException e)
                    {
                        Console.WriteLine(statement);
                        continue;
                    }
                    catch (InvalidOperationException)
                    {
                        Console.WriteLine(statement);
                    }

                }
            }
        }
    }
    public void Parse()
    {

        // if (Path.GetFileName(Filepath) == "GameUI.gd")
        // {
        //     Console.WriteLine(Filepath);
        //     Console.WriteLine(Prefix);
        // }
        if (GdShraptExtensions.BanPath.Contains(Prefix))
        {
            Parse(Content);
            return;
        }
        var statements = Reader.ParseFileContent(Content);
        if (statements is null)
        {
            return;
        }

        foreach (var node in statements.AllNodes)
        {
            ParseNode(node);
        }
    }

    [GeneratedRegex(@"\b(?:yield|loadData|saveData)\b")]
    private static partial Regex MyRegex();
}