namespace Snork.TextWrap
{
    public interface ITextWrapperOptionsBase
    {
        string InitialIndent { get; set; }
        string SubsequentIndent { get; set; }
        bool ExpandTabs { get; set; }
        bool ReplaceWhitespace { get; set; }
        bool FixSentenceEndings { get; set; }
        bool BreakLongWords { get; set; }
        bool DropWhitespace { get; set; }
        bool BreakOnHyphens { get; set; }
        int TabSize { get; set; }
        string Placeholder { get; set; }
    }

    public interface ITextWrapperOptions : ITextWrapperOptionsBase
    {
        int? MaxLines { get; }
    }
}