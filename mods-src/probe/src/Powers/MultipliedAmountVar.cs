using System.Globalization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Probe.Powers;

/// <summary>
/// A display-only DynamicVar that equals "power stacks × factor".
///
/// The game's power text only understands {Amount} (the stack count); SmartFormat has no math
/// extension, so {3*Amount} cannot be written directly. This var computes the multiple at render
/// time, which lets a description say "lose {BlockCost} block" and show the real number for the
/// current stack count.
/// </summary>
public sealed class MultipliedAmountVar : DynamicVar
{
    private readonly decimal _factor;

    public MultipliedAmountVar(string name, decimal factor) : base(name, factor)
    {
        _factor = factor;
    }

    private decimal Computed =>
        _owner is PowerModel power ? power.Amount * _factor : BaseValue;

    // SmartFormat formats a plain object through ToString(), so this is what the player sees.
    public override string ToString() => ((int)Computed).ToString(CultureInfo.InvariantCulture);

    protected override decimal GetBaseValueForIConvertible() => Computed;
}
