using System.Security.Cryptography;
using System.Text;
using GDShrapt.Reader;
using Newtonsoft.Json;

namespace BDCCChineseLocalization.Paratranz;

public class GDNodeInfo
{

    public GDNodeInfo(GDNode original, GDStringNode node, string prefix,string? filepath = "")
    {
        Node = node.Parts;
        Original = original;
        var translationHashIndex = TranslationHashIndexFile.GetTranslationHashIndex(string.IsNullOrWhiteSpace(filepath) ? prefix : filepath);
        prefix = translationHashIndex.GetPrefix(prefix, Original, Node);
        Token = TranslationToken.Create(prefix, Node.ToString(), "", Original.ToString());
        
        
    }
    public GDNodeInfo(GDNode original, GDNode node, string prefix,string? filepath = "")
    {
        Node = node;
        Original = original;
        var translationHashIndex = TranslationHashIndexFile.GetTranslationHashIndex(string.IsNullOrWhiteSpace(filepath) ? prefix : filepath);
        prefix = translationHashIndex.GetPrefix(prefix, Original, Node);
        Token = TranslationToken.Create(prefix, Node.ToString(), "", Original.ToString());
    }
    public GDNodeInfo(GDNode node, string prefix,string? filepath = "")
    {
        Node = node;
        Original = node;
        IsEquals = true;
        var translationHashIndex = TranslationHashIndexFile.GetTranslationHashIndex(string.IsNullOrWhiteSpace(filepath) ? prefix : filepath);
        prefix = translationHashIndex.GetPrefix(prefix, Node);
        Token = TranslationToken.Create(prefix, Node.ToString());
    }
    [JsonIgnore]
    public GDNode Original { get; private set; }
    [JsonIgnore]
    public GDNode Node { get;            private set; }
    public TranslationToken Token { get; private set; }
    [JsonIgnore]
    public bool IsEquals { get; private set; } = false;
    [JsonIgnore]
    public GDScriptReader Reader { get; } = new GDScriptReader();

    public bool ReplaceWith(TranslationToken token)
    {
        if (string.IsNullOrWhiteSpace(token.Translation))
        {
            return false;
        }
        // var nodeContext = Node.ToString();
        // if (token.Translation == nodeContext)
        // {
        //     return false;
        // }
        // if (token.Original != nodeContext)
        // {
        //     return false;
        // }
        GDNode newNode = default!;
        switch (Node)
        {
            case GDStringPartsList stringPartsList:
                var gdStringExpression = Reader.ParseExpression($"\"{token.Translation}\"") as GDStringExpression;
                if (gdStringExpression!.FirstChildNode is not GDStringNode stringNode)
                {
                    return false;
                }
                newNode = stringNode.Parts;

                break;
            case GDExpressionStatement:
                newNode = Reader.ParseExpression(token.Translation);
                break;

        }
        if (newNode is null)
        {
            return false;
        }
        Console.WriteLine($"Replacing {Node} with {newNode}");
        Console.WriteLine($"Type: {Node?.GetType()} -> {newNode?.GetType()}");

        return ReplaceWith(newNode!);

    }
    public bool ReplaceWith(GDNode newNode)
    {

        // if (!newNode.HasTokens)
        // {
        //     return false;
        // }
        // if (newNode.ToString() == Node.ToString())
        // {
        //     return false;
        // }
        Console.WriteLine($"Replacing {Node} with {newNode}");
        Node.Parent.Form.AddBeforeToken(newNode, Node);
        Node.RemoveFromParent();
        Node = newNode;
        Token.Translation = newNode.ToString();
        return true;
    }
}