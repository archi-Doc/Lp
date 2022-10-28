// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Services;

public interface IConsoleService
{
    public void Write(string? message = null);

    public void WriteLine(string? message = null);
}
