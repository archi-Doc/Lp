using ValueLink;

namespace Sandbox;

internal class TestClass0
{
    public TestClass0(Crystalizer crystalizer, ICrystal<ManualClass> manualCrystal, ICrystal<CombinedClass> combinedCrystal, ICrystal<StandardData.GoshujinClass> standardCrystal, AdvancedClass crystalClass)
    {
        this.crystalizer = crystalizer;

        this.manualCrystal = manualCrystal;
        this.combinedCrystal = combinedCrystal;
        this.standardCrystal = standardCrystal;
        this.crystalClass = crystalClass;
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

        await this.TestCrystal(this.crystalClass);

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

    private async Task TestCrystal(AdvancedClass c)
    {
        var child = await c.Child.Get();

        var children = await c.Children.Get();
        using (var w = await children.TryLockAsync(1, TryLockMode.GetOrCreate))
        {
            if (w is not null)
            {
                w.Name = "One";
                w.RemoveAndErase();
                w.Commit();
            }
        }

        var r = children.TryGet(1);

        await c.Children.Save(UnloadMode.TryUnload);

        r = (await c.Children.Get()).TryGet(1);
    }

    private Crystalizer crystalizer;
    private ICrystal<ManualClass> manualCrystal;
    private ICrystal<CombinedClass> combinedCrystal;
    private ICrystal<StandardData.GoshujinClass> standardCrystal;
    private AdvancedClass crystalClass;
    // private IBigCrystal<BaseData> crystalData;
    // private ValueClass.GoshujinClass valueClassGoshujin;
    // private StandardData.GoshujinClass standardGoshujin;
}
