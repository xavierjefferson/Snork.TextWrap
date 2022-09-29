namespace Snork.TextWrap
{
    public class TextWrapperOptions : TextWrapperOptionsBase, ITextWrapperOptions
    {
        public virtual int? MaxLines { get; set; } = null;
    }
}