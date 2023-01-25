// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace ZenItz;

public partial class Itz<TIdentifier>
{
    public interface IShip<TPayload> : IShip
    where TPayload : IPayload
    {
        void Set(in TIdentifier id, in TPayload value);

        bool TryGet(in TIdentifier id, out TPayload value);

        bool Remove(in TIdentifier id);
    }

    public interface IShip : ILPSerializable
    {
        void SetCapacity(int capacity);

        int Count();
    }
}
