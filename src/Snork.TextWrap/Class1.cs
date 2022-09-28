// Text wrapping and filling.
// 

using System.Text.RegularExpressions;

namespace Namespace
{

    using re;

    using System.Collections.Generic;

    using System;

    using System.Linq;

    using System.Collections;

    using System.Diagnostics;

    public static class Module
    {

        public static object @__all__ = new List<object> {
            "TextWrapper",
            "wrap",
            "fill",
            "dedent",
            "indent",
            "shorten"
        };

        public static string _whitespace = "\t\n\x0b\x0c\r ";

        // 
        //     Object for wrapping/filling text.  The public interface consists of
        //     the wrap() and fill() methods; the other methods are just there for
        //     subclasses to override in order to tweak the default behaviour.
        //     If you want to completely replace the main wrapping algorithm,
        //     you'll probably have to override _wrap_chunks().
        // 
        //     Several instance attributes control various aspects of wrapping:
        //       width (default: 70)
        //         the maximum width of wrapped lines (unless break_long_words
        //         is false)
        //       initial_indent (default: "")
        //         string that will be prepended to the first line of wrapped
        //         output.  Counts towards the line's width.
        //       subsequent_indent (default: "")
        //         string that will be prepended to all lines save the first
        //         of wrapped output; also counts towards each line's width.
        //       expand_tabs (default: true)
        //         Expand tabs in input text to spaces before further processing.
        //         Each tab will become 0 .. 'tabsize' spaces, depending on its position
        //         in its line.  If false, each tab is treated as a single character.
        //       tabsize (default: 8)
        //         Expand tabs in input text to 0 .. 'tabsize' spaces, unless
        //         'expand_tabs' is false.
        //       replace_whitespace (default: true)
        //         Replace all whitespace characters in the input text by spaces
        //         after tab expansion.  Note that if expand_tabs is false and
        //         replace_whitespace is true, every tab will be converted to a
        //         single space!
        //       fix_sentence_endings (default: false)
        //         Ensure that sentence-ending punctuation is always followed
        //         by two spaces.  Off by default because the algorithm is
        //         (unavoidably) imperfect.
        //       break_long_words (default: true)
        //         Break words longer than 'width'.  If false, those words will not
        //         be broken, and some lines might be longer than 'width'.
        //       break_on_hyphens (default: true)
        //         Allow breaking hyphenated words. If true, wrapping will occur
        //         preferably on whitespaces and right after hyphens part of
        //         compound words.
        //       drop_whitespace (default: true)
        //         Drop leading and trailing whitespace from lines.
        //       max_lines (default: None)
        //         Truncate wrapped lines.
        //       placeholder (default: ' [...]')
        //         Append to the last line of truncated text.
        //     
        public class TextWrapper
        {

            public object unicode_whitespace_trans = new Dictionary<object, object>
            {
            };

            public object uspace = ord(" ");

            static TextWrapper()
            {
                unicode_whitespace_trans[ord(x)] = uspace;
            }

            public const string word_punct = @"[\w!""\'&.,?]";

            public const string letter = @"[^\d\W]";

            public static readonly string whitespace = $@"[{Regex.Escape(_whitespace)}]";

            public static readonly string nowhitespace = "[^" + whitespace.Substring(1);

            public Regex wordsep_re = new Regex(String.Format(@"
        ( # any whitespace
          %(ws)s+
        | # em-dash between words
          (?<=%(wp)s) -{2,} (?=\w)
        | # word, possibly hyphenated
          %(nws)s+? (?:
            # hyphenated word
              -(?: (?<=%(lt)s{2}-) | (?<=%(lt)s-%(lt)s-))
              (?= %(lt)s -? %(lt)s)
            | # end of word
              (?=%(ws)s|\Z)
            | # em-dash
              (?<=%(wp)s) (?=-{2,}\w)
            )
        )", new Dictionary<object, object> {
                {
                    "wp",
                    word_punct},
                {
                    "lt",
                    letter},
                {
                    "ws",
                    whitespace},
                {
                    "nws",
                    nowhitespace}}), re.VERBOSE);

