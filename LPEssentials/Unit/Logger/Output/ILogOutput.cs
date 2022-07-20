// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogOutput
{
    public void Debug(string message);

    public void Information(string message);

    public void Warning(string message);

    public void Error(string message);

    public void Fatal(string message);
}
