// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;

namespace Lp.Services;

/// <summary>
/// This is a wrapper class that sends local console calls to a remote endpoint.
/// </summary>
internal class RemoteUserInterfaceService : IUserInterfaceService
{
    private readonly IRemoteUserInterfaceReceiver receiver;

    public RemoteUserInterfaceService(IRemoteUserInterfaceReceiver receiver)
    {
        this.receiver = receiver;
    }

    public bool EnableColor { get; set; } = true;

    public void Write(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor)
        => this.receiver.Write(message, color);

    public void WriteLine(string? message = null, ConsoleColor color = ConsoleHelper.DefaultColor)
        => this.receiver.WriteLine(message, color);

    public void WriteLineDefault(string? message)
        => this.receiver.WriteLineDefault(message);

    public void WriteLineWarning(string? message)
        => this.receiver.WriteLineWarning(message);

    public void WriteLineError(string? message)
        => this.receiver.WriteLineError(message);

    public void EnqueueLine(string? message = null)
    {
    }

    public Task<InputResult> ReadLine(CancellationToken cancellationToken)
        => this.receiver.ReadLine();

    public ConsoleKeyInfo ReadKey(bool intercept) => default;

    public bool KeyAvailable => false;

    public async Task Notify(ILogger? logger, LogLevel logLevel, string message)
    {
    }

    public Task<InputResult> ReadLine(bool cancelOnEscape, string? description)
        => this.receiver.ReadLine(cancelOnEscape, description);

    public Task<InputResult> ReadPassword(bool cancelOnEscape, string? description)
        => this.receiver.ReadPassword(cancelOnEscape, description);

    public Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description)
        => this.receiver.ReadYesNo(cancelOnEscape, description);
}
