﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ImmutableTree Version: 0.0.0.1
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

namespace ConsoleApplication9 {
	using System.Diagnostics;

	
	public partial class Basket {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static readonly Basket DefaultInstance = new Basket();
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Int32 size;
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Collections.Immutable.ImmutableList<Fruit> contents;
	
		/// <summary>Initializes a new instance of the Basket class.</summary>
		private Basket()
		{
		}
	
		/// <summary>Initializes a new instance of the Basket class.</summary>
		private Basket(System.Int32 size, System.Collections.Immutable.ImmutableList<Fruit> contents)
		{
			this.size = size;
			this.contents = contents;
			this.Validate();
		}
	
		public static Basket Default {
			get { return DefaultInstance; }
		}
	
		public System.Int32 Size {
			get { return this.size; }
		}
	
		public Basket WithSize(System.Int32 value) {
			if (value == this.Size) {
				return this;
			}
	
			return new Basket(value, this.Contents);
		}
	
		public System.Collections.Immutable.ImmutableList<Fruit> Contents {
			get { return this.contents; }
		}
	
		public Basket WithContents(System.Collections.Immutable.ImmutableList<Fruit> value) {
			if (value == this.Contents) {
				return this;
			}
	
			return new Basket(this.Size, value);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		public Basket With(
			System.Int32 size = default(System.Int32), 
			System.Collections.Immutable.ImmutableList<Fruit> contents = default(System.Collections.Immutable.ImmutableList<Fruit>),
			bool resetSize = false,
			bool resetContents = false) {
			return new Basket(
					resetSize ? default(System.Int32) : (size != default(System.Int32) ? size : this.Size),
					resetContents ? default(System.Collections.Immutable.ImmutableList<Fruit>) : (contents != default(System.Collections.Immutable.ImmutableList<Fruit>) ? contents : this.Contents));
		}
	
		public Builder ToBuilder() {
			return new Builder {
				Size = this.Size,
				Contents = this.Contents,
			};
		}
	
		/// <summary>Normalizes and/or validates all properties on this object.</summary>
		/// <exception type="ArgumentException">Thrown if any properties have disallowed values.</exception>
		partial void Validate();
	
		public partial class Builder {
			internal Builder() {
			}
	
			public System.Int32 Size { get; set; }
			public System.Collections.Immutable.ImmutableList<Fruit> Contents { get; set; }
	
			public Basket ToImmutable() {
				return Basket.Default.With(
					this.Size,
					this.Contents);
			}
		}
	}
	
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
			bool resetColor = false,
			bool resetSkinThickness = false) {
			return new Fruit(
					resetColor ? default(System.String) : (color != default(System.String) ? color : this.Color),
					resetSkinThickness ? default(System.Int32) : (skinThickness != default(System.Int32) ? skinThickness : this.SkinThickness));
		}
	
		public Builder ToBuilder() {
			return new Builder {
				Color = this.Color,
				SkinThickness = this.SkinThickness,
			};
		}
	
		/// <summary>Normalizes and/or validates all properties on this object.</summary>
		/// <exception type="ArgumentException">Thrown if any properties have disallowed values.</exception>
		partial void Validate();
	
		public partial class Builder {
			internal Builder() {
			}
	
			public System.String Color { get; set; }
			public System.Int32 SkinThickness { get; set; }
	
			public Fruit ToImmutable() {
				return Fruit.Default.With(
					this.Color,
					this.SkinThickness);
			}
		}
	}
}

