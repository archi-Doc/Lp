// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogger
{
    internal delegate void LogDelegate(string message);

    public void Log(string message);
}
