using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

namespace Pixsper.DisguiseTool.Commands;

[Description("Creates a CSV file with information on the contents of a disguise projects object directory")]
public class AuditProject : Command<AuditProject.Settings>
{
    public sealed class Settings : CommandSettings
    {

    }

    public override int Execute([NotNull]CommandContext context, [NotNull]Settings settings)
    {
        return 0;
    }
}