﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public partial class Itz<TIdentifier>
{
    public interface IShip<TPayload> : IShip
    where TPayload : IPayload
    {
        void Set(in TIdentifier id, in TPayload value);

        ItzResult Get(in TIdentifier id, out TPayload value);
    }

    public interface IShip : ILPSerializable
    {
        int Count();
    }
}
