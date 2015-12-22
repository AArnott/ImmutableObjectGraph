ImmutableObjectGraph
=======================

[![Build status](https://ci.appveyor.com/api/projects/status/sc0w4vlceulc2try?svg=true)](https://ci.appveyor.com/project/AArnott/immutableobjectgraph)
[![NuGet package](https://img.shields.io/nuget/v/ImmutableObjectGraph.svg)](https://nuget.org/packages/ImmutableObjectGraph)
[![Join the chat at https://gitter.im/AArnott/ImmutableObjectGraph](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/AArnott/ImmutableObjectGraph?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

This project offers code generation that makes writing immutable objects
much easier. For instance, the following mutable class:

```csharp
public class Fruit {
    public string Color { get; set; }
    public int SkinThickness { get; set; }
}
```

Is very short, easily written and maintainable. The equivalent immutable
type would require methods offering mutation, and ideally several other
support methods and even a Builder class for use in conveniently handling
the immutable object when mutation by creating new objects may be required.
These codebases for immutable objects can be quite large.

To reduce the burden of writing and maintaining such codebases, this project
generates immutable types for you based on a minimal definition of a class
that you define. 

Supported features
------------------

 * Field types may be value or reference types.
 * When field types are collections, immutable collections should be used that
   support the Builder pattern.
 * When field types refer to other types also defined in the template
   file, an entire library of immutable classes with members that
   reference each other can be constructed.
 * Batch property changes can be made with a single allocation using a single
   invocation of the `With` method.
 * Builder classes are generated to allow efficient multi-step mutation
   without producing unnecessary GC pressure.
 * Version across time without breaking changes by adding Create and With method
   overloads with an easy application of `[Generation(2)]`.

Usage
-----
You can begin using this project by simply installing a NuGet package:

    Install-Package ImmutableObjectGraph.Generation -Pre

On any source file that you use the `[GenerateImmutable]` attribute in,
set the Custom Tool property to: `MSBuild:GenerateCodeFromAttributes`

## Example source file

```csharp
[GenerateImmutable]
partial class Fruit
{
    readonly string color;
    readonly int skinThickness;
}
```

## Example generated code

The following code will be generated automatically for you and added to a source file
in your intermediate outputs folder:

```csharp
partial class Fruit
{
    [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
    private static readonly Fruit DefaultInstance = GetDefaultTemplate();
    private static int lastIdentityProduced;
    [System.Diagnostics.DebuggerBrowsableAttribute(System.Diagnostics.DebuggerBrowsableState.Never)]
    private readonly uint identity;
    protected Fruit(uint identity, System.String color, System.Int32 skinThickness, bool skipValidation)
    {
        this.identity = identity;
        this.color = color;
        this.skinThickness = skinThickness;
        if (!skipValidation)
        {
            this.Validate();
        }
    }

    public string Color
    {
        get
        {
            return this.color;
        }
    }

    public int SkinThickness
    {
        get
        {
            return this.skinThickness;
        }
    }

    internal protected uint Identity
    {
        get
        {
            return this.identity;
        }
    }

    public static Fruit Create(ImmutableObjectGraph.Optional<System.String> color = default(ImmutableObjectGraph.Optional<System.String>), ImmutableObjectGraph.Optional<System.Int32> skinThickness = default(ImmutableObjectGraph.Optional<System.Int32>))
    {
        var identity = ImmutableObjectGraph.Optional.For(NewIdentity());
        return DefaultInstance.WithFactory(color: ImmutableObjectGraph.Optional.For(color.GetValueOrDefault(DefaultInstance.Color)), skinThickness: ImmutableObjectGraph.Optional.For(skinThickness.GetValueOrDefault(DefaultInstance.SkinThickness)), identity: identity);
    }

    public Fruit With(ImmutableObjectGraph.Optional<System.String> color = default(ImmutableObjectGraph.Optional<System.String>), ImmutableObjectGraph.Optional<System.Int32> skinThickness = default(ImmutableObjectGraph.Optional<System.Int32>))
    {
        return (Fruit)this.WithCore(color: color, skinThickness: skinThickness);
    }

    static protected uint NewIdentity()
    {
        return (uint)System.Threading.Interlocked.Increment(ref lastIdentityProduced);
    }

    protected virtual Fruit WithCore(ImmutableObjectGraph.Optional<System.String> color = default(ImmutableObjectGraph.Optional<System.String>), ImmutableObjectGraph.Optional<System.Int32> skinThickness = default(ImmutableObjectGraph.Optional<System.Int32>))
    {
        return this.WithFactory(color: ImmutableObjectGraph.Optional.For(color.GetValueOrDefault(this.Color)), skinThickness: ImmutableObjectGraph.Optional.For(skinThickness.GetValueOrDefault(this.SkinThickness)), identity: ImmutableObjectGraph.Optional.For(this.Identity));
    }

    static partial void CreateDefaultTemplate(ref Template template);
    private static Fruit GetDefaultTemplate()
    {
        var template = new Template();
        CreateDefaultTemplate(ref template);
        return new Fruit(default(uint), template.Color, template.SkinThickness, skipValidation: true);
    }

    partial void Validate();
    private Fruit WithFactory(ImmutableObjectGraph.Optional<System.String> color = default(ImmutableObjectGraph.Optional<System.String>), ImmutableObjectGraph.Optional<System.Int32> skinThickness = default(ImmutableObjectGraph.Optional<System.Int32>), ImmutableObjectGraph.Optional<uint> identity = default(ImmutableObjectGraph.Optional<uint>))
    {
        if ((identity.IsDefined && identity.Value != this.Identity) || (color.IsDefined && color.Value != this.Color) || (skinThickness.IsDefined && skinThickness.Value != this.SkinThickness))
        {
            return new Fruit(identity: identity.GetValueOrDefault(this.Identity), color: color.GetValueOrDefault(this.Color), skinThickness: skinThickness.GetValueOrDefault(this.SkinThickness), skipValidation: false);
        }
        else
        {
            return this;
        }
    }

#pragma warning disable 649 // field initialization is optional in user code

    private struct Template
    {
        internal System.String Color;
        internal System.Int32 SkinThickness;
    }
#pragma warning restore 649
}
```

The integration of the code generator support in Visual Studio allows for you to
conveniently maintain your own code, and on every save or build of that file,
the code generator runs and automatically creates or updates the generated partial
class. 

Known Issues
------------

When defining more than one immutable type, you may need to keep the arguments
to the `[GenerateImmutable]` attribute consistent for every type. The generator
currently assumes that every type has the same arguments as every other type
and as a result, for example, generating a Builder from one type and referencing
another type, that other type will be assumed to also have a Builder even when
it does not, leading to compiler errors.

[RoslynGenNuPkg]: https://www.nuget.org/packages/immutableobjectgraph.generation
