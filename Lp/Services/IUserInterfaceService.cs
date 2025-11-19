// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

public abstract class IUserInterfaceService : IConsoleService
{
    public enum Mode
    {
        View,
        Console,
        Input,
    }

    public abstract void Write(string? message = null);

    public abstract void WriteLine(string? message = null);

    public abstract void EnqueueInput(string? message = null);

    public abstract Task<InputResult> ReadLine(string? prompt = null, CancellationToken cancellationToken = default(CancellationToken));

    public abstract ConsoleKeyInfo ReadKey(bool intercept);

    public abstract bool KeyAvailable { get; }

    public abstract Task<bool?> RequestYesOrNo(string? description);

    public abstract Task<string?> RequestString(bool enterToExit, string? description);

    public abstract Task<string?> RequestPassword(string? description);

    public abstract Task Notify(LogLevel level, string message);

    public Mode ChangeMode(Mode nextMode)
    {
        return (Mode)Interlocked.Exchange(ref this.currentMode, (int)nextMode);
    }

    public Mode CurrentMode => (Mode)this.currentMode;

    public bool IsViewMode => this.currentMode == (int)Mode.View;

    public bool IsConsoleMode => this.currentMode == (int)Mode.Console;

    public bool IsInputMode => this.currentMode == (int)Mode.Input;

    private int currentMode;
}
