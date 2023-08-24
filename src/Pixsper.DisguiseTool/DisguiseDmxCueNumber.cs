using System;
using System.Text.RegularExpressions;

namespace Pixsper.DisguiseTool;

readonly partial record struct DisguiseDmxCueNumber : IComparable<DisguiseDmxCueNumber>, IComparable
{
    private static readonly Regex FormatExpression = formatExpressionRegex();
    [GeneratedRegex("(\\d\\d)\\.(\\d\\d)\\.(\\d\\d)", RegexOptions.Compiled)]
    private static partial Regex formatExpressionRegex();

    public const int CueWholeMax = 9999;
    public const int CuePartMax = 99;

    public static DisguiseDmxCueNumber? TryParse(string value)
    {
        var match = FormatExpression.Match(value);
        if (!match.Success)
            throw new FormatException("DMX cue number must be formatted as XX.YY.ZZ");

        return new DisguiseDmxCueNumber(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value));
    }

    public DisguiseDmxCueNumber(int cue, int fractionalCue = 0)
    {
        if (cue > CueWholeMax)
            throw new ArgumentOutOfRangeException(nameof(cue), cue, $"Must be between 0 and {CueWholeMax}");

        CueX = cue / 100;
        CueY = cue % 100;

        if (fractionalCue is < 0 or > CuePartMax)
            throw new ArgumentOutOfRangeException(nameof(fractionalCue), fractionalCue, $"Must be between 0 and {CuePartMax}");

        CueZ = fractionalCue;
    }

    public DisguiseDmxCueNumber(int cueX, int cueY, int cueZ = 0)
    {
        if (cueX is < 0 or > CuePartMax)
            throw new ArgumentOutOfRangeException(nameof(cueX), cueX, $"Must be between 0 and {CuePartMax}");
        CueX = cueX;

        if (cueY is < 0 or > CuePartMax)
            throw new ArgumentOutOfRangeException(nameof(cueY), cueY, $"Must be between 0 and {CuePartMax}");
        CueY = cueY;

        if (cueZ is < 0 or > CuePartMax)
            throw new ArgumentOutOfRangeException(nameof(cueZ), cueZ, $"Must be between 0 and {CuePartMax}");
        CueZ = cueZ;
    }

    public int CueX { get; }
    public int CueY { get; }
    public int CueZ { get; }

    public decimal Value => CueX * 100m + CueY + CueZ * 0.01m;

    public override string ToString() => $"{CueX:00}.{CueY:00}.{CueZ:00}";

    public int CompareTo(DisguiseDmxCueNumber other) => Value.CompareTo(other.Value);

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return 1;
        return obj is DisguiseDmxCueNumber other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(DisguiseDmxCueNumber)}");
    }

    public static bool operator <(DisguiseDmxCueNumber left, DisguiseDmxCueNumber right) => left.CompareTo(right) < 0;

    public static bool operator >(DisguiseDmxCueNumber left, DisguiseDmxCueNumber right) => left.CompareTo(right) > 0;

    public static bool operator <=(DisguiseDmxCueNumber left, DisguiseDmxCueNumber right) => left.CompareTo(right) <= 0;

    public static bool operator >=(DisguiseDmxCueNumber left, DisguiseDmxCueNumber right) => left.CompareTo(right) >= 0;
}