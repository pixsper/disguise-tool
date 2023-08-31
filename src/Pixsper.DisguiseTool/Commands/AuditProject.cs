using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using FFMpegCore;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pixsper.DisguiseTool.Commands;

[Description("Creates a CSV file with information on the contents of a disguise projects object directory")]
public class AuditProject : AsyncCommand<AuditProject.Settings>
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

        [Description("Search strings")]
        [CommandOption("-s|--search")]
        public string[] SearchStrings { get; init; } = Array.Empty<string>();

        [Description("Output file name")]
        [CommandOption("-o|--output")]
        [DefaultValue("audit")]
        public string OutputFileName { get; init; } = "audit"; 

        [Description("Get media info")]
        [CommandOption("-m|--mediainfo")]
        [DefaultValue(false)]
        public bool IsRequestMediaInfo { get; init; }

        [Description("FFMmpeg binary folder")]
        [CommandOption("-f|--ffmpeg")]
        [DefaultValue("")]
        public string FfmpegBinary { get; init; } = string.Empty;

        [Description("Max parallel projects")]
        [CommandOption("-p|--parallel_projects")]
        [DefaultValue(16)]
        public int MaxParallelProjects { get; init; }

        [Description("Max parallel files")]
        [CommandOption("-l|--parallel_files")]
        [DefaultValue(64)]
        public int MaxParallelFiles { get; init; }

        [Description("Raw directory mode (paths are not disguise project directories)")]
        [CommandOption("-r|--raw")]
        [DefaultValue(false)]
        public bool IsRawDirectoryMode { get; init; }

        public override ValidationResult Validate()
        {
            return ProjectDirectoryPaths.Length > 0
                ? ValidationResult.Success() 
                : ValidationResult.Error("Must specify at least one disguise project path");
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var ffOptions = new FFOptions
        {
            BinaryFolder = settings.FfmpegBinary
        };

        var records = new ConcurrentBag<AuditRecord>();

        await Parallel.ForEachAsync(settings.ProjectDirectoryPaths, new ParallelOptions { MaxDegreeOfParallelism = settings.MaxParallelProjects },
            async (rawPath, ct) =>
            {
                if (!Directory.Exists(rawPath))
                {
                    AnsiConsole.MarkupLine($"[red]Couldn't find directory at path '{rawPath}'[/]");
                    return;
                }

                string targetPath;
                if (settings.IsRawDirectoryMode)
                {
                    targetPath = rawPath;
                }
                else
                {
                    targetPath = Path.Combine(rawPath, "objects");
                    if (!Directory.Exists(targetPath))
                    {
                        AnsiConsole.MarkupLine(
                            $"[red]Couldn't find objects directory in project '{Path.GetFileName(rawPath)}'. If this is not a disguise project directory, please use raw mode with -r option.[/]");
                        return;
                    }
                }

                await Parallel.ForEachAsync(Directory.EnumerateFiles(targetPath, "*.*", SearchOption.AllDirectories),
                    new ParallelOptions { MaxDegreeOfParallelism = settings.MaxParallelFiles, CancellationToken = ct },
                    async (file, ctInner) =>
                    {
                        var extension = Path.GetExtension(file).TrimStart('.');

                        if ((settings.IncludedFileExtensions.Length > 0 &&
                             !settings.IncludedFileExtensions.Contains(extension)) ||
                            settings.ExcludedFileExtensions.Contains(extension))
                            return;

                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);

                        if (settings.SearchStrings.Length > 0 &&
                            !settings.SearchStrings.Any(s => fileNameWithoutExtension.Contains(s)))
                            return;

                        try
                        {
                            var info = new FileInfo(file);

                            var relativePath = Path.GetRelativePath(targetPath, file);

                            var entry = new AuditRecord
                            {
                                ProjectPath = rawPath,
                                FileName = relativePath[..^Path.GetExtension(relativePath).Length],
                                Extension = extension,
                                CreationTime = info.CreationTime,
                                LastWriteTime = info.LastWriteTime,
                                SizeInMB = (info.Length / 1024d) / 1024d
                            };

                            if (settings.IsRequestMediaInfo)
                            {
                                IMediaAnalysis? mediaInfo;

                                try
                                {
                                    mediaInfo = await FFProbe.AnalyseAsync(file, ffOptions, ctInner).ConfigureAwait(false);
                                }
                                catch (Exception e)
                                {
                                    mediaInfo = null;
                                }

                                if (mediaInfo is not null)
                                {
                                    entry = entry with
                                    {
                                        Width = mediaInfo.PrimaryVideoStream?.Width,
                                        Height = mediaInfo.PrimaryVideoStream?.Height,
                                        CodecName = mediaInfo.PrimaryVideoStream?.CodecName,
                                        Duration = mediaInfo.Duration > TimeSpan.Zero ? mediaInfo.Duration : null,
                                        FrameRate = mediaInfo.Duration > TimeSpan.Zero
                                            ? mediaInfo.PrimaryVideoStream?.FrameRate
                                            : null,
                                    };
                                }
                            }

                            records.Add(entry);
                        }
                        catch (Exception e)
                        {
                            AnsiConsole.MarkupLine($"[red]Failed to open info on file '{file}', skipping...[/]");
                        }
                    });
            });

        var outputFileName = $"{settings.OutputFileName}_{DateTime.Now:yyyy-dd-MTHH-mm-ss}.csv";

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        };

        await using (var writer = new StreamWriter(outputFileName))
        await using (var csv = new CsvWriter(writer, config))
        {
            csv.WriteHeader<AuditRecord>();
            await csv.NextRecordAsync();
            await csv.WriteRecordsAsync(records);
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
        public double SizeInMB { get; init; }

        [Name("Width")]
        public int? Width { get; init; }

        [Name("Height")]
        public int? Height { get; init; }

        [Name("Codec Name")]
        public string? CodecName { get; init; }

        [Name("Duration")]
        public TimeSpan? Duration { get; init; }

        [Name("Framerate")]
        [Format("F2")]
        public double? FrameRate { get; init; }
    }
}