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

    Task IRemoteUserInterfaceReceiver.Write(string? message, ConsoleColor color)
    {
        this.consoleUserInterfaceService.Write(this.Prefix + message, color);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(string? message, ConsoleColor color)
    {
        this.consoleUserInterfaceService.WriteLine(this.Prefix + message, color);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLineDefault(string? message)
    {
        this.consoleUserInterfaceService.WriteLineDefault(this.Prefix + message);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLineError(string? message)
    {
        this.consoleUserInterfaceService.WriteLineError(this.Prefix + message);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLineWarning(string? message)
    {
        this.consoleUserInterfaceService.WriteLineWarning(this.Prefix + message);
        return Task.CompletedTask;
    }
}
