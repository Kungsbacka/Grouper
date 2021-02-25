using System;
using Xunit;
using GrouperLib.Core;
using System.Collections.Generic;

namespace GrouperLib.Test
{
    public class UpnValidatorTest
    {

        private readonly Dictionary<string, string> _validUpns = new Dictionary<string, string>()
        {
            { "Valid UPN", "jdoe@example.com" },
            { "Multiple sub domains", "jdoe@one.two.three" },
            { "Dots in name", "j.c.doe@example.com" }
        };

        private readonly Dictionary<string, string> _invalidUpns = new Dictionary<string, string>()
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

        [Fact]
        public void TestWithNullUpn()
        {
            foreach (string key in _invalidUpns.Keys)
            {
                Assert.Throws<ArgumentNullException>(() => { IsUpnValid(null); });
            }
        }


        private bool IsUpnValid(string upn)
        {
            GrouperDocument document = TestHelpers.MakeDocument(new
            {
                Members = new[]
                {
                    new
                    {
                        Source = GroupMemberSources.Static,
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
            UpnValidator upnValidator = new UpnValidator();
            List<ValidationError> validationErrors = new List<ValidationError>();
            upnValidator.Validate(document, document.Members[0], validationErrors);
            return validationErrors.Count == 0;
        }
    }
}
