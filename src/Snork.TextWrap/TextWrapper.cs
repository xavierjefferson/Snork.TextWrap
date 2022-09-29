using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

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
        protected static Dictionary<char, char> unicode_whitespace_trans = new Dictionary<char, char>();


        static TextWrapper()
        {
            foreach (var i in _whitespace)
                unicode_whitespace_trans[i] = ' ';
        }

        internal const string word_punct = @"[\w!""\'&.,?]";

        internal const string letter = @"[\\w]";
        internal const string _whitespace = "\t\n\v\f\r ";

        protected static readonly string whitespace = $"[{Regex.Escape(_whitespace)}]";

        protected static readonly string nowhitespace = "[^" + whitespace.Substring(1);

        protected static readonly string AnyWhitespace = $"{whitespace}+";
        protected static readonly Regex t0 = new Regex(AnyWhitespace);
        protected static readonly string EmDashBetweenWords = $"(?<={word_punct})-{{2,}}(?=\\w)";
        protected static readonly Regex t1 = new Regex(EmDashBetweenWords);

        protected static readonly string HyphenatedWord =
            $"-(?:(?<={letter}{{2}}-)|(?<={letter}-{letter}-))|(?={letter}-?{letter})";

        protected static readonly Regex t2 = new Regex(HyphenatedWord);
        protected static readonly string EndOfWord = $"(?={whitespace}|\\Z)";
        protected static readonly Regex t3 = new Regex(EndOfWord);
        protected static readonly string EmDash = $"(?<={word_punct})(?=-{{2,}}\\w)";
        protected static readonly Regex t4 = new Regex(EmDash);

        protected static readonly string WordPossiblyHyphenated =
            $"{nowhitespace}+?(?:{HyphenatedWord}|{EndOfWord}|{EmDash})";

        protected static readonly Regex t5 = new Regex(WordPossiblyHyphenated);

        protected static Regex wordsep_re =
            new Regex($"({AnyWhitespace}|{EmDashBetweenWords}|{WordPossiblyHyphenated})");


        protected static Regex wordsep_simple_re = new Regex($"({whitespace}+)");
        protected static Regex sentence_end_re = new Regex("[a-z][\\.\\!\\?][\\\"\']?\\Z");
        protected static Regex _whitespace_only_re = new Regex("^[ \t]+$", RegexOptions.Multiline);
        protected static Regex _leading_whitespace_re = new Regex("(^[ \t]*)(?:[^ \t\n])", RegexOptions.Multiline);

        public int Width { get; set; } = TextWrapperDefaults.Width;


        public ITextWrapperOptions Options { get; }

        public TextWrapper(ITextWrapperOptions? options = null)
        {
            Options = options ?? new TextWrapperOptions();
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
            if (Options.ExpandTabs) text = text.Replace("\t", new string(' ', Options.TabSize));
            if (Options.ReplaceWhitespace)
            {
                var sb = new StringBuilder();
                foreach (var character in text)
                    if (unicode_whitespace_trans.ContainsKey(character))
                        sb.Append(unicode_whitespace_trans[character]);
                    else
                        sb.Append(character);

                text = sb.ToString();
            }

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
        protected virtual List<string> _split(string text)
        {
            var chunks = new List<string>();
            Regex toUse;
            if (Options.BreakOnHyphens)
            {
                toUse = wordsep_re;
                var m = t0.Split(text);
                var n = t1.Split(text);
                var o = t2.Split(text);
                var p = t3.Split(text);
                var q = t4.Split(text);
                var r = t5.Split(text);
            }
            else
            {
                toUse = wordsep_simple_re;
            }

            chunks = PythonSplit(toUse, text).Where(i => i.Length > 0).ToList();

            return chunks;
        }

        private List<string> PythonSplit(Regex toUse, string text)
        {
            var result = new List<string>();
            var matches = toUse.Matches(text).Cast<Match>().ToList();
            var spl = toUse.Split(text);
            var last = 0;

            foreach (var match in matches)
            {
                result.Add(text.Substring(last, match.Index - last));
                result.Add(match.Groups[0].Value);
                last = match.Index + match.Length;
            }

            if (last < text.Length) result.Add(text.Substring(last));

            return result;
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
        public virtual void _fix_sentence_endings(List<string> chunks)
        {
            var i = 0;
            while (i < chunks.Count - 1)
                if (chunks[i + 1] == " " && sentence_end_re.IsMatch(chunks[i]))
                {
                    chunks[i + 1] = "  ";
                    i += 2;
                }
                else
                {
                    i += 1;
                }
        }

        public static int rfind(string input, char sub, int start, int end = int.MaxValue)
        {
            if (end == int.MaxValue || end > input.Length - 1) end = input.Length - 1;

            for (var i = end; i >= start; i--)
                if (input[i] == sub)
                    return i;

            return -1;
        }

        /// <summary>
        /// Handle a chunk of text (most likely a word, not whitespace) that
        /// is too long to fit in any line.
        /// </summary>
        /// <param name="reversed_chunks"></param>
        /// <param name="cur_line"></param>
        /// <param name="cur_len"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        protected virtual void HandleLongWord(List<string> reversed_chunks, List<string> cur_line, int cur_len,
            int width)
        {
            var space_left = 0;
            // Figure out when indent is larger than the specified width, and make
            // sure at least one character is stripped off on every pass
            if (width < 1)
                space_left = 1;
            else
                space_left = width - cur_len;
            // If we're allowed to break long words, then do so: put as much
            // of the next chunk onto the current line as will fit.
            if (Options.BreakLongWords)
            {
                var end = space_left;
                var chunk = reversed_chunks.Last();
                if (Options.BreakOnHyphens && chunk.Length > space_left)
                {
                    // break after last hyphen, but only if there are
                    // non-hyphens before it
                    for (var i = space_left; i >= 0; i--)
                    {
                    }

                    var hyphen = rfind(chunk, '-', 0, space_left);
                    if (hyphen > 0 && chunk.Substring(0, hyphen).Any(i => i != '-')) end = hyphen + 1;
                }

                cur_line.Add(chunk.Substring(0, end));
                reversed_chunks[^1] = chunk.Substring(end);
            }
            else if (!cur_line.Any())
            {
                // Otherwise, we have to preserve the long word intact.  Only add
                // it to the current line if there's nothing already there --
                // that minimizes how much we violate the width constraint.
                cur_line.Add(Pop(reversed_chunks));
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
                var currentLine = new List<string>();
                var currentLength = 0;
                // Figure out which static string will prefix this line.
                if (lines.Any())
                    indent = Options.SubsequentIndent;
                else
                    indent = Options.InitialIndent;
                // Maximum width for this line.
                var width = Width - indent.Length;
                // First chunk on line is whitespace -- drop it, unless this
                // is the very beginning of the text (ie. no lines started yet).
                if (Options.DropWhitespace && string.IsNullOrWhiteSpace(chunks.Last()) && lines.Any()) Pop(chunks);
                while (chunks.Any())
                {
                    var l = chunks.Last().Length;
                    // Can at least squeeze this chunk onto the current line.
                    if (currentLength + l <= width)
                    {
                        currentLine.Add(Pop(chunks));
                        currentLength += l;
                    }
                    else
                    {
                        // Nope, this line is full.
                        break;
                    }
                }

                // The current line is full, and the next chunk is too big to
                // fit on *any* line (not just this one).
                if (chunks.Any() && chunks.Last().Length > width)
                {
                    HandleLongWord(chunks, currentLine, currentLength, width);
                    currentLength = currentLine.Sum(i => i.Length);
                }

                // If the last chunk on this line is all whitespace, drop it.
                if (Options.DropWhitespace && currentLine.Any() && currentLine.Last().Trim() == "")
                {
                    currentLength -= currentLine.Last().Length;
                    currentLine.RemoveAt(currentLine.Count - 1);
                }

                if (currentLine.Any())
                {
                    if (Options.MaxLines == null || lines.Count + 1 < Options.MaxLines ||
                        ((!chunks.Any() ||
                          (Options.DropWhitespace && chunks.Count == 1 && chunks[0].Trim().Length == 0)) &&
                         currentLength <= width))
                    {
                        // Convert current line back to a string and store it in
                        // list of all lines (return value).
                        lines.Add(indent + string.Concat(currentLine));
                    }
                    else
                    {
                        if (currentLine.Any())
                        {
                            do
                            {
                                if (currentLine.Last().Trim().Length > 0 &&
                                    currentLength + Options.Placeholder.Count() <= width)
                                {
                                    currentLine.Add(Options.Placeholder);
                                    lines.Add(indent + string.Concat(currentLine));

                                    break;
                                }

                                currentLength -= currentLine.Last().Length;

                                currentLine.RemoveAt(currentLine.Count - 1);
                            } while (currentLine.Any());
                        }
                        else
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
            return _split(text);
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
        protected virtual List<string> _wrap(string text)
        {
            var chunks = SplitChunks(text);
            if (Options.FixSentenceEndings) _fix_sentence_endings(chunks);
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
        protected virtual string _fill(string text)
        {
            return string.Join('\n', _wrap(text));
        }

        /// <summary>
        /// Adds 'prefix' to the beginning of selected lines in 'text'.
        /// 
        /// If 'predicate' is provided, 'prefix' will only be added to the lines
        /// where 'predicate(line)' is True. If 'predicate' is not provided,
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
            var w = new TextWrapper(options ?? new ShortenTextWrapperOptions()) { Width = width };
            return w._fill(string.Join(" ", split(text.Trim())));
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
            text = _whitespace_only_re.Replace(text, string.Empty);
            var indents = _leading_whitespace_re.Matches(text).Cast<Match>().Select(i => i.Groups[0].Value)
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
                                { Index = aElement.Index, Item1 = aElement.Value, Item2 = bElement.Value }).ToList();
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
            var w = new TextWrapper(options) { Width = width };
            return w._wrap(text);
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
            var w = new TextWrapper(options) { Width = width };
            return w._fill(text);
        }

        protected static Regex NoWhitespaceRegex =
            new Regex($"{nowhitespace}+");

        protected static List<string> split(string input)
        {
            return NoWhitespaceRegex.Matches(input).Cast<Match>().Select(i => i.Groups[0].Value).ToList();
        }

        protected static T Pop<T>(List<T> list)
        {
            var p = list.Last();
            list.RemoveAt(list.Count - 1);
            return p;
        }
    }
}