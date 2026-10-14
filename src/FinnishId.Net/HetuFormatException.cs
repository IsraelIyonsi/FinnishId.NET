namespace FinnishId;

/// <summary>Thrown by <see cref="HetuNumber.Parse(string, bool)"/> when the code is invalid.</summary>
public sealed class HetuFormatException : FormatException
{
    /// <summary>Initializes a new instance carrying the rejection reason.</summary>
    /// <param name="error">The reason the code was rejected.</param>
    public HetuFormatException(HetuError error)
        : base($"Invalid Finnish personal identity code: {error}.")
    {
        Error = error;
    }

    /// <summary>Gets the reason the code was rejected.</summary>
    public HetuError Error { get; }
}
