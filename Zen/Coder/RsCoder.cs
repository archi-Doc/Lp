// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Buffers;

namespace LP.Zen;

public class RsCoder : IDisposable
{
    public RsCoder(int dataSize, int checkSize, int fieldGenPoly = GaloisField.PrimePoly)
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

        this.GaloisField = GaloisField.Get((int)fieldGenPoly);

        this.EnsureBuffers(false);
        var f = this.rentEF!;
        for (var x = 0; x < this.DataSize; x++)
        {
            f[x] = 1; // best
            // this.F[x] = 1;
        }

        for (var y = 1; y < this.CheckSize; y++)
        {
            for (var x = 0; x < this.DataSize; x++)
            {
                f[x + (this.DataSize * y)] = this.GaloisField.Multi[(f[x + (this.DataSize * (y - 1))] * GaloisField.Max) + this.GaloisField.GFI[x]]; // best
                // this.F[x + (this.DataSize * y)] = this.GaloisField.Multi[(this.F[x + (this.DataSize * (y - 1))] * GaloisField.Max) + x + 1]; // Obsolete
            }
        }

        for (var y = 0; y < this.CheckSize; y++)
        {
            for (var x = 0; x < this.DataSize; x++)
            {
                // f[x + (this.DataSize * y)] = this.GaloisField.GFI[x * y]; // soso
            }
        }

        /*var temp = new byte[this.DataSize];
        for (var x = 0; x < this.DataSize; x++)
        {
            temp[x] = 1;
        }

        for (var y = 0; y < this.CheckSize; y++)
        {
            for (var x = 0; x < this.DataSize; x++)
            {
                this.F[x + (this.DataSize * y)] = this.GaloisField.GFI[temp[x]];
                temp[x] = this.GaloisField.Multi[((x + 1) * GaloisField.Max) + temp[x]];
            }
        }*/

