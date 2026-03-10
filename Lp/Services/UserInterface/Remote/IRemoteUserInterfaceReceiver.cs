// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.NetServices;

[NetService]
public interface IRemoteUserInterfaceReceiver : INetService
{
    Task Write(string? message, ConsoleColor color);

    Task WriteLine(string? message, ConsoleColor color);

    Task WriteLineDefault(string? message);

    Task WriteLineWarning(string? message);

    Task WriteLineError(string? message);

    Task<InputResult> ReadLine();

    Task<InputResult> ReadLine(bool cancelOnEscape, string? description);

    Task<InputResult> ReadPassword(bool cancelOnEscape, string? description);

    Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description);

    string Prefix { get; set; }
}
