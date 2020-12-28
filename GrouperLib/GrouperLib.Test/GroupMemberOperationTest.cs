using System;
using Xunit;
using GrouperLib.Core;

namespace GrouperLib.Test
{
    public class GroupMemberOperationTest
    {
        [Fact]
        public void TestConstruction()
        {
            GrouperDocument document = TestHelpers.MakeDocument();
            GroupMember member = new GroupMember(Guid.Empty, "Member", GroupMemberTypes.OnPremAd);
            GroupMemberOperations operation = GroupMemberOperations.Add;
            GroupMemberOperation memberOperation = new GroupMemberOperation(document, member, operation);
            Assert.Equal(document.GroupId, memberOperation.GroupId);
            
            // CONTINUE HERE

            //Assert.Equal(member.Id)
        }
    }
}
