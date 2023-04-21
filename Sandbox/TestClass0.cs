namespace Sandbox;

internal class TestClass0
{
    public TestClass0(Crystalizer crystalizer, ICrystal<ManualClass> manualCrystal)
    {
        this.crystalizer = crystalizer;

        this.manualCrystal = manualCrystal;
        // this.manualCrystal.ConfigureFiler(new LocalFilerConfiguration("Manual2.data"));
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test0");

        await this.crystalizer.PrepareAndLoadAll();

        var manualClass = this.manualCrystal.Object;
        manualClass.Id = 1;
        Console.WriteLine(manualClass.ToString());
    }

    private Crystalizer crystalizer;
    private ICrystal<ManualClass> manualCrystal;
}
