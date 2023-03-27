using Crystalizer;

namespace Sandbox;

internal class TestClass
{
    public TestClass(CrystalizerClass crystalizer)
    {
        this.crystalizer = crystalizer;
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test1");
    }

    private CrystalizerClass crystalizer;
}
