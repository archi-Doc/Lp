// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public interface IBaseData : IData
{
    void Initialize(IFromDataToIO flake);
}
