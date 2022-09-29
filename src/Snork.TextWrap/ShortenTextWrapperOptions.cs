namespace Snork.TextWrap
{
    public class ShortenTextWrapperOptions : TextWrapperOptionsBase, ITextWrapperOptions
    {
        public virtual int? MaxLines => 1;
    }
}