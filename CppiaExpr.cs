namespace CPPIADecompiler;

// cppia expression
public sealed class CppiaExpr
{
    public string Op = "";
    public int FileId;
    public int Line;
    public List<int> Ops = new();
    public List<CppiaExpr> Kids = new();
    
    public List<StackVar> Vars = new();
    
    public List<CppiaConst?> Defaults = new();

    public int Op0 => Ops[0];
    public int Op1 => Ops[1];
    public int Op2 => Ops[2];

    public CppiaExpr(string op)
    {
        Op = op;
    }

    public int Count => Kids.Count;
    public CppiaExpr this[int index] => Kids[index];
}
