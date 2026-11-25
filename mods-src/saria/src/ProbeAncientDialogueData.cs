using System.Collections.Generic;

namespace Saria;

/// <summary>由 tools/import_writing.py 生成，不要手改。</summary>
internal static class ProbeAncientDialogueData
{
    /// <summary>先古 ID → (对话序号, 行数, 最后一句是否自带按钮文字)。</summary>
    public static readonly Dictionary<string, (int Index, int Lines, bool LastHasNext)[]> Sets = new()
    {
        ["DARV"] = new[] { (0, 4, false), (1, 1, false) },
        ["NEOW"] = new[] { (0, 2, true), (1, 2, false) },
        ["NONUPEIPE"] = new[] { (0, 2, true), (1, 2, false) },
        ["OROBAS"] = new[] { (0, 3, false), (1, 2, false) },
        ["PAEL"] = new[] { (0, 2, true), (1, 1, false) },
        ["TANX"] = new[] { (0, 3, false), (1, 1, false) },
        ["TEZCATARA"] = new[] { (0, 4, false), (1, 1, false) },
        ["THE_ARCHITECT"] = new[] { (0, 6, false), (1, 4, false), (2, 3, false) },
        ["VAKUU"] = new[] { (0, 3, false), (1, 1, false) },
    };

    public const string CharacterEntry = "SARIA-SARIA";
}
