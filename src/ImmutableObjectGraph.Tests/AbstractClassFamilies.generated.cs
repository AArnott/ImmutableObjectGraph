﻿// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     ImmutableTree Version: 0.0.0.1
//  
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------

namespace ImmutableObjectGraph.Tests {
	using System.Diagnostics;
	using System.Linq;
	using ImmutableObjectGraph;
	
	public abstract partial class Abstract1 {
		
		/// <summary>The last identity assigned to a created instance.</summary>
		private static int lastIdentityProduced;
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Int32 abstract1Field1;
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Int32 abstract1Field2;
	
		private readonly System.UInt32 identity;
	
		/// <summary>Initializes a new instance of the Abstract1 class.</summary>
		protected Abstract1(
			System.UInt32 identity,
			System.Int32 abstract1Field1,
			System.Int32 abstract1Field2,
			ImmutableObjectGraph.Optional<bool> skipValidation = default(ImmutableObjectGraph.Optional<bool>))
		{
			this.identity = identity;
			this.abstract1Field1 = abstract1Field1;
			this.abstract1Field2 = abstract1Field2;
		}
	
		public System.Int32 Abstract1Field1 {
			get { return this.abstract1Field1; }
		}
	
		public System.Int32 Abstract1Field2 {
			get { return this.abstract1Field2; }
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		public Abstract1 With(
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			return (Abstract1)this.WithCore(
				abstract1Field1: abstract1Field1,
				abstract1Field2: abstract1Field2);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		protected abstract Abstract1 WithCore(
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>));
	
		protected internal uint Identity {
			get { return (uint)this.identity; }
		}
	
		/// <summary>Returns a unique identity that may be assigned to a newly created instance.</summary>
		protected static System.UInt32 NewIdentity() {
			return (System.UInt32)System.Threading.Interlocked.Increment(ref lastIdentityProduced);
		}
		
		public virtual ConcreteOf2Abstracts ToConcreteOf2Abstracts(
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField2 = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			ConcreteOf2Abstracts that = this as ConcreteOf2Abstracts;
			if (that != null && this.GetType().IsEquivalentTo(typeof(ConcreteOf2Abstracts))) {
				if ((!abstract2Field1.IsDefined || abstract2Field1.Value == that.Abstract2Field1) && 
				    (!abstract2Field2.IsDefined || abstract2Field2.Value == that.Abstract2Field2) && 
				    (!concreteField1.IsDefined || concreteField1.Value == that.ConcreteField1) && 
				    (!concreteField2.IsDefined || concreteField2.Value == that.ConcreteField2)) {
					return that;
				}
			}
		
			return ConcreteOf2Abstracts.CreateWithIdentity(
				abstract1Field1: Optional.For(this.Abstract1Field1),
				abstract1Field2: Optional.For(this.Abstract1Field2),
				identity: this.Identity,
				abstract2Field1: abstract2Field1,
				abstract2Field2: abstract2Field2,
				concreteField1: concreteField1,
				concreteField2: concreteField2);
		}
	}
	
	public abstract partial class Abstract2 : Abstract1 {
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Int32 abstract2Field1;
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Int32 abstract2Field2;
	
		/// <summary>Initializes a new instance of the Abstract2 class.</summary>
		protected Abstract2(
			System.UInt32 identity,
			System.Int32 abstract1Field1,
			System.Int32 abstract1Field2,
			System.Int32 abstract2Field1,
			System.Int32 abstract2Field2,
			ImmutableObjectGraph.Optional<bool> skipValidation = default(ImmutableObjectGraph.Optional<bool>))
			: base(
				identity: identity,
				abstract1Field1: abstract1Field1,
				abstract1Field2: abstract1Field2)
		{
			this.abstract2Field1 = abstract2Field1;
			this.abstract2Field2 = abstract2Field2;
		}
	
		public System.Int32 Abstract2Field1 {
			get { return this.abstract2Field1; }
		}
	
