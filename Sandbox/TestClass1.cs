﻿using Arc.Threading;
using CrystalData.Datum;

namespace Sandbox;

internal class TestClass1
{
    public TestClass1(Crystalizer crystalizer, /*ICrystal<ManualClass> manualCrystal, ICrystal<CombinedClass> combinedCrystal, */IBigCrystal<BaseData> crystalData, ValueClass.GoshujinClass valueClassGoshujin, StandardData.GoshujinClass standardGoshujin)
    {
        this.crystalizer = crystalizer;

        /*this.manualCrystal = manualCrystal;
        this.combinedCrystal = combinedCrystal;*/
        this.crystalData = crystalData;
        this.valueClassGoshujin = valueClassGoshujin;
        this.standardGoshujin = standardGoshujin;
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

        using (var w = this.standardGoshujin.TryLock(0, ValueLink.TryLockMode.GetOrCreate))
        {
            if (w is not null)
            {
                w.Name = "Zero";
                w.Commit();
            }
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

        var n = this.valueClassGoshujin.Count;
        var tc = new ValueClass();
        var semaphore = new SemaphoreLock();
        tc.Name = "Test" + n.ToString();
        tc.Id = n;
        lock (this.valueClassGoshujin)
        {
            this.valueClassGoshujin.Add(tc);
        }
    }

    private Crystalizer crystalizer;
    // private ICrystal<ManualClass> manualCrystal;
    // private ICrystal<CombinedClass> combinedCrystal;
    private IBigCrystal<BaseData> crystalData;
    private ValueClass.GoshujinClass valueClassGoshujin;
    private StandardData.GoshujinClass standardGoshujin;
}