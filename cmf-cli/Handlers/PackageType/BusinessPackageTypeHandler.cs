﻿using Cmf.Common.Cli.Builders;
using Cmf.Common.Cli.Enums;
using Cmf.Common.Cli.Objects;
using Cmf.Common.Cli.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cmf.Common.Cli.Commands.restore;

namespace Cmf.Common.Cli.Handlers
{
    /// <summary>
    ///
    /// </summary>
    /// <seealso cref="Cmf.Common.Cli.Handlers.PackageTypeHandler" />
    public class BusinessPackageTypeHandler : PackageTypeHandler
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessPackageTypeHandler" /> class.
        /// </summary>
        /// <param name="cmfPackage">The CMF package.</param>
        public BusinessPackageTypeHandler(CmfPackage cmfPackage) : base(cmfPackage)
        {
            cmfPackage.SetDefaultValues(
            targetDirectory:
                "BusinessTier",
            targetLayer:
                "host");

            BuildSteps = new IBuildCommand[]
            {
                new ExecuteCommand<RestoreCommand>()
                {
                    Command = new RestoreCommand(),
                    DisplayName = "cmf restore",
                    Execute = command =>
                    {
                        command.Execute(cmfPackage.GetFileInfo().Directory, null);
                    }
                },
                new DotnetCommand()
                {
                    Command = "restore",
                    DisplayName = "NuGet restore",
                    Solution = this.fileSystem.FileInfo.FromFileName(Path.Join(cmfPackage.GetFileInfo().Directory.FullName, "Business.sln")),
                    NuGetConfig = this.fileSystem.FileInfo.FromFileName(Path.Join(FileSystemUtilities.GetProjectRoot(this.fileSystem, throwException: true).FullName, "NuGet.Config")),
                    WorkingDirectory = cmfPackage.GetFileInfo().Directory
                },
                new DotnetCommand()
                {
                    Command = "build",
                    DisplayName = "Build Business Solution",
                    Solution = this.fileSystem.FileInfo.FromFileName(Path.Join(cmfPackage.GetFileInfo().Directory.FullName, "Business.sln")),
                    Configuration = "Release",
                    WorkingDirectory = cmfPackage.GetFileInfo().Directory,
                    Args = new [] { "--no-restore "}
                }
            };
        }

        /// <summary>
        /// Bumps the specified CMF package.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="buildNr">The version for build Nr.</param>
        /// <param name="bumpInformation">The bump information.</param>
        public override void Bump(string version, string buildNr, Dictionary<string, object> bumpInformation = null)
        {
            base.Bump(version, buildNr, bumpInformation);

            string[] versionTags = null;
            if (!string.IsNullOrWhiteSpace(version))
            {
                versionTags = version.Split('.');
            }

            // Assembly Info
            string[] filesToUpdate = this.fileSystem.Directory.GetFiles(".", "AssemblyInfo.cs", SearchOption.AllDirectories);
            string pattern = @"Version\(\""[0-9.]*\""\)";
            foreach (var filePath in filesToUpdate)
            {
                string text = this.fileSystem.File.ReadAllText(filePath);
                var metadataVersionInfo = Regex.Match(text, pattern, RegexOptions.Singleline)?.Value?.Split("\"")[1].Split('.');
                string major = versionTags != null && versionTags.Length > 0 ? versionTags[0] : metadataVersionInfo[0];
                string minor = versionTags != null && versionTags.Length > 1 ? versionTags[1] : metadataVersionInfo[1];
                string patch = versionTags != null && versionTags.Length > 2 ? versionTags[2] : metadataVersionInfo[2];
                string build = !string.IsNullOrEmpty(buildNr) ? buildNr : "0";
                string newVersion = string.Format(@"Version(""{0}.{1}.{2}.{3}"")", major, minor, patch, build);
                text = Regex.Replace(text, pattern, newVersion, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                this.fileSystem.File.WriteAllText(filePath, text);
            }
        }
    }
}