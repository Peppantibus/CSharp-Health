using CSharpHealth.Cli.Commands;

Environment.ExitCode = ScanCommand.Run(args, Console.Out, Console.Error);
