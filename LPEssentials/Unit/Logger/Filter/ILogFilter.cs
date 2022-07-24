// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

public interface ILogFilter
{
    internal delegate ILog? FilterDelegate(LogFilterParameter param);

    public ILog? Filter(LogFilterParameter param);
}
