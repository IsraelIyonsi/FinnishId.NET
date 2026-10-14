namespace FinnishId;

/// <summary>The decoded content of a valid Finnish personal identity code.</summary>
/// <param name="Code">The code as supplied (11 characters).</param>
/// <param name="DateOfBirth">The full date of birth; the century is determined by the century sign.</param>
/// <param name="Sex">The sex encoded in the parity of the individual number.</param>
/// <param name="IndividualNumber">The three-digit individual number (ZZZ).</param>
/// <param name="CenturySign">The century sign character (one of + - Y X W V U A B C D E F).</param>
/// <param name="IsTemporary">True when the individual number is in the 900-999 range reserved for artificial or temporary codes.</param>
public sealed record HetuInfo(
    string Code,
    DateOnly DateOfBirth,
    HetuSex Sex,
    int IndividualNumber,
    char CenturySign,
    bool IsTemporary);
