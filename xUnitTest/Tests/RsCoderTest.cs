using LP.Zen;
using Xunit;

namespace xUnitTest;

public class RsCoderTest
{
    [Fact]
    public void Test1()
    {// (n, m) = (data, check)
        (int n, int m)[] nm = new[] { (4, 2), (4, 4), (4, 8), (8, 4), (8, 8), (8, 10), (16, 8), (16, 16), (5, 3), (5, 5), (13, 3), (13, 7), };
        var sizes = new[] { 0, 1, 16, 256, 1000, 10_000 };

        var sources = new byte[sizes.Length][];
        for (var n = 0; n < sizes.Length; n++)
        {
            sources[n] = new byte[sizes[n]];
            Random.Shared.NextBytes(sources[n]);
        }

        foreach (var x in nm)
        {
            TestNM(x.n, x.m, sources);
        }
    }

    private void TestNM(int n, int m, byte[][] sources)
    {
        // using (var coder = new RsCoder)

        var coder = new RsCoder(n, m);
        foreach (var x in sources)
        {
            TestSource(coder, x);
        }
    }

    private void TestSource(RsCoder coder, byte[] source)
    {
    }
}
