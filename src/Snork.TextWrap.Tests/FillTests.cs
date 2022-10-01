using System.Runtime.CompilerServices;

namespace Snork.TextWrap.Tests;

public abstract class MyTestBase : TestBase
{
    public abstract string Filename { get; }
}
public class FillTests : MyTestBase
{
    public override string Filename { get {
        return "fill.json";
    }}

    public static List<object[]> GetFillDropwhitespace()
    {
        return GetList("fill.json", i => i.drop_whitespace, i =>9000- i.width);
    }

    public static List<object[]> GetFillNoDropWhitespace()
    {
        return GetList("fill.json", i => !i.drop_whitespace, i => 9000 - i.width);
    }
    public static List<object[]> GetFillBreakOnHyphens()
    {
        return GetList("fill.json", i => i.break_on_hyphens, i => 9000 - i.width);
    }

    public static List<object[]> GetFillNoBreakOnHyphens()
    {
        return GetList("fill.json", i => !i.break_on_hyphens, i=>9000 - i.width);
    }

    public static List<object[]> GetFillBreakLongWords()
    {
        return GetList("fill.json", i => i.break_long_words, i => 9000 - i.width);
    }

    public static List<object[]> GetFillNoBreakLongWords()
    {
        return GetList("fill.json", i => !i.break_long_words);
    }
    [Theory]
    [MemberData(nameof(GetFillDropwhitespace))]
    public static void TestFillDropwhitespace(string id, bool break_long_words, int width, bool break_on_hyphens,
        bool drop_whitespace,
        bool expand_tabs, string expected, bool fix_sentence_endings, string initial_indent,
        string subsequent_indent, int? max_lines, string placeholder, int tabsize, string text,
        bool replace_whitespace)
    {
        TestFill(id, break_long_words, width, break_on_hyphens, drop_whitespace, expand_tabs, expected,
            fix_sentence_endings, initial_indent, subsequent_indent, max_lines, placeholder, tabsize, text,
            replace_whitespace);
    }

    [Theory]
    [MemberData(nameof(GetFillNoDropWhitespace))]
    public static void TestFillNoDropWhitespace(string id, bool break_long_words, int width, bool break_on_hyphens,
        bool drop_whitespace,
        bool expand_tabs, string expected, bool fix_sentence_endings, string initial_indent,
        string subsequent_indent, int? max_lines, string placeholder, int tabsize, string text,
        bool replace_whitespace)
    {
        TestFill(id, break_long_words, width, break_on_hyphens, drop_whitespace, expand_tabs, expected,
            fix_sentence_endings, initial_indent, subsequent_indent, max_lines, placeholder, tabsize, text,
            replace_whitespace);
    }
    [Theory]
    [MemberData(nameof(GetFillBreakLongWords))]
    public static void TestFillBreakLongWords(string id, bool break_long_words, int width, bool break_on_hyphens,
        bool drop_whitespace,
        bool expand_tabs, string expected, bool fix_sentence_endings, string initial_indent,
        string subsequent_indent, int? max_lines, string placeholder, int tabsize, string text,
        bool replace_whitespace)
    {
        TestFill(id, break_long_words, width, break_on_hyphens, drop_whitespace, expand_tabs, expected,
            fix_sentence_endings, initial_indent, subsequent_indent, max_lines, placeholder, tabsize, text,
            replace_whitespace);
    }

    [Theory]
    [MemberData(nameof(GetFillNoBreakLongWords))]
    public static void TestFillNoBreakLongWords(string id, bool break_long_words, int width, bool break_on_hyphens,
        bool drop_whitespace,
        bool expand_tabs, string expected, bool fix_sentence_endings, string initial_indent,
        string subsequent_indent, int? max_lines, string placeholder, int tabsize, string text,
        bool replace_whitespace)
    {
        TestFill(id, break_long_words, width, break_on_hyphens, drop_whitespace, expand_tabs, expected,
            fix_sentence_endings, initial_indent, subsequent_indent, max_lines, placeholder, tabsize, text,
            replace_whitespace);
    }

    [Theory]
    [MemberData(nameof(GetFillBreakOnHyphens))]
    public static void TestFillBreakOnHyphens(string id, bool break_long_words, int width, bool break_on_hyphens,
        bool drop_whitespace,
        bool expand_tabs, string expected, bool fix_sentence_endings, string initial_indent,
        string subsequent_indent, int? max_lines, string placeholder, int tabsize, string text,
        bool replace_whitespace)
    {
        TestFill(id, break_long_words, width, break_on_hyphens, drop_whitespace, expand_tabs, expected,
            fix_sentence_endings, initial_indent, subsequent_indent, max_lines, placeholder, tabsize, text,
            replace_whitespace);
    }

    [Theory]
    [MemberData(nameof(GetFillNoBreakOnHyphens))]
    public static void TestFillNoBreakOnHyphens(string id, bool break_long_words, int width, bool break_on_hyphens,
        bool drop_whitespace,
        bool expand_tabs, string expected, bool fix_sentence_endings, string initial_indent,
        string subsequent_indent, int? max_lines, string placeholder, int tabsize, string text,
        bool replace_whitespace)
    {
        TestFill(id, break_long_words, width, break_on_hyphens, drop_whitespace, expand_tabs, expected,
            fix_sentence_endings, initial_indent, subsequent_indent, max_lines, placeholder, tabsize, text,
            replace_whitespace);
    }

    private static void TestFill(string id, bool break_long_words, int width, bool break_on_hyphens, bool drop_whitespace,
        bool expand_tabs, string expected, bool fix_sentence_endings, string initial_indent,
        string subsequent_indent,
        int? max_lines, string placeholder, int tabsize, string text, bool replace_whitespace)
    {
        var options = new TextWrapperOptions()
        {
            BreakLongWords = break_long_words,
            BreakOnHyphens = break_on_hyphens,
            DropWhitespace = drop_whitespace,
            ExpandTabs = expand_tabs,
            FixSentenceEndings = fix_sentence_endings,
            InitialIndent = initial_indent,
            SubsequentIndent = subsequent_indent,
            TabSize = tabsize,
            Placeholder = placeholder,
            MaxLines = max_lines,
            ReplaceWhitespace = replace_whitespace
        };
        var actual = TextWrapper.Fill(text, width, options);
        Assert.Equal(expected.Replace("\r\n", "\0").Replace("\n", "\r\n").Replace("\0", "\r\n"), actual);
    }
}