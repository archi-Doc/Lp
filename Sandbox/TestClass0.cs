using CrystalData.Datum;

namespace Sandbox;

internal class TestClass0
{
    public TestClass0(Crystalizer crystalizer, ICrystal<ManualClass> manualCrystal, ICrystal<CombinedClass> combinedCrystal, IBigCrystal<BaseData> crystalData)
    {
        this.crystalizer = crystalizer;

        this.manualCrystal = manualCrystal;
        this.combinedCrystal = combinedCrystal;
        this.crystalData = crystalData;
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test0");

        // this.crystalizer.ResetConfigurations();
        await this.crystalizer.SaveConfigurations(new LocalFileConfiguration("Local/Configuration.tinyhand"));
        // await this.crystalizer.LoadConfigurations(new LocalFileConfiguration("Local/Configuration.tinyhand"));

        await this.crystalizer.PrepareAndLoadAll();

        var manualClass = this.manualCrystal.Object;
        manualClass.Id++;
        Console.WriteLine(manualClass.ToString());

        var combinedClass = this.combinedCrystal.Object;
        combinedClass.Manual2.Id += 2;
        Console.WriteLine(combinedClass.ToString());

        ulong fileId = 0;
        combinedCrystal.Storage.PutAndForget(ref fileId, new(new byte[] { 1, 2, 3, }));

        var data = this.crystalData.Object;
        using (var op = data.Lock<ObjectDatum<LocalFileConfiguration>>())
        {
            if (op.Datum is not null)
            {
                op.Datum.Set(new LocalFileConfiguration("test"));
            }
        }
    }

    private Crystalizer crystalizer;
    private ICrystal<ManualClass> manualCrystal;
    private ICrystal<CombinedClass> combinedCrystal;
    private IBigCrystal<BaseData> crystalData;
}
