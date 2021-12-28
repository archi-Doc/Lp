// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Netsphere;

internal enum FlowFunctionType
{
    RTT,
    ARR,
}

internal class FlowFunction
{
    public const int MaxSamples = 4;
    public const int MinimumSamples = 2;
    public const double MinimumDifferenceRatio = 0.05d;
    public const double MinimumARR = 0.05d;

    public FlowFunction(FlowFunctionType type)
    {
        this.Type = type;
    }

    public bool TryAdd(double x, double y)
    {
        if (this.numberOfSamples >= MaxSamples)
        {
            return false;
        }

        if (this.Type == FlowFunctionType.RTT)
        {// RTT
            if (this.minY > y)
            {
                this.minY = y;
            }

            if (!this.EnoughDifference(this.minY, y))
            {
                return false;
            }
        }
        else
        {// ARR
            if (y < MinimumARR)
            {
                return false;
            }
        }

        for (var n = 0; n < this.numberOfSamples; n++)
        {
            if (!this.EnoughDifference(this.x[n], x) || !this.EnoughDifference(this.y[n], y))
            {
                return false;
            }
        }

        this.x[this.numberOfSamples] = x;
        this.y[this.numberOfSamples] = y;
        this.numberOfSamples++;
        return true;
    }

    public bool TryGet(out double result)
    {
        if (this.numberOfSamples < MinimumSamples)
        {
            result = 0;
            return false;
        }

        Span<double> x = stackalloc double[this.numberOfSamples];
        Span<double> y = stackalloc double[this.numberOfSamples];
        if (this.Type == FlowFunctionType.RTT)
        {// RTT
            for (var n = 0; n < this.numberOfSamples; n++)
            {
                x[n] = this.x[n];
                y[n] = this.y[n] - this.minY;
            }
        }
        else
        {// ARR
            for (var n = 0; n < this.numberOfSamples; n++)
            {
                x[n] = this.x[n];
                y[n] = this.y[n];
            }
        }

        double sx2 = 0;
        for (var n = 0; n < this.numberOfSamples; n++)
        {
            sx2 += x[n] * x[n];
        }

        double sy = 0;
        for (var n = 0; n < this.numberOfSamples; n++)
        {
            sy += y[n];
        }

        double sxy = 0;
        for (var n = 0; n < this.numberOfSamples; n++)
        {
            sxy += x[n] * y[n];
        }

        double sx = 0;
        for (var n = 0; n < this.numberOfSamples; n++)
        {
            sx += x[n];
        }

        var a = (this.numberOfSamples * sxy) - (sx * sy);
        if (a == 0)
        {
            result = 0;
            return false;
        }

        result = ((sxy * sx) - (sx2 * sy)) / a;
        return true;
    }

    public void Reset()
    {
        this.numberOfSamples = 0;
    }

    public FlowFunctionType Type { get; }

    private int numberOfSamples;
    private double[] x = new double[MaxSamples]; // SendCapacityPerRound
    private double[] y = new double[MaxSamples]; // RTT or ARR
    private double minY = double.MaxValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool EnoughDifference(double target, double value)
    {
        if (target == 0)
        {
            return false;
        }

        return Math.Abs(target - value) / target > MinimumDifferenceRatio;
    }
}
