namespace FinnishId;

/// <summary>The reason a Finnish personal identity code was rejected.</summary>
public enum HetuError
{
    /// <summary>The code is valid.</summary>
    None = 0,

    /// <summary>The input is null or is not exactly 11 characters long.</summary>
    InvalidLength,

    /// <summary>The DDMMYY part is not six ASCII digits or is not a real calendar date in the century given by the sign.</summary>
    InvalidDate,

    /// <summary>The century sign is not one of the signs defined by the specification.</summary>
    InvalidCenturySign,

    /// <summary>The individual number is not three ASCII digits, or is outside the enforced range.</summary>
    InvalidIndividualNumber,

    /// <summary>The control character does not match the one computed from the date and the individual number.</summary>
    InvalidControlCharacter,
}
