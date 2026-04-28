// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using Lp.Services;

namespace Lp.NetServices;

[NetObject]
public class RemoteUserInterfaceReceiverAgent : IRemoteUserInterfaceReceiver
{
    private readonly ExecutionStack executionStack;
    private readonly ConsoleUserInterfaceService consoleUserInterfaceService;

    public string OutputPrefix { get; set; } = "[Remote] ";

    public string InputPrefix { get; set; } = "Remote >> ";

    public CancellationToken CancellationToken { get; set; }

    public RemoteUserInterfaceReceiverAgent(ExecutionStack executionStack, ConsoleUserInterfaceService consoleUserInterfaceService)
    {
        this.executionStack = executionStack;
        this.consoleUserInterfaceService = consoleUserInterfaceService;
    }

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadLine(CancellationToken cancellationToken)
    {
        return this.consoleUserInterfaceService.ReadLine(cancellationToken);
    }

    async Task<NetResultAndValue<string>> IRemoteUserInterfaceReceiver.ReadLine(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        var result = await this.consoleUserInterfaceService.ReadLine(cancelOnEscape, this.InputPrefix + description, this.CancellationToken);
        return new(result.Text);

        /*using (var scope = this.executionStack.Push((x, signal) =>
        {
            if (signal == ExecutionSignal.Exit)
            {
                x.CancellationTokenSource.Cancel();
            }
        }))
        {
            var result = await this.consoleUserInterfaceService.ReadLine(cancelOnEscape, this.InputPrefix + description, scope.CancellationToken);
            var state = TransmissionContext.Current.ServerConnection.CurrentState;
            return new(result.Text);
        }*/
    }

    Task<InputResult> IRemoteUserInterfaceReceiver.ReadPassword(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        return this.consoleUserInterfaceService.ReadPassword(cancelOnEscape, this.InputPrefix + description, cancellationToken);
    }

    Task<InputResultKind> IRemoteUserInterfaceReceiver.ReadYesNo(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        return this.consoleUserInterfaceService.ReadYesNo(cancelOnEscape, this.InputPrefix + description, cancellationToken);
    }

    Task IRemoteUserInterfaceReceiver.Write(string? message, ConsoleColor color)
    {
        this.consoleUserInterfaceService.Write(this.OutputPrefix + message, color);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(string? message, ConsoleColor color)
    {
        var r = StringHelper.AppendPrefix(this.OutputPrefix, message);
        this.consoleUserInterfaceService.WriteLine(r.Rent.AsSpan(0, r.Length), color);
        if (r.Rent.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Rent);
        }

        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(LogLevel logLevel, string? message)
    {
        var r = StringHelper.AppendPrefix(this.OutputPrefix, message);
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
