using System;
using System.IO;
using Cake.Common;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Clean;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;

public static class Program
{
    public static int Main(string[] args)
    {
        return new CakeHost().UseContext<BuildContext>().Run(args);
    }
}

public class BuildContext : FrostingContext
{
    public const string ProjectName = "SailboatHotkeys";
    public string BuildConfiguration { get; set; }
    public string Version { get; }
    public string Name { get; }
    public string GameVersion { get; }
    public string Author { get; }
    public bool SkipJsonValidation { get; set; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        BuildConfiguration = context.Argument("configuration", "Release");
        SkipJsonValidation = context.Argument("skipJsonValidation", false);
        var modInfo = context.DeserializeJsonFromFile<ModInfo>(
            $"../{BuildContext.ProjectName}/modinfo.json"
        );
        Version = modInfo.Version;
        Name = modInfo.ModID;
        foreach (var dependency in modInfo.Dependencies)
        {
            if (dependency.ModID == "game")
            {
                GameVersion = dependency.Version;
            }
        }
        foreach (var author in modInfo.Authors)
        {
            Author = author;
            break;
        }
    }
}

[TaskName("ValidateJson")]
public sealed class ValidateJsonTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        if (context.SkipJsonValidation)
        {
            return;
        }
        var jsonFiles = context.GetFiles($"../{BuildContext.ProjectName}/assets/**/*.json");
        foreach (var file in jsonFiles)
        {
            try
            {
                var json = File.ReadAllText(file.FullPath);
                JToken.Parse(json);
            }
            catch (JsonException ex)
            {
                throw new Exception(
                    $"Validation failed for JSON file: {file.FullPath}{Environment.NewLine}{ex.Message}",
                    ex
                );
            }
        }
    }
}

[TaskName("Build")]
[IsDependentOn(typeof(ValidateJsonTask))]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetClean(
            $"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
            new DotNetCleanSettings { Configuration = context.BuildConfiguration }
        );

        context.DotNetPublish(
            $"../{BuildContext.ProjectName}/{BuildContext.ProjectName}.csproj",
            new DotNetPublishSettings { Configuration = context.BuildConfiguration }
        );
    }
}

[TaskName("Package")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        string releaseDir =
            $"../Releases/{context.Author}{context.Name}-v{context.Version}_v{context.GameVersion}";
        context.EnsureDirectoryExists("../Releases");
        context.CleanDirectory("../Releases");
        context.EnsureDirectoryExists(releaseDir);
        context.CopyFiles(
            $"../{BuildContext.ProjectName}/bin/{context.BuildConfiguration}/Mods/mod/publish/*",
            releaseDir
        );
        context.CopyFile(
            $"../{BuildContext.ProjectName}/modinfo.json",
            $"{releaseDir}/modinfo.json"
        );
        if (context.FileExists($"../{BuildContext.ProjectName}/modicon.png"))
        {
            context.CopyFile(
                $"../{BuildContext.ProjectName}/modicon.png",
                $"{releaseDir}/modicon.png"
            );
        }
        context.Zip(releaseDir, $"{releaseDir}.zip");
    }
}

[TaskName("Default")]
[IsDependentOn(typeof(PackageTask))]
public class DefaultTask : FrostingTask { }
