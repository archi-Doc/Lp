// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

public abstract class IUserInterfaceService : IConsoleService
{
    public abstract void Write(string? message = null);

    public abstract void WriteLine(string? message = null);

    public abstract void EnqueueLine(string? message = null);

    public abstract Task<InputResult> ReadLine(CancellationToken cancellationToken = default(CancellationToken));

    public abstract Task<InputResult> ReadLine(bool cancelOnEscape, string? description);

    public abstract ConsoleKeyInfo ReadKey(bool intercept);

    public abstract bool KeyAvailable { get; }

    public abstract Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description);

    public abstract Task<InputResult> ReadPassword(bool cancelOnEscape, string? description);

    public abstract Task Notify(LogLevel level, string message);
}
