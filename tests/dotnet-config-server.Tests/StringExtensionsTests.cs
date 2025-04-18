#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Utilities;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

public sealed class StringExtensionsTests
{
    [Fact]
    public void IsNullOrWhiteSpace_NullValue_ReturnsTrue()
    {
        string? value = null;
        value.IsNullOrWhiteSpace().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrWhiteSpace_EmptyString_ReturnsTrue()
    {
        "".IsNullOrWhiteSpace().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrWhiteSpace_WhitespaceOnly_ReturnsTrue()
    {
        "   ".IsNullOrWhiteSpace().Should().BeTrue();
    }

    [Fact]
    public void IsNullOrWhiteSpace_NonEmptyString_ReturnsFalse()
    {
        "hello".IsNullOrWhiteSpace().Should().BeFalse();
    }

    [Fact]
    public void Truncate_StringExceedsMaxLength_TruncatesWithSuffix()
    {
        var result = "Hello World".Truncate(7);

        result.Should().Be("Hell...");
        result.Length.Should().Be(7);
    }

    [Fact]
    public void Truncate_StringWithinMaxLength_ReturnsOriginal()
    {
        var result = "Hi".Truncate(10);

        result.Should().Be("Hi");
    }

    [Fact]
    public void Truncate_StringEqualToMaxLength_ReturnsOriginal()
    {
        var result = "Hello".Truncate(5);

        result.Should().Be("Hello");
    }

    [Fact]
    public void Truncate_EmptyString_ReturnsEmpty()
    {
        var result = "".Truncate(5);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Truncate_WithCustomSuffix_UsesCustomSuffix()
    {
        var result = "Hello World".Truncate(8, " [+]");

        result.Should().Be("Hell [+]");
        result.Length.Should().Be(8);
    }

    [Fact]
    public void ToKebabCase_PascalCase_ConvertsToKebab()
    {
        var result = "HelloWorld".ToKebabCase();

        result.Should().Be("hello-world");
    }

    [Fact]
    public void ToKebabCase_MultiWordPascalCase_ConvertsCorrectly()
    {
        var result = "MyConfigurationService".ToKebabCase();

        result.Should().Be("my-configuration-service");
    }

    [Fact]
    public void ToKebabCase_EmptyString_ReturnsEmpty()
    {
        "".ToKebabCase().Should().BeEmpty();
    }

    [Fact]
    public void ToSnakeCase_PascalCase_ConvertsToSnake()
    {
        var result = "HelloWorld".ToSnakeCase();

        result.Should().Be("hello_world");
    }

    [Fact]
    public void ToSnakeCase_EmptyString_ReturnsEmpty()
    {
        "".ToSnakeCase().Should().BeEmpty();
    }

    [Fact]
    public void ToPascalCase_KebabCase_ConvertsToPascal()
    {
        var result = "hello-world".ToPascalCase();

        result.Should().Be("HelloWorld");
    }

    [Fact]
    public void ToPascalCase_SnakeCase_ConvertsToPascal()
    {
        var result = "hello_world".ToPascalCase();

        result.Should().Be("HelloWorld");
    }

    [Fact]
    public void ToPascalCase_EmptyString_ReturnsEmpty()
    {
        "".ToPascalCase().Should().BeEmpty();
    }

    [Fact]
    public void MatchesPattern_MatchingPattern_ReturnsTrue()
    {
        "12345".MatchesPattern(@"^\d+$").Should().BeTrue();
    }

    [Fact]
    public void MatchesPattern_NonMatchingPattern_ReturnsFalse()
    {
        "abc".MatchesPattern(@"^\d+$").Should().BeFalse();
    }

    [Fact]
    public void RemoveWhitespace_StringWithSpaces_RemovesAll()
    {
        "Hello World Test".RemoveWhitespace().Should().Be("HelloWorldTest");
    }

    [Fact]
    public void RemoveWhitespace_StringWithTabs_RemovesTabs()
    {
        "col1\tcol2".RemoveWhitespace().Should().Be("col1col2");
    }

    [Fact]
    public void Repeat_PositiveCount_RepeatsString()
    {
        "ab".Repeat(3).Should().Be("ababab");
    }

    [Fact]
    public void Repeat_ZeroCount_ReturnsEmpty()
    {
        "ab".Repeat(0).Should().BeEmpty();
    }

    [Fact]
    public void Repeat_NegativeCount_ReturnsEmpty()
    {
        "ab".Repeat(-1).Should().BeEmpty();
    }

    [Fact]
    public void IsValidEmail_ValidEmailAddress_ReturnsTrue()
    {
        "user@example.com".IsValidEmail().Should().BeTrue();
    }

    [Fact]
    public void IsValidEmail_InvalidEmailAddress_ReturnsFalse()
    {
        "not-an-email".IsValidEmail().Should().BeFalse();
    }

    [Fact]
    public void IsValidEmail_EmailMissingAtSign_ReturnsFalse()
    {
        "userexample.com".IsValidEmail().Should().BeFalse();
    }

    [Fact]
    public void UrlEncode_StringWithSpaces_EncodesCorrectly()
    {
        var result = "hello world".UrlEncode();

        result.Should().Be("hello%20world");
    }

    [Fact]
    public void UrlDecode_EncodedString_DecodesCorrectly()
    {
        var result = "hello%20world".UrlDecode();

        result.Should().Be("hello world");
    }

    [Fact]
    public void UrlEncode_ThenUrlDecode_ReturnsOriginal()
    {
        var original = "config/key?value=hello world&other=test";
        var encoded = original.UrlEncode();
        var decoded = encoded.UrlDecode();

        decoded.Should().Be(original);
    }

    [Fact]
    public void CommonPrefix_BothStartWithSamePrefix_ReturnsCommonPart()
    {
        var result = "config.database.host".CommonPrefix("config.database.port");

        result.Should().Be("config.database.");
    }

    [Fact]
    public void CommonPrefix_NoCommonPrefix_ReturnsEmpty()
    {
        var result = "abc".CommonPrefix("xyz");

        result.Should().BeEmpty();
    }

    [Fact]
    public void CommonPrefix_OneStringIsPrefix_ReturnsShortString()
    {
        var result = "config".CommonPrefix("config.host");

        result.Should().Be("config");
    }

    [Fact]
    public void ToSafeFileName_ValidFileName_ReturnsUnchanged()
    {
        var result = "valid-file-name_2024.json".ToSafeFileName();

        result.Should().Be("valid-file-name_2024.json");
    }

    [Fact]
    public void ToSafeFileName_StringWithForwardSlash_RemovesIt()
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        if (invalidChars.Length == 0)
            return; // no invalid chars on this OS

        var invalidChar = invalidChars[0];
        var input = $"file{invalidChar}name";

        var result = input.ToSafeFileName();

        result.Should().NotContain(invalidChar.ToString());
        result.Should().Contain("file");
        result.Should().Contain("name");
    }
}
