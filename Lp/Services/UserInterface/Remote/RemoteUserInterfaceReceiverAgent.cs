// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Services;

namespace Lp.NetServices;

[NetObject]
public class RemoteUserInterfaceReceiverAgent : IRemoteUserInterfaceReceiver
{
    private readonly ConsoleUserInterfaceService consoleUserInterfaceService;

    public string Prefix { get; set; } = "[Remote] ";

    public RemoteUserInterfaceReceiverAgent(ConsoleUserInterfaceService consoleUserInterfaceService)
    {
        this.consoleUserInterfaceService = consoleUserInterfaceService;
    }

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadLine()
        => this.consoleUserInterfaceService.ReadLine(default);

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadLine(bool cancelOnEscape, string? description)
        => this.consoleUserInterfaceService.ReadLine(cancelOnEscape, this.Prefix + description);

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadPassword(bool cancelOnEscape, string? description)
        => this.consoleUserInterfaceService.ReadPassword(cancelOnEscape, this.Prefix + description);

    Task<InputResultKind> IRemoteUserInterfaceReceiver.ReadYesNo(bool cancelOnEscape, string? description)
        => this.consoleUserInterfaceService.ReadYesNo(cancelOnEscape, this.Prefix + description);

    Task IRemoteUserInterfaceReceiver.Write(ReadOnlySpan<char> message, ConsoleColor color)
    {
        this.consoleUserInterfaceService.Write(string.Concat(this.Prefix, message), color);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(ReadOnlySpan<char> message, ConsoleColor color)
    {
        this.consoleUserInterfaceService.WriteLine(string.Concat(this.Prefix, message), color);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLineDefault(ReadOnlySpan<char> message)
    {
        this.consoleUserInterfaceService.WriteLineDefault(string.Concat(this.Prefix, message));
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLineError(ReadOnlySpan<char> message)
    {
        this.consoleUserInterfaceService.WriteLineError(string.Concat(this.Prefix, message));
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLineWarning(ReadOnlySpan<char> message)
    {
        this.consoleUserInterfaceService.WriteLineWarning(string.Concat(this.Prefix, message));
        return Task.CompletedTask;
    }
}
