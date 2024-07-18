using GDShrapt.Reader;
using Newtonsoft.Json;

namespace BDCCChineseLocalization.Paratranz;

public class GDNodeInfo
{

    public GDNodeInfo(GDNode original, GDStringNode node, string? prefix)
    {
        Node = node.Parts;
        Original = original;
        Token = TranslationToken.Create($"{prefix}_{Node.StartLine}_{Node.StartColumn}", Node.ToString(), "", Original.ToString());
    }
    public GDNodeInfo(GDNode original, GDNode node, string? prefix)
    {
        Node = node;
        Original = original;
        Token = TranslationToken.Create($"{prefix}_{Node.StartLine}_{Node.StartColumn}", Node.ToString(), "", Original.ToString());
    }
    public GDNodeInfo(GDNode node, string? prefix)
    {
        Node = node;
        Original = node;
        IsEquals = true;
        Token = TranslationToken.Create($"{prefix}_{Node.StartLine}", Node.ToString());
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
                Console.WriteLine($"Treating as GDStringPartsList {token.Translation}");
                var gdStringExpression = Reader.ParseExpression($"\"{token.Translation}\"") as GDStringExpression;
                if (gdStringExpression!.FirstChildNode is not GDStringNode stringNode)
                {
                    return false;
                }
                newNode = stringNode.Parts;
                
                break;
            case GDDualOperatorExpression or GDStringExpression or GDReturnExpression:
                newNode = Reader.ParseExpression(token.Translation);
                break;

        }
        return ReplaceWith(newNode);

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