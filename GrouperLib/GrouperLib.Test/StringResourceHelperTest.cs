using GrouperLib.Language;
using System.Globalization;
using System.Reflection;

namespace GrouperLib.Test;

/// <summary>
/// Covers <see cref="StringResourceHelper"/>, the lookup behind every validation message an
/// operator sees through Test-GrouperDocument or the API.
///
/// Every test pins the UI culture explicitly and restores it afterwards. Resource lookup reads
/// <c>Thread.CurrentThread.CurrentUICulture</c>, which <see cref="StringResourceHelper.SetLanguage"/>
/// mutates as a side effect, so relying on the ambient culture would make results depend on the
/// machine's locale and on test execution order.
/// </summary>
public class StringResourceHelperTest
{
    private static T InCulture<T>(string culture, Func<T> action)
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(culture);
            return action();
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    /// <summary>Every ResourceString constant, discovered by reflection.</summary>
    public static TheoryData<string> AllResourceIds()
    {
        TheoryData<string> data = [];
        foreach (FieldInfo field in typeof(ResourceString).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType == typeof(string) && field.GetValue(null) is string id)
            {
                data.Add(id);
            }
        }
        return data;
    }

    /// <summary>
    /// Guards against a constant existing with no matching entry in a .resx, which would only
    /// surface at runtime as an ArgumentException in place of the intended message.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllResourceIds))]
    public void TestEveryResourceIdResolvesInEnglish(string resourceId)
    {
        string text = InCulture("en", () => new StringResourceHelper().GetString(resourceId));

        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Theory]
    [MemberData(nameof(AllResourceIds))]
    public void TestEveryResourceIdResolvesInSwedish(string resourceId)
    {
        string text = InCulture("sv", () => new StringResourceHelper().GetString(resourceId));

        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    [Fact]
    public void TestSwedishDiffersFromEnglish()
    {
        string en = InCulture("en", () => new StringResourceHelper().GetString(ResourceString.ValidationErrorNoMemberObjects));
        string sv = InCulture("sv", () => new StringResourceHelper().GetString(ResourceString.ValidationErrorNoMemberObjects));

        Assert.NotEqual(en, sv);
    }

    [Fact]
    public void TestSetLanguageChangesSubsequentLookups()
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            StringResourceHelper helper = new();
            helper.SetLanguage("en");
            string en = helper.GetString(ResourceString.ValidationErrorNoMemberObjects);
            helper.SetLanguage("sv");
            string sv = helper.GetString(ResourceString.ValidationErrorNoMemberObjects);

            Assert.NotEqual(en, sv);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    /// <summary>An empty or null language is ignored rather than resetting the culture.</summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TestSetLanguageIgnoresEmptyInput(string? language)
    {
        CultureInfo original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("sv");
            new StringResourceHelper().SetLanguage(language!);

            Assert.Equal("sv", CultureInfo.CurrentUICulture.Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void TestFormatArgumentsAreSubstituted()
    {
        string text = InCulture("en", () =>
            new StringResourceHelper().GetString(ResourceString.ValidationErrorDuplicateRuleName, "Organisation"));

        Assert.Contains("Organisation", text);
    }

    /// <summary>A null argument renders as the literal "&lt;NULL&gt;" rather than blank.</summary>
    [Fact]
    public void TestNullArgumentRendersAsNullPlaceholder()
    {
        string text = InCulture("en", () =>
            new StringResourceHelper().GetString(ResourceString.ValidationErrorDuplicateRuleName, [null]));

        Assert.Contains("<NULL>", text);
    }

    [Fact]
    public void TestUnknownResourceIdThrows()
    {
        Assert.Throws<ArgumentException>(() => new StringResourceHelper().GetString("NoSuchResourceId"));
    }

    [Fact]
    public void TestNullResourceIdThrows()
    {
        Assert.Throws<ArgumentNullException>(() => new StringResourceHelper().GetString(null!));
    }

    /// <summary>Passing no arguments must not trip the string.Format path.</summary>
    [Fact]
    public void TestLookupWithoutArgumentsSucceeds()
    {
        string text = InCulture("en", () =>
            new StringResourceHelper().GetString(ResourceString.ValidationErrorNoMemberObjects));

        Assert.False(string.IsNullOrWhiteSpace(text));
    }
}
