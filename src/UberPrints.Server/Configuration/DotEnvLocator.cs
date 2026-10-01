namespace UberPrints.Server.Configuration;

internal static class DotEnvLocator
{
  // Integration tests set this so a developer's real .env never leaks secrets (e.g. the Discord bot token) into the test host
  internal const string SkipVariable = "UBERPRINTS_SKIP_DOTENV";

  private const string FileName = ".env";
  private const string SolutionFileName = "UberPrints.sln";
  private const string GitMarker = ".git";

  internal static string? Find(string startDirectory)
  {
    if (Environment.GetEnvironmentVariable(SkipVariable) is "1" or "true")
      return null;

    for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
    {
      var candidate = Path.Combine(directory.FullName, FileName);
      if (File.Exists(candidate))
        return candidate;

      if (IsRepositoryRoot(directory.FullName))
        return null;
    }

    return null;
  }

  // In a git worktree .git is a file, not a directory
  private static bool IsRepositoryRoot(string directory) =>
    File.Exists(Path.Combine(directory, SolutionFileName))
    || File.Exists(Path.Combine(directory, GitMarker))
    || Directory.Exists(Path.Combine(directory, GitMarker));
}