        // Random...
        /*var array = GetUniqueRandomNumbers(new Random(191), 0, 256, 256).ToArray();
        for (var y = 0; y < this.F.Length; y++)
        {
            this.F[y] = this.GaloisField.GFI[(byte)array[y]];
        }*/
    }

    public static System.Collections.Generic.IEnumerable<int> GetUniqueRandomNumbers(Random r, int start, int end, int count)
    {
        var work = new int[end - start + 1];
        for (int n = start, i = 0; n <= end; n++, i++)
        {
            work[i] = n;
        }

        for (int resultPos = 0; resultPos < count; resultPos++)
        {
            int nextResultPos = r.Next(resultPos, work.Length);
            (work[resultPos], work[nextResultPos]) = (work[nextResultPos], work[resultPos]);
        }

        return work.Take(count);
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
        var ef = this.rentEF;

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

        // encode core (n = 4, 8, other)
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

        // Rent buffers
        this.EnsureBuffers(true);
        var ef = this.rentEF!;
        var el = this.rentEL!;

        var u = 0; // data
        var v = 0; // check
        var z = 0;
        var s = new byte[n][];
        for (var x = 0; x < n; x++)
        {
            if (x == u && source[u] != null)
            {// Data
                z = u;
                u++;
            }
            else
            {// Search valid check
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
        /*for (var x = 0; x < n; x++)
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
        }*/

        this.MakeReverseMatrix(el, n, s);

        // check
        for (var y = 0; y < n; y++)
        {
            for (var x = 0; x < n; x++)
            {
                if (x == y)
                {
                    if (el[x + (y * n * 2)] != 1)
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    if (el[x + (y * n * 2)] != 0)
                    {
                        throw new Exception();
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

    public override string ToString() => $"RsCoder Data: {this.DataSize}, Check: {this.CheckSize}";

    public void InvalidateEncodedBufferForUnitTest(Random random, int number)
    {
        if (this.rentEncodeBuffer == null)
        {
            return;
        }
        else if (this.rentEncodeBuffer.Length < number)
        {
            throw new InvalidOperationException();
        }

        while (true)
        {
            var invalidNumber = this.rentEncodeBuffer.Count(a => a == null);
            if (invalidNumber >= number)
            {
                return;
            }

            int i;
            do
            {
                i = random.Next(this.rentEncodeBuffer.Length);
            }
            while (this.rentEncodeBuffer[i] == null);

            ArrayPool<byte>.Shared.Return(this.rentEncodeBuffer[i]);
            this.rentEncodeBuffer[i] = null!; // Invalidate
        }
    }

    public void InvalidateEncodedBufferForUnitTest(uint bufferbits)
    {
        if (this.rentEncodeBuffer == null)
        {
            return;
        }

        for (var i = 0; i < this.rentEncodeBuffer.Length; i++)
        {
            if ((bufferbits & (1 << i)) == 0)
            {
                ArrayPool<byte>.Shared.Return(this.rentEncodeBuffer[i]);
                this.rentEncodeBuffer[i] = null!; // Invalidate
            }
        }
    }

    public void TestReverseMatrix(uint sourceBits)
    {
        var n = this.DataSize;
        var m = this.CheckSize;

        this.EnsureBuffers(true);
        var ef = this.rentEF!;
        var el = this.rentEL!;
        el.AsSpan().Fill(0);

        var u = 0; // data
        var v = 0; // check
        var z = 0;
        for (var x = 0; x < n; x++)
        {
            if (x == u && ((sourceBits & (1 << u)) != 0))
            {// Data
                z = u;
                u++;
            }
            else
            {// Search valid check
                while (((sourceBits & (1 << u)) == 0) && u < n)
                {
                    u++;
                }

                while ((sourceBits & (1 << (n + v))) == 0)
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
                el[z + (x * n * 2)] = 1;
            }
            else
            {// check
                for (var y = 0; y < n; y++)
                {
                    el[y + (x * n * 2)] = ef[y + ((z - n) * n)];
                }
            }

            el[x + (x * n * 2) + n] = 1;
        }

        this.MakeReverseMatrix(el, n, null);
    }

    private void MakeReverseMatrix(byte[] el, int n, byte[][]? s)
    {
        var multi = this.GaloisField.Multi;
        var div = this.GaloisField.Div;
        var pivot = false;
        for (var x = 0; x < n; x++)
        {
            // Pivot
            /*var max = el[x + (x * n * 2)];
            var maxLine = x;
            for (var y = x + 1; y < n; y++)
            {
                // if (el[x + (y * n * 2)] != 0 && (el[x + (y * n * 2)] < max || max == 0))
                if (el[x + (y * n * 2)] > max)
                {
                    max = el[x + (y * n * 2)];
                    maxLine = y;
                }
            }*/

            if (el[x + (x * n * 2)] != 0)
            {
                goto Normalize;
            }

            pivot = true;

            // Pivoting (Row)
            for (var y = x + 1; y < n; y++)
            {
                if (el[x + (y * n * 2)] != 0)
                {
                    for (var u = 0; u < (n * 2); u++)
                    {
                        var temp = el[u + (y * n * 2)];
                        el[u + (y * n * 2)] = el[u + (x * n * 2)];
                        el[u + (x * n * 2)] = temp;
                    }

                    goto Normalize;
                }
            }

            // Pivoting (Column)
            for (var y = x + 1; y < n; y++)
            {
                if (el[y + (x * n * 2)] != 0)
                {
                    for (var u = 0; u < n; u++)
                    {
                        var temp = el[y + (u * n * 2)];
                        el[y + (u * n * 2)] = el[x + (u * n * 2)];
                        el[x + (u * n * 2)] = temp;
                    }

                    if (s != null)
                    {
                        var temp = s[y];
                        s[y] = s[x];
                        s[x] = temp;
                    }

                    goto Normalize;
                }
            }

            // el[x + (x * n * 2)] is 0...
            throw new InvalidDataException("Sorry for this.");

Normalize:
            var e = el[x + (x * n * 2)];
            if (e != 1)
            {
                for (var y = 0; y < (n * 2); y++)
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
                        e = div[(e * GaloisField.Max) + 1];
                        for (var u = 0; u < (n * 2); u++)
                        {
                            el[u + (y * n * 2)] ^= multi[(el[u + (x * n * 2)] * GaloisField.Max) + e];
                        }
                    }
                }
            }
        }

        if (pivot)
        {
            Console.WriteLine("Pivoting");
        }
    }

    private byte[]? rentEF;

    private byte[]? rentEL;

    private byte[][]? rentEncodeBuffer;

    private byte[]? rentDecodeBuffer;

    public string MatrixToString(byte[] m)
    {
        int row, column;
        var length = m.Length;
        if (length == (this.DataSize * this.DataSize))
        {
            row = this.DataSize;
            column = this.DataSize;
        }
        else if (length == (this.DataSize * this.DataSize * 2))
        {
            row = this.DataSize;
            column = this.DataSize * 2;
        }
        else if ((length % this.DataSize) == 0)
        {
            row = length / this.DataSize;
            column = this.DataSize;
        }
        else
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        for (var y = 0; y < row; y++)
        {
            for (var x = 0; x < column; x++)
            {
                sb.Append(string.Format("{0, 3}", m[x + (y * column)]));
                sb.Append(", ");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void EnsureBuffers(bool decodeBuffer)
    {
        if (this.rentEF == null)
        {
            this.rentEF = ArrayPool<byte>.Shared.Rent(this.DataSize * this.CheckSize);
            Array.Fill<byte>(this.rentEF, 0);
        }

        if (decodeBuffer)
        {
            if (this.rentEL == null)
            {
                this.rentEL = ArrayPool<byte>.Shared.Rent(this.DataSize * this.DataSize * 2);
            }
        }
    }

    private void ReturnBuffers()
    {
        if (this.rentEF != null)
        {
            ArrayPool<byte>.Shared.Return(this.rentEF);
            this.rentEF = null;
        }

        if (this.rentEL != null)
        {
            ArrayPool<byte>.Shared.Return(this.rentEL);
            this.rentEL = null;
        }
    }

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
        else
        {
            for (var n = 0; n < this.TotalSize; n++)
            {
                if (this.rentEncodeBuffer[n] == null)
                {// Rent
                    this.rentEncodeBuffer[n] = ArrayPool<byte>.Shared.Rent(length);
                }
                else if (this.rentEncodeBuffer[n].Length < length)
                {// Insufficient buffer, return and rent.
                    ArrayPool<byte>.Shared.Return(this.rentEncodeBuffer[n]);
                    this.rentEncodeBuffer[n] = ArrayPool<byte>.Shared.Rent(length);
                }
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

#pragma warning disable SA1124 // Do not use regions
    #region IDisposable Support
#pragma warning restore SA1124 // Do not use regions

    private bool disposed = false; // To detect redundant calls.

    /// <summary>
    /// Finalizes an instance of the <see cref="RsCoder"/> class.
    /// </summary>
    ~RsCoder()
    {
        this.Dispose(false);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// free managed/native resources.
    /// </summary>
    /// <param name="disposing">true: free managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed)
        {
            if (disposing)
            {
                // free managed resources.
                this.ReturnBuffers();
                this.ReturnDecodeBuffer();
                this.ReturnEncodeBuffer();
            }

            // free native resources here if there are any.
            this.disposed = true;
        }
    }
    #endregion
}
