// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Data;
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

    private LpSettings lpSettings;

    public ConsoleUserInterfaceService(SimpleConsole simpleConsole)
    {
        this.simpleConsole = simpleConsole;
        this.lpSettings = new();
    }

    internal void Load(CrystalControl crystalControl)
    {
        this.lpSettings = crystalControl.GetCrystal<LpSettings>().Data;
    }

    public bool EnableColor { get; set; } = true;

    public void Write(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor)
        => this.simpleConsole.Write(message, color);

    public void WriteLine(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor)
        => this.simpleConsole.WriteLine(message, color);

    public void WriteLineDefault(string? message)
        => this.WriteLine(message, this.lpSettings.Color.Default);

    public void WriteLineWarning(string? message)
        => this.WriteLine(message, this.lpSettings.Color.Warning);

    public void WriteLineError(string? message)
        => this.WriteLine(message, this.lpSettings.Color.Error);

    public void EnqueueLine(string? message = null)
        => this.simpleConsole.EnqueueInput(message);

    public Task<InputResult> ReadLine(CancellationToken cancellationToken)
        => this.simpleConsole.ReadLine(default, cancellationToken);

    public ConsoleKeyInfo ReadKey(bool intercept)
        => ((IConsoleService)this.simpleConsole).ReadKey(intercept);

    public bool KeyAvailable
        => ((IConsoleService)this.simpleConsole).KeyAvailable;

    public async Task Notify(ILogger? logger, LogLevel logLevel, string message)
    {
        var logWriter = logger?.TryGet(logLevel);
        if (logWriter is not null)
        {
            if (logWriter.OutputType != typeof(EmptyLogger))
            {
                logWriter.Log(message);
                return;
            }
        }

        this.simpleConsole.WriteLine(message);
    }

    public Task<InputResult> ReadPassword(bool cancelOnEscape, string? description)
    {
        var options = this.passwordOptions with
        {
            CancelOnEscape = cancelOnEscape,
            Prompt = description ?? string.Empty,
        };

        return this.simpleConsole.ReadLine(options);
    }

    public Task<InputResult> ReadLine(bool cancelOnEscape, string? description)
    {
        var options = new ReadLineOptions
        {
            CancelOnEscape = cancelOnEscape,
            MultilineDelimiter = default,
            Prompt = description ?? string.Empty,
        };

        return this.simpleConsole.ReadLine(options);
    }

    public async Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description)
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
