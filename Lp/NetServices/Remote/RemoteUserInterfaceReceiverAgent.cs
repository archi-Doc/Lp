// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using Lp.Services;

namespace Lp.NetServices;

[NetObject]
public class RemoteUserInterfaceReceiverAgent : IRemoteUserInterfaceReceiver
{
    private readonly ExecutionStack executionStack;
    private readonly IUserInterfaceService userInterfaceService;

    public string OutputPrefix { get; set; } = "[Remote] ";

    public string InputPrefix { get; set; } = "Remote >> ";

    public CancellationToken CancellationToken { get; set; }

    public long Id { get; set; }

    public RemoteUserInterfaceReceiverAgent(ExecutionStack executionStack, IUserInterfaceService userInterfaceService)
    {
        this.executionStack = executionStack;
        this.userInterfaceService = userInterfaceService;
    }

    async Task<NetResultAndValue<string>> IRemoteUserInterfaceReceiver.ReadLine(CancellationToken cancellationToken)
    {
        this.executionStack.TryGetCancellationToken(this.Id, out cancellationToken);
        var result = await this.userInterfaceService.ReadLine(cancellationToken);
        return new(result.Text);
    }

    async Task<NetResultAndValue<string>> IRemoteUserInterfaceReceiver.ReadLine(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        this.executionStack.TryGetCancellationToken(this.Id, out cancellationToken);
        var result = await this.userInterfaceService.ReadLine(cancelOnEscape, this.InputPrefix + description, cancellationToken);
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

    async Task<NetResultAndValue<string>> IRemoteUserInterfaceReceiver.ReadPassword(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        this.executionStack.TryGetCancellationToken(this.Id, out cancellationToken);
        var result = await this.userInterfaceService.ReadPassword(cancelOnEscape, this.InputPrefix + description, cancellationToken);
        return new(result.Text);
    }

    Task<InputResultKind> IRemoteUserInterfaceReceiver.ReadYesNo(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        return this.userInterfaceService.ReadYesNo(cancelOnEscape, this.InputPrefix + description, cancellationToken);
    }

    Task IRemoteUserInterfaceReceiver.Write(string? message, ConsoleColor color)
    {
        this.userInterfaceService.Write(this.OutputPrefix + message, color);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(string? message, ConsoleColor color)
    {
        var r = StringHelper.AppendPrefix(this.OutputPrefix, message);
        this.userInterfaceService.WriteLine(r.Rent.AsSpan(0, r.Length), color);
        if (r.Rent.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Rent);
        }

        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(LogLevel logLevel, string? message)
    {
        var r = StringHelper.AppendPrefix(this.OutputPrefix, message);
        this.userInterfaceService.WriteLine(logLevel, r.Rent.AsSpan(0, r.Length).ToString());
        if (r.Rent.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Rent);
        }

        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.ReturnInputControl(long id)
    {
        this.executionStack.TrySetCompleted(id);
        return Task.CompletedTask;
    }
}
