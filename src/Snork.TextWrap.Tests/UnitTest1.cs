using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace Snork.TextWrap.Tests
{
    public class UnitTest1
    {
        private const string quote = "Do small things with great love";

        //[Theory]
        //[InlineData("things", 10, null, -1)]
        //[InlineData("t", 2, null, 25)]
        //[InlineData("things", 10, null, -1)]
        //[InlineData("things", 10, null, -1)]
        //[InlineData("things", 10, null, -1)]
        //public void TestRFind(string sub, int start, int? end, int expected)
        //{
        //    if (end.HasValue)
        //    {
        //        Assert.Equal(expected, TextWrapper.rfind(sub, start, end.Value));
        //    }
        //}
        [Fact]
        public void Test1()
        {
            var a = TextWrapper.Wrap(
                "    There are many variations of passages of Lorem Ipsum available, but the majority have suffered alteration in some form, by injected humour, or randomised words which don't look even slightly believable. If you are going to use a passage of Lorem Ipsum, you need to be sure there isn't anything embarrassing hidden in the middle of text. All the Lorem Ipsum generators on the Internet tend to repeat predefined chunks as necessary, making this the first true generator on the Internet. It uses a dictionary of over 200 Latin words, combined with a handful of model sentence structures, to generate Lorem Ipsum which looks reasonable. The generated Lorem Ipsum is therefore always free from repetition, injected humour, or non-characteristic words etc.",
                50, new TextWrapperOptions(){DropWhitespace = true,});

        }
        [Fact]
        public void TestFill()
        {
            var a = TextWrapper.Fill(
                "    There are many variations of passages of Lorem Ipsum available, but the majority have suffered alteration in some form, by injected humour, or randomised words which don't look even slightly believable. If you are going to use a passage of Lorem Ipsum, you need to be sure there isn't anything embarrassing hidden in the middle of text. All the Lorem Ipsum generators on the Internet tend to repeat predefined chunks as necessary, making this the first true generator on the Internet. It uses a dictionary of over 200 Latin words, combined with a handful of model sentence structures, to generate Lorem Ipsum which looks reasonable. The generated Lorem Ipsum is therefore always free from repetition, injected humour, or non-characteristic words etc.",
                50, new TextWrapperOptions() { DropWhitespace = true });

        }
        [Fact]
        public void TestShorten()
        {
            var a = TextWrapper.Shorten("Hello world!", 12);
            Assert.Equal("Hello world!", a);
            a = TextWrapper.Shorten("Hello world!", 11);
            Assert.Equal("Hello [...]", a);
        }
    }
}