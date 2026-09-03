namespace CPPIADecompiler;

public static class Boilerplate
{
    // Exact full names mapped to the fingerprints of the generated code they are allowed to
    // hold. Matching the name is never enough on its own, so a type that borrows one of these
    // names to carry something else still gets written out.
    // Regenerate with --fingerprints.
    static readonly Dictionary<string, string[]> Known = new(StringComparer.Ordinal)
    {
        ["cpp.cppia.HostClasses"] = ["61b6877481a78f1b"],
        ["funkin.ui.debug.charting.ChartEditorLiveInputStyle"] = ["c65cd078885cd1c7"],
        ["funkin.ui.debug.charting.ChartEditorTheme"] = ["32efaa4dab1664e5"],
        ["funkin.ui.debug.stageeditor.StageEditorTheme"] = ["c9155c93f2409f2c"],
        ["haxe._EntryPoint.Lock"] = ["cbfc7844240abd75"],
        ["haxe._EntryPoint.Mutex"] = ["7b037bccd9703eab"],
        ["haxe._EntryPoint.Thread"] = ["60ff2b966cfad813"],
        ["haxe.lang.Iterable"] = ["5e7e0bcd40f0c61c"],
        ["haxe.lang.Iterator"] = ["e395f129ceaf0d6d"]
    };

    public static bool IsKnownName(string fullName) => Known.ContainsKey(fullName);

    public static bool Matches(string fullName, string print) =>
        Known.TryGetValue(fullName, out string[]? prints) && Array.IndexOf(prints, print) >= 0;
}
