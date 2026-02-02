// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimplePrompt;

namespace Lp.Services;

internal class ConsoleUserInterfaceService : IUserInterfaceService
{
    private const string YesOrNoSuffix = "[Y/n] ";

    private readonly SimpleConsole simpleConsole;

    private readonly ReadLineOptions passwordOptions = new()
    {
        AllowEmptyLineInput = true,
        CancelOnEscape = false,
        MaxInputLength = 100,
        MaskingCharacter = '*',
        MultilineDelimiter = default,
    };

    public ConsoleUserInterfaceService(SimpleConsole simpleConsole)
    {
        this.simpleConsole = simpleConsole;
    }

    public override void Write(string? message = null)
        => this.simpleConsole.Write(message);

    public override void WriteLine(string? message = null)
        => this.simpleConsole.WriteLine(message);

    public override void EnqueueLine(string? message = null)
        => this.simpleConsole.EnqueueInput(message);

    public override Task<InputResult> ReadLine(CancellationToken cancellationToken)
        => this.simpleConsole.ReadLine(default, cancellationToken);

    public override ConsoleKeyInfo ReadKey(bool intercept)
        => ((IConsoleService)this.simpleConsole).ReadKey(intercept);

    public override bool KeyAvailable
        => ((IConsoleService)this.simpleConsole).KeyAvailable;

    public override async Task Notify(LogLevel level, string message)
        => this.simpleConsole.WriteLine(message); // this.logger.TryGet(level)?.Log(message);

    public override Task<InputResult> ReadPassword(bool cancelOnEscape, string? description)
    {
        var options = this.passwordOptions with
        {
            CancelOnEscape = cancelOnEscape,
            Prompt = description ?? string.Empty,
        };

        return this.simpleConsole.ReadLine(options);
    }

    public override Task<InputResult> ReadLine(bool cancelOnEscape, string? description)
    {
        var options = new ReadLineOptions
        {
            CancelOnEscape = cancelOnEscape,
            MultilineDelimiter = default,
            Prompt = description ?? string.Empty,
        };

        return this.simpleConsole.ReadLine(options);
    }

    public override async Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description)
    {
        var options = ReadLineOptions.YesNo with
        {
            CancelOnEscape = cancelOnEscape,
            Prompt = description == null ? YesOrNoSuffix : $"{description} {YesOrNoSuffix}",
        };

        while (true)
        {
            var result = await this.simpleConsole.ReadLine(options).ConfigureAwait(false);
            if (result.Kind == InputResultKind.Terminated ||
                result.Kind == InputResultKind.Canceled)
            {// Ctrl+C
                // this.WriteLine();
                return result.Kind; // throw new PanicException();
            }

            var text = result.Text.Trim().ToLowerInvariant();
            if (text == "y" || text == "yes")
            {
                return InputResultKind.Success;
            }
            else if (text == "n" || text == "no")
            {
                return InputResultKind.No;
            }
        }
    }
}
