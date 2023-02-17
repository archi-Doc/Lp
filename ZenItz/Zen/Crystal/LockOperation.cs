// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using ZenItz;

namespace CrystalData;

public partial class BaseData<TParent>
{
    internal const ZenResult NullDataResult = ZenResult.Removed;

    public struct LockOperation<TDatum> : IDisposable
        where TDatum : IData
    {
        public LockOperation(BaseData<TParent> data)
        {
            this.data = data;
        }

        public bool IsValid => this.datum != null;

        public TDatum? Datum => this.datum;

        public void Dispose()
        {
            this.Exit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Enter()
        {
            if (!this.lockTaken)
            {
                this.lockTaken = this.data.semaphore.Enter();
            }

            return this.lockTaken;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Exit()
        {
            this.datum = default;
            if (this.lockTaken)
            {
                this.data.semaphore.Exit();
                this.lockTaken = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetData(TDatum data)
            => this.datum = data;

        private readonly BaseData<TParent> data;
        private TDatum? datum;
        private bool lockTaken;
    }
}
