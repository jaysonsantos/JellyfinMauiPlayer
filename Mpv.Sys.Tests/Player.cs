namespace Mpv.Sys.Tests;

public class Player
{
    [Fact]
    public void Test1()
    {
        var client = new MpvClient();
        Assert.NotNull(client);
        client.Dispose();
    }

    [Fact]
    public void Version()
    {
        Assert.Equal(131077uL, MpvClient.Version());
    }
}
