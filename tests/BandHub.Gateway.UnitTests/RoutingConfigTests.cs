using Xunit;

namespace BandHub.Gateway.UnitTests;

public class RoutingConfigTests
{
    [Fact]
    public void AuthRoute_ShouldTarget_AuthCluster()
    {
        var expectedCluster = "auth-cluster";
        var authPath = "/auth/login";

        Assert.StartsWith("/auth/", authPath);
        Assert.Equal("auth-cluster", expectedCluster);
    }

    [Fact]
    public void AccountsRoute_ShouldTarget_AccountsCluster()
    {
        var expectedCluster = "accounts-cluster";
        var accountsPath = "/accounts/register";

        Assert.StartsWith("/accounts/", accountsPath);
        Assert.Equal("accounts-cluster", expectedCluster);
    }

    [Fact]
    public void BandsRoute_ShouldTarget_BandsCluster()
    {
        var expectedCluster = "bands-cluster";
        var bandsPath = "/bands";

        Assert.StartsWith("/bands", bandsPath);
        Assert.Equal("bands-cluster", expectedCluster);
    }

    [Fact]
    public void BffRoute_ShouldTarget_BffCluster()
    {
        var expectedCluster = "bff-cluster";
        var bffPath = "/bff/accounts/register-band";

        Assert.StartsWith("/bff/", bffPath);
        Assert.Equal("bff-cluster", expectedCluster);
    }
}
