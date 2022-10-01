# Snork.TextWrap
Text Wrapping and Filling

[![Latest version](https://img.shields.io/nuget/v/Snork.TextWrap.svg)](https://www.nuget.org/packages/Snork.TextWrap/) 

This library is a port of the fantastic Python [*textwrap*](https://docs.python.org/3/library/textwrap.html) module (credits: Gregory P. Ward, Python Software Foundation).  There are two main methods, *Wrap* and *Fill*.  Given a string as input and a fixed line width, the Wrap function returns an instance of `List<string>` with the input broken up into separate lines.  The Fill function is very similar, except that it concatenates the lines into one large string.

