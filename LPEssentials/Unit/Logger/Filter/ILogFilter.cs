// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogFilter
{
    internal delegate ILogger? FilterDelegate(LogFilterParameter param);

    public ILogger? Filter(LogFilterParameter param);
}
