// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.Content;
using Lp.NetServices;

namespace Lp.Services;

internal class VirtualUserInterfaceService : IUserInterfaceService
{
    private enum State
    {
        NotInitialized,
        Local,
        Remote,
    }

    #region FieldAndProperty

    public bool IsInitialized { get; private set; }

    public ServerConnection? Connection { get; private set; }

    public IRemoteUserInterfaceReceiver? receiver { get; private set; }

    bool IConsoleService.KeyAvailable => this.consoleUserInterfaceService.KeyAvailable;

    bool IConsoleService.EnableColor
    {
        get => this.consoleUserInterfaceService.EnableColor;
        set => this.consoleUserInterfaceService.EnableColor = value;
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

        this.Connection = default;
        return true;
    }

    public bool InitializeRemote(ServerConnection connection)
    {
        if (this.IsInitialized)
        {
            return false;
        }

        this.Connection = connection;
        return true;
    }

    void IUserInterfaceService.EnqueueLine(string? message)
    {
        if (this.Connection is null)
        {
            this.consoleUserInterfaceService.EnqueueLine(message);
        }
    }

    Task<InputResult> IUserInterfaceService.ReadLine(bool cancelOnEscape, string? description)
    {
        if (this.Connection is null)
        {
            this.consoleUserInterfaceService.EnqueueLine(message);
        }
        else
        {
            this.Connection.Get
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
}
