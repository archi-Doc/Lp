// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Services;

public interface IUserInterfaceService
{
    public Task<bool> RequestYesOrNo(string? description);

    public Task<string> RequestString(string? description);
}
