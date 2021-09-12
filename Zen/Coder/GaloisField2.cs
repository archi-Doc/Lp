// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Zen;

public class GaloisField2
{
    public const int Max = 256;
    public const int Mask = Max - 1;
    public const int PrimePoly = 285; // 285, 435

    public static GaloisField2 Get(int fieldGenPoly)
    {
        GaloisField2? field;
        if (!fieldCache.TryGetValue(fieldGenPoly, out field))
        {
            field = new GaloisField2(fieldGenPoly);
            fieldCache[fieldGenPoly] = field;
        }

        return field;
    }

    private static Dictionary<int, GaloisField2> fieldCache = new();

    private GaloisField2(int fieldGenPoly)
    {
        this.GF = new byte[Max];
        this.GF[0] = (byte)Mask;
        this.GFI = new byte[Max];
        this.GFI[0] = 0;

        var y = 1;
        unchecked
        {
            for (var x = 1; x < Max; x++)
            {
                this.GF[y] = (byte)(x - 1);
                this.GFI[x] = (byte)y;
                y <<= 1;
                if (y >= Max)
                {
                    y = (y ^ fieldGenPoly) & Mask;
                }
            }
        }

        this.Inverse = new byte[Max];
        for (var a = 0; a < Max; a++)
        {
            this.Inverse[0] = 0;
            for (var b = 1; b < Max; b++)
            {
                this.Inverse[this.GFI[b]] = this.InternalDiv(1, this.GFI[b]);
            }
        }

        this.Multi = new byte[Max * Max];
        this.Div = new byte[Max * Max];
        for (var a = 0; a < Max; a++)
        {
            for (var b = 0; b < Max; b++)
            {
                var i = (a * Max) + b;
                this.Multi[i] = this.InternalMulti(a, b);
                this.Div[i] = this.InternalDiv(a, b);
            }
        }

        for (var a = 0; a < Max; a++)
        {
            for (var b = 0; b < Max; b++)
            {
                var i = (a * Max) + b;
                // this.Div[i] = this.Multi[(a * Max) + this.Inverse[b]];
            }
        }
    }

    public byte[] GF { get; }

    public byte[] GFI { get; }

    public byte[] Multi { get; }

    public byte[] Div { get; }

    public byte[] Inverse { get; }

    internal byte InternalMulti(int a, int b)
    {
        if (a == 0 || b == 0)
        {
            return 0;
        }

        int c = this.GF[a] + this.GF[b];
        return this.GFI[(c % Mask) + 1];
    }

    internal byte InternalDiv(int a, int b)
    {
        if (a == 0)
        {
            return 0;
        }

        int c = this.GF[a] - this.GF[b] + Mask;
        return this.GFI[(c % Mask) + 1];
    }
}
