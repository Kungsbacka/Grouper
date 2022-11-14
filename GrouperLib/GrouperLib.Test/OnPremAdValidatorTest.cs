﻿using System;
using Xunit;
using GrouperLib.Core;
using System.Collections.Generic;
using System.Linq;

namespace GrouperLib.Test
{
    public class OnPremAdValidatorTest
    {
        private static readonly Guid groupId = Guid.Parse("baefe5f4-d404-491d-89d0-fb192afa3c1d");
        private static readonly Guid targetGroupId = Guid.Parse("2422bdbd-8a4d-4996-99db-d9ed29294779");

        [Fact]
        public void TestValidateWithLegalDocument()
        {

            GrouperDocument document = TestHelpers.MakeDocument(new
            {
                GroupId = groupId,
                Members = new []
                {
                    new
                    {
                        Source = GroupMemberSource.OnPremAdGroup,
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
            validator.Validate(document, document.Members.First(), errors);
            Assert.True(errors.Count == 0);
        }

        [Fact]
        public void TestValidateWithBrokenDocument()
        {
            GrouperDocument document = TestHelpers.MakeDocument(new
            {
                GroupId = groupId,
                Members = new []
                {
                    new
                    {
                        Source = GroupMemberSource.OnPremAdGroup,
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
            validator.Validate(document, document.Members.First(), errors);
            Assert.True(errors.Count > 0);
        }
    }
}
