// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Drawing;
using Lp.Services;
using SimplePrompt;

namespace Lp.NetServices;

[NetObject]
public class RemoteUserInterfaceReceiverAgent : IRemoteUserInterfaceReceiver
{
    private readonly ConsoleUserInterfaceService consoleUserInterfaceService;
    private readonly SimpleConsole simpleConsole;
    private readonly string prefix;

    public RemoteUserInterfaceReceiverAgent(ConsoleUserInterfaceService consoleUserInterfaceService, SimpleConsole simpleConsole)
    {
        this.consoleUserInterfaceService = consoleUserInterfaceService;
        this.simpleConsole = simpleConsole;
        this.prefix = "[Remote] ";
    }

    // (Remote) Enter password: 
    Task<InputResult> IRemoteUserInterfaceReceiver.ReadLine()
        => this.consoleUserInterfaceService.ReadLine(default);

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadLine(bool cancelOnEscape, string? description)
        => this.consoleUserInterfaceService.ReadLine(cancelOnEscape, this.prefix + description);

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadPassword(bool cancelOnEscape, string? description)
        => this.consoleUserInterfaceService.ReadPassword(cancelOnEscape, this.prefix + description);

    Task<InputResultKind> IRemoteUserInterfaceReceiver.ReadYesNo(bool cancelOnEscape, string? description)
        => this.consoleUserInterfaceService.ReadYesNo(cancelOnEscape, this.prefix + description);

    Task IRemoteUserInterfaceReceiver.Write(string? message, ConsoleColor color)
    {
        this.consoleUserInterfaceService.Write(this.prefix + message, color);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(string? message, ConsoleColor color)
    {
        this.consoleUserInterfaceService.WriteLine(this.prefix + message, color);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLineDefault(string? message)
    {
        this.consoleUserInterfaceService.WriteLineDefault(this.prefix + message);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLineError(string? message)
    {
        this.consoleUserInterfaceService.WriteLineError(this.prefix + message);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLineWarning(string? message)
    {
        this.consoleUserInterfaceService.WriteLineWarning(this.prefix + message);
        return Task.CompletedTask;
    }
}
