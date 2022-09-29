namespace Snork.TextWrap
{
    public abstract class TextWrapperOptionsBase : ITextWrapperOptionsBase
    {
        public virtual string InitialIndent { get; set; } = TextWrapperDefaults.InitialIndent;
        public virtual string SubsequentIndent { get; set; } = TextWrapperDefaults.SubsequentIndent;
        public virtual bool ExpandTabs { get; set; } = TextWrapperDefaults.ExpandTabs;
        public virtual bool ReplaceWhitespace { get; set; } = TextWrapperDefaults.ReplaceWhitespace;
        public virtual bool FixSentenceEndings { get; set; } = TextWrapperDefaults.FixSentenceEndings;
        public virtual bool BreakLongWords { get; set; } = TextWrapperDefaults.BreakLongWords;
        public virtual bool DropWhitespace { get; set; } = TextWrapperDefaults.DropWhitespace;
        public virtual bool BreakOnHyphens { get; set; } = TextWrapperDefaults.BreakOnHyphens;
        public virtual int TabSize { get; set; } = TextWrapperDefaults.TabSize;
        public virtual string Placeholder { get; set; } = TextWrapperDefaults.Placeholder;
    }
}