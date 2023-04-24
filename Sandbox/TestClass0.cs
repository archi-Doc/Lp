namespace Sandbox;

internal class TestClass0
{
    public TestClass0(Crystalizer crystalizer, ICrystal<ManualClass> manualCrystal, ICrystal<CombinedClass> combinedCrystal)
    {
        this.crystalizer = crystalizer;

        this.manualCrystal = manualCrystal;
        this.combinedCrystal = combinedCrystal;
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test0");

        await this.crystalizer.PrepareAndLoadAll();

        var manualClass = this.manualCrystal.Object;
        manualClass.Id++;
        Console.WriteLine(manualClass.ToString());

        var combinedClass = this.combinedCrystal.Object;
        combinedClass.Manual2.Id += 2;
        Console.WriteLine(combinedClass.ToString());

        ulong fileId = 0;
        combinedCrystal.Storage.PutAndForget(ref fileId, new(new byte[] { 1, 2, 3, }));
    }

    private Crystalizer crystalizer;
    private ICrystal<ManualClass> manualCrystal;
    private ICrystal<CombinedClass> combinedCrystal;
}
