// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;

namespace LP.Zen;

public class RsCoder
{
    public RsCoder(int dataSize, int checkSize, int fieldGenPoly = 285)
    {
        this.DataSize = dataSize;
        if (dataSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dataSize));
        }

        this.CheckSize = checkSize;
        if (checkSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(dataSize));
        }

        this.TotalSize = dataSize + checkSize;
        if (this.TotalSize > GaloisField.Max)
        {
            throw new ArgumentOutOfRangeException();
        }

        this.GaloisField = GaloisField.Get(fieldGenPoly);
    }

    public GaloisField GaloisField { get; }

    public int TotalSize { get; }

    public int DataSize { get; }

    public int CheckSize { get; }

    public byte[]? Source { get; set; }

    public byte[][]? EncodedBuffer => this.rentEncodeBuffer;

    public int EncodedBufferLength { get; set; }

    public byte[]? DecodedBuffer => this.rentDecodeBuffer;

    public int DecodedBufferLength { get; set; }

    public unsafe void Encode(byte[] source, int length)
    {
        var nm = this.TotalSize;
        var n = this.DataSize;
        var m = this.CheckSize;
        var multi = this.GaloisField.Multi;

        if ((length % n) != 0)
        {
            throw new InvalidDataException("Length of source data must be a multiple of RsCoder.DataSize.");
        }

        this.EncodedBufferLength = length / n;
        this.EnsureEncodeBuffer(this.EncodedBufferLength);
        var destination = this.rentEncodeBuffer!;
        var destinationLength = this.EncodedBufferLength;

        var ef = new byte[n * m];
        for (var x = 0; x < n; x++)
        {
            ef[x] = 1;
        }

        for (var y = 1; y < m; y++)
        {
            for (var x = 0; x < n; x++)
            {
                ef[x + (n * y)] = multi[(ef[x + (n * (y - 1))] * GaloisField.Max) + (x + 1)];
            }
        }

        /*encode core (original)
        Span<byte> b = source;
        for (var x = 0; x < destinationLength; x++)
        {
            for (var y = 0; y < n; y++)
            {// data
                destination[y][x] = b[y];
            }

            for (var y = 0; y < m; y++)
            {
                var d = 0;
                for (var z = 0; z < n; z++)
                {
                    d ^= multi[b[z], ef[(y * n) + z]];
                }

                destination[n + y][x] = (byte)d;
            }

            b = b.Slice(n);
        }*/

        // encode core (n = 4, 8, 16, other)
        /*if (n == 16)
        {
        }
        else*/
        {
            fixed (byte* ps = source, pef = ef, pm = multi)
            {// 0..n: $"pd{i} = destination[{i}], "
                var ps2 = ps;
                for (var x = 0; x < destinationLength; x++)
                {
                    for (var y = 0; y < n; y++)
                    {// 0..n: $"pd{i}[x] = ps2[{i}];"
                        destination[y][x] = ps2[y];
                    }

                    for (var y = 0; y < m; y++)
                    {
                        var d = 0;
                        var yn = y * n;
                        for (var z = 0; z < n; z++)
                        {// 0..n: $"d ^= pm[(ps2[{i}] * GaloisField.Max) + pef[yn + {i}]];"
                            d ^= pm[(ps2[z] * GaloisField.Max) + pef[yn + z]];
                        }

                        destination[n + y][x] = (byte)d;
                    }

                    ps2 += n;
                }
            }
        }
    }

    public unsafe void Decode(byte[]?[] source, int length)
    {
        var nm = this.TotalSize;
        var n = this.DataSize;
        var m = this.CheckSize;
        var multi = this.GaloisField.Multi;
        var div = this.GaloisField.Div;

        for (var x = 0; x < nm; x++)
        {
            if (source[x] != null && source[x]!.Length < length)
            {
                throw new InvalidDataException("The length of source byte arrays must be greater than 'length'.");
            }
        }

        this.DecodedBufferLength = length * n;
        this.EnsureDecodeBuffer(this.DecodedBufferLength);
        var destination = this.rentDecodeBuffer!;

        var ef = new byte[n * m];
        for (var x = 0; x < n; x++)
        {
            ef[x] = 1;
        }

        for (var y = 1; y < m; y++)
        {
            for (var x = 0; x < n; x++)
            {
                ef[x + (n * y)] = multi[(ef[x + (n * (y - 1))] * GaloisField.Max) + x + 1];
            }
        }

        var el = new byte[n * 2 * n];
        var u = 0; // data
        var v = 0; // check
        var z = 0;
        var s = new byte[n][];
        for (var x = 0; x < n; x++)
        {
            if (x == u && source[u] != null)
            {
                z = u;
                u++;
            }
            else
            {
                while (source[u] == null && u < n)
                {
                    u++;
                }

                while (source[n + v] == null)
                {
                    v++;
                    if (v >= m)
                    {
                        throw new InvalidDataException("The number of valid byte arrays must be greater than RsCoder.DataSize.");
                    }
                }

                z = n + v;
                v++;
            }

            if (z < n)
            {// data
                for (var y = 0; y < n; y++)
                {
                    if (y == z)
                    {
                        el[y + (x * n * 2)] = 1;
                    }
                    else
                    {
                        el[y + (x * n * 2)] = 0;
                    }
                }
            }
            else
            {// check
                for (var y = 0; y < n; y++)
                {
                    el[y + (x * n * 2)] = ef[y + ((z - n) * n)];
                }
            }

            for (var y = 0; y < n; y++)
            {
                if (y == x)
                {
                    el[y + (x * n * 2) + n] = 1;
                }
                else
                {
                    el[y + (x * n * 2) + n] = 0;
                }
            }

            s[x] = source[z]!;
        }

        // reverse
        for (var x = 0; x < n; x++)
        {
            var e = el[x + (x * n * 2)];
            if (e == 0)
            {
                throw new InvalidDataException();
            }
            else if (e != 1)
            {
                for (var y = x; y < (n * 2); y++)
                {
                    el[y + (x * n * 2)] = div[(el[y + (x * n * 2)] * GaloisField.Max) + e];
                }
            }

            for (var y = 0; y < n; y++)
            {
                if (x != y)
                {
                    e = el[x + (y * n * 2)];
                    if (e != 0)
                    {
                        for (u = x; u < (n * 2); u++)
                        {
                            el[u + (y * n * 2)] ^= multi[(el[u + (x * n * 2)] * GaloisField.Max) + e];
                        }
                    }
                }
            }
        }

        // copy reverse
        var er = new byte[n * n]; // byte[n, n] is slower.
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                er[x + (n * y)] = el[n + x + (y * n * 2)];
            }
        }

        // decode core (original)
        /*var i = 0;
        for (var x = 0; x < sourceLength; x++)
        {
            for (var y = 0; y < n; y++)
            {
                u = 0;
                for (z = 0; z < n; z++)
                {
                    u ^= multi[er[z + (y * n)], s[z][x]];
                }

                destination[i++] = (byte)u; // fixed
            }
        }*/

        // decode core (n = 4, 8, 16, other)
        /*if (n == 16)
        {
        }
        else*/
        {
            fixed (byte* um = multi, uer = er, ud = destination)
            {
                var ud2 = ud;
                for (var x = 0; x < length; x++)
                {
                    for (var y = 0; y < n; y++)
                    {
                        u = 0;
                        for (z = 0; z < n; z++)
                        {
                            u ^= um[(uer[z + (y * n)] * GaloisField.Max) + s[z][x]];
                        }

                        *ud2++ = (byte)u; // fixed
                    }
                }
            }
        }
    }

    private byte[][]? rentEncodeBuffer;

    private byte[]? rentDecodeBuffer;

    private void EnsureEncodeBuffer(int length)
    {
        if (this.rentEncodeBuffer == null)
        {// Rent a buffer.
            this.rentEncodeBuffer = new byte[this.TotalSize][];
            for (var n = 0; n < this.TotalSize; n++)
            {
                this.rentEncodeBuffer[n] = ArrayPool<byte>.Shared.Rent(length);
            }
        }
        else if (this.rentEncodeBuffer[0] == null || this.rentEncodeBuffer[0]!.Length < length)
        {// Insufficient buffer, return and rent. rentEncodeBuffer[n] is guaranteed to have valid value.
            this.ReturnEncodeBuffer();
            for (var n = 0; n < this.TotalSize; n++)
            {
                this.rentEncodeBuffer[n] = ArrayPool<byte>.Shared.Rent(length);
            }
        }
    }

    private void ReturnEncodeBuffer()
    {
        if (this.rentEncodeBuffer != null)
        {
            for (var n = 0; n < this.rentEncodeBuffer.Length; n++)
            {
                if (this.rentEncodeBuffer[n] != null)
                {
                    ArrayPool<byte>.Shared.Return(this.rentEncodeBuffer[n]!);
                    this.rentEncodeBuffer[n] = null!;
                }
            }
        }
    }

    private void EnsureDecodeBuffer(int length)
    {
        if (this.rentDecodeBuffer == null)
        {// Rent a buffer.
            this.rentDecodeBuffer = ArrayPool<byte>.Shared.Rent(length);
        }
        else if (this.rentDecodeBuffer.Length < length)
        {// Insufficient buffer, return and rent.
            this.ReturnDecodeBuffer();
            this.rentDecodeBuffer = ArrayPool<byte>.Shared.Rent(length);
        }
    }

    private void ReturnDecodeBuffer()
    {
        if (this.rentDecodeBuffer != null)
        {
            ArrayPool<byte>.Shared.Return(this.rentDecodeBuffer);
            this.rentDecodeBuffer = null;
        }
    }
}
