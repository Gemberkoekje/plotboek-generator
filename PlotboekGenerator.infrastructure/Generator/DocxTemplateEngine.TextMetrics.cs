using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace PlotboekGenerator.infrastructure.Generator;

/// <summary>
/// Font metrics behind the page-layout estimate: how wide a piece of text renders and how tall a
/// line of it is.  Split out from <c>DocxTemplateEngine.PageLayout</c> so the flow logic there
/// stays about pages rather than glyphs.
/// </summary>
public static partial class DocxTemplateEngine
{
    /// <summary>Word's default body font size (Calibri 11pt) expressed in half-points.</summary>
    private const int DefaultFontSizeHalfPoints = 22;

    /// <summary>A half-point is ten twips.</summary>
    private const double TwipsPerHalfPoint = 10;

    /// <summary>
    /// Rendered line height relative to the font size for single line spacing.
    /// Measured at 13.45pt for 11pt Calibri, the font this template uses throughout.
    /// </summary>
    private const double SingleLineHeightRatio = 1.222;

    /// <summary>
    /// Advance width of every printable ASCII character as a fraction of the font size, in
    /// Calibri - the font this template uses throughout.  Indexed by <c>character - ' '</c>.
    /// Averaging one ratio over all characters is not good enough: a line of capitals or
    /// digits is a third wider than the same number of lowercase letters, which is the
    /// difference between a paragraph taking two lines and three.
    /// </summary>
    private static readonly double[] GlyphAdvances =
    [
        0.226, 0.303, 0.418, 0.498, 0.507, 0.686, 0.638, 0.222, // (space) ! " # $ % & '
        0.303, 0.303, 0.498, 0.498, 0.249, 0.306, 0.252, 0.386, // ( ) * + , - . /
        0.507, 0.507, 0.507, 0.507, 0.507, 0.507, 0.507, 0.507, // 0 1 2 3 4 5 6 7
        0.507, 0.507, 0.268, 0.268, 0.498, 0.498, 0.498, 0.463, // 8 9 : ; < = > ?
        0.894, 0.579, 0.544, 0.533, 0.615, 0.488, 0.459, 0.631, // @ A B C D E F G
        0.623, 0.252, 0.319, 0.520, 0.420, 0.855, 0.646, 0.662, // H I J K L M N O
        0.517, 0.673, 0.543, 0.459, 0.487, 0.642, 0.567, 0.890, // P Q R S T U V W
        0.519, 0.487, 0.468, 0.303, 0.386, 0.303, 0.498, 0.498, // X Y Z [ \ ] ^ _
        0.303, 0.479, 0.525, 0.423, 0.525, 0.498, 0.305, 0.471, // ` a b c d e f g
        0.525, 0.230, 0.239, 0.455, 0.230, 0.799, 0.525, 0.527, // h i j k l m n o
        0.525, 0.525, 0.349, 0.391, 0.335, 0.525, 0.452, 0.715, // p q r s t u v w
        0.434, 0.454, 0.396, 0.303, 0.498, 0.303, 0.498,        // x y z { | } ~
    ];

    /// <summary>Advance for an unlisted Latin character, close to the Calibri average.</summary>
    private const double FallbackGlyphAdvance = 0.5;

    /// <summary>Advance for an emoji or other full-width glyph, which fall back to a symbol font.</summary>
    private const double WideGlyphAdvance = 1.0;

    /// <summary>First code point of the symbol/emoji planes, all treated as full-width.</summary>
    private const int FirstSymbolCodePoint = 0x2100;

    /// <summary>Emoji variation selector; carries no width of its own.</summary>
    private const int VariationSelector = 0xFE0F;

    /// <summary>Zero-width joiner, used to compose multi-part emoji.</summary>
    private const int ZeroWidthJoiner = 0x200D;

    /// <summary>Calibri Bold runs about three percent wider than the regular weight.</summary>
    private const double BoldGlyphFactor = 1.03;

    /// <summary>
    /// Fraction of each line lost to the ragged right edge, since lines break on whole words.
    /// </summary>
    private const double RaggedRightAllowance = 0.02;

    /// <summary>Rendered width of a piece of text at the given size and weight, in twips.</summary>
    private static double MeasureText(string text, int fontSizeHalfPoints, bool bold)
    {
        var ems = 0d;
        foreach (var character in text)
            ems += GlyphAdvance(character);

        if (bold) ems *= BoldGlyphFactor;

        return ems * fontSizeHalfPoints * TwipsPerHalfPoint;
    }

    /// <summary>Advance width of a single character as a fraction of the font size.</summary>
    private static double GlyphAdvance(char character)
        => character switch
        {
            >= ' ' and <= '~' => GlyphAdvances[character - ' '],
            ' ' => GlyphAdvances[0],                       // non-breaking space
            '‘' or '’' => 0.222,                      // curly single quotes
            '“' or '”' => 0.418,                      // curly double quotes
            '–' => 0.498,                                  // en dash
            '—' => 0.618,                                  // em dash
            '•' => 0.350,                                  // bullet, prefixed to list items
            '…' => 0.750,                                  // ellipsis
            _ when IsZeroWidth(character) => 0,                 // emoji modifiers render as nothing
            _ when char.IsLowSurrogate(character) => 0,         // the high surrogate carries the width
            _ when char.IsHighSurrogate(character) || character >= FirstSymbolCodePoint => WideGlyphAdvance,
            _ => FallbackGlyphAdvance,
        };

    /// <summary>Variation selectors and zero-width joiners take no space of their own.</summary>
    private static bool IsZeroWidth(char character)
        => character == VariationSelector || character == ZeroWidthJoiner;

    /// <summary>Resolves the effective size (half-points) and weight of the run owning a text node.</summary>
    private static (int Size, bool Bold) ResolveRunFormat(OpenXmlElement runOrParent, ParagraphFormat format, StyleMetrics styles)
    {
        var properties = (runOrParent as Run)?.RunProperties;
        var bold = properties?.Bold is { } weight && weight.Val?.Value != false;

        var size = properties?.FontSize?.Val?.Value;
        if (!string.IsNullOrEmpty(size) && int.TryParse(size, out var halfPoints) && halfPoints > 0)
            return (halfPoints, bold);

        var characterStyle = properties?.RunStyle?.Val?.Value;
        return !string.IsNullOrEmpty(characterStyle) && styles.Formats.TryGetValue(characterStyle, out var resolved)
            ? (resolved.FontSizeHalfPoints, bold)
            : (format.FontSizeHalfPoints, bold);
    }

    /// <summary>
    /// Height of a single rendered line, honouring an explicit <c>w:spacing</c> line rule.
    /// The line box grows to the tallest run on it, which is how the rich-text converter's
    /// headings (larger font on the runs, not on the paragraph) end up taller than body text.
    /// </summary>
    private static double ComputeLineHeight(ParagraphFormat format, int fontSizeHalfPoints)
    {
        var single = fontSizeHalfPoints * TwipsPerHalfPoint * SingleLineHeightRatio;

        if (format.LineSpacing <= 0)
            return single;

        if (format.LineRule == LineSpacingRuleValues.Exact)
            return format.LineSpacing;

        return format.LineRule == LineSpacingRuleValues.AtLeast
            ? Math.Max(single, format.LineSpacing)
            : single * (format.LineSpacing / TwentiethsOfALine);
    }
}
