using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Pixsper.DisguiseTool.Commands;

[Description("Converts a disguise cue table file to an LX cue list for import into a lighting console")]
public class CreateCueList : Command<CreateCueList.Settings>
{
    public enum LxCueListFormat
    {
        EosCsv
    }

    public enum CueSelectMode
    {
        All,
        DmxFormatOnly,
        StandardFormatOnly,
    }

    public sealed class Settings : CommandSettings
    {
        [Description("Disguise cue table file path")]
        [CommandArgument(0, "<path>")]
        public string CueTablePath { get; init; } = string.Empty;

        [Description("Cue list format")]
        [CommandOption("-f|--format")]
        [DefaultValue(LxCueListFormat.EosCsv)]
        public LxCueListFormat Format { get; init; }

        [Description("Cue select mode")]
        [CommandOption("-s|--select")]
        [DefaultValue(CueSelectMode.All)]
        public CueSelectMode SelectMode { get; init; }

        public override ValidationResult Validate()
        {
            return string.IsNullOrWhiteSpace(CueTablePath) || CueTablePath.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0
                ? ValidationResult.Error("Disguise cue table path invalid")
                : ValidationResult.Success();
        }
    }

    private static readonly Regex CueExpression = new("^CUE ([0-9.]+)$", RegexOptions.Compiled);

    public override int Execute([NotNull]CommandContext context, [NotNull]Settings settings)
    {
        var cueTablePath = Path.GetFullPath(settings.CueTablePath);
        var table = DisguiseCueTable.Read(cueTablePath);

        table.Write(Path.GetFileNameWithoutExtension(cueTablePath) + "_copy.txt");

        return 0;
    }
}