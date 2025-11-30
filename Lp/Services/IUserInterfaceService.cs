// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

public abstract class IUserInterfaceService : IConsoleService
{
    public abstract void Write(string? message = null);

    public abstract void WriteLine(string? message = null);

    public abstract void EnqueueInput(string? message = null);

    public abstract Task<InputResult> ReadLine(CancellationToken cancellationToken = default(CancellationToken));

    public abstract ConsoleKeyInfo ReadKey(bool intercept);

    public abstract bool KeyAvailable { get; }

    public abstract Task<bool?> RequestYesOrNo(string? description);

    public abstract Task<string?> RequestString(bool cancelOnEscape, string? description);

    public abstract Task<string?> RequestPassword(string? description);

    public abstract Task Notify(LogLevel level, string message);
}