		public System.Int32 Abstract2Field2 {
			get { return this.abstract2Field2; }
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		public Abstract2 With(
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field2 = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			return (Abstract2)this.WithCore(
				abstract1Field1: abstract1Field1,
				abstract1Field2: abstract1Field2,
				abstract2Field1: abstract2Field1,
				abstract2Field2: abstract2Field2);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		protected abstract Abstract2 WithCore(
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field2 = default(ImmutableObjectGraph.Optional<System.Int32>));
		
		public virtual ConcreteOf2Abstracts ToConcreteOf2Abstracts(
			ImmutableObjectGraph.Optional<System.Int32> concreteField1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField2 = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			ConcreteOf2Abstracts that = this as ConcreteOf2Abstracts;
			if (that != null && this.GetType().IsEquivalentTo(typeof(ConcreteOf2Abstracts))) {
				if ((!concreteField1.IsDefined || concreteField1.Value == that.ConcreteField1) && 
				    (!concreteField2.IsDefined || concreteField2.Value == that.ConcreteField2)) {
					return that;
				}
			}
		
			return ConcreteOf2Abstracts.CreateWithIdentity(
				abstract1Field1: Optional.For(this.Abstract1Field1),
				abstract1Field2: Optional.For(this.Abstract1Field2),
				abstract2Field1: Optional.For(this.Abstract2Field1),
				abstract2Field2: Optional.For(this.Abstract2Field2),
				identity: this.Identity,
				concreteField1: concreteField1,
				concreteField2: concreteField2);
		}
		
		public override ConcreteOf2Abstracts ToConcreteOf2Abstracts(
				ImmutableObjectGraph.Optional<System.Int32> abstract2Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
				ImmutableObjectGraph.Optional<System.Int32> abstract2Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
				ImmutableObjectGraph.Optional<System.Int32> concreteField1 = default(ImmutableObjectGraph.Optional<System.Int32>),
				ImmutableObjectGraph.Optional<System.Int32> concreteField2 = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			return base.ToConcreteOf2Abstracts(
					abstract2Field1: Optional.For(abstract2Field1.GetValueOrDefault(this.Abstract2Field1)),
					abstract2Field2: Optional.For(abstract2Field2.GetValueOrDefault(this.Abstract2Field2)),
					concreteField1: concreteField1,
					concreteField2: concreteField2);
		}
	}
	
	public partial class ConcreteOf2Abstracts : Abstract2 {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static readonly ConcreteOf2Abstracts DefaultInstance = GetDefaultTemplate();
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Int32 concreteField1;
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Int32 concreteField2;
	
		/// <summary>Initializes a new instance of the ConcreteOf2Abstracts class.</summary>
		protected ConcreteOf2Abstracts(
			System.UInt32 identity,
			System.Int32 abstract1Field1,
			System.Int32 abstract1Field2,
			System.Int32 abstract2Field1,
			System.Int32 abstract2Field2,
			System.Int32 concreteField1,
			System.Int32 concreteField2,
			ImmutableObjectGraph.Optional<bool> skipValidation = default(ImmutableObjectGraph.Optional<bool>))
			: base(
				identity: identity,
				abstract1Field1: abstract1Field1,
				abstract1Field2: abstract1Field2,
				abstract2Field1: abstract2Field1,
				abstract2Field2: abstract2Field2)
		{
			this.concreteField1 = concreteField1;
			this.concreteField2 = concreteField2;
			if (!skipValidation.Value) {
				this.Validate();
			}
		}
	
		public static ConcreteOf2Abstracts Create(
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField2 = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			var identity = Optional.For(NewIdentity());
			return DefaultInstance.WithFactory(
				abstract1Field1: Optional.For(abstract1Field1.GetValueOrDefault(DefaultInstance.Abstract1Field1)),
				abstract1Field2: Optional.For(abstract1Field2.GetValueOrDefault(DefaultInstance.Abstract1Field2)),
				abstract2Field1: Optional.For(abstract2Field1.GetValueOrDefault(DefaultInstance.Abstract2Field1)),
				abstract2Field2: Optional.For(abstract2Field2.GetValueOrDefault(DefaultInstance.Abstract2Field2)),
				concreteField1: Optional.For(concreteField1.GetValueOrDefault(DefaultInstance.ConcreteField1)),
				concreteField2: Optional.For(concreteField2.GetValueOrDefault(DefaultInstance.ConcreteField2)),
				identity: Optional.For(identity.GetValueOrDefault(DefaultInstance.Identity)));
		}
	
		public System.Int32 ConcreteField1 {
			get { return this.concreteField1; }
		}
	
		public System.Int32 ConcreteField2 {
			get { return this.concreteField2; }
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		protected override Abstract1 WithCore(
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			return this.WithFactory(
				abstract1Field1: abstract1Field1,
				abstract1Field2: abstract1Field2);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		protected override Abstract2 WithCore(
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field2 = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			return this.WithFactory(
				abstract1Field1: abstract1Field1,
				abstract1Field2: abstract1Field2,
				abstract2Field1: abstract2Field1,
				abstract2Field2: abstract2Field2);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		public ConcreteOf2Abstracts With(
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField2 = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			return (ConcreteOf2Abstracts)this.WithCore(
				abstract1Field1: abstract1Field1,
				abstract1Field2: abstract1Field2,
				abstract2Field1: abstract2Field1,
				abstract2Field2: abstract2Field2,
				concreteField1: concreteField1,
				concreteField2: concreteField2);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		protected virtual ConcreteOf2Abstracts WithCore(
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField2 = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			var identity = default(ImmutableObjectGraph.Optional<System.UInt32>);
			return this.WithFactory(
				abstract1Field1: Optional.For(abstract1Field1.GetValueOrDefault(this.Abstract1Field1)),
				abstract1Field2: Optional.For(abstract1Field2.GetValueOrDefault(this.Abstract1Field2)),
				abstract2Field1: Optional.For(abstract2Field1.GetValueOrDefault(this.Abstract2Field1)),
				abstract2Field2: Optional.For(abstract2Field2.GetValueOrDefault(this.Abstract2Field2)),
				concreteField1: Optional.For(concreteField1.GetValueOrDefault(this.ConcreteField1)),
				concreteField2: Optional.For(concreteField2.GetValueOrDefault(this.ConcreteField2)),
				identity: Optional.For(identity.GetValueOrDefault(this.Identity)));
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		private ConcreteOf2Abstracts WithFactory(
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> abstract2Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField1 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.Int32> concreteField2 = default(ImmutableObjectGraph.Optional<System.Int32>),
			ImmutableObjectGraph.Optional<System.UInt32> identity = default(ImmutableObjectGraph.Optional<System.UInt32>)) {
			if (
				(identity.IsDefined && identity.Value != this.Identity) || 
				(abstract1Field1.IsDefined && abstract1Field1.Value != this.Abstract1Field1) || 
				(abstract1Field2.IsDefined && abstract1Field2.Value != this.Abstract1Field2) || 
				(abstract2Field1.IsDefined && abstract2Field1.Value != this.Abstract2Field1) || 
				(abstract2Field2.IsDefined && abstract2Field2.Value != this.Abstract2Field2) || 
				(concreteField1.IsDefined && concreteField1.Value != this.ConcreteField1) || 
				(concreteField2.IsDefined && concreteField2.Value != this.ConcreteField2)) {
				return new ConcreteOf2Abstracts(
					identity: identity.GetValueOrDefault(this.Identity),
					abstract1Field1: abstract1Field1.GetValueOrDefault(this.Abstract1Field1),
					abstract1Field2: abstract1Field2.GetValueOrDefault(this.Abstract1Field2),
					abstract2Field1: abstract2Field1.GetValueOrDefault(this.Abstract2Field1),
					abstract2Field2: abstract2Field2.GetValueOrDefault(this.Abstract2Field2),
					concreteField1: concreteField1.GetValueOrDefault(this.ConcreteField1),
					concreteField2: concreteField2.GetValueOrDefault(this.ConcreteField2));
			} else {
				return this;
			}
		}
	
		/// <summary>Normalizes and/or validates all properties on this object.</summary>
		/// <exception type="ArgumentException">Thrown if any properties have disallowed values.</exception>
		partial void Validate();
	
		/// <summary>Provides defaults for fields.</summary>
		/// <param name="template">The struct to set default values on.</param>
		static partial void CreateDefaultTemplate(ref Template template);
	
		/// <summary>Returns a newly instantiated ConcreteOf2Abstracts whose fields are initialized with default values.</summary>
		private static ConcreteOf2Abstracts GetDefaultTemplate() {
			var template = new Template();
			CreateDefaultTemplate(ref template);
			return new ConcreteOf2Abstracts(
				default(System.UInt32),
				template.Abstract1Field1,
				template.Abstract1Field2,
				template.Abstract2Field1,
				template.Abstract2Field2,
				template.ConcreteField1,
				template.ConcreteField2,
				skipValidation: true);
		}
	
		/// <summary>A struct with all the same fields as the containing type for use in describing default values for new instances of the class.</summary>
		private struct Template {
			internal System.Int32 Abstract1Field1 { get; set; }
	
			internal System.Int32 Abstract1Field2 { get; set; }
	
			internal System.Int32 Abstract2Field1 { get; set; }
	
			internal System.Int32 Abstract2Field2 { get; set; }
	
			internal System.Int32 ConcreteField1 { get; set; }
	
			internal System.Int32 ConcreteField2 { get; set; }
		}
		
		internal static ConcreteOf2Abstracts CreateWithIdentity(
				ImmutableObjectGraph.Optional<System.Int32> abstract1Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
				ImmutableObjectGraph.Optional<System.Int32> abstract1Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
				ImmutableObjectGraph.Optional<System.Int32> abstract2Field1 = default(ImmutableObjectGraph.Optional<System.Int32>),
				ImmutableObjectGraph.Optional<System.Int32> abstract2Field2 = default(ImmutableObjectGraph.Optional<System.Int32>),
				ImmutableObjectGraph.Optional<System.Int32> concreteField1 = default(ImmutableObjectGraph.Optional<System.Int32>),
				ImmutableObjectGraph.Optional<System.Int32> concreteField2 = default(ImmutableObjectGraph.Optional<System.Int32>),
				ImmutableObjectGraph.Optional<System.UInt32> identity = default(ImmutableObjectGraph.Optional<System.UInt32>)) {
			if (!identity.IsDefined) {
				identity = NewIdentity();
			}
		
			return DefaultInstance.WithFactory(
					abstract1Field1: Optional.For(abstract1Field1.GetValueOrDefault(DefaultInstance.Abstract1Field1)),
					abstract1Field2: Optional.For(abstract1Field2.GetValueOrDefault(DefaultInstance.Abstract1Field2)),
					abstract2Field1: Optional.For(abstract2Field1.GetValueOrDefault(DefaultInstance.Abstract2Field1)),
					abstract2Field2: Optional.For(abstract2Field2.GetValueOrDefault(DefaultInstance.Abstract2Field2)),
					concreteField1: Optional.For(concreteField1.GetValueOrDefault(DefaultInstance.ConcreteField1)),
					concreteField2: Optional.For(concreteField2.GetValueOrDefault(DefaultInstance.ConcreteField2)),
					identity: Optional.For(identity.GetValueOrDefault(DefaultInstance.Identity)));
		}
	}
}

