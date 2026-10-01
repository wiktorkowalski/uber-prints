using UberPrints.Server.Configuration;

namespace UberPrints.Server.UnitTests;

public sealed class DotEnvLocatorTests : IDisposable
{
  private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("dotenv-locator-");

  public void Dispose() => _root.Delete(recursive: true);

  [Fact]
  public void Find_ReturnsNearestEnvFileWalkingUp()
  {
    File.WriteAllText(Path.Combine(_root.FullName, "UberPrints.sln"), string.Empty);
    File.WriteAllText(Path.Combine(_root.FullName, ".env"), string.Empty);
    var start = _root.CreateSubdirectory(Path.Combine("src", "UberPrints.Server", "bin"));

    var path = DotEnvLocator.Find(start.FullName);

    Assert.Equal(Path.Combine(_root.FullName, ".env"), path);
  }

  [Fact]
  public void Find_StopsAtRepositoryRoot()
  {
    // A .env above the repo root (e.g. the home directory) must not be picked up
    File.WriteAllText(Path.Combine(_root.FullName, ".env"), string.Empty);
    var repo = _root.CreateSubdirectory("repo");
    File.WriteAllText(Path.Combine(repo.FullName, ".git"), "gitdir: elsewhere");
    var start = repo.CreateSubdirectory("src");

    Assert.Null(DotEnvLocator.Find(start.FullName));
  }
}
