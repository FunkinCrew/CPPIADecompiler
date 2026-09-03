namespace CPPIADecompiler;

public enum ClassKind
{
    Class,
    Interface,
    Enum
}

public enum Access
{
    Normal,
    None,
    Resolve,
    Call,
    CallNative
}

public sealed class CppiaModule
{
    public bool Binary;
    public List<string> Strings = new();
    public List<string> Types = new();
    public List<CppiaClass> Classes = new();
    public CppiaExpr? Main;
    public List<CppiaResource> Resources = new();
    
    public Dictionary<int, string> LocalNames = new();

    public string Str(int id) => id >= 0 && id < Strings.Count ? Strings[id] : $"<string {id}>";

    public string RawType(int id) => id >= 0 && id < Types.Count ? Types[id] : $"<type {id}>";

    public string Type(int id) => TypeNames.Normalize(RawType(id));
}

public sealed class CppiaResource
{
    public int NameId;
    public int Length;
    public byte[] Data = Array.Empty<byte>();
}

public sealed class CppiaClass
{
    public ClassKind Kind;
    public int TypeId;
    public int SuperId;
    public List<int> Implements = new();
    public bool ImplementsDynamic;
    public List<CppiaFunction> Functions = new();
    public List<CppiaVar> Vars = new();
    public List<CppiaEnumCtor> EnumCtors = new();
    public CppiaExpr? EnumMeta;
    
    public List<object> Members = new();
}

public sealed class CppiaFunction
{
    public bool IsStatic;
    public bool IsDynamic;
    public int NameId;
    public int ReturnType;
    public List<CppiaArg> Args = new();
    public CppiaExpr? Body;
}

public struct CppiaArg
{
    public int NameId;
    public bool Optional;
    public int TypeId;
}

public sealed class CppiaVar
{
    public bool IsStatic;
    public Access Read;
    public Access Write;
    public bool IsVirtual;
    public int NameId;
    public int TypeId;
    public CppiaExpr? Init;
}

public sealed class CppiaEnumCtor
{
    public int NameId;
    public List<CppiaArg> Args = new();
}

public sealed class StackVar
{
    public int NameId;
    public int Id;
    public bool Capture;
    public int TypeId;
}

public enum ConstKind
{
    Int,
    Float,
    String,
    Null,
    This,
    Super
}

public sealed class CppiaConst
{
    public ConstKind Kind;
    public int Value;
}
