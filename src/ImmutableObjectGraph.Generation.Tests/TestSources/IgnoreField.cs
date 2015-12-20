[ImmutableObjectGraph.Generation.GenerateImmutable(GenerateBuilder = true, Delta = true, DefineInterface = true, DefineWithMethodsPerProperty = true)]
partial class Foo
{
    // Should get ignored by the generator
    static bool staticField = false;

    readonly bool field;

    // Should get ignored by the generator
    [ImmutableObjectGraph.Generation.Ignore]
    readonly bool userField;

    protected Foo()
    {
        this.userField = false;
    }

    public bool CustomProperty { get { return this.userField; } }

    public static bool StaticProperty { get { return staticField; } }

    // Backing field of AutoProperty should also get ignored
    public bool AutoProperty { get; set; }
}