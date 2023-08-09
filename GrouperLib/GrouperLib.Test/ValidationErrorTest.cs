using System;
using Xunit;
using GrouperLib.Core;
using GrouperLib.Language;
using Moq;

namespace GrouperLib.Test
{
    public class ValidationErrorTest
    {
        [Fact]
        public void TestConstruction()
        {
            string property = "property";
            string errorId = "errorId";
            string message = "message";
            var stringResourceHelperMock = new Mock<IStringResourceHelper>();
            stringResourceHelperMock
                .Setup(m => m.GetString(It.IsAny<string>(), It.IsAny<object[]>()))
                .Returns(message);
            ValidationError validationError = new(stringResourceHelperMock.Object, property, errorId);
            Assert.Equal(validationError.PropertyName, property);
            Assert.Equal(validationError.ErrorId, errorId);
            Assert.Equal(validationError.ErrorMessage, message);
        }

        [Fact]
        public void TestSerialzedNames()
        {

            //GroupMemberOperations operation = GroupMemberOperations.Add;
            //GroupMemberOperation memberOperation = new GroupMemberOperation(TestHelpers.DefaultDocumentId, TestHelpers.DefaultGroupName, member, operation);
            //string json = JsonConvert.SerializeObject(memberOperation);
            //JObject obj = JObject.Parse(json);
            //Assert.True(obj.ContainsKey("groupId"));
            //Assert.True(obj.ContainsKey("groupName"));
            //Assert.True(obj.ContainsKey("member"));
            //Assert.True(obj.ContainsKey("operation"));

        }
    }
}
