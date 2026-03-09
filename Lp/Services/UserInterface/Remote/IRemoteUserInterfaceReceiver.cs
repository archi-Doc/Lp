// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.NetServices;

[NetService]
public interface IRemoteUserInterfaceReceiver : INetService
{
    Task Write(ReadOnlySpan<char> message, ConsoleColor color);

    Task WriteLine(ReadOnlySpan<char> message, ConsoleColor color);

    Task WriteLineDefault(ReadOnlySpan<char> message);

    Task WriteLineWarning(ReadOnlySpan<char> message);

    Task WriteLineError(ReadOnlySpan<char> message);

    Task<InputResult> ReadLine();

    Task<InputResult> ReadLine(bool cancelOnEscape, string? description);

    Task<InputResult> ReadPassword(bool cancelOnEscape, string? description);

    Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description);
}
