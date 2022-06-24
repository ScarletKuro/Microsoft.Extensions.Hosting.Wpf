using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using Serilog;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    "continuous",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    On = new[] { GitHubActionsTrigger.Push },
    PublishArtifacts = true,
    InvokedTargets = new[] { nameof(Compile), nameof(Pack) },
    ImportSecrets = new[] { nameof(NuGetApiKey) })]
[GitHubActions(
    "release",
    GitHubActionsImage.UbuntuLatest,
    FetchDepth = 0,
    OnPushBranches = new []{ "main", "master" },
    OnPushTags = new[] { @"\d+\.\d+\.\d+" },
    PublishArtifacts = true,
    InvokedTargets = new[] { nameof(Pack) },
    ImportSecrets = new[] { nameof(NuGetApiKey) })]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = Configuration.Release; //always want to be release mode even on local machine

    [Parameter] string NugetApiUrl = "https://api.nuget.org/v3/index.json"; //default
    [Parameter][Secret] readonly string NuGetApiKey;

    bool IsTag => GitHubActions.Instance?.Ref?.StartsWith("refs/tags/") ?? false;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath PackagesDirectory => ArtifactsDirectory / "packages";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(ArtifactsDirectory);
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Pack => _ => _
        .After(Compile)
        .Produces(PackagesDirectory / "*.nupkg")
        .Produces(PackagesDirectory / "*.snupkg")
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() => {
            DotNetPack(_ => _
                .Apply(PackSettings));

            ReportSummary(_ => _
                .AddPair("Packages", PackagesDirectory.GlobFiles("*.nupkg").Count.ToString()));
        });

    Target Push => _ => _
        .DependsOn(Pack)
        .OnlyWhenStatic(() => IsTag && IsServerBuild && GitRepository.IsOnMainOrMasterBranch())
        .Requires(() => NuGetApiKey)
        .Requires(() => Configuration.Equals(Configuration.Release))
        .Executes(() =>
        {
            Log.Information("Running push to packages directory.");

            Assert.True(!string.IsNullOrEmpty(NuGetApiKey));

            GlobFiles(PackagesDirectory, "*.nupkg", "*.snupkg")
                .ForEach(x =>
                {
                    x.NotNullOrEmpty();
                    DotNetNuGetPush(s => s
                        .SetTargetPath(x)
                        .SetSource(NugetApiUrl)
                        .SetApiKey(NuGetApiKey)
                    );
                });
        });

    Configure<DotNetPackSettings> PackSettings => _ => _
        .SetProject(Solution)
        .SetConfiguration(Configuration)
        .SetNoBuild(SucceededTargets.Contains(Compile))
        .SetOutputDirectory(PackagesDirectory);
}
