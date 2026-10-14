using FinnishId;

namespace FinnishId.Tests;

// Sources for vectors (see README "Sources"):
//  - 131052-308T : Statistics Finland / DVV style example, also python-stdnum and fin-id-utils documentation.
//  - 131052+308T : fin-id-utils usage example (same digits, century sign +).
//  - 111111-111C, 010101-0101, 010101B0101, 010594Y9032, 010594Y9021, 020594X903P : rOpenGov "hetu" R package manual examples.
//  - Century signs and 900-999 temporary range: DVV and Statistics Finland (https://stat.fi/meta/kas/hetu.html).
//
// Hand computation of the control character (independent of the code under test).
// Table T = "0123456789ABCDEFHJKLMNPRSTUVWXY" (index 0..30; G I O Q Z omitted).
//   131052-308T : number = 131052 308 = 131052308.  31 * 4227493 = 131052283.  131052308 - 131052283 = 25.
//                 T[25]: 0-9 are digits, 10 A, 11 B, 12 C, 13 D, 14 E, 15 F, 16 H, 17 J, 18 K, 19 L, 20 M,
//                 21 N, 22 P, 23 R, 24 S, 25 T  =>  'T'.  Matches.
//   111111-111C : number = 111111 111 = 111111111.  31 * 3584229 = 111111099.  111111111 - 111111099 = 12.
//                 T[12] = 'C'.  Matches.
public class HetuNumberTests
{
    public static TheoryData<string, string, HetuSex, int> ValidCodes => new()
    {
        { "131052-308T", "1952-10-13", HetuSex.Female, 308 },
        { "131052+308T", "1852-10-13", HetuSex.Female, 308 },
        { "111111-111C", "1911-11-11", HetuSex.Male, 111 },
        { "010101-0101", "1901-01-01", HetuSex.Female, 10 },
        { "010101B0101", "2001-01-01", HetuSex.Female, 10 },
        { "010101A0101", "2001-01-01", HetuSex.Female, 10 },
        { "290200A002C", "2000-02-29", HetuSex.Female, 2 },
        { "010101-101Y", "1901-01-01", HetuSex.Male, 101 },
    };

    public static TheoryData<char, int> CenturySigns => new()
    {
        { '+', 1800 }, { '-', 1900 }, { 'Y', 1900 }, { 'X', 1900 }, { 'W', 1900 }, { 'V', 1900 }, { 'U', 1900 },
        { 'A', 2000 }, { 'B', 2000 }, { 'C', 2000 }, { 'D', 2000 }, { 'E', 2000 }, { 'F', 2000 },
    };

    public static TheoryData<string, HetuError> InvalidCodes => new()
    {
        { "131052-308U", HetuError.InvalidControlCharacter },
        { "131052-3081", HetuError.InvalidControlCharacter },
        { "131052-308t", HetuError.InvalidControlCharacter },
        { "321052-3082", HetuError.InvalidDate },
        { "131352-3087", HetuError.InvalidDate },
        { "000052-308B", HetuError.InvalidDate },
        { "290200-002C", HetuError.InvalidDate },
        { "290205-002N", HetuError.InvalidDate },
        { "13105A-308T", HetuError.InvalidDate },
        { "131052G308T", HetuError.InvalidCenturySign },
        { "131052Z308T", HetuError.InvalidCenturySign },
        { "131052a308T", HetuError.InvalidCenturySign },
        { "131052 308T", HetuError.InvalidCenturySign },
        { "131052-30AT", HetuError.InvalidIndividualNumber },
        { "010101-001R", HetuError.InvalidIndividualNumber },
        { "010101-000P", HetuError.InvalidIndividualNumber },
        { "010101-900R", HetuError.InvalidIndividualNumber },
        { "010594Y9032", HetuError.InvalidIndividualNumber },
        { "131052-308", HetuError.InvalidLength },
        { "131052-308TT", HetuError.InvalidLength },
        { " 131052-308T", HetuError.InvalidLength },
        { "", HetuError.InvalidLength },
    };

    public static TheoryData<string, string, HetuSex, int> TemporaryCodes => new()
    {
        { "010594Y9032", "1994-05-01", HetuSex.Male, 903 },
        { "010594Y9021", "1994-05-01", HetuSex.Female, 902 },
        { "020594X903P", "1994-05-02", HetuSex.Male, 903 },
        { "150885-9988", "1985-08-15", HetuSex.Female, 998 },
    };