            public object wordsep_simple_re = re.compile(String.Format(@"(%s+)", whitespace));

            public object sentence_end_re = re.compile("[a-z][\.\!\?][\"\']?\Z");

            public TextWrapper(
                object width = 70,
                object initial_indent = "",
                object subsequent_indent = "",
                object expand_tabs = true,
                object replace_whitespace = true,
                object fix_sentence_endings = false,
                object break_long_words = true,
                object drop_whitespace = true,
                object break_on_hyphens = true,
                object tabsize = 8,
                object max_lines = null,
                object placeholder = " [...]")
            {
                this.width = width;
                this.initial_indent = initial_indent;
                this.subsequent_indent = subsequent_indent;
                this.expand_tabs = expand_tabs;
                this.replace_whitespace = replace_whitespace;
                this.fix_sentence_endings = fix_sentence_endings;
                this.break_long_words = break_long_words;
                this.drop_whitespace = drop_whitespace;
                this.break_on_hyphens = break_on_hyphens;
                this.tabsize = tabsize;
                this.max_lines = max_lines;
                this.placeholder = placeholder;
            }

            // -- Private methods -----------------------------------------------
            // (possibly useful for subclasses to override)
            // _munge_whitespace(text : string) -> string
            // 
            //         Munge whitespace in text: expand tabs and convert all other
            //         whitespace characters to spaces.  Eg. " foo\\tbar\\n\\nbaz"
            //         becomes " foo    bar  baz".
            //         
            public virtual object _munge_whitespace(object text)
            {
                if (this.expand_tabs)
                {
                    text = text.expandtabs(this.tabsize);
                }
                if (this.replace_whitespace)
                {
                    text = text.translate(this.unicode_whitespace_trans);
                }
                return text;
            }

            // _split(text : string) -> [string]
            // 
            //         Split the text to wrap into indivisible chunks.  Chunks are
            //         not quite the same as words; see _wrap_chunks() for full
            //         details.  As an example, the text
            //           Look, goof-ball -- use the -b option!
            //         breaks into the following chunks:
            //           'Look,', ' ', 'goof-', 'ball', ' ', '--', ' ',
            //           'use', ' ', 'the', ' ', '-b', ' ', 'option!'
            //         if break_on_hyphens is True, or in:
            //           'Look,', ' ', 'goof-ball', ' ', '--', ' ',
            //           'use', ' ', 'the', ' ', '-b', ' ', option!'
            //         otherwise.
            //         
            public virtual object _split(object text)
            {
                object chunks;
                if (object.ReferenceEquals(this.break_on_hyphens, true))
                {
                    chunks = this.wordsep_re.split(text);
                }
                else
                {
                    chunks = this.wordsep_simple_re.split(text);
                }
                chunks = (from c in chunks
                          where c
                          select c).ToList();
                return chunks;
            }

            // _fix_sentence_endings(chunks : [string])
            // 
            //         Correct for sentence endings buried in 'chunks'.  Eg. when the
            //         original text contains "... foo.\\nBar ...", munge_whitespace()
            //         and split() will convert that to [..., "foo.", " ", "Bar", ...]
            //         which has one too few spaces; this method simply changes the one
            //         space to two.
            //         
            public virtual object _fix_sentence_endings(object chunks)
            {
                var i = 0;
                var patsearch = this.sentence_end_re.search;
                while (i < chunks.Count - 1)
                {
                    if (chunks[i + 1] == " " && patsearch(chunks[i]))
                    {
                        chunks[i + 1] = "  ";
                        i += 2;
                    }
                    else
                    {
                        i += 1;
                    }
                }
            }

