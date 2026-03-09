// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.NetServices;

[NetService]
public interface IRemoteUserInterfaceReceiver : INetService
{
    Task<NetResult> Write(string? message, ConsoleColor color);

    Task<NetResult> WriteLine(string? message, ConsoleColor color);

    Task<NetResult> WriteLineDefault(string? message);

    Task<NetResult> WriteLineWarning(string? message);

    Task<NetResult> WriteLineError(string? message);

    Task<InputResult> ReadLine();

    Task<InputResult> ReadLine(bool cancelOnEscape, string? description);

    Task<InputResult> ReadPassword(bool cancelOnEscape, string? description);

    Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description);
}
