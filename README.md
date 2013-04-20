ImmutableObjectGraph
=======================

This project hosts a [T4 template][1] that makes writing immutable objects
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

To reduce the burden of writing and maintaining such codebases, the
T4 templates found in this project generate the code for immutable objects
for you based on a template mutable class that you supply. 

Supported features
------------------
Currently, the T4 template transformation supports a small subset of C#
features being present on the mutable template type that otherwise might
appear in a type in C#. 

 * Only fields are supported. 
 * Field types may be value or reference types.
 * When field types are collections, immutable collections should be used that
   support the Builder pattern.
 * When field types refer to other types also defined in the mutable template
   file, multiple immutable object types are defined in the generated file.
   In this way, an entire library of immutable classes with members that
   reference each other can be constructed.
 * Batch property changes can be made with a single allocation using a single
   invocation of the **With** method.
 * Builder classes are generated to allow efficient multi-step mutation
   without producing unnecessary GC pressure.

Usage
-----
This project is a sample. Its suggested use is to copy the
`ImmutableObjectGraph.tt` file to your own project and include it into your own
T4 template as demonstrated in the `Demo\Fruit.tt` or 
`ImmutableObjectGraph.Tests\Person.tt` files.

Example
-------
A live example can be seen by comparing the source file `Fruit.tt` to the 
generated output found in `Fruit.generated.cs`. Both of these files are in 
this project.

For example, the T4 transformation can take the following mutable template class:

    class Fruit {
        string color;
        int skinThickness;
    }

And generate code such as:

    public partial class Fruit {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Fruit DefaultInstance = new Fruit();
    
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
    
        public static Fruit Default {
            get { return DefaultInstance; }
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
            System.String color = default(System.String), 
            System.Int32 skinThickness = default(System.Int32),
            bool defaultColor = false,
            bool defaultSkinThickness = false) {
            return new Fruit(
                    defaultColor ? default(System.String) : (color != default(System.String) ? color : this.Color),
                    defaultSkinThickness ? default(System.Int32) : (skinThickness != default(System.Int32) ? skinThickness : this.SkinThickness));
        }
    
        public Builder ToBuilder() {
            return new Builder(this);
        }
    
        /// <summary>Normalizes and/or validates all properties on this object.</summary>
        /// <exception type="ArgumentException">Thrown if any properties have disallowed values.</exception>
        partial void Validate();
    
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
                    if (this.color != value) {
                        this.color = value;
                        this.immutable = null;
                    }
                }
            }
    
            public System.Int32 SkinThickness {
                get {
                    return this.skinThickness;
                }
    
                set {
                    if (this.skinThickness != value) {
                        this.skinThickness = value;
                        this.immutable = null;
                    }
                }
            }
    
            public Fruit ToImmutable() {
                if (this.immutable == null) {
                    this.immutable = Fruit.Default.With(
                        this.color,
                        this.skinThickness);
                }
    
                return this.immutable;
            }
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

  [1]: http://www.bing.com/search?setmkt=en-US&q=visual+studio+t4