namespace Sandbox;

[TinyhandObject]
internal partial class ManualClass
{
    [Key(0)]
    public int Id { get; set; }

    public override string ToString()
        => $"Manual class {this.Id}";
}

[TinyhandObject]
internal partial class CombinedClass
{
    [Key(0)]
    public ManualClass Manual1 { get; set; } = default!;

    [Key(1)]
    public ManualClass Manual2 { get; set; } = default!;

    public override string ToString()
        => $"{this.Manual1.ToString()} {this.Manual2.ToString()}";
}

internal class TestClass
{
    public TestClass(Crystalizer crystalizer, ICrystal<ManualClass> manualCrystal, ICrystal<CombinedClass> combinedCrystal, ManualClass manualClass)
    {
        this.crystalizer = crystalizer;

        this.manualCrystal = manualCrystal;
        this.manualCrystal.ConfigureFiler(new LocalFilerConfiguration("Manual2.data"));

        // this.manualCrystal.Setup();
        this.combinedCrystal = combinedCrystal;
        this.manualClass0 = manualClass;
    }

    public async Task Test1()
    {
        Console.WriteLine("Sandbox test1");

        await this.crystalizer.PrepareAndLoad();

        this.manualCrystal.Configure(new(Crystalization.Manual, new LocalFilerConfiguration(string.Empty, "test")));

        var manualClass = this.manualCrystal.Object;
        manualClass.Id = 1;
        Console.WriteLine(manualClass.ToString());

        var manualCrystal2 = this.crystalizer.Create<ManualClass>();
        var manualClass2 = manualCrystal2.Object;
        manualClass2.Id = 2;
        Console.WriteLine(manualClass.ToString());
        Console.WriteLine(manualClass2.ToString());

        var combinedClass = this.combinedCrystal.Object;
        combinedClass.Manual2.Id = 2;
        Console.WriteLine(combinedClass.ToString());

        Console.WriteLine(this.manualClass0.ToString());

        await this.crystalizer.SaveAndTerminate();
    }

    private Crystalizer crystalizer;
    private ManualClass manualClass0;
    private ICrystal<ManualClass> manualCrystal;
    private ICrystal<CombinedClass> combinedCrystal;
}
