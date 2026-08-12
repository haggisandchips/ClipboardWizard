using ClipboardWizard.Model;
using ClipboardWizard.View.Control;
using System.Windows;

namespace ClipboardWizard.Tests.View
{
    public class ContentConverterTests
    {
        private readonly ContentConverter _converter = new();

        [Fact]
        public void Convert_PrefersDescriptionWhenPresent()
        {
            Snippet snippet = new() { Description = "desc", Content = "content" };

            object result = _converter.Convert(snippet, typeof(string), null, null!);

            Assert.Equal("desc", result);
        }

        [Fact]
        public void Convert_FallsBackToContentWhenDescriptionIsEmpty()
        {
            Snippet snippet = new() { Description = "", Content = "content" };

            object result = _converter.Convert(snippet, typeof(string), null, null!);

            Assert.Equal("content", result);
        }

        [Fact]
        public void Convert_ReturnsEmptyString_WhenValueIsNotASnippet()
        {
            object result = _converter.Convert("not a snippet", typeof(string), null, null!);

            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Convert_ForImageSnippet_ReturnsDescriptionEvenThoughContentIsEmpty()
        {
            Snippet snippet = new() { Type = SnippetType.Image, Description = "a photo", Content = null };

            object result = _converter.Convert(snippet, typeof(string), null, null!);

            Assert.Equal("a photo", result);
        }

        [Fact]
        public void Convert_ForImageSnippetWithNoDescription_ReturnsEmptyString()
        {
            Snippet snippet = new() { Type = SnippetType.Image, Description = null };

            object result = _converter.Convert(snippet, typeof(string), null, null!);

            Assert.Equal(string.Empty, result);
        }
    }

    public class IsNotEmptyConverterTests
    {
        private readonly IsNotEmptyConverter _converter = new();

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("text", true)]
        public void Convert_ReflectsWhetherStringIsEmpty(string? value, bool expected)
        {
            object result = _converter.Convert(value!, typeof(bool), null, null!);

            Assert.Equal(expected, result);
        }
    }

    public class StyleConverterTests
    {
        private readonly StyleConverter _converter = new();

        [Fact]
        public void Convert_ReturnsActiveStyle_WhenStateIsActive()
        {
            Style active = new();
            Style inactive = new();

            object? result = _converter.Convert(new object[] { State.Active, active, inactive }, typeof(Style), null, null!);

            Assert.Same(active, result);
        }

        [Fact]
        public void Convert_ReturnsInactiveStyle_WhenStateIsInactive()
        {
            Style active = new();
            Style inactive = new();

            object? result = _converter.Convert(new object[] { State.Inactive, active, inactive }, typeof(Style), null, null!);

            Assert.Same(inactive, result);
        }
    }
}
