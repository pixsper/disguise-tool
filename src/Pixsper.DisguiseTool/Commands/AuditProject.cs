using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pixsper.DisguiseTool.Commands;

[Description("Creates a CSV file with information on the contents of a disguise projects object directory")]
public class AuditProject : Command<AuditProject.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [Description("Disguise project directory path")]
        [CommandArgument(0, "<project_path>")]
        public string[] ProjectDirectoryPaths { get; init; } = Array.Empty<string>();

        [Description("File extensions to include in audit. If not provided all file types will be audited.")]
        [CommandOption("-i|--include")]
        public string[] IncludedFileExtensions { get; init; } = Array.Empty<string>();

        [Description("File extensions to exclude from audit.")]
        [CommandOption("-e|--exclude")]
        public string[] ExcludedFileExtensions { get; init; } = Array.Empty<string>();

        [Description("Output file name")]
        [CommandOption("-o|--output")]
        [DefaultValue("audit")]
        public string OutputFileName { get; init; } = "audit"; 

        public override ValidationResult Validate()
        {
            return ProjectDirectoryPaths.Length > 0
                ? ValidationResult.Success() 
                : ValidationResult.Error("Must specify at least one disguise project path");
        }
    }

    public override int Execute([NotNull]CommandContext context, [NotNull]Settings settings)
    {
        var records = new List<AuditRecord>();


        foreach (var projectPath in settings.ProjectDirectoryPaths)
        {
            if (!Directory.Exists(projectPath))
            {
                AnsiConsole.MarkupLine($"[red]Couldn't find project directory at path '{projectPath}'[/]");
                continue;
            }

            var objectsPath = Path.Combine(projectPath, "objects");

            if (!Directory.Exists(objectsPath))
            {
                AnsiConsole.MarkupLine(
                    $"[red]Couldn't find objects directory in project '{Path.GetFileName(projectPath)}'[/]");
                continue;
            }
        
            foreach (var file in Directory.EnumerateFiles(objectsPath, "*.*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(file).TrimStart('.');

                if ((settings.IncludedFileExtensions.Length > 0 && !settings.IncludedFileExtensions.Contains(extension)) || 
                    settings.ExcludedFileExtensions.Contains(extension))
                    continue;
                
                try
                {
                    var info = new FileInfo(file);

                    var relativePath = Path.GetRelativePath(objectsPath, file); 

                    var entry = new AuditRecord
                    {
                        ProjectPath = projectPath,
                        FileName = relativePath[..^Path.GetExtension(relativePath).Length],
                        Extension = extension,
                        CreationTime = info.CreationTime,
                        LastWriteTime = info.LastWriteTime,
                        SizeInMegaBytes = (info.Length / 1024d) / 1024d
                    };

                    records.Add(entry);
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine($"[red]Failed to open info on file '{file}', skipping...[/]");
                }
            }
        }

        var outputFileName = $"{settings.OutputFileName}_{DateTime.Now:yyyy-dd-MTHH-mm-ss}.csv";

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        using (var writer = new StreamWriter(outputFileName))
        using (var csv = new CsvWriter(writer, config))
        {
            csv.WriteHeader<AuditRecord>();
            csv.NextRecord();
            csv.WriteRecords(records);
        }

        return 0;
    }

    record AuditRecord
    {
        [Name("ProjectPath")]
        public string ProjectPath { get; init; } = string.Empty;

        [Name("File Name")]
        public string FileName { get; init; } = string.Empty;

        [Name("Extensions")]
        public string Extension { get; init; } = string.Empty;

        [Name("Creation Time")] 
        [Format("s")]
        public DateTime CreationTime { get; init; }

        [Name("Last Write Time")]
        [Format("s")]
        public DateTime LastWriteTime { get; init; }

        [Name("Size (MB)")]
        [Format("N2")]
        public double SizeInMegaBytes { get; init; }
    }
}