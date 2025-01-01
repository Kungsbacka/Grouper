using GrouperLib.Core;

namespace GrouperLib.Test;

public class StringExtensionsTest
{
    [Fact]
    public void TestIEqualsWithNullLeftParameter()
    {
        Assert.False(((string?)null).IEquals("string"));
    }

    [Fact]
    public void TestIEqualsWithNullRightParameter()
    {
        Assert.False("string".IEquals(null));
    }

    [Fact]
    public void TestIEqualsWithBothParametersNull()
    {
        Assert.False(((string?)null).IEquals(null));
    }

    [Fact]
    public void TestIEqualsShouldBeEqual1()
    {
        Assert.True("string".IEquals("string"));
    }

    [Fact]
    public void TestIEqualsShouldBeEqual2()
    {
        Assert.True("STRING".IEquals("string"));
    }

    [Fact]
    public void TestIEqualsShouldNotBeEqual()
    {
        Assert.False("apples".IEquals("oranges"));
    }
}