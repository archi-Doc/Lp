// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILog
{
    public void Log(int eventId, string message, Exception? exception = null);

    public Type OutputType { get; }
}
