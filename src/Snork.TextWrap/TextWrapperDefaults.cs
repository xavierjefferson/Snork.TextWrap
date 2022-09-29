namespace Snork.TextWrap
{
    public static class TextWrapperDefaults
    {
        public const int Width = 70;
        public const bool ReplaceWhitespace = true;
        public const bool ExpandTabs = true;
        public const bool FixSentenceEndings = false;
        public const bool BreakLongWords = true;
        public const bool DropWhitespace = true;
        public const bool BreakOnHyphens = true;
        public const int TabSize = 8;
        public const string Placeholder = " [...]";
        public const string InitialIndent = "";
        public const string SubsequentIndent = "";
    }
}