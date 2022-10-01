using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Snork.TextWrap
{
    /// <summary>
    /// Object for wrapping/filling text.  The public interface consists of
    /// the wrap() and fill() methods; the other methods are just there for
    /// subclasses to override in order to tweak the default behaviour.
    /// If you want to completely replace the main wrapping algorithm,
    /// you'll probably have to override _wrap_chunks().
    /// 
    /// Several instance attributes control various aspects of wrapping:
    /// width (default: 70)
    /// the maximum width of wrapped lines (unless break_long_words
    /// is false)
    /// initial_indent (default: "")
    /// string that will be prepended to the first line of wrapped
    /// output.  Counts towards the line's width.
    /// subsequent_indent (default: "")
    /// string that will be prepended to all lines save the first
    /// of wrapped output; also counts towards each line's width.
    /// expand_tabs (default: true)
    /// Expand tabs in input text to spaces before further processing.
    /// Each tab will become 0 .. 'tabsize' spaces, depending on its position
    /// in its line.  If false, each tab is treated as a single character.
    /// tabsize (default: 8)
    /// Expand tabs in input text to 0 .. 'tabsize' spaces, unless
    /// 'expand_tabs' is false.
    /// replace_whitespace (default: true)
    /// Replace all whitespace characters in the input text by spaces
    /// after tab expansion.  Note that if expand_tabs is false and
    /// replace_whitespace is true, every tab will be converted to a
    /// single space!
    /// fix_sentence_endings (default: false)
    /// Ensure that sentence-ending punctuation is always followed
    /// by two spaces.  Off by default because the algorithm is
    /// (unavoidably) imperfect.
    /// break_long_words (default: true)
    /// Break words longer than 'width'.  If false, those words will not
    /// be broken, and some lines might be longer than 'width'.
    /// break_on_hyphens (default: true)
    /// Allow breaking hyphenated words. If true, wrapping will occur
    /// preferably on whitespaces and right after hyphens part of
    /// compound words.
    /// drop_whitespace (default: true)
    /// Drop leading and trailing whitespace from lines.
    /// max_lines (default: None)
    /// Truncate wrapped lines.
    /// placeholder (default: ' [...]')
    /// Append to the last line of truncated text.
    /// 
    /// </summary>
    public class TextWrapper
    {
        //internal const string WordPunctuationRegexPattern = @"[\w!""\'&.,?]";
        internal const string WordPunctuationRegexPattern = @"[\w!""'&.,?]";
        internal const string LetterRegexPattern = @"[^\\d\\W]";
        internal const string WhitespaceCharacters = "\t\n\v\f\r ";

        protected static readonly string WhitespaceRegexPattern = $"[{Regex.Escape(WhitespaceCharacters)}]";
        protected static readonly string NoWhitespaceRegexPattern = "[^" + WhitespaceRegexPattern.Substring(1);
        protected static readonly string AnyWhiteSpaceRegexPattern = $"{WhitespaceRegexPattern}+";

        //protected static readonly string
        //    EmDashBetweenWordsPattern = $"(?<={WordPunctuationRegexPattern})-{{2,}}(?=\\w)";
        protected static readonly string
            EmDashBetweenWordsPattern = $"(?<={WordPunctuationRegexPattern}) -{{2,}} (?=\\\\w)";

        protected static readonly string HyphenatedWordRegexPattern =
            $"-(?: (?<={LetterRegexPattern}{{2}}-)|(?<={LetterRegexPattern}-{LetterRegexPattern}-))|(?= {LetterRegexPattern} -? {LetterRegexPattern})";

        protected static readonly string EndOfWordRegexPattern = $"(?={WhitespaceRegexPattern}|\\Z)";
        protected static readonly string EmDashRegexPattern = $"(?<={WordPunctuationRegexPattern})(?=-{{2,}}\\w)";

        protected static readonly string WordPossiblyHyphenatedRegexPattern =
            //$"{NoWhitespaceRegexPattern}+?(?:{HyphenatedWordRegexPattern}|{EndOfWordRegexPattern}|{EmDashRegexPattern})";
            $"{NoWhitespaceRegexPattern}+?-(?:(?<={LetterRegexPattern}{{2}}-)|(?<={LetterRegexPattern}-[^\\d\\W]-))";

        protected static Regex WordSeparatorRegex =
            new Regex(
                $"({AnyWhiteSpaceRegexPattern}|{EmDashBetweenWordsPattern}|{WordPossiblyHyphenatedRegexPattern})");

        protected static Regex WordSeparatorSimpleRegex = new Regex($"({WhitespaceRegexPattern}+)");
        protected static Regex SentenceEndRegex = new Regex("[a-z][\\.\\!\\?][\\\"\']?\\Z");
        protected static Regex WhitespaceOnlyRegex = new Regex("^[ \t]+$", RegexOptions.Multiline);
        protected static Regex LeadingWhitespaceRegex = new Regex("(^[ \t]*)(?:[^ \t\n])", RegexOptions.Multiline);

        public int Width { get; }


        public ITextWrapperOptions Options { get; }

        public TextWrapper(int width = TextWrapperDefaults.Width, ITextWrapperOptions? options = null)
        {
            Width = width;
            Options = options ?? new TextWrapperOptions();
        }

        internal static string ExpandTabs(string input, int tabSize)
        {
            var stringBuilder = new StringBuilder();
            var column = 0;
            foreach (var character in input)
                switch (character)
                {
                    case '\n':
                    case '\r':
                        column = 0;
                        stringBuilder.Append(character);
                        break;
                    case '\t':
                        if (tabSize > 0)
                        {
                            var tabs = tabSize - column % tabSize;
                            stringBuilder.Append(new String(' ', tabs));
                            column = 0;
                        }
                        break;
                    default:
                        column++;
                        stringBuilder.Append(character);
                        break;
                }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// (possibly useful for subclasses to override)
        /// _munge_whitespace(text : string) -> string
        /// 
        /// Munge whitespace in text: expand tabs and convert all other
        /// whitespace characters to spaces.  Eg. " foo\\tbar\\n\\nbaz"
        /// becomes " foo    bar  baz".
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected virtual string MungeWhitespace(string text)
        {
            if (Options.ExpandTabs) text = ExpandTabs(text, Options.TabSize);
            if (Options.ReplaceWhitespace) text = Regex.Replace(text, WhitespaceRegexPattern, " ");
            return text;
        }

        /// <summary>
        /// Split the text to wrap into indivisible chunks.  Chunks are
        /// not quite the same as words; see _wrap_chunks() for full
        /// details.  As an example, the text
        /// Look, goof-ball -- use the -b option!
        /// breaks into the following chunks:
        /// 'Look,', ' ', 'goof-', 'ball', ' ', '--', ' ',
        /// 'use', ' ', 'the', ' ', '-b', ' ', 'option!'
        /// if break_on_hyphens is True, or in:
        /// 'Look,', ' ', 'goof-ball', ' ', '--', ' ',
        /// 'use', ' ', 'the', ' ', '-b', ' ', option!'
        /// otherwise.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected virtual List<string> SplitByWordSeparators(string text)
        {
            var regexToUse = Options.BreakOnHyphens ? WordSeparatorRegex : WordSeparatorSimpleRegex;
            var chunks = regexToUse.Split(text).Where(i => i.Length > 0).ToList();
            return chunks;
        }

        /// <summary>
        /// Correct for sentence endings buried in 'chunks'.  Eg. when the
        /// original text contains "... foo.\\nBar ...", munge_whitespace()
        /// and split() will convert that to [..., "foo.", " ", "Bar", ...]
        /// which has one too few spaces; this method simply changes the one
        /// space to two.
        /// </summary>
        /// <param name="chunks"></param>
        /// <returns></returns>
        public virtual void FixSentenceEndings(List<string> chunks)
        {
            var i = 0;
            while (i < chunks.Count - 1)
                if (chunks[i + 1] == " " && SentenceEndRegex.IsMatch(chunks[i]))
                {
                    chunks[i + 1] = "  ";
                    i += 2;
                }
                else
                {
                    i += 1;
                }
        }

        /// <summary>
        /// Return the highest index in the string where substring sub is found,
        /// such that sub is contained within s[start:end].
        /// Optional arguments start and end are interpreted as
        /// in slice notation. Return -1 on failure.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="sub"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        private static int RFind(string input, char sub, int start, int end)
        {
            for (var i = end; i >= start; i--)
                if (input[i] == sub)
                    return i;

            return -1;
        }

        /// <summary>
        /// Handle a chunk of text (most likely a word, not whitespace) that
        /// is too long to fit in any line.
        /// </summary>
        /// <param name="reversedChunks"></param>
        /// <param name="lineBuilder"></param>
        /// <param name="currentLength"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        protected virtual void HandleLongWord(List<string> reversedChunks, LineBuilder lineBuilder, int currentLength,
            int width)
        {
            int spaceLeft;
            // Figure out when indent is larger than the specified width, and make
            // sure at least one character is stripped off on every pass
            if (width < 1)
                spaceLeft = 1;
            else
                spaceLeft = width - currentLength;
            // If we're allowed to break long words, then do so: put as much
            // of the next chunk onto the current line as will fit.
            if (Options.BreakLongWords)
            {
                var end = spaceLeft;
                var chunk = reversedChunks.Last();
                if (Options.BreakOnHyphens && chunk.Length > spaceLeft)
                {
                    // break after last hyphen, but only if there are
                    // non-hyphens before it

                    var hyphen = RFind(chunk, '-', 0, spaceLeft);
                    if (hyphen > 0 && chunk.Substring(0, hyphen).Any(i => i != '-')) end = hyphen + 1;
                }

                lineBuilder.Add(chunk.Substring(0, end));
                reversedChunks[^1] = chunk.Substring(end);
            }
            else if (!lineBuilder.Any())
            {
                // Otherwise, we have to preserve the long word intact.  Only add
                // it to the current line if there's nothing already there --
                // that minimizes how much we violate the width constraint.
                lineBuilder.Add(Pop(reversedChunks));
                // If we're not allowed to break long words, and there's already
                // text on the current line, do nothing.  Next time through the
                // main loop of _wrap_chunks(), we'll wind up here again, but
                // cur_len will be zero, so the next line will be entirely
                // devoted to the long word that we can't handle right now.
            }
        }

        /// <summary>
        /// Wrap a sequence of text chunks and return a list of lines of
        /// length 'this.width' or less.  (If 'break_long_words' is false,
        /// some lines may be longer than this.)  Chunks correspond roughly
        /// to words and the whitespace between them: each chunk is
        /// indivisible (modulo 'break_long_words'), but a line break can
        /// come between any two chunks.  Chunks should not have internal
        /// whitespace; ie. a chunk is either all whitespace or a "word".
        /// Whitespace chunks will be removed from the beginning and end of
        /// lines, but apart from that whitespace is preserved.
        /// </summary>
        /// <param name="chunks"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected virtual List<string> WrapChunks(List<string> chunks)
        {
            string indent;
            var lines = new List<string>();
            if (Width <= 0) throw new InvalidOperationException($"invalid width {Width} (must be > 0)");
            if (Options.MaxLines != null)
            {
                if (Options.MaxLines > 1)
                    indent = Options.SubsequentIndent;
                else
                    indent = Options.InitialIndent;
                if (indent.Count() + Options.Placeholder.TrimStart().Length > Width)
                    throw new InvalidOperationException("placeholder too large for max width");
            }

            // Arrange in reverse order so items can be efficiently popped
            // from a stack of chucks.
            chunks.Reverse();
            while (chunks.Any())
            {
                // Start the list of chunks that will make up the current line.
                // cur_len is just the length of all the chunks in cur_line.
                var lineBuilder = new LineBuilder();

                // Figure out which static string will prefix this line.
                indent = lines.Any() ? Options.SubsequentIndent : Options.InitialIndent;
                // Maximum width for this line.
                var width = Width - indent.Length;
                // First chunk on line is whitespace -- drop it, unless this
                // is the very beginning of the text (ie. no lines started yet).
                if (Options.DropWhitespace && string.IsNullOrWhiteSpace(chunks.Last()) && lines.Any()) Pop(chunks);
                while (chunks.Any())
                {
                    var l = chunks.Last().Length;
                    // Can at least squeeze this chunk onto the current line.
                    if (lineBuilder.Length + l <= width)
                        lineBuilder.Add(Pop(chunks));
                    else
                        // Nope, this line is full.
                        break;
                }

                // The current line is full, and the next chunk is too big to
                // fit on *any* line (not just this one).
                if (chunks.Any() && chunks.Last().Length > width)
                    HandleLongWord(chunks, lineBuilder, lineBuilder.Length, width);

                // If the last chunk on this line is all whitespace, drop it.
                if (Options.DropWhitespace && lineBuilder.Any() && string.IsNullOrWhiteSpace(lineBuilder.Last()))
                    lineBuilder.RemoveLast();

                if (lineBuilder.Any())
                {
                    if (Options.MaxLines == null || lines.Count + 1 < Options.MaxLines ||
                        ((!chunks.Any() ||
                          (Options.DropWhitespace && chunks.Count == 1 && string.IsNullOrWhiteSpace(chunks[0]))) &&
                         lineBuilder.Length <= width))
                    {
                        // Convert current line back to a string and store it in
                        // list of all lines (return value).
                        lines.Add(indent + lineBuilder.Concat());
                    }
                    else
                    {
                        var ended = false;
                        while (lineBuilder.Any())
                        {
                            if (!string.IsNullOrWhiteSpace(lineBuilder.Last()) &&
                                lineBuilder.Length + Options.Placeholder.Count() <= width)
                            {
                                lineBuilder.Add(Options.Placeholder);
                                lines.Add(indent + lineBuilder.Concat());
                                ended = true;
                                break;
                            }

                            lineBuilder.RemoveLast();
                        }

                        if (!ended)
                        {
                            if (lines.Any())
                            {
                                var previousLine = lines.Last().TrimEnd();
                                if (previousLine.Length + Options.Placeholder.Length <= Width)
                                {
                                    lines[^1] = previousLine + Options.Placeholder;
                                    break;
                                }
                            }

                            lines.Add(indent + Options.Placeholder.TrimStart());
                        }

                        break;
                    }
                }
            }

            return lines;
        }

        protected virtual List<string> SplitChunks(string text)
        {
            text = MungeWhitespace(text);
            return SplitByWordSeparators(text);
        }


        /// <summary>
        /// 
        /// Reformat the single paragraph in 'text' so it fits in lines of
        /// no more than 'this.width' columns, and return a list of wrapped
        /// lines.  Tabs in 'text' are expanded,
        /// and all other whitespace characters (including newline) are
        /// converted to space.  
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected virtual List<string> _Wrap(string text)
        {
            var chunks = SplitChunks(text);
            if (Options.FixSentenceEndings) FixSentenceEndings(chunks);
            return WrapChunks(chunks);
        }


        /// 
        /// <summary>
        /// Reformat the single paragraph in 'text' to fit in lines of no
        /// more than 'self.width' columns, and return a new string
        /// containing the entire wrapped paragraph.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        protected virtual string _Fill(string text)
        {
            return string.Join(Environment.NewLine, _Wrap(text));
        }

        /// <summary>
        /// Adds 'prefix' to the beginning of selected lines in 'text'.
        /// 
        /// If 'predicate' is provided, 'prefix' will only be added to the lines
        /// where 'predicate(line)' is true. If 'predicate' is not provided,
        /// it will default to adding 'prefix' to all non-empty lines that do not
        /// consist solely of whitespace characters.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="prefix"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static string Indent(string text, string prefix, Func<string, bool>? predicate = null)
        {
            if (predicate == null) predicate = line => !string.IsNullOrWhiteSpace(line);
            var result = new List<string>();
            using (var stringReader = new StringReader(text))
            {
                while (true)
                {
                    var line = stringReader.ReadLine();
                    if (line == null) break;
                    result.Add(predicate(line) ? prefix + line : line);
                }
            }

            return string.Join(Environment.NewLine, result);
        }

        /// <summary>
        /// Collapse and truncate the given text to fit in the given width.
        /// 
        /// The text first has its whitespace collapsed.  If it then fits in
        /// the *width*, it is returned as is.  Otherwise, as many words
        /// as possible are joined and then the placeholder is appended::
        /// 
        /// >>> TextWrapper.Shorten("Hello world!", width=12)
        /// 'Hello world!'
        /// >>> TextWrapper.Shorten("Hello world!", width=11)
        /// 'Hello [...]'
        /// </summary>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string Shorten(string text, int width, ShortenTextWrapperOptions? options = null)
        {
            var w = new TextWrapper(width, options ?? new ShortenTextWrapperOptions());
            return w._Fill(string.Join(" ", SplitByWhitespace(text.Trim())));
        }

        /// <summary>
        /// Remove any common leading whitespace from every line in `text`.
        /// 
        /// This can be used to make triple-quoted strings line up with the left
        /// edge of the display, while still presenting them in the source code
        /// in indented form.
        /// 
        /// Note that tabs and spaces are both treated as whitespace, but they
        /// are not equal: the lines "  hello" and "\\thello" are
        /// considered to have no common leading whitespace.
        /// 
        /// Entirely blank lines are normalized to a newline character.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Dedent(string text)
        {
            // Look for the longest leading string of spaces and tabs common to
            // all lines.
            string? margin = null;
            text = WhitespaceOnlyRegex.Replace(text, string.Empty);
            var indents = LeadingWhitespaceRegex.Matches(text).Select(i => i.Groups[0].Value)
                .ToList();
            foreach (var indent in indents)
                if (margin == null)
                {
                    margin = indent;
                }
                else if (indent.StartsWith(margin))
                {
                    // Current line more deeply indented than previous winner:
                    // no change (previous winner is still on top).
                }
                else if (margin.StartsWith(indent))
                {
                    // Current line consistent with and no deeper than previous winner:
                    // it's the new winner.
                    margin = indent;
                }
                else
                {
                    // Find the largest common whitespace between current line and previous
                    // winner.
                    var valueTuples = margin.Select((value, index) => new { Value = value, Index = index })
                        .Join(indent.Select((value, index) => new { Value = value, Index = index }), i => i.Index,
                            i => i.Index,
                            (aElement, bElement) => new
                                { aElement.Index, Item1 = aElement.Value, Item2 = bElement.Value }).ToList();
                    var c = valueTuples.FirstOrDefault(i => i.Item1 != i.Item2);
                    if (c != null) margin = margin.Substring(c.Index);
                }

            if (!string.IsNullOrEmpty(margin))
                text = Regex.Replace($"^{margin}", text, string.Empty, RegexOptions.Multiline);
            return text;
        }

        /// <summary>
        /// Wrap a single paragraph of text, returning a list of wrapped lines.
        /// 
        /// Reformat the single paragraph in 'text' so it fits in lines of no
        /// more than 'width' columns, and return a list of wrapped lines.  By
        /// default, tabs in 'text' are expanded, and
        /// all other whitespace characters (including newline) are converted to
        /// space.  See TextWrapper class for available keyword args to customize
        /// wrapping behaviour.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static List<string> Wrap(string text, int width = TextWrapperDefaults.Width,
            TextWrapperOptions? options = null)
        {
            var w = new TextWrapper(width, options);
            return w._Wrap(text);
        }

        /// <summary>
        /// Fill a single paragraph of text, returning a new string.
        /// 
        /// Reformat the single paragraph in 'text' to fit in lines of no more
        /// than 'width' columns, and return a new string containing the entire
        /// wrapped paragraph.  As with wrap(), tabs are expanded and other
        /// whitespace characters converted to space.  See TextWrapper class for
        /// available keyword args to customize wrapping behaviour.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="width"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public static string Fill(string text, int width = TextWrapperDefaults.Width,
            TextWrapperOptions? options = null)
        {
            var w = new TextWrapper(width, options);
            return w._Fill(text);
        }

        protected static Regex NoWhitespaceRegex =
            new Regex($"{NoWhitespaceRegexPattern}+");

        /// <summary>
        /// Split using whitespace between words
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        protected static List<string> SplitByWhitespace(string input)
        {
            return NoWhitespaceRegex.Matches(input).Select(i => i.Groups[0].Value).ToList();
        }

        protected static T Pop<T>(List<T> list)
        {
            var last = list.Last();
            list.RemoveAt(list.Count - 1);
            return last;
        }
    }
}