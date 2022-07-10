// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Services;

public enum UserInterfaceNotifyLevel
{
    Debug,
    Information,
    Warning,
    Error,
    Fatal,
}

public interface IUserInterfaceService
{
    public Task<bool?> RequestYesOrNo(string? description);

    public Task<string?> RequestString(string? description);

    public Task<string?> RequestPassword(string? description);

    public Task Notify(UserInterfaceNotifyLevel level, string message);
}
