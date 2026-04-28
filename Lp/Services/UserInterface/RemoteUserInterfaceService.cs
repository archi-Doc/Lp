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

    public void Write(string? message = null, ConsoleColor color = (ConsoleColor)(-1))
    {
        this.receiver.Write(message, color);
    }

    public void WriteLine(LogLevel logLevel, string? message)
    {
        this.receiver.WriteLine(logLevel, message);
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

    public ConsoleKeyInfo ReadKey(bool intercept) => default;

    public bool KeyAvailable => false;

    public Task<InputResult> ReadLine(CancellationToken cancellationToken)
        => this.receiver.ReadLine(cancellationToken);

    public async Task<InputResult> ReadLine(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
    {
        var result = await this.receiver.ReadLine(cancelOnEscape, description, cancellationToken).ConfigureAwait(false);
        if (result.IsSuccess)
        {
            return new(result.Value ?? string.Empty);
        }
        else
        {
            // await this.receiver.ReturnInputControl(this.receiver.Id).ConfigureAwait(false);//
            // TransmissionContext.Current.ServerConnection.Close();
            return new(InputResultKind.Terminated);
        }
    }

    public Task<InputResult> ReadPassword(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
        => this.receiver.ReadPassword(cancelOnEscape, description, cancellationToken);

    public Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description, CancellationToken cancellationToken = default)
        => this.receiver.ReadYesNo(cancelOnEscape, description, cancellationToken);
}
