using GrouperLib.Core;
using GrouperLib.Database;
using System.Runtime.Versioning;

namespace GrouperLib.Test;

/// <summary>
/// Covers the part of <see cref="DocumentDb"/> that can be reached without a database. Storing is
/// gated on validation and the gate sits in front of the connection, so an invalid document is
/// refused rather than written. That is what lets the read side hand back an older revision which
/// no longer validates without turning it into a way to store one.
/// </summary>
[SupportedOSPlatform("windows")]
public class DocumentDbTest
{
    private const string UnreachableDatabase = "Server=(local);Database=NoSuchDatabase;Connect Timeout=1;";

    [Fact]
    public async Task TestStoreRejectsAnInvalidDocumentBeforeReachingTheDatabase()
    {
        DocumentDb documentDb = new(UnreachableDatabase, "Test");
        GrouperDocument invalid = TestHelpers.MakeDocument(new { GroupName = "" });

        InvalidGrouperDocumentException exception = await Assert.ThrowsAsync<InvalidGrouperDocumentException>(
            () => documentDb.StoreDocumentAsync(invalid));

        Assert.NotEmpty(exception.ValidationErrors);
    }

    [Fact]
    public async Task TestStoreRejectsNull()
    {
        DocumentDb documentDb = new(UnreachableDatabase, "Test");

        await Assert.ThrowsAsync<ArgumentNullException>(() => documentDb.StoreDocumentAsync(null!));
    }
}
