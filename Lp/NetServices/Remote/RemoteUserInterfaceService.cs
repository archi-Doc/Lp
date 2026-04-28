// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
using System.Drawing;
using Lp.NetServices;

namespace Lp.Services;

/// <summary>
/// This is a wrapper class that sends local console calls to a remote endpoint.
/// </summary>
internal class RemoteUserInterfaceService : IUserInterfaceService
{
    public string Prefix { get; set; } = "Remote >> ";

    private readonly IRemoteUserInterfaceReceiver receiver;
    private readonly ConsoleUserInterfaceService console;

    public RemoteUserInterfaceService(IRemoteUserInterfaceReceiver receiver, ConsoleUserInterfaceService console)
    {
        this.receiver = receiver;
        this.console = console;
    }

    public bool EnableColor { get; set; } = true;

    public void Write(string? message = null, ConsoleColor color = (ConsoleColor)(-1))
    {
        this.receiver.Write(this.Prefix + message, color);
    }

    public void WriteLine(LogLevel logLevel, string? message)
    {
        this.receiver.WriteLine(logLevel, message);

        /*var r = StringHelper.AppendPrefix(this.Prefix, message);
        this.console.WriteLine(logLevel, r.Rent.AsSpan(0, r.Length));
        if (r.Rent.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Rent);
        }*/
    }

    public void WriteLine(string? message = default, ConsoleColor color = ConsoleHelper.DefaultColor)
    {
        this.receiver.WriteLine(message, color);

        /*var r = StringHelper.AppendPrefix(this.Prefix, message);
        this.console.WriteLine(r.Rent.AsSpan(0, r.Length), color);
        if (r.Rent.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Rent);
        }*/
    }

    public void WriteLine(ReadOnlySpan<char> message, ConsoleColor color = ConsoleHelper.DefaultColor)
    {
        this.receiver.WriteLine(message.ToString(), color);

        /*var r = StringHelper.AppendPrefix(this.Prefix, message);
        this.console.WriteLine(r.Rent.AsSpan(0, r.Length), color);
        if (r.Rent.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Rent);
        }*/
    }

    public void EnqueueLine(string? message = null)
    {
    }

    public Task<InputResult> ReadLine(CancellationToken cancellationToken)
        => this.receiver.ReadLine(cancellationToken);

    public ConsoleKeyInfo ReadKey(bool intercept) => default;

    public bool KeyAvailable => false;

    /*public async Task Notify(ILogger? logger, LogLevel logLevel, string message)
    {
    }*/

    public async Task<InputResult> ReadLine(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
    {
        var result = await this.receiver.ReadLine(cancelOnEscape, description, cancellationToken);
        if (result.IsSuccess)
        {
            return new(result.Value ?? string.Empty);
        }
        else
        {
            // TransmissionContext.Current.ServerConnection.Close();
            return new(InputResultKind.Terminated);
        }
    }

    public Task<InputResult> ReadPassword(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
        => this.receiver.ReadPassword(cancelOnEscape, description, cancellationToken);

    public Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
        => this.receiver.ReadYesNo(cancelOnEscape, description, cancellationToken);
}
