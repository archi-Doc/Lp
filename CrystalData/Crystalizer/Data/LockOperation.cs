// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using CrystalData.Datum;

namespace CrystalData;

public partial class BaseData
{
    public struct LockOperation<TDatum> : IDisposable
        where TDatum : IDatum
    {
        public LockOperation(BaseData data)
        {
            this.data = data;
        }

        public bool IsValid => this.datum != null;

        public TDatum? Datum => this.datum;

        public CrystalResult Result => this.result;

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
        internal async Task<bool> EnterAsync()
        {
            if (!this.lockTaken)
            {
                this.lockTaken = await this.data.semaphore.EnterAsync().ConfigureAwait(false);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetResult(CrystalResult result)
            => this.result = result;

        private readonly BaseData data;
        private bool lockTaken;
        private TDatum? datum;
        private CrystalResult result;
    }
}
