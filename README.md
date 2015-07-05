ImmutableObjectGraph
=======================

[![Join the chat at https://gitter.im/AArnott/ImmutableObjectGraph](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/AArnott/ImmutableObjectGraph?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![Build status](https://ci.appveyor.com/api/projects/status/sc0w4vlceulc2try?svg=true)](https://ci.appveyor.com/project/AArnott/immutableobjectgraph)

This project offers code generation that makes writing immutable objects
much easier. For instance, the following mutable class:

    public class Fruit {
        public string Color { get; set; }
        public int SkinThickness { get; set; }
    }

Is very short, easily written and maintainable. The equivalent immutable
type would require methods offering mutation, and ideally several other
support methods and even a Builder class for use in conveniently handling
the immutable object when mutation by creating new objects may be required.
These codebases for immutable objects can be quite large.

To reduce the burden of writing and maintaining such codebases, this project
generates immutable types for you based on a minimal definition of a class
that you define. This is done using either Roslyn or the (deprecated)
[T4 templates][T4].

Supported features
------------------
Currently, the T4 template transformation supports a small subset of C#
features being present on the mutable template type that otherwise might
appear in a type in C#. 

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

Limitations unique to the T4 templates if you use them:

 * Only fields are supported. 

Usage
-----
You can begin using this project by simply installing a NuGet package.
The recommended package is [ImmutableObjectGraph.Generation][RoslynGenNuPkg]
which uses Roslyn for its code generation:

    Install-Package ImmutableObjectGraph.Generation -Pre

The deprecated T4 templates may be used instead by installing the
[ImmutableObjectGraph.T4][T4GenNuPkg] NuGet package:

    Install-Package ImmutableObjectGraph.T4 -Pre

Example
-------

### Roslyn code generation

TODO

### T4 templates
A live example can be seen by comparing the source file `Message.tt` to the 
generated output found in `Message.generated.cs`. Both of these files are in 
this project.

For example, the T4 transformation can take the following mutable template class:

    class Fruit {
        string color;
        int skinThickness;
    }

And generate code such as:

    public partial class Fruit {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Fruit DefaultInstance = GetDefaultTemplate();
    
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly System.String color;
    
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly System.Int32 skinThickness;
    
        /// <summary>Initializes a new instance of the Fruit class.</summary>
        private Fruit()
        {
        }
    
        /// <summary>Initializes a new instance of the Fruit class.</summary>
        private Fruit(System.String color, System.Int32 skinThickness)
        {
            this.color = color;
            this.skinThickness = skinThickness;
            this.Validate();
        }
    
        public static Fruit Create(
            ImmutableObjectGraph.Optional<System.String> color = default(ImmutableObjectGraph.Optional<System.String>), 
            ImmutableObjectGraph.Optional<System.Int32> skinThickness = default(ImmutableObjectGraph.Optional<System.Int32>)) {
            return DefaultInstance.With(
                color.IsDefined ? color : ImmutableObjectGraph.Optional.For(DefaultInstance.color), 
                skinThickness.IsDefined ? skinThickness : ImmutableObjectGraph.Optional.For(DefaultInstance.skinThickness));
        }
    
        public System.String Color {
            get { return this.color; }
        }
    
        public Fruit WithColor(System.String value) {
            if (value == this.Color) {
                return this;
            }
    
            return new Fruit(value, this.SkinThickness);
        }
    
        public System.Int32 SkinThickness {
            get { return this.skinThickness; }
        }
    
        public Fruit WithSkinThickness(System.Int32 value) {
            if (value == this.SkinThickness) {
                return this;
            }
    
            return new Fruit(this.Color, value);
        }
    
        /// <summary>Returns a new instance of this object with any number of properties changed.</summary>
        public Fruit With(
            ImmutableObjectGraph.Optional<System.String> color = default(ImmutableObjectGraph.Optional<System.String>), 
            ImmutableObjectGraph.Optional<System.Int32> skinThickness = default(ImmutableObjectGraph.Optional<System.Int32>)) {
            if (
                (color.IsDefined && color.Value != this.Color) || 
                (skinThickness.IsDefined && skinThickness.Value != this.SkinThickness)) {
                return new Fruit(
                    color.IsDefined ? color.Value : this.Color,
                    skinThickness.IsDefined ? skinThickness.Value : this.SkinThickness);
            } else {
                return this;
            }
        }
    
        public Builder ToBuilder() {
            return new Builder(this);
        }
    
        /// <summary>Normalizes and/or validates all properties on this object.</summary>
        /// <exception type="ArgumentException">Thrown if any properties have disallowed values.</exception>
        partial void Validate();
    
        /// <summary>Provides defaults for fields.</summary>
        /// <param name="template">The struct to set default values on.</param>
        static partial void CreateDefaultTemplate(ref Template template);
    
        /// <summary>Returns a newly instantiated Fruit whose fields are initialized with default values.</summary>
        private static Fruit GetDefaultTemplate() {
            var template = new Template();
            CreateDefaultTemplate(ref template);
            return new Fruit(
                template.Color, 
                template.SkinThickness);
        }
    
        public partial class Builder {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private Fruit immutable;
    
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private System.String color;
    
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private System.Int32 skinThickness;
    
            internal Builder(Fruit immutable) {
                this.immutable = immutable;
    
                this.color = immutable.Color;
                this.skinThickness = immutable.SkinThickness;
            }
    
            public System.String Color {
                get {
                    return this.color;
                }
    
                set {
                    this.color = value;
                }
            }
    
            public System.Int32 SkinThickness {
                get {
                    return this.skinThickness;
                }
    
                set {
                    this.skinThickness = value;
                }
            }
    
            public Fruit ToImmutable() {
                return this.immutable = this.immutable.With(
                    ImmutableObjectGraph.Optional.For(this.color),
                    ImmutableObjectGraph.Optional.For(this.skinThickness));
            }
        }
    
        /// <summary>A struct with all the same fields as the containing type for use in describing default values for new instances of the class.</summary>
        private struct Template {
            internal System.String Color { get; set; }
    
            internal System.Int32 SkinThickness { get; set; }
        }
    }


The integration of T4 template support in Visual Studio allows for you to
conveniently maintain your mutable template class, and on every Save
of that file, the code generator runs and automatically updates your
immutable class. 

The generated code uses *partial* classes, so that you may supply additional
behavior on the generated types within your project in separate source files
so that your additions do not get reverted with each run of the code
generator.

  [T4]: http://www.bing.com/search?setmkt=en-US&q=visual+studio+t4
  [T4GenNuPkg]: https://www.nuget.org/packages/immutableobjectgraph.t4
  [RoslynGenNuPkg]: https://www.nuget.org/packages/immutableobjectgraph.generation
  
