using Pixsper.DisguiseTool.Commands;
using Spectre.Console.Cli;

var app = new CommandApp();
app.Configure(config =>
{
    config.AddCommand<AuditProject>("audit");
    //config.AddCommand<CreateCueList>("cuelist");
});
return app.Run(args);