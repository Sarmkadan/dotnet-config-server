#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetConfigServer.Utilities;
using FluentAssertions;
using Xunit;

namespace DotnetConfigServer.Tests;

/// <summary>
/// Provides unit tests for the <see cref="DotnetConfigServer.Utilities.StringExtensions"/> static class.
/// </summary>
public sealed class StringExtensionsTests
{
    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.IsNullOrWhiteSpace"/> returns true when the input string is null.
    /// </summary>
    [Fact]
    public void IsNullOrWhiteSpace_NullValue_ReturnsTrue()
    {
        string? value = null;
        value.IsNullOrWhiteSpace().Should().BeTrue();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.IsNullOrWhiteSpace"/> returns true when the input string is empty.
    /// </summary>
    [Fact]
    public void IsNullOrWhiteSpace_EmptyString_ReturnsTrue()
    {
        "".IsNullOrWhiteSpace().Should().BeTrue();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.IsNullOrWhiteSpace"/> returns true when the input string contains only whitespace characters.
    /// </summary>
    [Fact]
    public void IsNullOrWhiteSpace_WhitespaceOnly_ReturnsTrue()
    {
        " ".IsNullOrWhiteSpace().Should().BeTrue();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.IsNullOrWhiteSpace"/> returns false when the input string is a non-empty value.
    /// </summary>
    [Fact]
    public void IsNullOrWhiteSpace_NonEmptyString_ReturnsFalse()
    {
        "hello".IsNullOrWhiteSpace().Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.Truncate"/> truncates a string when it exceeds the specified maximum length, appending an ellipsis suffix.
    /// </summary>
    [Fact]
    public void Truncate_StringExceedsMaxLength_TruncatesWithSuffix()
    {
        var result = "Hello World".Truncate(7);

        result.Should().Be("Hell...");
        result.Length.Should().Be(7);
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.Truncate"/> returns the original string unchanged when its length is within the specified maximum length.
    /// </summary>
    [Fact]
    public void Truncate_StringWithinMaxLength_ReturnsOriginal()
    {
        var result = "Hi".Truncate(10);

        result.Should().Be("Hi");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.Truncate"/> returns the original string unchanged when its length equals the specified maximum length.
    /// </summary>
    [Fact]
    public void Truncate_StringEqualToMaxLength_ReturnsOriginal()
    {
        var result = "Hello".Truncate(5);

        result.Should().Be("Hello");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.Truncate"/> returns an empty string when the input string is empty.
    /// </summary>
    [Fact]
    public void Truncate_EmptyString_ReturnsEmpty()
    {
        var result = "".Truncate(5);

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.Truncate"/> uses a custom suffix when truncating a string to a specified maximum length.
    /// </summary>
    [Fact]
    public void Truncate_WithCustomSuffix_UsesCustomSuffix()
    {
        var result = "Hello World".Truncate(8, " [+]");

        result.Should().Be("Hell [+]");
        result.Length.Should().Be(8);
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.ToKebabCase"/> converts a PascalCase string to kebab-case format.
    /// </summary>
    [Fact]
    public void ToKebabCase_PascalCase_ConvertsToKebab()
    {
        var result = "HelloWorld".ToKebabCase();

        result.Should().Be("hello-world");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.ToKebabCase"/> converts a multi-word PascalCase string to kebab-case format correctly.
    /// </summary>
    [Fact]
    public void ToKebabCase_MultiWordPascalCase_ConvertsCorrectly()
    {
        var result = "MyConfigurationService".ToKebabCase();

        result.Should().Be("my-configuration-service");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.ToKebabCase"/> returns an empty string when the input string is empty.
    /// </summary>
    [Fact]
    public void ToKebabCase_EmptyString_ReturnsEmpty()
    {
        "".ToKebabCase().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.ToSnakeCase"/> converts a PascalCase string to snake_case format.
    /// </summary>
    [Fact]
    public void ToSnakeCase_PascalCase_ConvertsToSnake()
    {
        var result = "HelloWorld".ToSnakeCase();

        result.Should().Be("hello_world");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.ToSnakeCase"/> returns an empty string when the input string is empty.
    /// </summary>
    [Fact]
    public void ToSnakeCase_EmptyString_ReturnsEmpty()
    {
        "".ToSnakeCase().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.ToPascalCase"/> converts a kebab-case string to PascalCase format.
    /// </summary>
    [Fact]
    public void ToPascalCase_KebabCase_ConvertsToPascal()
    {
        var result = "hello-world".ToPascalCase();

        result.Should().Be("HelloWorld");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.ToPascalCase"/> converts a snake_case string to PascalCase format.
    /// </summary>
    [Fact]
    public void ToPascalCase_SnakeCase_ConvertsToPascal()
    {
        var result = "hello_world".ToPascalCase();

        result.Should().Be("HelloWorld");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.ToPascalCase"/> returns an empty string when the input string is empty.
    /// </summary>
    [Fact]
    public void ToPascalCase_EmptyString_ReturnsEmpty()
    {
        "".ToPascalCase().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.MatchesPattern"/> returns true when the input string matches the specified regular expression pattern.
    /// </summary>
    [Fact]
    public void MatchesPattern_MatchingPattern_ReturnsTrue()
    {
        "12345".MatchesPattern(@"^\d+$").Should().BeTrue();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.MatchesPattern"/> returns false when the input string does not match the specified regular expression pattern.
    /// </summary>
    [Fact]
    public void MatchesPattern_NonMatchingPattern_ReturnsFalse()
    {
        "abc".MatchesPattern(@"^\d+$").Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.RemoveWhitespace"/> removes all whitespace characters from a string.
    /// </summary>
    [Fact]
    public void RemoveWhitespace_StringWithSpaces_RemovesAll()
    {
        "Hello World Test".RemoveWhitespace().Should().Be("HelloWorldTest");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.RemoveWhitespace"/> removes tab characters from a string.
    /// </summary>
    [Fact]
    public void RemoveWhitespace_StringWithTabs_RemovesTabs()
    {
        "col1\tcol2".RemoveWhitespace().Should().Be("col1col2");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.Repeat"/> repeats the input string the specified number of times.
    /// </summary>
    [Fact]
    public void Repeat_PositiveCount_RepeatsString()
    {
        "ab".Repeat(3).Should().Be("ababab");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.Repeat"/> returns an empty string when the count is zero.
    /// </summary>
    [Fact]
    public void Repeat_ZeroCount_ReturnsEmpty()
    {
        "ab".Repeat(0).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.Repeat"/> returns an empty string when the count is negative.
    /// </summary>
    [Fact]
    public void Repeat_NegativeCount_ReturnsEmpty()
    {
        "ab".Repeat(-1).Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.IsValidEmail"/> returns true for a valid email address format.
    /// </summary>
    [Fact]
    public void IsValidEmail_ValidEmailAddress_ReturnsTrue()
    {
        "user@example.com".IsValidEmail().Should().BeTrue();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.IsValidEmail"/> returns false for an invalid email address format.
    /// </summary>
    [Fact]
    public void IsValidEmail_InvalidEmailAddress_ReturnsFalse()
    {
        "not-an-email".IsValidEmail().Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.IsValidEmail"/> returns false when the email address is missing the @ sign.
    /// </summary>
    [Fact]
    public void IsValidEmail_EmailMissingAtSign_ReturnsFalse()
    {
        "userexample.com".IsValidEmail().Should().BeFalse();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.UrlEncode"/> correctly encodes a string with spaces to URL-safe format.
    /// </summary>
    [Fact]
    public void UrlEncode_StringWithSpaces_EncodesCorrectly()
    {
        var result = "hello world".UrlEncode();

        result.Should().Be("hello%20world");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.UrlDecode"/> correctly decodes a URL-encoded string back to its original format.
    /// </summary>
    [Fact]
    public void UrlDecode_EncodedString_DecodesCorrectly()
    {
        var result = "hello%20world".UrlDecode();

        result.Should().Be("hello world");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.UrlEncode"/> followed by <see cref="DotnetConfigServer.Utilities.StringExtensions.UrlDecode"/> returns the original string.
    /// </summary>
    [Fact]
    public void UrlEncode_ThenUrlDecode_ReturnsOriginal()
    {
        var original = "config/key?value=hello world&other=test";
        var encoded = original.UrlEncode();
        var decoded = encoded.UrlDecode();

        decoded.Should().Be(original);
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.CommonPrefix"/> returns the common prefix when both strings start with the same sequence.
    /// </summary>
    [Fact]
    public void CommonPrefix_BothStartWithSamePrefix_ReturnsCommonPart()
    {
        var result = "config.database.host".CommonPrefix("config.database.port");

        result.Should().Be("config.database.");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.CommonPrefix"/> returns an empty string when the two input strings have no common prefix.
    /// </summary>
    [Fact]
    public void CommonPrefix_NoCommonPrefix_ReturnsEmpty()
    {
        var result = "abc".CommonPrefix("xyz");

        result.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.CommonPrefix"/> returns the shorter string when one string is a prefix of the other.
    /// </summary>
    [Fact]
    public void CommonPrefix_OneStringIsPrefix_ReturnsShortString()
    {
        var result = "config".CommonPrefix("config.host");

        result.Should().Be("config");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.ToSafeFileName"/> returns the filename unchanged when it contains only valid filename characters.
    /// </summary>
    [Fact]
    public void ToSafeFileName_ValidFileName_ReturnsUnchanged()
    {
        var result = "valid-file-name_2024.json".ToSafeFileName();

        result.Should().Be("valid-file-name_2024.json");
    }

    /// <summary>
    /// Tests that <see cref="DotnetConfigServer.Utilities.StringExtensions.ToSafeFileName"/> removes invalid filename characters from a string containing forward slashes or other invalid characters.
    /// </summary>
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