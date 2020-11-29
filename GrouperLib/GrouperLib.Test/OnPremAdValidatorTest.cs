using System;
using Xunit;
using GrouperLib.Core;
using System.Collections.Generic;

namespace GrouperLib.Test
{
    public class OnPremAdValidatorTest
    {
        private static readonly Guid documentId = Guid.Parse("3cbc0481-23b0-4860-a58a-7a723ee250c5");
        private static readonly Guid groupId = Guid.Parse("baefe5f4-d404-491d-89d0-fb192afa3c1d");
        private static readonly string groupName = "Test Group";
        private static readonly Guid targetGroupId = Guid.Parse("2422bdbd-8a4d-4996-99db-d9ed29294779");
        private static readonly GroupStores store = GroupStores.OnPremAd;
        private static readonly GroupOwnerActions ownerAction = GroupOwnerActions.KeepExisting;

        [Fact]
        public void TestOnPremAdValidatorValidateWithLegalDocument()
        {

            GrouperDocument document = TestHelpers.MakeDocument(new
            {
                Id = documentId,
                Interval = 0,
                GroupName = groupName,
                GroupId = groupId,
                Store = store,
                Owner = ownerAction,
                Members = new []
                {
                    new
                    {
                        Action = GroupMemberActions.Include,
                        Source = GroupMemberSources.OnPremAdGroup,
                        Rules = new []
                        {
                            new
                            {
                                Name = "Group",
                                Value = targetGroupId
                            }
                        }
                    }
                }
            });
            ICustomValidator validator = new OnPremAdValidator();
            List<ValidationError> errors = new List<ValidationError>();
            validator.Validate(document, document.Members[0], errors);
            Assert.True(errors.Count == 0);
        }

        [Fact]
        public void TestOnPremAdValidatorValidateWithBrokenDocument()
        {
            GrouperDocument document = TestHelpers.MakeDocument(new
            {
                Id = documentId,
                Interval = 0,
                GroupName = groupName,
                GroupId = groupId,
                Store = store,
                Owner = ownerAction,
                Members = new []
                {
                    new
                    {
                        Action = GroupMemberActions.Include,
                        Source = GroupMemberSources.OnPremAdGroup,
                        Rules = new[]
                        {
                            new
                                {
                                    Name = "Group",
                                    Value = groupId
                                }
                        }
                    }
                }
            });
            ICustomValidator validator = new OnPremAdValidator();
            List<ValidationError> errors = new List<ValidationError>();
            validator.Validate(document, document.Members[0], errors);
            Assert.True(errors.Count > 0);
        }
    }
}
