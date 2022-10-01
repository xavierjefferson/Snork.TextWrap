namespace Snork.TextWrap
{
    public interface ITextWrapperOptions : ITextWrapperOptionsBase
    {
        int? MaxLines { get; }
    }
}