using System.Globalization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Probe.Powers;

/// <summary>
/// A display-only DynamicVar that equals "power stacks × factor".
///
/// 游戏 2026-11 更新后，SmartFormat 取的是 DynamicVar.ShownValue（= (int)PreviewValue），
/// 原来那层 `GetBaseValueForIConvertible()` 钩子没了，所以只能在层数变化时把 PreviewValue 算好写进去
/// （见 <c>ProbeSwiftPower.RefreshVars</c>）。这个类本身只负责“按倍率刷新”。
/// </summary>
public sealed class MultipliedAmountVar : DynamicVar
{
    private readonly decimal _factor;

    public MultipliedAmountVar(string name, decimal factor) : base(name, factor)
    {
        _factor = factor;
    }

    /// <summary>按当前层数刷新显示值（BaseValue / PreviewValue 都要写，后者才是显示用的）。</summary>
    public void Refresh(int powerAmount)
    {
        decimal value = powerAmount * _factor;
        BaseValue = value;
        PreviewValue = value;
    }

    public override string ToString() => ((int)PreviewValue).ToString(CultureInfo.InvariantCulture);
}
