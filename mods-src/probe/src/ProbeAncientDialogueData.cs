using System.Collections.Generic;

namespace Probe;

/// <summary>由 tools/import_writing.py 生成，不要手改。</summary>
internal static class ProbeAncientDialogueData
{
    /// <summary>先古 ID → (对话序号, 行数)。对话序号就是 loc 键里的 X。</summary>
    public static readonly Dictionary<string, (int Index, int Lines)[]> Sets = new()
    {
        ["DARV"] = new[] { (0, 5), (1, 1) },
        ["NEOW"] = new[] { (0, 3), (1, 2) },
        ["NONUPEIPE"] = new[] { (0, 3), (1, 2) },
        ["OROBAS"] = new[] { (0, 3), (1, 2) },
        ["PAEL"] = new[] { (0, 3), (1, 1) },
        ["TANX"] = new[] { (0, 4), (1, 1) },
        ["TEZCATARA"] = new[] { (0, 5), (1, 1) },
        ["THE_ARCHITECT"] = new[] { (0, 6), (1, 4), (2, 3) },
        ["VAKUU"] = new[] { (0, 4), (1, 1) },
    };

    public const string CharacterEntry = "PROBE-SARIA";
}
