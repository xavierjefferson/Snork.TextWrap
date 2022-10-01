
# Snork.TextWrap
Text Wrapping and Filling

[![Latest version](https://img.shields.io/nuget/v/Snork.TextWrap.svg)](https://www.nuget.org/packages/Snork.TextWrap/) 

This library is a port of the fantastic Python [*textwrap*](https://docs.python.org/3/library/textwrap.html) module (credits: Gregory P. Ward, Python Software Foundation).  There are two main methods, *Wrap* and *Fill*.  Given a string as input and a fixed line width, the Wrap function returns an instance of `List<string>` with the input broken up into separate lines.  The Fill function is very similar, except that it concatenates the lines into one large string.

## Example 1:

    var test = TextWrapper.Fill(@"It was the White Rabbit, trotting slowly back again" + 
    @" and looking anxiously about as it went, as if it had lost something; Alice " + 
    @"heard it muttering to itself, ""The Duchess! The Duchess! Oh, my dear paws! " +
    @"Oh, my fur and whiskers! She'll get me executed, as sure as ferrets are " +
    @"ferrets! Where _can_ I have dropped them, I wonder?"" Alice guessed in " + 
    @"a moment that it was looking for the fan and the pair of white kid-gloves " + 
    @"and she very good-naturedly began hunting about for them, but they were " +
    @"nowhere to be seen--everything seemed to have changed since her swim in " +
    @"the pool, and the great hall, with the glass table and the little door, " +
    @"had vanished completely.", 30);
    
    Console.WriteLine(test);

## Example 1 Output:

    It was the White Rabbit,
    trotting slowly back again and
    looking anxiously about as it
    went, as if it had lost
    something; Alice heard it
    muttering to itself, "The
    Duchess! The Duchess! Oh, my
    dear paws! Oh, my fur and
    whiskers! She'll get me
    executed, as sure as ferrets
    are ferrets! Where _can_ I
    have dropped them, I wonder?"
    Alice guessed in a moment that
    it was looking for the fan and
    the pair of white kid-gloves
    and she very good-naturedly
    began hunting about for them,
    but they were nowhere to be
    seen--everything seemed to
    have changed since her swim in
    the pool, and the great hall,
    with the glass table and the
    little door, had vanished
    completely.

## Usage:

### Wrap
    public static List<string> Wrap(string text, int width = 70, TextWrapperOptions? options = null)
Wrap a single paragraph of text, returning a list of wrapped lines. Reformat the single paragraph in 'text' so it fits in lines of no  more than 'width' columns, and return a list of wrapped lines.  By default, tabs in 'text' are expanded, and all other whitespace characters (including newline) are converted to space.  See TextWrapperOptions class below for available properties to customize wrapping behavior.
### Fill
     public static string Fill(string text, int width = 70, TextWrapperOptions? options = null)
Fill a single paragraph of text, returning a new string. Reformat the single paragraph in 'text' to fit in lines of no more than 'width' columns, and return a new string containing the entire wrapped paragraph.  As with wrap(), tabs are expanded and other whitespace characters converted to space.  See TextWrapperOptions class for available properties to customize wrapping behavior.
### Shorten
    public static string Shorten(string text, int width, ShortenTextWrapperOptions? options = null)
Collapse and truncate the given text to fit in the given width.  The text first has its whitespace collapsed.  If it then fits in the `width`, it is returned as is.  Otherwise, as many words as possible are joined and then the placeholder is appended.

The `ShortenTextWrapperOptions` class works very similarly to the `TextWrapperOptions` class, except that you can't specify `MaxLines`.
### Indent
    public static string Indent(string text, string prefix, Func<string, bool>? predicate = null)'
Adds `prefix` to the beginning of selected lines in `text`. If `predicate` is provided, `prefix` will only be added to the lines where `predicate(line)` is true. If `predicate` is not provided, it will default to adding `prefix` to all non-empty lines that do not consist solely of whitespace characters.
### Dedent
     public static string Dedent(string text)
Remove any common leading whitespace from every line in `text`. This can be used to make triple-quoted strings line up with the left edge of the display, while still presenting them in the source code in indented form.

Note that tabs and spaces are both treated as whitespace, but they are not equal: the lines `"  hello"` and `"\thello"` are considered to have no common leading whitespace.

Entirely blank lines are normalized to a newline character.
## TextWrapperOptions Class
### BreakLongWords
    public virtual bool BreakLongWords { get; set; } 
(default: `true`) If `true`, then words longer than `width` will be broken in order to ensure that no lines are longer than width. If it is false, long words will not be broken, and some lines may be longer than `width`. (Long words will be put on a line by themselves, in order to minimize the amount by which width is exceeded.)
### BreakOnHyphens
    public virtual bool BreakOnHyphens { get; set; };
(default: `true`) If `true`, wrapping will occur preferably on whitespaces and right after hyphens in compound words, as it is customary in English. If false, only whitespaces will be considered as potentially good places for line breaks, but you need to set `BreakLongWords` to false if you want truly insecable words.
### DropWhitespace
    public virtual bool DropWhitespace { get; set; }
(default: `true`) If `true`, whitespace at the beginning and ending of every line (after wrapping but before indenting) is dropped. Whitespace at the beginning of the paragraph, however, is not dropped if non-whitespace follows it. If whitespace being dropped takes up an entire line, the whole line is dropped.
### ExpandTabs        
    public virtual bool ExpandTabs { get; set; };
(default: `true`) If `true`, then all tab characters in text will be expanded to spaces.
### FixSentenceEndings        
    public virtual bool FixSentenceEndings { get; set; }
(default: `false`) If `true`, TextWrapper attempts to detect sentence endings and ensure that sentences are always separated by exactly two spaces. This is generally desired for text in a monospaced font. However, the sentence detection algorithm is imperfect: it assumes that a sentence ending consists of a lowercase letter followed by one of '.', '!', or '?', possibly followed by one of '"' or "'", followed by a space. One problem with this is algorithm is that it is unable to detect the difference between “Dr.” in

    [...] Dr. Frankenstein's monster [...]

and “Spot.” in

    [...] See Spot. See Spot run [...]

Since the sentence detection algorithm relies on finding English lowercase letters, and a convention of using two spaces after a period to separate sentences on the same line, it is specific to English-language texts.
### InitialIndent        
    public virtual string InitialIndent { get; set; }
(default: `String.Empty`) String that will be prepended to the first line of wrapped output. Counts towards the length of the first line. The empty string is not indented.
### MaxLines
    public virtual int? MaxLines { get; set; }
(default: `null`) If not null, then the output will contain at most `MaxLines` lines, with placeholder appearing at the end of the output.
### Placeholder
    public virtual string Placeholder { get; set; }
(default: `" [...]"`) String that will appear at the end of the output text if it has been truncated.
### ReplaceWhitespace
    public virtual bool ReplaceWhitespace { get; set; }
(default: `true`) If `true`, after tab expansion but before wrapping, the wrap() method will replace each whitespace character with a single space. The whitespace characters replaced are as follows: tab, newline, vertical tab, formfeed, and carriage return ('\t\n\v\f\r').

Note If `ExpandTabs` is `false` and `ReplaceWhitespace` is true, each tab character will be replaced by a single space, which is not the same as tab expansion.

Note If `ReplaceWhitespace` is false, newlines may appear in the middle of a line and cause strange output. For this reason, text should be split into paragraphs which are wrapped separately.
###  SubsequentIndent      
    public virtual string SubsequentIndent { get; set; }
(default: `String.Empty`) String that will be prepended to all lines of wrapped output except the first. Counts towards the length of each line except the first.
### TabSize        
    public virtual int TabSize { get; set; }
(default: `8`) If `ExpandTabs` is true, then all tab characters in text will be expanded to zero or more spaces, depending on the current column and the given tab size.