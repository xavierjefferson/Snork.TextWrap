namespace Snork.TextWrap.Tests;

public class ShortenTests : TestBase
{
    [Theory]
    [MemberData(nameof(GetShorten))]
    public static void TestShorten(string id, bool break_long_words, int width, bool break_on_hyphens, bool drop_whitespace,
        bool expand_tabs, string expected, bool fix_sentence_endings, string initial_indent,
        string subsequent_indent, int? max_lines, string placeholder, int tabsize, string text,
        bool replace_whitespace)
    {
        var options = new ShortenTextWrapperOptions()
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
            ReplaceWhitespace = replace_whitespace
        };
        var actual = TextWrapper.Shorten(text, width, options);
        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    public static List<object[]> GetShorten()
    {
        return GetList("shorten.json", i => true);
    }
}