    [Theory]
    [MemberData(nameof(ValidCodes))]
    public void ValidCodesDecode(string code, string expectedDate, HetuSex sex, int individual)
    {
        Assert.True(HetuNumber.IsValid(code));
        Assert.Equal(HetuError.None, HetuNumber.Validate(code));
        var info = HetuNumber.Parse(code);
        Assert.Equal(DateOnly.Parse(expectedDate, System.Globalization.CultureInfo.InvariantCulture), info.DateOfBirth);
        Assert.Equal(sex, info.Sex);
        Assert.Equal(individual, info.IndividualNumber);
        Assert.Equal(code[6], info.CenturySign);
        Assert.False(info.IsTemporary);
        Assert.Equal(code, info.Code);
    }

    [Theory]
    [MemberData(nameof(CenturySigns))]
    public void EveryCenturySignMapsToItsCentury(char sign, int centuryBase)
    {
        var control = HetuNumber.ComputeControlCharacter(10101, 10);
        var code = $"010101{sign}010{control}";
        var info = HetuNumber.Parse(code);
        Assert.Equal(centuryBase + 1, info.DateOfBirth.Year);
    }

    [Theory]
    [MemberData(nameof(InvalidCodes))]
    public void InvalidCodesReportDistinctReason(string code, HetuError expected)
    {
        Assert.False(HetuNumber.IsValid(code));
        Assert.Equal(expected, HetuNumber.Validate(code));
        Assert.False(HetuNumber.TryParse(code, out var info, out var error));
        Assert.Null(info);
        Assert.Equal(expected, error);
        var ex = Assert.Throws<HetuFormatException>(() => HetuNumber.Parse(code));
        Assert.Equal(expected, ex.Error);
    }

    [Fact]
    public void NullIsInvalidLength()
    {
        Assert.False(HetuNumber.IsValid(null));
        Assert.Equal(HetuError.InvalidLength, HetuNumber.Validate(null));
        Assert.False(HetuNumber.TryParse(null, out _));
    }

    [Theory]
    [MemberData(nameof(TemporaryCodes))]
    public void TemporaryRangeIsRejectedByDefaultAndAcceptedOnRequest(string code, string expectedDate, HetuSex sex, int individual)
    {
        Assert.False(HetuNumber.IsValid(code));
        Assert.Equal(HetuError.InvalidIndividualNumber, HetuNumber.Validate(code));
        Assert.True(HetuNumber.TryParse(code, out var info, allowTemporary: true));
        Assert.True(info!.IsTemporary);
        Assert.Equal(DateOnly.Parse(expectedDate, System.Globalization.CultureInfo.InvariantCulture), info.DateOfBirth);
        Assert.Equal(sex, info.Sex);
        Assert.Equal(individual, info.IndividualNumber);
    }

    [Theory]
    [InlineData(131052, 308, 'T')]
    [InlineData(111111, 111, 'C')]
    [InlineData(10101, 10, '1')]
    [InlineData(10101, 100, 'X')]
    [InlineData(10101, 102, '0')]
    [InlineData(0, 0, '0')]
    public void ControlCharacterMatchesHandComputation(int dateDigits, int individual, char expected)
    {
        Assert.Equal(expected, HetuNumber.ComputeControlCharacter(dateDigits, individual));
    }

    [Fact]
    public void ControlTableOmitsGIOQAndZ()
    {
        var produced = new string(Enumerable.Range(0, 31).Select(i => HetuNumber.ComputeControlCharacter(0, i)).ToArray());
        Assert.Equal("0123456789ABCDEFHJKLMNPRSTUVWXY", produced);
        Assert.DoesNotContain('G', produced);
        Assert.DoesNotContain('I', produced);
        Assert.DoesNotContain('O', produced);
        Assert.DoesNotContain('Q', produced);
        Assert.DoesNotContain('Z', produced);
    }

    [Fact]
    public void NonAsciiDigitsAreRejected()
    {
        // Arabic-Indic digit one in the day field must not pass as a digit.
        Assert.Equal(HetuError.InvalidDate, HetuNumber.Validate("\u0661" + "31052-308T"));
    }

    [Fact]
    public void ErrorMessageNamesTheReason()
    {
        var ex = Assert.Throws<HetuFormatException>(() => HetuNumber.Parse("131052-308U"));
        Assert.Contains("InvalidControlCharacter", ex.Message);
    }
}
