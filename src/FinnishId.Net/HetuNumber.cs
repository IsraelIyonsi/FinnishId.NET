using System.Diagnostics.CodeAnalysis;

namespace FinnishId;

/// <summary>
/// Validator and decoder for the Finnish personal identity code (HETU, henkilotunnus), format DDMMYYCZZZQ.
/// Input is matched exactly: no trimming, upper-case century sign and control letter.
/// </summary>
public static class HetuNumber
{
    private const int CodeLength = 11;
    private const int DateLength = 6;
    private const int DayOffset = 0;
    private const int MonthOffset = 2;
    private const int YearOffset = 4;
    private const int CenturySignOffset = 6;
    private const int IndividualOffset = 7;
    private const int IndividualLength = 3;
    private const int ControlOffset = 10;
    private const int ControlDivisor = 31;
    private const int IndividualDecimalShift = 1000;
    private const int MaxDateDigits = 999999;
    private const int MinIndividualNumber = 2;
    private const int MaxOfficialIndividualNumber = 899;
    private const int MaxIndividualNumber = 999;
    private const int Decimal = 10;
    private const int MonthsPerYear = 12;
    private const int OddDivisor = 2;

    private const string ControlCharacters = "0123456789ABCDEFHJKLMNPRSTUVWXY";

    private static readonly IReadOnlyDictionary<char, int> CenturyBase = new Dictionary<char, int>
    {
        ['+'] = 1800,
        ['-'] = 1900,
        ['Y'] = 1900,
        ['X'] = 1900,
        ['W'] = 1900,
        ['V'] = 1900,
        ['U'] = 1900,
        ['A'] = 2000,
        ['B'] = 2000,
        ['C'] = 2000,
        ['D'] = 2000,
        ['E'] = 2000,
        ['F'] = 2000,
    };

    /// <summary>Checks a code against the specification.</summary>
    /// <param name="code">The 11-character code.</param>
    /// <param name="allowTemporary">When true, individual numbers 900-999 (artificial or temporary codes) are accepted.</param>
    /// <returns>True when the code is valid.</returns>
    public static bool IsValid(string? code, bool allowTemporary = false) =>
        Validate(code, allowTemporary) == HetuError.None;

    /// <summary>Checks a code and returns the first reason it is invalid, or <see cref="HetuError.None"/>.</summary>
    /// <param name="code">The 11-character code.</param>
    /// <param name="allowTemporary">When true, individual numbers 900-999 are accepted.</param>
    /// <returns>The rejection reason.</returns>
    public static HetuError Validate(string? code, bool allowTemporary = false) =>
        Decode(code, allowTemporary, out _);

    /// <summary>Tries to decode a code.</summary>
    /// <param name="code">The 11-character code.</param>
    /// <param name="info">The decoded record when valid; otherwise null.</param>
    /// <param name="allowTemporary">When true, individual numbers 900-999 are accepted.</param>
    /// <returns>True when the code is valid.</returns>
    public static bool TryParse(string? code, [NotNullWhen(true)] out HetuInfo? info, bool allowTemporary = false) =>
        Decode(code, allowTemporary, out info) == HetuError.None;

    /// <summary>Tries to decode a code and reports why it failed.</summary>
    /// <param name="code">The 11-character code.</param>
    /// <param name="info">The decoded record when valid; otherwise null.</param>
    /// <param name="error">The rejection reason, or <see cref="HetuError.None"/>.</param>
    /// <param name="allowTemporary">When true, individual numbers 900-999 are accepted.</param>
    /// <returns>True when the code is valid.</returns>
    public static bool TryParse(
        string? code,
        [NotNullWhen(true)] out HetuInfo? info,
        out HetuError error,
        bool allowTemporary = false)
    {
        error = Decode(code, allowTemporary, out info);
        return error == HetuError.None;
    }

    /// <summary>Decodes a code, throwing when it is invalid.</summary>
    /// <param name="code">The 11-character code.</param>
    /// <param name="allowTemporary">When true, individual numbers 900-999 are accepted.</param>
    /// <returns>The decoded record.</returns>
    /// <exception cref="HetuFormatException">The code is invalid; <see cref="HetuFormatException.Error"/> says why.</exception>
    public static HetuInfo Parse(string code, bool allowTemporary = false)
    {
        if (!TryParse(code, out var info, out var error, allowTemporary))
        {
            throw new HetuFormatException(error);
        }

        return info;
    }

    /// <summary>Computes the control character for the six DDMMYY digits followed by the individual number.</summary>
    /// <param name="dateDigits">The DDMMYY digits as an integer (leading zeros dropped), 0 to 999999.</param>
    /// <param name="individualNumber">The individual number, 0 to 999.</param>
    /// <returns>The control character.</returns>
    public static char ComputeControlCharacter(int dateDigits, int individualNumber)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(dateDigits);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(dateDigits, MaxDateDigits);
        ArgumentOutOfRangeException.ThrowIfNegative(individualNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(individualNumber, MaxIndividualNumber);

        return ControlCharacters[((dateDigits * IndividualDecimalShift) + individualNumber) % ControlDivisor];
    }

    private static HetuError Decode(string? code, bool allowTemporary, out HetuInfo? info)
    {
        info = null;
        if (code is null || code.Length != CodeLength)
        {
            return HetuError.InvalidLength;
        }

        if (!TryReadDigits(code, DayOffset, DateLength, out var dateDigits))
        {
            return HetuError.InvalidDate;
        }

        var sign = code[CenturySignOffset];
        if (!CenturyBase.TryGetValue(sign, out var centuryBase))
        {
            return HetuError.InvalidCenturySign;
        }

        var day = TwoDigitValue(code, DayOffset);
        var month = TwoDigitValue(code, MonthOffset);
        var year = centuryBase + TwoDigitValue(code, YearOffset);
        if (month is < 1 or > MonthsPerYear || day < 1 || day > DateTime.DaysInMonth(year, month))
        {
            return HetuError.InvalidDate;
        }

        if (!TryReadDigits(code, IndividualOffset, IndividualLength, out var individual))
        {
            return HetuError.InvalidIndividualNumber;
        }

        var maxAllowed = allowTemporary ? MaxIndividualNumber : MaxOfficialIndividualNumber;
        if (individual < MinIndividualNumber || individual > maxAllowed)
        {
            return HetuError.InvalidIndividualNumber;
        }

        if (code[ControlOffset] != ComputeControlCharacter(dateDigits, individual))
        {
            return HetuError.InvalidControlCharacter;
        }

        info = new HetuInfo(
            code,
            new DateOnly(year, month, day),
            individual % OddDivisor == 1 ? HetuSex.Male : HetuSex.Female,
            individual,
            sign,
            individual > MaxOfficialIndividualNumber);
        return HetuError.None;
    }

    private static int TwoDigitValue(string code, int offset) =>
        ((code[offset] - '0') * Decimal) + (code[offset + 1] - '0');

    private static bool TryReadDigits(string code, int offset, int length, out int value)
    {
        value = 0;
        for (var i = offset; i < offset + length; i++)
        {
            if (code[i] is < '0' or > '9')
            {
                return false;
            }

            value = (value * Decimal) + (code[i] - '0');
        }

        return true;
    }
}