            // _handle_long_word(chunks : [string],
            //                              cur_line : [string],
            //                              cur_len : int, width : int)
            // 
            //         Handle a chunk of text (most likely a word, not whitespace) that
            //         is too long to fit in any line.
            //         
            public virtual object _handle_long_word(object reversed_chunks, object cur_line, object cur_len, object width)
            {
                object space_left;
                // Figure out when indent is larger than the specified width, and make
                // sure at least one character is stripped off on every pass
                if (width < 1)
                {
                    space_left = 1;
                }
                else
                {
                    space_left = width - cur_len;
                }
                // If we're allowed to break long words, then do so: put as much
                // of the next chunk onto the current line as will fit.
                if (this.break_long_words)
                {
                    var end = space_left;
                    var chunk = reversed_chunks[-1];
                    if (this.break_on_hyphens && chunk.Count > space_left)
                    {
                        // break after last hyphen, but only if there are
                        // non-hyphens before it
                        var hyphen = chunk.rfind("-", 0, space_left);
                        if (hyphen > 0 && any(from c in chunk[::hyphen]
                                              select c != "-"))
                        {
                            end = hyphen + 1;
                        }
                    }
                    cur_line.append(chunk[::end]);
                    reversed_chunks[-1] = chunk[end];
                }
                else if (!cur_line)
                {
                    // Otherwise, we have to preserve the long word intact.  Only add
                    // it to the current line if there's nothing already there --
                    // that minimizes how much we violate the width constraint.
                    cur_line.append(reversed_chunks.pop());
                    // If we're not allowed to break long words, and there's already
                    // text on the current line, do nothing.  Next time through the
                    // main loop of _wrap_chunks(), we'll wind up here again, but
                    // cur_len will be zero, so the next line will be entirely
                    // devoted to the long word that we can't handle right now.
                }
            }

            // _wrap_chunks(chunks : [string]) -> [string]
            // 
            //         Wrap a sequence of text chunks and return a list of lines of
            //         length 'self.width' or less.  (If 'break_long_words' is false,
            //         some lines may be longer than this.)  Chunks correspond roughly
            //         to words and the whitespace between them: each chunk is
            //         indivisible (modulo 'break_long_words'), but a line break can
            //         come between any two chunks.  Chunks should not have internal
            //         whitespace; ie. a chunk is either all whitespace or a "word".
            //         Whitespace chunks will be removed from the beginning and end of
            //         lines, but apart from that whitespace is preserved.
            //         
            public virtual object _wrap_chunks(object chunks)
            {
                object indent;
                var lines = new List<object>();
                if (this.width <= 0)
                {
                    throw ValueError(String.Format("invalid width %r (must be > 0)", this.width));
                }
                if (this.max_lines != null)
                {
                    if (this.max_lines > 1)
                    {
                        indent = this.subsequent_indent;
                    }
                    else
                    {
                        indent = this.initial_indent;
                    }
                    if (indent.Count + this.placeholder.lstrip().Count > this.width)
                    {
                        throw ValueError("placeholder too large for max width");
                    }
                }
                // Arrange in reverse order so items can be efficiently popped
                // from a stack of chucks.
                chunks.reverse();
                while (chunks)
                {
                    // Start the list of chunks that will make up the current line.
                    // cur_len is just the length of all the chunks in cur_line.
                    var cur_line = new List<object>();
                    var cur_len = 0;
                    // Figure out which static string will prefix this line.
                    if (lines)
                    {
                        indent = this.subsequent_indent;
                    }
                    else
                    {
                        indent = this.initial_indent;
                    }
                    // Maximum width for this line.
                    var width = this.width - indent.Count;
                    // First chunk on line is whitespace -- drop it, unless this
                    // is the very beginning of the text (ie. no lines started yet).
                    if (this.drop_whitespace && chunks[-1].strip() == "" && lines)
                    {
                        chunks.Remove(-1);
                    }
                    while (chunks)
                    {
                        var l = chunks[-1].Count;
                        // Can at least squeeze this chunk onto the current line.
                        if (cur_len + l <= width)
                        {
                            cur_line.append(chunks.pop());
                            cur_len += l;
                        }
                        else
                        {
                            // Nope, this line is full.
                            break;
                        }
                    }
                    // The current line is full, and the next chunk is too big to
                    // fit on *any* line (not just this one).
                    if (chunks && chunks[-1].Count > width)
                    {
                        this._handle_long_word(chunks, cur_line, cur_len, width);
                        cur_len = map(len, cur_line).Sum();
                    }
                    // If the last chunk on this line is all whitespace, drop it.
                    if (this.drop_whitespace && cur_line && cur_line[-1].strip() == "")
                    {
                        cur_len -= cur_line[-1].Count;
                        cur_line.Remove(-1);
                    }
                    if (cur_line)
                    {
                        if (this.max_lines == null || lines.Count + 1 < this.max_lines || (!chunks || this.drop_whitespace && chunks.Count == 1 && !chunks[0].strip()) && cur_len <= width)
                        {
                            // Convert current line back to a string and store it in
                            // list of all lines (return value).
                            lines.append(indent + "".join(cur_line));
                        }
                        else
                        {
                            if (cur_line)
                            {
                                do
                                {
                                    if (cur_line[-1].strip() && cur_len + this.placeholder.Count <= width)
                                    {
                                        cur_line.append(this.placeholder);
                                        lines.append(indent + "".join(cur_line));
                                        break;
                                    }
                                    cur_len -= cur_line[-1].Count;
                                    cur_line.Remove(-1);
                                } while (cur_line);
                            }
                            else
                            {
                                if (lines)
                                {
                                    var prev_line = lines[-1].rstrip();
                                    if (prev_line.Count + this.placeholder.Count <= this.width)
                                    {
                                        lines[-1] = prev_line + this.placeholder;
                                        break;
                                    }
                                }
                                lines.append(indent + this.placeholder.lstrip());
                            }
                            break;
                        }
                    }
                }
                return lines;
            }

