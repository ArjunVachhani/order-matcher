namespace OrderMatcher.Tests;

public class UserIdTests
{
    [Fact]
    public void WriteBytesMinTest()
    {
        var bytes = new byte[UserId.SizeOfUserId];
        UserId.MinValue.WriteBytes(bytes);
        var userId = UserId.ReadUserId(bytes);
        Assert.Equal(UserId.MinValue, userId);
    }

    [Fact]
    public void WriteBytesMaxTest()
    {
        var bytes = new byte[UserId.SizeOfUserId];
        UserId.MaxValue.WriteBytes(bytes);
        var userId = UserId.ReadUserId(bytes);
        Assert.Equal(UserId.MaxValue, userId);
    }

    [Fact]
    public void WriteBytesTest()
    {
        var bytes = new byte[UserId.SizeOfUserId];
        var userId = new UserId(10);
        userId.WriteBytes(bytes);
        var actualUserId = UserId.ReadUserId(bytes);
        Assert.Equal(userId, actualUserId);
    }

    [Fact]
    public void StaticWriteBytesMinTest()
    {
        var bytes = new byte[UserId.SizeOfUserId];
        UserId.WriteBytes(bytes, UserId.MinValue);
        var userId = UserId.ReadUserId(bytes);
        Assert.Equal(UserId.MinValue, userId);
    }

    [Fact]
    public void StaticWriteBytesMaxTest()
    {
        var bytes = new byte[UserId.SizeOfUserId];
        UserId.WriteBytes(bytes, UserId.MaxValue);
        var userId = UserId.ReadUserId(bytes);
        Assert.Equal(UserId.MaxValue, userId);
    }

    [Fact]
    public void StaticWriteBytesTest()
    {
        var bytes = new byte[UserId.SizeOfUserId];
        var userId = new UserId(10);
        UserId.WriteBytes(bytes, userId);
        var actualUserId = UserId.ReadUserId(bytes);
        Assert.Equal(userId, actualUserId);
    }
}
