using ValueLink;

namespace Sandbox;

internal class TestClass0
{
    public TestClass0(Crystalizer crystalizer, ICrystal<ManualClass> manualCrystal, ICrystal<CombinedClass> combinedCrystal, ICrystal<StandardData.GoshujinClass> standardCrystal)
    {
        this.crystalizer = crystalizer;

        this.manualCrystal = manualCrystal;
        this.combinedCrystal = combinedCrystal;
        this.standardCrystal = standardCrystal;
        /*this.crystalData = crystalData;
        this.valueClassGoshujin = valueClassGoshujin;
        this.standardGoshujin = standardGoshujin;*/
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test0");

        // await this.crystalizer.SaveConfigurations(new LocalFileConfiguration("CrystalConfiguration.tinyhand"));

        var result = await this.crystalizer.PrepareAndLoadAll();
        if (result.IsFailure())
        {
            return;
        }

        // await this.crystalizer.TestJournalAll();

        var m = this.manualCrystal.Data;
        // m.Id++;
        Console.WriteLine($"Manual id: {m.Id}");

        var c = this.combinedCrystal.Data;
        c.Manual1.Id++;
        c.Manual2.Id += 2;
        Console.WriteLine($"Combined: {c.ToString()}");

        var g = this.standardCrystal.Data;
        using (var w = g.TryLock(0, ValueLink.TryLockMode.GetOrCreate))
        {
            if (w is not null)
            {
                w.Name = "Zero";
                w.Age += 1d;
                Console.WriteLine(w.Commit());
            }
        }

        _ = Task.Run(async () =>
        {
            using (var w = g.TryLock(0, ValueLink.TryLockMode.GetOrCreate))
            {
                await Task.Delay(2000);
            }
        });

        /*await this.standardCrystal.Save(UnloadMode.TryUnload);
        var st = ((IGoshujinSemaphore)g).State;
        var w2 = g.TryLock(0, ValueLink.TryLockMode.GetOrCreate);*/
    }

    private Crystalizer crystalizer;
    private ICrystal<ManualClass> manualCrystal;
    private ICrystal<CombinedClass> combinedCrystal;
    private ICrystal<StandardData.GoshujinClass> standardCrystal;
    // private ValueClass.GoshujinClass valueClassGoshujin;
    // private StandardData.GoshujinClass standardGoshujin;
}
