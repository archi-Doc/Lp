// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Unit;
using Lp;

namespace xUnitTest;

/// <summary>
/// An <see cref="IUserInterfaceService"/> that answers password prompts from a script.<br/>
/// A <see langword="null"/> entry, or the end of the script, is answered as <see cref="InputResultKind.Terminated"/>.
/// </summary>
internal sealed class ScriptedUserInterfaceService : IUserInterfaceService
{
    private readonly Queue<string?> passwords;

    public ScriptedUserInterfaceService(params string?[] passwords)
    {
        this.passwords = new(passwords);
    }

    public int PasswordRequests { get; private set; }

    public List<string> Lines { get; } = new();

    public bool EnableColor { get; set; }

    public bool KeyAvailable => false;

    public Task<InputResult> ReadPassword(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
    {
        this.PasswordRequests++;
        return Task.FromResult(this.passwords.TryDequeue(out var password) && password is not null ?
            new InputResult(password) :
            new InputResult(InputResultKind.Terminated));
    }

    public Task<InputResult> ReadLine(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
        => Task.FromResult(new InputResult(InputResultKind.Terminated));

    public Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
        => Task.FromResult(InputResultKind.Terminated);

    public Task<InputResult> ReadLineAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(new InputResult(InputResultKind.Terminated));

    public ConsoleKeyInfo ReadKey(bool intercept) => default;

    public void EnqueueLine(string? message = null)
    {
    }

    public void WriteLine(LogLevel logLevel, string? message)
        => this.Lines.Add(message ?? string.Empty);

    public void Write(string? message = null, ConsoleColor color = ConsoleColor.Gray)
    {
    }

    public void Write(ReadOnlySpan<char> message, ConsoleColor color = ConsoleColor.Gray)
    {
    }

    public void WriteLine(string? message = null, ConsoleColor color = ConsoleColor.Gray)
        => this.Lines.Add(message ?? string.Empty);

    public void WriteLine(ReadOnlySpan<char> message, ConsoleColor color = ConsoleColor.Gray)
        => this.Lines.Add(message.ToString());
}
