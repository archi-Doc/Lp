// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using Lp.Services;

namespace Lp.NetServices;

[NetObject]
public class RemoteUserInterfaceReceiverAgent : IRemoteUserInterfaceReceiver
{
    private readonly ExecutionStack executionStack;
    private readonly ConsoleUserInterfaceService consoleUserInterfaceService;

    public string Prefix { get; set; } = "[Remote] ";

    public RemoteUserInterfaceReceiverAgent(ExecutionStack executionStack, ConsoleUserInterfaceService consoleUserInterfaceService)
    {
        this.executionStack = executionStack;
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
        var r = StringHelper.AppendPrefix(this.Prefix, message);
        this.consoleUserInterfaceService.WriteLine(r.Rent.AsSpan(0, r.Length), color);
        if (r.Rent.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Rent);
        }

        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(LogLevel logLevel, string? message)
    {
        var r = StringHelper.AppendPrefix(this.Prefix, message);
        this.consoleUserInterfaceService.WriteLine(logLevel, r.Rent.AsSpan(0, r.Length));
        if (r.Rent.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Rent);
        }

        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.ReturnInputControl(long id, CancellationToken cancellationToken)
    {
        this.executionStack.TrySetCompleted(id);
        return Task.CompletedTask;
    }
}
