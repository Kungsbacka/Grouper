using GrouperLib.Core;
using GrouperLib.Language;

namespace GrouperLib.Test;

/// <summary>
/// Covers the JSON entry points into the document model -- <c>FromJson</c>, <c>ToJson</c> and the
/// <see cref="StringOrBooleanConverter"/> -- which is how every document reaches Grouper from the
/// database, the API and PSGrouper.
/// </summary>
public class GrouperDocumentJsonTest
{
    private const string ValidJson = """
        {
          "id": "aa11bb22-cc33-dd44-ee55-ff6677889900",
          "groupId": "bb22cc33-dd44-ee55-ff66-778899001122",
          "groupName": "Test Group",
          "store": "AzureAd",
          "owner": "KeepExisting",
          "members": [
            { "source": "Static", "action": "Include",
              "rules": [ { "name": "Upn", "value": "member@example.com" } ] }
          ]
        }
        """;

    private static string JsonWithIncludeManager(string rawValue) => $$"""
        {
          "id": "aa11bb22-cc33-dd44-ee55-ff6677889900",
          "groupId": "bb22cc33-dd44-ee55-ff66-778899001122",
          "groupName": "Test Group",
          "store": "AzureAd",
          "members": [
            { "source": "Personalsystem", "action": "Include",
              "rules": [ { "name": "Organisation", "value": "011JABCDEF12" },
                         { "name": "IncludeManager", "value": {{rawValue}} } ] }
          ]
        }
        """;

    [Fact]
    public void TestValidJsonDeserializes()
    {
        List<ValidationError> errors = [];
        GrouperDocument? document = GrouperDocument.FromJson(ValidJson, errors);

        Assert.Empty(errors);
        Assert.NotNull(document);
        Assert.Equal("Test Group", document.GroupName);
        Assert.Equal(GroupStore.AzureAd, document.Store);
        Assert.Equal(GroupOwnerAction.KeepExisting, document.Owner);
        Assert.Single(document.Members);
    }

    [Fact]
    public void TestOmittedOwnerDefaultsToKeepExisting()
    {
        string json = ValidJson.Replace("\"owner\": \"KeepExisting\",", "");

        GrouperDocument document = GrouperDocument.FromJson(json);

        Assert.Equal(GroupOwnerAction.KeepExisting, document.Owner);
    }

