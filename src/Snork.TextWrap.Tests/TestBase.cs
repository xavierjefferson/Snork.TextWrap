using System.Linq.Expressions;
using Newtonsoft.Json;

namespace Snork.TextWrap.Tests;

public class TestBase
{
    protected const int MaxTests = int.MaxValue;

    public static List<object[]> GetList(string filename, Func<StringResultTestCase, bool> func, Expression<Func<StringResultTestCase, object>>? ssort = null)
    {
        IQueryable<StringResultTestCase> items = JsonConvert.DeserializeObject<List<StringResultTestCase>>(File.ReadAllText(filename)).AsQueryable();
        if (ssort != null) items = items.OrderBy(ssort);
        var objectsEnumerable = items.AsEnumerable().Where(func).Select(i => new object[]
        {   i.id,
            i.break_long_words, i.width, i.break_on_hyphens, i.drop_whitespace, i.expand_tabs, i.expected,
            i.fix_sentence_endings, i.initial_indent, i.subsequent_indent, i.max_lines, i.placeholder, i.tabsize,
            i.text, i.replace_whitespace
        });
        
        return objectsEnumerable.Take(MaxTests).ToList();
    }
}