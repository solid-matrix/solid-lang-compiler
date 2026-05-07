namespace SolidLang.Parser;

/// <summary>
/// Represents a span of text in a source file.
/// </summary>
public readonly struct TextSpan
{
    /// <summary>
    /// The start position of the span (inclusive).
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// The length of the span.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// The end position of the span (exclusive).
    /// </summary>
    public int End => Start + Length;

    public TextSpan(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public static TextSpan FromBounds(int start, int end)
    {
        return new TextSpan(start, end - start);
    }

    public bool Contains(int position)
    {
        return position >= Start && position < End;
    }

    public bool Contains(TextSpan span)
    {
        return span.Start >= Start && span.End <= End;
    }

    public bool OverlapsWith(TextSpan span)
    {
        return span.Start < End && span.End > Start;
    }

    public TextSpan? Intersection(TextSpan span)
    {
        int start = Math.Max(Start, span.Start);
        int end = Math.Min(End, span.End);

        if (start >= end)
            return null;

        return FromBounds(start, end);
    }

    public override string ToString()
    {
        return $"[{Start}..{End})";
    }

    public override bool Equals(object? obj)
    {
        return obj is TextSpan span && Equals(span);
    }

    public bool Equals(TextSpan other)
    {
        return Start == other.Start && Length == other.Length;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Start, Length);
    }

    public static bool operator ==(TextSpan left, TextSpan right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TextSpan left, TextSpan right)
    {
        return !left.Equals(right);
    }
}
