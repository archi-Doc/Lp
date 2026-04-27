// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Data;
using SimplePrompt;

namespace Lp.Services;

public class ConsoleUserInterfaceService : IUserInterfaceService
{
    public const string YesOrNoSuffix = "[Y/n] ";

    private readonly SimpleConsole simpleConsole;

    public ReadLineOptions PasswordOptions { get; } = new()
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

    public void Write(ReadOnlySpan<char> message = default, ConsoleColor color = ConsoleHelper.DefaultColor)
        => this.simpleConsole.Write(message, color);

    public void WriteLine(ReadOnlySpan<char> message = default, ConsoleColor color = ConsoleHelper.DefaultColor)
        => this.simpleConsole.WriteLine(message, color);

    public void Write(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor)
        => this.simpleConsole.Write(message, color);

    public void WriteLine(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor)
        => this.simpleConsole.WriteLine(message, color);

#pragma warning disable SA1118 // Parameter should not span multiple lines
    public void WriteLine(LogLevel logLevel, string? message)
    => this.WriteLine(
        message,
        logLevel switch
        {
            LogLevel.Debug => this.lpSettings.Color.Information,
            LogLevel.Information => this.lpSettings.Color.Information,
            LogLevel.Warning => this.lpSettings.Color.Warning,
            LogLevel.Error => this.lpSettings.Color.Error,
            LogLevel.Fatal => this.lpSettings.Color.Fatal,
            _ => this.lpSettings.Color.Information,
        });

    public void WriteLine(LogLevel logLevel, ReadOnlySpan<char> message)
    => this.WriteLine(
        message,
        logLevel switch
        {
            LogLevel.Debug => this.lpSettings.Color.Information,
            LogLevel.Information => this.lpSettings.Color.Information,
            LogLevel.Warning => this.lpSettings.Color.Warning,
            LogLevel.Error => this.lpSettings.Color.Error,
            LogLevel.Fatal => this.lpSettings.Color.Fatal,
            _ => this.lpSettings.Color.Information,
        });
#pragma warning restore SA1118 // Parameter should not span multiple lines

    public void EnqueueLine(string? message = null)
        => this.simpleConsole.EnqueueInput(message);

    public Task<InputResult> ReadLine(CancellationToken cancellationToken)
        => this.simpleConsole.ReadLine(default, cancellationToken);

    public ConsoleKeyInfo ReadKey(bool intercept)
        => ((IConsoleService)this.simpleConsole).ReadKey(intercept);

    public bool KeyAvailable
        => ((IConsoleService)this.simpleConsole).KeyAvailable;

    /*public async Task Notify(ILogger? logger, LogLevel logLevel, string message)
    {
        var logWriter = logger?.GetWriter(logLevel);
        if (logWriter is not null)
        {
            // if (logWriter.OutputType != typeof(EmptyLogger))
            // {
            //    logWriter.Log(message);
            //    return;
            // }
        }

        this.simpleConsole.WriteLine(message);
    }*/

    public Task<InputResult> ReadPassword(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        var options = this.PasswordOptions with
        {
            CancelOnEscape = cancelOnEscape,
            Prompt = description ?? string.Empty,
        };

        return this.simpleConsole.ReadLine(options, cancellationToken);
    }

    public Task<InputResult> ReadLine(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        var options = new ReadLineOptions
        {
            CancelOnEscape = cancelOnEscape,
            MultilineDelimiter = default,
            Prompt = description ?? string.Empty,
        };

        return this.simpleConsole.ReadLine(options, cancellationToken);
    }

    public async Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        var options = ReadLineOptions.YesNo with
        {
            CancelOnEscape = cancelOnEscape,
            Prompt = description == null ? YesOrNoSuffix : $"{description} {YesOrNoSuffix}",
        };

        while (true)
        {
            var result = await this.simpleConsole.ReadLine(options, cancellationToken).ConfigureAwait(false);
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
