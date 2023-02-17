// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace ZenItz.Datum;

public partial class DatumBase<T>
{
    internal const ZenResult NullDataResult = ZenResult.Removed;

    public struct LockOperation<TData> : IDisposable
        where TData : IData
    {
        public LockOperation(DatumBase<T> datum)
        {
            this.datum = datum;
        }

        public bool IsValid => this.data != null;

        public TData? Data => this.data;

        public void Dispose()
        {
            this.Exit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Enter()
        {
            if (!this.lockTaken)
            {
                this.lockTaken = this.datum.semaphore.Enter();
            }

            return this.lockTaken;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Exit()
        {
            this.data = default;
            if (this.lockTaken)
            {
                this.datum.semaphore.Exit();
                this.lockTaken = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetData(TData data)
            => this.data = data;

        private readonly DatumBase<T> datum;
        private TData? data;
        private bool lockTaken;
    }
}
