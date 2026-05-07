using System.Text;

namespace SolidLang.Parser;

/// <summary>
/// Represents source code text with efficient access and manipulation.
/// </summary>
public sealed class SourceText
{
    private readonly string _text;

    public SourceText(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
        Length = _text.Length;
    }

    /// <summary>
    /// The length of the source text.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the character at the specified position.
    /// </summary>
    public char this[int position]
    {
        get
        {
            if (position < 0 || position >= Length)
                throw new ArgumentOutOfRangeException(nameof(position));
            return _text[position];
        }
    }

    /// <summary>
    /// Gets a substring of the source text.
    /// </summary>
    public string GetText(int start, int length)
    {
        if (start < 0 || start > Length)
            throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0 || start + length > Length)
            throw new ArgumentOutOfRangeException(nameof(length));

        return _text.Substring(start, length);
    }

    /// <summary>
    /// Gets a substring for the specified span.
    /// </summary>
    public string GetText(TextSpan span)
    {
        return GetText(span.Start, span.Length);
    }

    /// <summary>
    /// Gets the line and column for a given position.
    /// </summary>
    public (int Line, int Column) GetLineAndColumn(int position)
    {
        if (position < 0 || position > Length)
            throw new ArgumentOutOfRangeException(nameof(position));

        int line = 1;
        int column = 1;

        for (int i = 0; i < position; i++)
        {
            if (_text[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }

    /// <summary>
    /// Gets the start position of the specified line.
    /// </summary>
    public int GetLineStart(int line)
    {
        if (line < 1)
            throw new ArgumentOutOfRangeException(nameof(line));

        int currentLine = 1;
        for (int i = 0; i < Length; i++)
        {
            if (currentLine == line)
                return i;

            if (_text[i] == '\n')
                currentLine++;
        }

        return Length;
    }

    /// <summary>
    /// Gets the end position of the specified line (excluding line break).
    /// </summary>
    public int GetLineEnd(int line)
    {
        if (line < 1)
            throw new ArgumentOutOfRangeException(nameof(line));

        int currentLine = 1;
        for (int i = 0; i < Length; i++)
        {
            if (currentLine == line && (_text[i] == '\n' || _text[i] == '\r'))
                return i;

            if (_text[i] == '\n')
                currentLine++;
        }

        return Length;
    }

    /// <summary>
    /// Gets the entire content of the specified line.
    /// </summary>
    public string GetLineContent(int line)
    {
        int start = GetLineStart(line);
        int end = GetLineEnd(line);
        return GetText(start, end - start);
    }

    /// <summary>
    /// Creates a SourceText from a string.
    /// </summary>
    public static SourceText From(string text)
    {
        return new SourceText(text);
    }

    /// <summary>
    /// Creates a SourceText from a file.
    /// </summary>
    public static SourceText FromFile(string path)
    {
        var text = File.ReadAllText(path, Encoding.UTF8);
        return new SourceText(text);
    }

    public override string ToString()
    {
        return _text;
    }
}
