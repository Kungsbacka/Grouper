using GrouperLib.Core;

namespace GrouperLib.Test;

public class UpnValidatorTest
{

    private readonly Dictionary<string, string> _validUpns = new()
    {
        { "Valid UPN", "jdoe@example.com" },
        { "Multiple sub domains", "jdoe@one.two.three" },
        { "Dots in name", "j.c.doe@example.com" }
    };

    private readonly Dictionary<string, string> _invalidUpns = new()
    {
        { "Empty string", "" },
        { "Missing at (@)", "jdoeexample.com" },
        { "Missing name", "@example.com" },
        { "Missing domain", "jdoe@" },
        { "No top domain", "jdoe@example" },
        { "Username starts with -", "-jdoe@example.com" },
        { "Username starts with .", ".jdoe@example.com" },
        { "Username ends with -", "jdoe-@example.com" },
        { "Username ends with .", "jdoe.@example.com" },
        { "Username contains ..", "j..doe@example.com" },
        { "Invalid char ! in username", "j!doe@example.com" },
        { "Invalid char @ in username", "j@doe@example.com" },
        { "Invalid char # in username", "j#doe@example.com" },
        { "Invalid char $ in username", "j$doe@example.com" },
        { "Invalid char % in username", "j%doe@example.com" },
        { "Invalid char ^ in username", "j^doe@example.com" },
        { "Invalid char & in username", "j&doe@example.com" },
        { "Invalid char * in username", "j*doe@example.com" },
        { "Invalid char ( in username", "j(doe@example.com" },
        { "Invalid char ) in username", "j)doe@example.com" },
        { "Invalid char + in username", "j+doe@example.com" },
        { "Invalid char = in username", "j=doe@example.com" },
        { "Invalid char [ in username", "j[doe@example.com" },
        { "Invalid char ] in username", "j]doe@example.com" },
        { "Invalid char { in username", "j{doe@example.com" },
        { "Invalid char } in username", "j}doe@example.com" },
        { "Invalid char \\ in username", "j\\doe@example.com" },
        { "Invalid char / in username", "j/doe@example.com" },
        { "Invalid char | in username", "j|doe@example.com" },
        { "Invalid char ; in username", "j;doe@example.com" },
        { "Invalid char : in username", "j:doe@example.com" },
        { "Invalid char \" in username", "j\"doe@example.com" },
        { "Invalid char < in username", "j<doe@example.com" },
        { "Invalid char > in username", "j>doe@example.com" },
        { "Invalid char ? in username", "j?doe@example.com" },
        { "Invalid char , in username", "j,doe@example.com" }
    };

    [Fact]
    public void TestWithValidUpn()
    {
        foreach (string key in _validUpns.Keys)
        {
            Assert.True(IsUpnValid(_validUpns[key]), key);
        }
    }

    [Fact]
    public void TestWithInvalidUpn()
    {
        foreach (string key in _invalidUpns.Keys)
        {
            Assert.False(IsUpnValid(_invalidUpns[key]), key);
        }
    }

    private static bool IsUpnValid(string upn)
    {
        GrouperDocument document = TestHelpers.MakeDocument(new
        {
            Members = new[]
            {
                new
                {
                    Source = GroupMemberSource.Static,
                    Rules = new []
                    {
                        new
                        {
                            Name = "Upn",
                            Value = upn
                        }
                    }
                }
            }
        });
        UpnValidator upnValidator = new();
        List<ValidationError> validationErrors = new();
        upnValidator.Validate(document, document.Members.First(), validationErrors);
        return validationErrors.Count == 0;
    }
}