            public virtual object _split_chunks(object text)
            {
                text = this._munge_whitespace(text);
                return this._split(text);
            }

            // -- Public interface ----------------------------------------------
            // wrap(text : string) -> [string]
            // 
            //         Reformat the single paragraph in 'text' so it fits in lines of
            //         no more than 'self.width' columns, and return a list of wrapped
            //         lines.  Tabs in 'text' are expanded with string.expandtabs(),
            //         and all other whitespace characters (including newline) are
            //         converted to space.
            //         
            public virtual object wrap(object text)
            {
                var chunks = this._split_chunks(text);
                if (this.fix_sentence_endings)
                {
                    this._fix_sentence_endings(chunks);
                }
                return this._wrap_chunks(chunks);
            }

            // fill(text : string) -> string
            // 
            //         Reformat the single paragraph in 'text' to fit in lines of no
            //         more than 'self.width' columns, and return a new string
            //         containing the entire wrapped paragraph.
            //         
            public virtual object fill(object text)
            {
                return "\n".join(this.wrap(text));
            }
        }

        // -- Convenience interface ---------------------------------------------
        // Wrap a single paragraph of text, returning a list of wrapped lines.
        // 
        //     Reformat the single paragraph in 'text' so it fits in lines of no
        //     more than 'width' columns, and return a list of wrapped lines.  By
        //     default, tabs in 'text' are expanded with string.expandtabs(), and
        //     all other whitespace characters (including newline) are converted to
        //     space.  See TextWrapper class for available keyword args to customize
        //     wrapping behaviour.
        //     
        public static object wrap(object text, object width = 70, Hashtable kwargs)
        {
            var w = TextWrapper(width: width, kwargs);
            return w.wrap(text);
        }

