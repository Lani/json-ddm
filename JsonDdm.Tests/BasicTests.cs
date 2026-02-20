namespace JsonDdm.Tests;

public class BasicTests
{
    [Fact]
    public void SanityCheck()
    {
        Assert.NotNull(new JsonDdm());
    }
}
