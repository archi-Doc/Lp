using LP.Zen;
using Xunit;

namespace xUnitTest;

public class RsCoderTest
{
    [Fact]
    public void Test1()
    {// (n, m) = (data, check)
        (int n, int m)[] nm = new[] { (4, 2), (4, 4), (4, 8), (8, 4), (8, 8), (8, 10), (16, 8), (16, 16), (5, 3), (5, 5), (13, 3), (13, 7), };
        // (int n, int m)[] nm = new[] { (4, 2), (4, 4), (8, 8), (16, 16), };
        var sizes = new[] { 0, 4, 16, 256, 1000, 10_000 };

        var random = new Random(42);
        var sources = new byte[sizes.Length][];
        for (var n = 0; n < sizes.Length; n++)
        {
            sources[n] = new byte[sizes[n]];
            random.NextBytes(sources[n]);
        }

        foreach (var x in nm)
        {
            TestNM(x.n, x.m, sources, random);
        }
    }

    private void TestNM(int n, int m, byte[][] sources, Random random)
    {
        // using (var coder = new RsCoder)

        using (var coder = new RsCoder(n, m))
        {
            foreach (var x in sources)
            {
                TestSource(coder, x, random);
            }
        }
    }

    private void TestSource(RsCoder coder, byte[] source, Random random)
    {
        var length = source.Length;
        length = (length / coder.DataSize) * coder.DataSize; // length must be a multiple of coder.DataSize

        // Simple encode and decode.
        coder.Encode(source, length);
        coder.Decode(coder.EncodedBuffer!, coder.EncodedBufferLength);
        TestHelper.AlmostEqual(source, coder.DecodedBuffer, length).IsTrue();

        for (var i = 1; i <= coder.CheckSize; i++)
        {
            if (coder.DataSize == 4 && coder.CheckSize == 2 && i == 1)
            {
                var a = 44;
            }
            if (coder.DataSize == 4 && coder.CheckSize == 8 && i == 7)
            {
                var a = 44;
            }

            coder.Encode(source, length);
            coder.InvalidateEncodedBufferForUnitTest(random, i);
            coder.Decode(coder.EncodedBuffer!, coder.EncodedBufferLength);
            TestHelper.AlmostEqual(source, coder.DecodedBuffer, length).IsTrue();
        }
    }
}
