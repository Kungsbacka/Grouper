using GrouperLib.Database;

namespace GrouperLib.Test;

/// <summary>
/// Covers <c>GrouperLib.Database.Helpers.TranslateWildcard</c>, which converts the glob syntax
/// operators type into SQL LIKE syntax before it reaches
/// <c>get_document_by_group_name</c> and <c>get_document_by_member_rule</c>.
///
/// Two jobs at once: translating <c>*</c> and <c>?</c> into <c>%</c> and <c>_</c>, and escaping
/// the LIKE metacharacters a user did not intend as wildcards. Getting the second wrong turns a
/// literal search for "100%" into a match-everything query.
/// </summary>
public class TranslateWildcardTest
{
    [Fact]
    public void TestNullReturnsNull()
    {
        Assert.Null(Helpers.TranslateWildcard(null));
    }

    /// <summary>Empty string collapses to null, not to an empty pattern.</summary>
    [Fact]
    public void TestEmptyStringReturnsNull()
    {
        Assert.Null(Helpers.TranslateWildcard(""));
    }

    [Theory]
    [InlineData("abc", "abc")]
    [InlineData("Test Group", "Test Group")]
    public void TestPlainTextIsUnchanged(string input, string expected)
    {
        Assert.Equal(expected, Helpers.TranslateWildcard(input));
    }

    [Theory]
    [InlineData("*", "%")]
    [InlineData("a*b", "a%b")]
    [InlineData("*abc*", "%abc%")]
    [InlineData("?", "_")]
    [InlineData("a?b", "a_b")]
    [InlineData("*abc?", "%abc_")]
    public void TestGlobOperatorsBecomeLikeOperators(string input, string expected)
    {
        Assert.Equal(expected, Helpers.TranslateWildcard(input));
    }

    /// <summary>
    /// LIKE metacharacters the user typed literally get bracketed so they match themselves.
    /// </summary>
    [Theory]
    [InlineData("100%", "100[%]")]
    [InlineData("a_b", "a[_]b")]
    [InlineData("a[b", "a[[]b")]
    [InlineData("a]b", "a[]]b")]
    [InlineData("a[b]", "a[[]b[]]")]
    public void TestLikeMetacharactersAreEscaped(string input, string expected)
    {
        Assert.Equal(expected, Helpers.TranslateWildcard(input));
    }

    /// <summary>A backslash-escaped glob operator is emitted literally instead of translated.</summary>
    [Theory]
    [InlineData(@"a\*b", "a*b")]
    [InlineData(@"a\?b", "a?b")]
    [InlineData(@"\*", "*")]
    [InlineData(@"\?", "?")]
    public void TestEscapedGlobOperatorsAreLiteral(string input, string expected)
    {
        Assert.Equal(expected, Helpers.TranslateWildcard(input));
    }

    /// <summary>A doubled backslash yields one literal backslash.</summary>
    [Theory]
    [InlineData(@"a\\b", @"a\b")]
    [InlineData(@"\\", @"\")]
    public void TestDoubledBackslashIsOneLiteralBackslash(string input, string expected)
    {
        Assert.Equal(expected, Helpers.TranslateWildcard(input));
    }

    /// <summary>
    /// Characterizes the edge cases around a backslash that escapes nothing meaningful. A
    /// backslash before an ordinary character, or at the very end of the input, is silently
    /// dropped rather than kept or rejected. Note the lone-backslash case returns an empty
    /// string and not null -- the null shortcut only fires for null/empty *input*.
    /// </summary>
    [Theory]
    [InlineData(@"\a", "a")]
    [InlineData(@"a\", "a")]
    [InlineData(@"\", "")]
    public void TestBackslashThatEscapesNothingIsDropped(string input, string expected)
    {
        Assert.Equal(expected, Helpers.TranslateWildcard(input));
    }

    /// <summary>
    /// The <c>when escapeMode</c> guards exist only for <c>*</c> and <c>?</c>, so escaping a LIKE
    /// metacharacter changes nothing -- it is bracketed either way, and the backslash is dropped.
    /// </summary>
    [Theory]
    [InlineData(@"\%", "[%]")]
    [InlineData(@"\_", "[_]")]
    public void TestEscapingALikeMetacharacterIsRedundant(string input, string expected)
    {
        Assert.Equal(expected, Helpers.TranslateWildcard(input));
    }

    [Fact]
    public void TestGlobsAndMetacharactersCombine()
    {
        // "*" translates, "%" is escaped, "\?" is literal.
        Assert.Equal("%[%]?", Helpers.TranslateWildcard(@"*%\?"));
    }
}
