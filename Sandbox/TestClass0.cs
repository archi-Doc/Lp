using CrystalData.Datum;

namespace Sandbox;

internal class TestClass0
{
    public TestClass0(Crystalizer crystalizer, /*ICrystal<ManualClass> manualCrystal, ICrystal<CombinedClass> combinedCrystal, */IBigCrystal<BaseData> crystalData)
    {
        this.crystalizer = crystalizer;

        /*this.manualCrystal = manualCrystal;
        this.combinedCrystal = combinedCrystal;*/
        this.crystalData = crystalData;
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test0");

        this.crystalizer.ResetConfigurations();
        await this.crystalizer.LoadConfigurations(new LocalFileConfiguration("Local/Configurations.tinyhand"));
        // await this.crystalizer.SaveConfigurations(new LocalFileConfiguration("Local/Configurations.tinyhand"));

        var result = await this.crystalizer.PrepareAndLoadAll();
        if (result.IsFailure())
        {
            return;
        }

        // await this.crystalizer.TestJournalAll();

        /*var manualClass = this.manualCrystal.Data;
        manualClass.Id++;
        Console.WriteLine(manualClass.ToString());

        var combinedClass = this.combinedCrystal.Data;
        combinedClass.Manual2.Id += 2;
        Console.WriteLine(combinedClass.ToString());*/

        // combinedClass.WriteRecord(); // -> Locator

        // ulong fileId = 0;
        // combinedCrystal.Storage.PutAndForget(ref fileId, new(new byte[] { 1, 2, 3, }));

        var data = this.crystalData.Data;
        using (var op = data.Lock<ObjectDatum<LocalFileConfiguration>>())
        {
            if (op.Datum is not null)
            {
                var datum = op.Datum.Get();
                op.Datum.Set(new LocalFileConfiguration("test1"));
            }
        }
    }

    private Crystalizer crystalizer;
    // private ICrystal<ManualClass> manualCrystal;
    // private ICrystal<CombinedClass> combinedCrystal;
    private IBigCrystal<BaseData> crystalData;
}
