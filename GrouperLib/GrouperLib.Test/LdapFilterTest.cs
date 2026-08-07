using GrouperLib.Store;

namespace GrouperLib.Test;

/// <summary>
/// Covers the pure LDAP filter construction in <c>Ldap</c> and <c>Helpers</c>. Every on-premises
/// group and member lookup goes through <c>ConvertToLdapGuidString</c>, and a byte-order or
/// escaping mistake there does not throw -- it silently matches nothing, so a document would
/// report the group as missing.
/// </summary>
public class LdapFilterTest
{
    /// <summary>
    /// objectGUID is stored little-endian for the first three components, so the escaped form is
    /// <c>Guid.ToByteArray()</c> order rather than the textual order. For
    /// 00112233-4455-6677-8899-aabbccddeeff the first four bytes reverse to 33 22 11 00.
    /// </summary>
    [Theory]
    [InlineData("00112233-4455-6677-8899-aabbccddeeff",
        @"\33\22\11\00\55\44\77\66\88\99\AA\BB\CC\DD\EE\FF")]
    [InlineData("00000000-0000-0000-0000-000000000000",
        @"\00\00\00\00\00\00\00\00\00\00\00\00\00\00\00\00")]
    [InlineData("ffffffff-ffff-ffff-ffff-ffffffffffff",
        @"\FF\FF\FF\FF\FF\FF\FF\FF\FF\FF\FF\FF\FF\FF\FF\FF")]
    [InlineData("baefe5f4-d404-491d-89d0-fb192afa3c1d",
        @"\F4\E5\EF\BA\04\D4\1D\49\89\D0\FB\19\2A\FA\3C\1D")]
    public void TestGuidIsEscapedInLittleEndianByteOrder(string guid, string expected)
    {
        Assert.Equal(expected, Ldap.ConvertToLdapGuidString(Guid.Parse(guid)));
    }

    /// <summary>Always 16 bytes as three characters each, so exactly 48 characters.</summary>
    [Fact]
    public void TestEscapedGuidIsAlwaysFortyEightCharacters()
    {
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(48, Ldap.ConvertToLdapGuidString(Guid.NewGuid()).Length);
        }
    }

    /// <summary>Hex digits are upper-case, which is what the escaping helper produces.</summary>
    [Fact]
    public void TestEscapedGuidUsesUpperCaseHex()
    {
        string escaped = Ldap.ConvertToLdapGuidString(Guid.Parse("aabbccdd-eeff-0011-2233-445566778899"));

        Assert.DoesNotContain(escaped, escaped.ToLowerInvariant());
        Assert.All(escaped.Where(char.IsLetter), c => Assert.True(char.IsUpper(c), $"'{c}' should be upper-case"));
    }

    [Fact]
    public void TestObjectGuidFilterWrapsTheEscapedGuid()
    {
        Guid guid = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");

        Assert.Equal(
            @"(objectGUID=\33\22\11\00\55\44\77\66\88\99\AA\BB\CC\DD\EE\FF)",
            Ldap.GetObjectGuidFilter(guid));
    }

    [Fact]
    public void TestMemberOfFilterWrapsTheDistinguishedName()
    {
        Assert.Equal(
            "(memberOf=CN=My Group,OU=Groups,DC=example,DC=com)",
            Ldap.GetMemberOfFilter("CN=My Group,OU=Groups,DC=example,DC=com"));
    }

    /// <summary>
    /// Characterizes that the DN is interpolated verbatim. LDAP special characters in a DN are not
    /// escaped here, which is safe today because every DN comes from a directory search result
    /// rather than from a Grouper document -- worth knowing before that changes.
    /// </summary>
    [Fact]
    public void TestMemberOfFilterDoesNotEscapeTheDistinguishedName()
    {
        Assert.Equal(@"(memberOf=CN=a(b)c*,DC=x)", Ldap.GetMemberOfFilter(@"CN=a(b)c*,DC=x"));
    }

    [Theory]
    [InlineData(0x0, '0')]
    [InlineData(0x9, '9')]
    [InlineData(0xA, 'A')]
    [InlineData(0xF, 'F')]
    [InlineData(0x10, '0')]      // only the low nibble is used
    [InlineData(0xFF, 'F')]
    public void TestHexCharMapsTheLowNibble(int value, char expected)
    {
        Assert.Equal(expected, Helpers.HexChar(value));
    }
}
