using Newtonsoft.Json;

namespace Snork.TextWrap.Tests;

public class WrapTests : TestBase
{
    public static List<object[]> GetWrap()
    {
        var items = JsonConvert.DeserializeObject<List<ListResultTestCase>>(File.ReadAllText("wrap.json"));
        return items.Select(i => new object[]
        {   i.id,
            i.break_long_words, i.width, i.break_on_hyphens, i.drop_whitespace, i.expand_tabs, i.expected,
            i.fix_sentence_endings, i.initial_indent, i.subsequent_indent, i.max_lines, i.placeholder, i.tabsize,
            i.text, i.replace_whitespace
        }).Take(MaxTests).ToList();
    }


    [Theory]
    [MemberData(nameof(GetWrap))]
    public static void TestWrap(string id, bool break_long_words, int width, bool break_on_hyphens, bool drop_whitespace,
        bool expand_tabs, List<string> expected, bool fix_sentence_endings, string initial_indent,
        string subsequent_indent, int? max_lines, string placeholder, int tabsize, string text,
        bool replace_whitespace)
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
        var actual = TextWrapper.Wrap(text, width, options);
        Assert.NotNull(actual);
        Assert.Equal(actual.Count, expected.Count);
    }
}