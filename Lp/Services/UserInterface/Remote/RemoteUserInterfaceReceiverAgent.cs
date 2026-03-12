// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;
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
        var r = this.AppendPrefix(message);
        this.consoleUserInterfaceService.WriteLine(r.Buffer.AsSpan(0, r.Length), color);
        if (r.Buffer.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Buffer);
        }

        return Task.CompletedTask;
    }

    Task IRemoteUserInterfaceReceiver.WriteLine(LogLevel logLevel, string? message)
    {
        var r = this.AppendPrefix(message);
        this.consoleUserInterfaceService.WriteLine(logLevel, r.Buffer.AsSpan(0, r.Length));
        if (r.Buffer.Length > 0)
        {
            ArrayPool<char>.Shared.Return(r.Buffer);
        }

        return Task.CompletedTask;
    }

    private (char[] Buffer, int Length) AppendPrefix(string? message)
    {
        if (message is null)
        {
            return ([], 0);
        }

        var prefix = this.Prefix;
        var source = message.AsSpan();
        var prefixCount = 1 + source.Count('\n');
        var maxLength = message.Length + (prefix.Length * prefixCount);
        var rent = ArrayPool<char>.Shared.Rent(maxLength);
        var destination = rent.AsSpan();

        prefix.CopyTo(destination);
        destination = destination.Slice(prefix.Length);
        foreach (var x in source)
        {
            if (x == '\r')
            {
            }
            else
            {
                destination[0] = x;
                destination = destination.Slice(1);
                if (x == '\n')
                {
                    prefix.CopyTo(destination);
                    destination = destination.Slice(prefix.Length);
                }
            }
        }

        return (rent, rent.Length - destination.Length);
    }
}