    [Fact]
    public void TestOmittedIntervalDefaultsToZero()
    {
        Assert.Equal(0, GrouperDocument.FromJson(ValidJson).Interval);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TestNullOrEmptyJsonReportsMissingError(string? json)
    {
        List<ValidationError> errors = [];
        GrouperDocument? document = GrouperDocument.FromJson(json!, errors);

        Assert.Null(document);
        Assert.Contains(ResourceString.ValidationJsonMissingError, errors.Select(e => e.ErrorId));
    }

    [Fact]
    public void TestMalformedJsonReportsParsingError()
    {
        List<ValidationError> errors = [];
        GrouperDocument? document = GrouperDocument.FromJson("{ not valid json", errors);

        Assert.Null(document);
        Assert.Contains(ResourceString.ValidationJsonParsingError, errors.Select(e => e.ErrorId));
    }

    [Fact]
    public void TestUnknownEnumValueReportsParsingError()
    {
        List<ValidationError> errors = [];
        GrouperDocument? document = GrouperDocument.FromJson(ValidJson.Replace("AzureAd", "NoSuchStore"), errors);

        Assert.Null(document);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void TestValidJsonDescribingAnInvalidDocumentReportsDocumentErrors()
    {
        // Parses fine, but the group name is empty.
        List<ValidationError> errors = [];
        GrouperDocument? document =
            GrouperDocument.FromJson(ValidJson.Replace("\"Test Group\"", "\"\""), errors);

        Assert.Null(document);
        Assert.Contains(ResourceString.ValidationErrorGroupNameIsNullOrEmpty, errors.Select(e => e.ErrorId));
    }

    /// <summary>The overload without an error list throws instead of returning null.</summary>
    [Fact]
    public void TestFromJsonThrowsOnInvalidDocument()
    {
        Assert.Throws<InvalidGrouperDocumentException>(() => GrouperDocument.FromJson("{ not valid json"));
    }

    // ---- StringOrBooleanConverter --------------------------------------------------------

    /// <summary>
    /// A rule value may arrive as a JSON boolean rather than a string -- this is why
    /// <see cref="StringOrBooleanConverter"/> exists. It renders via <c>bool.ToString()</c>, so the
    /// stored value is "True"/"False" with a capital letter; the IncludeManager regex is
    /// case-insensitive, so both forms validate.
    /// </summary>
    [Theory]
    [InlineData("true", "True")]
    [InlineData("false", "False")]
    public void TestBooleanRuleValueIsCoercedToString(string rawJson, string expected)
    {
        GrouperDocument document = GrouperDocument.FromJson(JsonWithIncludeManager(rawJson));

        GrouperDocumentRule rule = document.Members.Single().Rules.Single(r => r.Name == "IncludeManager");
        Assert.Equal(expected, rule.Value);
    }

    [Theory]
    [InlineData("\"true\"", "true")]
    [InlineData("\"false\"", "false")]
    public void TestStringRuleValueIsUnchanged(string rawJson, string expected)
    {
        GrouperDocument document = GrouperDocument.FromJson(JsonWithIncludeManager(rawJson));

        GrouperDocumentRule rule = document.Members.Single().Rules.Single(r => r.Name == "IncludeManager");
        Assert.Equal(expected, rule.Value);
    }

    /// <summary>A boolean rule value survives a round-trip, re-serialized as a quoted string.</summary>
    [Fact]
    public void TestBooleanRuleValueRoundTripsAsAString()
    {
        GrouperDocument document = GrouperDocument.FromJson(JsonWithIncludeManager("true"));

        string json = document.ToJson();
        GrouperDocument reparsed = GrouperDocument.FromJson(json);

        Assert.Contains("\"True\"", json);
        Assert.Equal("True", reparsed.Members.Single().Rules.Single(r => r.Name == "IncludeManager").Value);
    }

    // ---- ToJson -------------------------------------------------------------------------

    [Fact]
    public void TestToJsonRoundTripsThroughFromJson()
    {
        GrouperDocument original = GrouperDocument.FromJson(ValidJson);

        GrouperDocument roundTripped = GrouperDocument.FromJson(original.ToJson());

        Assert.Equal(original.Id, roundTripped.Id);
        Assert.Equal(original.GroupId, roundTripped.GroupId);
        Assert.Equal(original.GroupName, roundTripped.GroupName);
        Assert.Equal(original.Store, roundTripped.Store);
        Assert.Equal(original.Owner, roundTripped.Owner);
        Assert.Equal(original.Members.Count, roundTripped.Members.Count);
    }

    [Fact]
    public void TestCompactJsonHasNoLineBreaksAndIndentedDoes()
    {
        GrouperDocument document = GrouperDocument.FromJson(ValidJson);

        Assert.DoesNotContain('\n', document.ToJson());
        Assert.Contains('\n', document.ToJson(indented: true));
    }

    /// <summary>
    /// The document is stored with <c>ToJson(indented: false)</c>, so a non-ASCII group name must
    /// survive without being \u-escaped -- the serializer uses relaxed escaping for that reason.
    /// </summary>
    [Fact]
    public void TestNonAsciiGroupNameIsNotEscaped()
    {
        GrouperDocument document =
            GrouperDocument.FromJson(ValidJson.Replace("Test Group", "Elever i klass 7A på Testskolan"));

        string json = document.ToJson();

        Assert.Contains("på", json);
        Assert.DoesNotContain("\\u00e5", json);
    }

    [Fact]
    public void TestCreateReturnsNullAndCollectsErrorsForAnInvalidDocument()
    {
        List<ValidationError> errors = [];
        GrouperDocument? document = GrouperDocument.Create(
            Guid.NewGuid(), 0, Guid.Empty, "", GroupStore.AzureAd, GroupOwnerAction.AddAll, [], errors);

        Assert.Null(document);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void TestCreateThrowsWithoutAnErrorList()
    {
        Assert.Throws<InvalidGrouperDocumentException>(() => GrouperDocument.Create(
            Guid.NewGuid(), 0, Guid.Empty, "", GroupStore.AzureAd, GroupOwnerAction.AddAll, []));
    }

    // ---- Reading a document written under earlier rules ----------------------------------

    /// <summary>
    /// A document of the kind this exists for. Elevregister's "Klass" rule used to hold a class
    /// display name and now holds a class GUID, so revisions written before that change no longer
    /// satisfy the rules -- and still have to be readable, or they could never be corrected.
    /// </summary>
    private const string OutdatedJson = """
        {
          "id": "aa11bb22-cc33-dd44-ee55-ff6677889900",
          "groupId": "bb22cc33-dd44-ee55-ff66-778899001122",
          "groupName": "Test Group",
          "store": "AzureAd",
          "members": [
            { "source": "Elevregister", "action": "Include",
              "rules": [ { "name": "Klass", "value": "7A" } ] }
          ]
        }
        """;

    [Fact]
    public void TestOutdatedDocumentIsRejectedByFromJson()
    {
        List<ValidationError> errors = [];
        GrouperDocument? document = GrouperDocument.FromJson(OutdatedJson, errors);

        Assert.Null(document);
        Assert.Contains(ResourceString.ValidationErrorRuleValueDoesNotValidate, errors.Select(e => e.ErrorId));
    }

    [Fact]
    public void TestOutdatedDocumentIsReadableWithoutValidation()
    {
        GrouperDocument document = GrouperDocument.FromJsonUnvalidated(OutdatedJson);

        Assert.Equal("7A", document.Members.Single().Rules.Single().Value);
    }

    [Fact]
    public void TestUnvalidatedParseReportsNoErrorsOfItsOwn()
    {
        List<ValidationError> parseErrors = [];
        GrouperDocument? document = GrouperDocument.FromJsonUnvalidated(OutdatedJson, parseErrors);

        Assert.NotNull(document);
        Assert.Empty(parseErrors);
    }

    /// <summary>
    /// Skipping validation is not the same as accepting anything. JSON that cannot be turned into a
    /// document at all is corruption rather than an outdated document, and still fails.
    /// </summary>
    [Theory]
    [InlineData("{ not valid json")]
    [InlineData("")]
    public void TestUnvalidatedParseStillRejectsUnreadableJson(string json)
    {
        Assert.Throws<InvalidGrouperDocumentException>(() => GrouperDocument.FromJsonUnvalidated(json));
    }

    [Fact]
    public void TestUnvalidatedParseStillRejectsAnUnknownStore()
    {
        Assert.Throws<InvalidGrouperDocumentException>(
            () => GrouperDocument.FromJsonUnvalidated(ValidJson.Replace("AzureAd", "NoSuchStore")));
    }

    [Fact]
    public void TestValidateReportsWhatFromJsonWouldHaveRejected()
    {
        GrouperDocument document = GrouperDocument.FromJsonUnvalidated(OutdatedJson);

        IReadOnlyList<ValidationError> errors = document.Validate();

        Assert.Contains(ResourceString.ValidationErrorRuleValueDoesNotValidate, errors.Select(e => e.ErrorId));
    }

    [Fact]
    public void TestValidateFindsNothingWrongWithAValidDocument()
    {
        Assert.Empty(GrouperDocument.FromJson(ValidJson).Validate());
    }

    /// <summary>
    /// The errors travel with the exception, so a caller that only catches still learns what was
    /// wrong rather than being told the document was bad and nothing more.
    /// </summary>
    [Fact]
    public void TestExceptionCarriesTheValidationErrors()
    {
        InvalidGrouperDocumentException exception =
            Assert.Throws<InvalidGrouperDocumentException>(() => GrouperDocument.FromJson(OutdatedJson));

        Assert.Contains(ResourceString.ValidationErrorRuleValueDoesNotValidate,
            exception.ValidationErrors.Select(e => e.ErrorId));
        Assert.Contains("Klass", exception.Message);
    }
}