        // Fill a single paragraph of text, returning a new string.
        // 
        //     Reformat the single paragraph in 'text' to fit in lines of no more
        //     than 'width' columns, and return a new string containing the entire
        //     wrapped paragraph.  As with wrap(), tabs are expanded and other
        //     whitespace characters converted to space.  See TextWrapper class for
        //     available keyword args to customize wrapping behaviour.
        //     
        public static object fill(object text, object width = 70, Hashtable kwargs)
        {
            var w = TextWrapper(width: width, kwargs);
            return w.fill(text);
        }

        // Collapse and truncate the given text to fit in the given width.
        // 
        //     The text first has its whitespace collapsed.  If it then fits in
        //     the *width*, it is returned as is.  Otherwise, as many words
        //     as possible are joined and then the placeholder is appended::
        // 
        //         >>> textwrap.shorten("Hello  world!", width=12)
        //         'Hello world!'
        //         >>> textwrap.shorten("Hello  world!", width=11)
        //         'Hello [...]'
        //     
        public static object shorten(object text, object width, Hashtable kwargs)
        {
            var w = TextWrapper(width: width, max_lines: 1, kwargs);
            return w.fill(" ".join(text.strip().split()));
        }

        public static object _whitespace_only_re = re.compile("^[ \t]+$", re.MULTILINE);

        public static object _leading_whitespace_re = re.compile("(^[ \t]*)(?:[^ \t\n])", re.MULTILINE);

        // Remove any common leading whitespace from every line in `text`.
        // 
        //     This can be used to make triple-quoted strings line up with the left
        //     edge of the display, while still presenting them in the source code
        //     in indented form.
        // 
        //     Note that tabs and spaces are both treated as whitespace, but they
        //     are not equal: the lines "  hello" and "\\thello" are
        //     considered to have no common leading whitespace.
        // 
        //     Entirely blank lines are normalized to a newline character.
        //     
        public static object dedent(object text)
        {
            // Look for the longest leading string of spaces and tabs common to
            // all lines.
            object margin = null;
            text = _whitespace_only_re.sub("", text);
            var indents = _leading_whitespace_re.findall(text);
            foreach (var indent in indents)
            {
                if (margin == null)
                {
                    margin = indent;
                }
                else if (indent.startswith(margin))
                {
                    // Current line more deeply indented than previous winner:
                    // no change (previous winner is still on top).
                }
                else if (margin.startswith(indent))
                {
                    // Current line consistent with and no deeper than previous winner:
                    // it's the new winner.
                    margin = indent;
                }
                else
                {
                    // Find the largest common whitespace between current line and previous
                    // winner.
                    foreach (var _tup_1 in zip(margin, indent).Select((_p_1, _p_2) => Tuple.Create(_p_2, _p_1)))
                    {
                        var i = _tup_1.Item1;
                        (x, y) = _tup_1.Item2;
                        if (x != y)
                        {
                            margin = margin[::i];
                            break;
                        }
                    }
                }
            }
            // sanity check (testing/debugging only)
            if (0 && margin)
            {
                foreach (var line in text.split("\n"))
                {
                    Debug.Assert(!line || line.startswith(margin));
                    Debug.Assert(String.Format("line = %r, margin = %r", line, margin));
                }
            }
            if (margin)
            {
                text = re.sub(@"(?m)^" + margin, "", text);
            }
            return text;
        }

        // Adds 'prefix' to the beginning of selected lines in 'text'.
        // 
        //     If 'predicate' is provided, 'prefix' will only be added to the lines
        //     where 'predicate(line)' is True. If 'predicate' is not provided,
        //     it will default to adding 'prefix' to all non-empty lines that do not
        //     consist solely of whitespace characters.
        //     
        public static object indent(object text, object prefix, object predicate = null)
        {
            if (predicate == null)
            {
            }
            Func<object, object> predicate = line => {
                return line.strip();
            };
            Func<object> prefixed_lines = () => {
                foreach (var line in text.splitlines(true))
                {
                    yield return predicate(line) ? prefix + line : line;
                }
            };
            return "".join(prefixed_lines());
        }
    }
}