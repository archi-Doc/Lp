// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Drawing;
using Lp.Data;
using Lp.NetServices.Remote;

namespace Lp.NetServices;

[NetObject]
public class RemoteUserInterfaceReceiverAgent : IRemoteUserInterfaceReceiver
{
    private const int WriterCapacity = 16;
    // public const string GroupName = "RemoteUi";

    // private readonly ExecutionRoot executionRoot;
    private readonly LpSettings lpSettings;
    private readonly ExecutionStack executionStack;
    private readonly OrderedLineWriter writer;

    public IUserInterfaceService UserInterfaceService { get; set; }

    public string OutputPrefix { get; set; } = "[Remote] ";

    public string InputPrefix { get; set; } = "Remote >> ";

    public CancellationToken CancellationToken { get; set; }

    public int Id { get; set; }

    public RemoteUserInterfaceReceiverAgent(ExecutionRoot executionRoot, ExecutionStack executionStack, IUserInterfaceService userInterfaceService, LpSettings lpSettings)
    {
        // this.executionRoot = executionRoot;
        this.executionStack = executionStack;
        // this.executionGroup = this.executionRoot.GetOrAddGroup(false, GroupName);
        this.UserInterfaceService = userInterfaceService;
        this.lpSettings = lpSettings;

        this.writer = new(WriterCapacity, message =>
        {
            var r = StringHelper.AppendPrefix(this.OutputPrefix, message);
            this.UserInterfaceService.WriteLine(r.Rent.AsSpan(0, r.Length));
            if (r.Rent.Length > 0)
            {
                ArrayPool<char>.Shared.Return(r.Rent);
            }
        });
    }

    async Task<NetResultAndValue<string>> IRemoteUserInterfaceReceiver.ReadLine(CancellationToken cancellationToken)
    {
        var core = this.executionStack.Find(this.Id);
        if (core is not null)
        {
            cancellationToken = core.CancellationToken;
        }

        var result = await this.UserInterfaceService.ReadLine(cancellationToken);
        return new(result.Text);
    }

    async Task<NetResultAndValue<string>> IRemoteUserInterfaceReceiver.ReadLine(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        var core = this.executionStack.Find(this.Id);
        if (core is not null)
        {
            cancellationToken = core.CancellationToken;
        }

        var result = await this.UserInterfaceService.ReadLine(cancelOnEscape, this.InputPrefix + description, cancellationToken);
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
        var core = this.executionStack.Find(this.Id);
        if (core is not null)
        {
            cancellationToken = core.CancellationToken;
        }

        var result = await this.UserInterfaceService.ReadPassword(cancelOnEscape, this.InputPrefix + description, cancellationToken);
        return new(result.Text);
    }

    Task<InputResultKind> IRemoteUserInterfaceReceiver.ReadYesNo(bool cancelOnEscape, string? description, CancellationToken cancellationToken)
    {
        var core = this.executionStack.Find(this.Id);
        if (core is not null)
        {
            cancellationToken = core.CancellationToken;
        }

        return this.UserInterfaceService.ReadYesNo(cancelOnEscape, this.InputPrefix + description, cancellationToken);
    }

    Task IRemoteUserInterfaceReceiver.Write(string? message, ConsoleColor color)
    {
        this.UserInterfaceService.Write(this.OutputPrefix + message, color);
        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(int lineNumber, string? message, ConsoleColor color)
    {
        this.writer.Add(lineNumber, message);

        /*var r = StringHelper.AppendPrefix(this.OutputPrefix, message);
        this.UserInterfaceService.WriteLine(r.Rent.AsSpan(0, r.Length), color);
        if (r.Rent.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Rent);
        }*/

        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(int lineNumber, LogLevel logLevel, string? message)
    {
        var color = logLevel switch
        {
            LogLevel.Debug => this.lpSettings.Color.Information,
            LogLevel.Information => this.lpSettings.Color.Information,
            LogLevel.Warning => this.lpSettings.Color.Warning,
            LogLevel.Error => this.lpSettings.Color.Error,
            LogLevel.Fatal => this.lpSettings.Color.Fatal,
            _ => this.lpSettings.Color.Information,
        };

        this.writer.Add(lineNumber, message);

        /*var r = StringHelper.AppendPrefix(this.OutputPrefix, message);
        this.UserInterfaceService.WriteLine(logLevel, r.Rent.AsSpan(0, r.Length).ToString());
        if (r.Rent.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Rent);
        }*/

        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.ReturnInputControl(int id)
    {
        if (this.executionStack.Find(id) is TaskCompletionGroup group)
        {
            group.TrySetCompleted();
        }

        return Task.CompletedTask;
    }
}
