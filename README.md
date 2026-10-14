# FinnishId.Net

Zero-dependency validator and decoder for the Finnish personal identity code, the **HETU** (henkilötunnus). .NET 8, AOT-compatible, no allocations beyond the decoded record.

## Why

KYC and onboarding flows for Finnish users need more than a regex. A HETU carries a date of birth, the sex, and a control character; since 1 January 2023 it also uses new century signs that most older validators reject. FinnishId.Net checks all of it against the DVV specification and tells you precisely why a code failed.

## Install

```
dotnet add package FinnishId.Net
```

## Usage

```csharp
using FinnishId;

HetuNumber.IsValid("131052-308T");                 // true

if (HetuNumber.TryParse("131052-308T", out var info))
{
    info.DateOfBirth;       // 1952-10-13 (DateOnly, century known from the sign)
    info.Sex;               // HetuSex.Female (308 is even)
    info.IndividualNumber;  // 308
    info.CenturySign;       // '-'
}

HetuNumber.Validate("131052-308U");                // HetuError.InvalidControlCharacter
HetuNumber.Parse("131052-308U");                   // throws HetuFormatException (.Error carries the reason)

// Artificial / temporary codes (individual number 900-999)
HetuNumber.IsValid("010594Y9032");                         // false
HetuNumber.IsValid("010594Y9032", allowTemporary: true);   // true
```

Matching is exact: 11 characters, no trimming, upper-case letters. Normalize input before calling if you accept looser user input.

## Structure: DDMMYYCZZZQ

| Part | Chars | Meaning |
|------|-------|---------|
| DDMMYY | 0-5 | Date of birth |
| C | 6 | Century sign |
| ZZZ | 7-9 | Individual number; odd = male, even = female |
| Q | 10 | Control character |

### Century signs (expanded 1 January 2023)

| Century | Signs |
|---------|-------|
| 1800s | `+` |
| 1900s | `-` `Y` `X` `W` `V` `U` |
| 2000s | `A` `B` `C` `D` `E` `F` |

The additional signs were introduced because the supply of individual numbers per birth date was running out. All of them are accepted here, and the century is always fully determined by the sign, so there is no two-digit-year ambiguity.

### Individual number

Official codes use **002-899**. Numbers 900-999 are reserved for artificial or temporary codes and are rejected unless `allowTemporary: true`; decoded records expose `IsTemporary`. 000 and 001 are never valid.

### Control character

Concatenate DDMMYY and ZZZ into a nine-digit number, take it modulo 31, and index into `0123456789ABCDEFHJKLMNPRSTUVWXY` (the letters G, I, O, Q and Z are omitted). Example: `131052308 mod 31 = 25`, and index 25 is `T`.

## Errors

`HetuError`: `InvalidLength`, `InvalidDate`, `InvalidCenturySign`, `InvalidIndividualNumber`, `InvalidControlCharacter`. Checks run in that order.

## Sources

- DVV (Digital and Population Data Services Agency): https://dvv.fi/en/personal-identity-code
- Statistics Finland, personal identity code: https://stat.fi/meta/kas/hetu.html
- Valtioneuvosto announcement of the new century signs: https://valtioneuvosto.fi/-/10623/henkilotunnusten-riittavyys-varmistetaan-uusilla-valimerkeilla
- Finlex statute 690/2022 (amendment effective 1 January 2023): https://finlex.fi/api/media/statute/17253/mainPdf/main.pdf
- Test examples: python-stdnum `stdnum.fi.hetu`, fin-id-utils, and the rOpenGov `hetu` R package manual.

## License

MIT
