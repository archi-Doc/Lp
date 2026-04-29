// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.NetServices;

[NetService]
public interface IRemoteUserInterfaceReceiver : INetService
{
    Task Write(string? message, ConsoleColor color);

    Task WriteLine(string? message, ConsoleColor color);

    Task WriteLine(LogLevel logLevel, string? message);

    Task<NetResultAndValue<string>> ReadLine(CancellationToken cancellationToken);

    Task<NetResultAndValue<string>> ReadLine(bool cancelOnEscape, string? description, CancellationToken cancellationToken);

    Task<NetResultAndValue<string>> ReadPassword(bool cancelOnEscape, string? description, CancellationToken cancellationToken);

    Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description, CancellationToken cancellationToken);

    Task ReturnInputControl(long id);

    string OutputPrefix { get; set; }

    string InputPrefix { get; set; }

    CancellationToken CancellationToken { get; set; }

    long Id { get; set; }
}
