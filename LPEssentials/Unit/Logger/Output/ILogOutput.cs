// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogOutput
{
    internal delegate void OutputDelegate(LogOutputParameter param);

    public void Output(LogOutputParameter param);
}
