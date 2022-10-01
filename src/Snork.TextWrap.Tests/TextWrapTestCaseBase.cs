namespace Snork.TextWrap.Tests;

public class TextWrapTestCaseBase
{
    public bool fix_sentence_endings { get; set; }
    public int? max_lines { get; set; }
    public string initial_indent { get; set; }
    public string subsequent_indent { get; set; }
    public int width { get; set; }
    public string text { get; set; }
    public bool break_on_hyphens { get; set; }
    public bool expand_tabs { get; set; }
    public bool replace_whitespace { get; set; }
    public bool break_long_words { get; set; }
    public bool drop_whitespace { get; set; }
    public string placeholder { get; set; }
    public int tabsize { get; set; }
}