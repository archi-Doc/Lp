// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.NetServices;

namespace Lp.Services;

internal class VirtualUserInterfaceService : IUserInterfaceService
{
    public enum State
    {
        NotInitialized,
        Local,
        Remote,
    }

    #region FieldAndProperty

    public State CurrentState { get; private set; }

    public ServerConnection? Connection { get; private set; }

    public IRemoteUserInterfaceReceiver? receiver { get; private set; }

    public bool IsInitialized => this.CurrentState != State.NotInitialized;

    public bool IsLocal => this.CurrentState == State.Local;

    public bool IsRemote => this.CurrentState == State.Remote;

    bool IConsoleService.KeyAvailable => this.IsLocal ? this.consoleUserInterfaceService.KeyAvailable : false;

    bool IConsoleService.EnableColor
    {
        get => this.IsLocal ? this.consoleUserInterfaceService.EnableColor : true;
        set
        {
            if (this.IsLocal)
            {
                this.consoleUserInterfaceService.EnableColor = value;
            }
        }
    }

    private readonly ConsoleUserInterfaceService consoleUserInterfaceService;

    #endregion

    public VirtualUserInterfaceService(ConsoleUserInterfaceService consoleUserInterfaceService)
    {
        this.consoleUserInterfaceService = consoleUserInterfaceService;
    }

    public bool InitializeLocal()
    {
        if (this.IsInitialized)
        {
            return false;
        }

        this.CurrentState = State.Local;
        this.Connection = default;
        return true;
    }

    public bool InitializeRemote(ServerConnection connection)
    {
        if (this.IsInitialized)
        {
            return false;
        }

        this.CurrentState = State.Remote;
        this.Connection = connection;
        return true;
    }

    void IUserInterfaceService.EnqueueLine(string? message)
    {
        if (this.IsLocal)
        {
            this.consoleUserInterfaceService.EnqueueLine(message);
        }
    }

    Task<InputResult> IUserInterfaceService.ReadLine(bool cancelOnEscape, string? description)
    {
        if (this.IsLocal)
        {
            return this.consoleUserInterfaceService.ReadLine(cancelOnEscape, description);
        }
        else if (this.IsRemote)
        {

        }
        else
        {
            return Task.FromResult(default(InputResult));
        }
    }

    Task<InputResultKind> IUserInterfaceService.ReadYesNo(bool cancelOnEscape, string? description)
    {
        throw new NotImplementedException();
    }

    Task<InputResult> IUserInterfaceService.ReadPassword(bool cancelOnEscape, string? description)
    {
        throw new NotImplementedException();
    }

    Task IUserInterfaceService.Notify(ILogger? logger, LogLevel level, string message)
    {
        throw new NotImplementedException();
    }

    void IUserInterfaceService.WriteLineDefault(string? message)
    {
        throw new NotImplementedException();
    }

    void IUserInterfaceService.WriteLineWarning(string? message)
    {
        throw new NotImplementedException();
    }

    void IUserInterfaceService.WriteLineError(string? message)
    {
        throw new NotImplementedException();
    }

    void IConsoleService.Write(string? message, ConsoleColor color)
    {
        throw new NotImplementedException();
    }

    void IConsoleService.WriteLine(string? message, ConsoleColor color)
    {
        throw new NotImplementedException();
    }

    Task<InputResult> IConsoleService.ReadLine(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    ConsoleKeyInfo IConsoleService.ReadKey(bool intercept)
    {
        throw new NotImplementedException();
    }

    private bool TryGetReceiver([MaybeNullWhen(false)] IRemoteUserInterfaceReceiver receiver)
    {
        if (this.Connection is { } connection &&
            connection.GetS)
    }
}
