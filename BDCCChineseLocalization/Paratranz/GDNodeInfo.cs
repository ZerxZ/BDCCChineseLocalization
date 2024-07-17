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
    public GDNode           Original { get; private set; }
    [JsonIgnore]
    public GDNode           Node     { get; private set; }
    public TranslationToken Token    { get; private set; }
    [JsonIgnore]
    public bool             IsEquals { get; private set; } = false;
    [JsonIgnore]
    public GDScriptReader Reader { get; } = new GDScriptReader();
    // public void SetToken(string key)
    // {
    //     if (IsEquals)
    //     {
    //         Token = TranslationToken.Create(key, Original.ToString());
    //         // if (Original.FirstChildNode is GDReturnExpression gdReturnExpression)
    //         // {
    //         //     Token.Type = "Return:" + string.Join(',', gdReturnExpression.Nodes.Select(x => x.GetType().Name));
    //         // }
    //         // else
    //         // {
    //         //     Token.Type = "Child:" + string.Join(',', Original.Nodes.Select(x => x.GetType().Name));
    //         // }
    //         
    //     }
    //     else
    //     {
    //         Token = TranslationToken.Create(key, Node.ToString(), "", Original.ToString());
    //         // Token.Type = "Parent:" + string.Join(',', Node.Parents.Select(x => x.GetType().Name));
    //     }
    //
    // }

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