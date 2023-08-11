using Arc.Threading;
using CrystalData.Datum;

namespace Sandbox;

internal class TestClass0
{
    public TestClass0(Crystalizer crystalizer, ICrystal<ManualClass> manualCrystal/*, ICrystal<CombinedClass> combinedCrystal, IBigCrystal<BaseData> crystalData, ValueClass.GoshujinClass valueClassGoshujin, StandardData.GoshujinClass standardGoshujin*/)
    {
        this.crystalizer = crystalizer;

        this.manualCrystal = manualCrystal;
        /*this.combinedCrystal = combinedCrystal;
        this.crystalData = crystalData;
        this.valueClassGoshujin = valueClassGoshujin;
        this.standardGoshujin = standardGoshujin;*/
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test0");

        var result = await this.crystalizer.PrepareAndLoadAll();
        if (result.IsFailure())
        {
            return;
        }

        var m = this.manualCrystal.Data;
        m.Id++;
    }

    private Crystalizer crystalizer;
    private ICrystal<ManualClass> manualCrystal;
    // private ICrystal<CombinedClass> combinedCrystal;
    // private IBigCrystal<BaseData> crystalData;
    // private ValueClass.GoshujinClass valueClassGoshujin;
    // private StandardData.GoshujinClass standardGoshujin;
}
