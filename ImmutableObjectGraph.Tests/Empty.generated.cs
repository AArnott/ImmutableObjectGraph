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
	
	public partial class Empty {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static readonly Empty DefaultInstance = GetDefaultTemplate();
		
		/// <summary>The last identity assigned to a created instance.</summary>
		private static int lastIdentityProduced;
	
		private readonly System.Int32 identity;
	
		/// <summary>Initializes a new instance of the Empty class.</summary>
		protected Empty(
			System.Int32 identity)
		{
			this.identity = identity;
			this.Validate();
		}
	
		public static Empty Create() {
			var identity = Optional.For(NewIdentity());
			return DefaultInstance;
		}
	
		protected internal System.Int32 Identity {
			get { return this.identity; }
		}
	
		/// <summary>Returns a unique identity that may be assigned to a newly created instance.</summary>
		protected static System.Int32 NewIdentity() {
			return System.Threading.Interlocked.Increment(ref lastIdentityProduced);
		}
	
		/// <summary>Normalizes and/or validates all properties on this object.</summary>
		/// <exception type="ArgumentException">Thrown if any properties have disallowed values.</exception>
		partial void Validate();
	
		/// <summary>Provides defaults for fields.</summary>
		/// <param name="template">The struct to set default values on.</param>
		static partial void CreateDefaultTemplate(ref Template template);
	
		/// <summary>Returns a newly instantiated Empty whose fields are initialized with default values.</summary>
		private static Empty GetDefaultTemplate() {
			var template = new Template();
			CreateDefaultTemplate(ref template);
			return new Empty(
				default(System.Int32));
		}
	
		/// <summary>A struct with all the same fields as the containing type for use in describing default values for new instances of the class.</summary>
		private struct Template {	}
		
		internal static Empty CreateWithIdentity(
				ImmutableObjectGraph.Optional<System.Int32> identity = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			if (!identity.IsDefined) {
				identity = NewIdentity();
			}
		
			return DefaultInstance;
		}
		
		public virtual EmptyDerived ToEmptyDerived() {
			EmptyDerived that = this as EmptyDerived;
			if (that != null && this.GetType().IsEquivalentTo(typeof(EmptyDerived))) {
				return that;
			}
		
			return EmptyDerived.CreateWithIdentity(
				identity: this.Identity);
		}
		
		public virtual NotSoEmptyDerived ToNotSoEmptyDerived(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			NotSoEmptyDerived that = this as NotSoEmptyDerived;
			if (that != null && this.GetType().IsEquivalentTo(typeof(NotSoEmptyDerived))) {
				if ((!oneField.IsDefined || oneField.Value == that.OneField)) {
					return that;
				}
			}
		
			return NotSoEmptyDerived.CreateWithIdentity(
				identity: this.Identity,
				oneField: oneField);
		}
	}
	
	public partial class EmptyDerived : Empty {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static readonly EmptyDerived DefaultInstance = GetDefaultTemplate();
	
		/// <summary>Initializes a new instance of the EmptyDerived class.</summary>
		protected EmptyDerived(
			System.Int32 identity)
			: base(
				identity: identity)
		{
			this.Validate();
		}
	
		public static EmptyDerived Create() {
			var identity = Optional.For(NewIdentity());
			return DefaultInstance;
		}
	
		/// <summary>Normalizes and/or validates all properties on this object.</summary>
		/// <exception type="ArgumentException">Thrown if any properties have disallowed values.</exception>
		partial void Validate();
	
		/// <summary>Provides defaults for fields.</summary>
		/// <param name="template">The struct to set default values on.</param>
		static partial void CreateDefaultTemplate(ref Template template);
	
		/// <summary>Returns a newly instantiated EmptyDerived whose fields are initialized with default values.</summary>
		private static EmptyDerived GetDefaultTemplate() {
			var template = new Template();
			CreateDefaultTemplate(ref template);
			return new EmptyDerived(
				default(System.Int32));
		}
	
		/// <summary>A struct with all the same fields as the containing type for use in describing default values for new instances of the class.</summary>
		private struct Template {	}
		
		internal static EmptyDerived CreateWithIdentity(
				ImmutableObjectGraph.Optional<System.Int32> identity = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			if (!identity.IsDefined) {
				identity = NewIdentity();
			}
		
			return DefaultInstance;
		}
		
		public Empty ToEmpty() {
			return Empty.CreateWithIdentity(
				identity: this.Identity);
		}
	}
	
	public partial class NotSoEmptyDerived : Empty {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static readonly NotSoEmptyDerived DefaultInstance = GetDefaultTemplate();
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Boolean oneField;
	
		/// <summary>Initializes a new instance of the NotSoEmptyDerived class.</summary>
		protected NotSoEmptyDerived(
			System.Int32 identity,
			System.Boolean oneField)
			: base(
				identity: identity)
		{
			this.oneField = oneField;
			this.Validate();
		}
	
		public static NotSoEmptyDerived Create(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			var identity = Optional.For(NewIdentity());
			return DefaultInstance.WithFactory(
				oneField: Optional.For(oneField.GetValueOrDefault(DefaultInstance.OneField)),
				identity: Optional.For(identity.GetValueOrDefault(DefaultInstance.Identity)));
		}
	
		public System.Boolean OneField {
			get { return this.oneField; }
		}
		
		/// <summary>Returns a new instance with the OneField property set to the specified value.</summary>
		public NotSoEmptyDerived WithOneField(System.Boolean value) {
			if (value == this.OneField) {
				return this;
			}
		
			return this.With(oneField: Optional.For(value));
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		public NotSoEmptyDerived With(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			return (NotSoEmptyDerived)this.WithCore(
				oneField: oneField);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		protected virtual NotSoEmptyDerived WithCore(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			var identity = default(ImmutableObjectGraph.Optional<System.Int32>);
			return this.WithFactory(
				oneField: Optional.For(oneField.GetValueOrDefault(this.OneField)),
				identity: Optional.For(identity.GetValueOrDefault(this.Identity)));
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		private NotSoEmptyDerived WithFactory(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>),
			ImmutableObjectGraph.Optional<System.Int32> identity = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			if (
				(identity.IsDefined && identity.Value != this.Identity) || 
				(oneField.IsDefined && oneField.Value != this.OneField)) {
				return new NotSoEmptyDerived(
					identity: identity.GetValueOrDefault(this.Identity),
					oneField: oneField.GetValueOrDefault(this.OneField));
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
	
		/// <summary>Returns a newly instantiated NotSoEmptyDerived whose fields are initialized with default values.</summary>
		private static NotSoEmptyDerived GetDefaultTemplate() {
			var template = new Template();
			CreateDefaultTemplate(ref template);
			return new NotSoEmptyDerived(
				default(System.Int32), 
				template.OneField);
		}
	
		/// <summary>A struct with all the same fields as the containing type for use in describing default values for new instances of the class.</summary>
		private struct Template {
			internal System.Boolean OneField { get; set; }
		}
		
		internal static NotSoEmptyDerived CreateWithIdentity(
				ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>),
				ImmutableObjectGraph.Optional<System.Int32> identity = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			if (!identity.IsDefined) {
				identity = NewIdentity();
			}
		
			return DefaultInstance.WithFactory(
					oneField: Optional.For(oneField.GetValueOrDefault(DefaultInstance.OneField)),
					identity: Optional.For(identity.GetValueOrDefault(DefaultInstance.Identity)));
		}
		
		public Empty ToEmpty() {
			return Empty.CreateWithIdentity(
				identity: this.Identity);
		}
		
		public new Builder ToBuilder() {
			return new Builder(this);
		}
		
		public new partial class Builder {
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private NotSoEmptyDerived immutable;
		
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			protected System.Boolean oneField;
		
			internal Builder(NotSoEmptyDerived immutable) {
				this.immutable = immutable;
		
				this.oneField = immutable.OneField;
			}
		
			public System.Boolean OneField {
				get {
					return this.oneField;
				}
		
				set {
					this.oneField = value;
				}
			}
		
			public new NotSoEmptyDerived ToImmutable() {
				return this.immutable = this.immutable.With(
					ImmutableObjectGraph.Optional.For(this.OneField));
			}
		}
	}
	
	public partial class NonEmptyBase {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static readonly NonEmptyBase DefaultInstance = GetDefaultTemplate();
		
		/// <summary>The last identity assigned to a created instance.</summary>
		private static int lastIdentityProduced;
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Boolean oneField;
	
		private readonly System.Int32 identity;
	
		/// <summary>Initializes a new instance of the NonEmptyBase class.</summary>
		protected NonEmptyBase(
			System.Int32 identity,
			System.Boolean oneField)
		{
			this.identity = identity;
			this.oneField = oneField;
			this.Validate();
		}
	
		public static NonEmptyBase Create(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			var identity = Optional.For(NewIdentity());
			return DefaultInstance.WithFactory(
				oneField: Optional.For(oneField.GetValueOrDefault(DefaultInstance.OneField)),
				identity: Optional.For(identity.GetValueOrDefault(DefaultInstance.Identity)));
		}
	
		public System.Boolean OneField {
			get { return this.oneField; }
		}
		
		/// <summary>Returns a new instance with the OneField property set to the specified value.</summary>
		public NonEmptyBase WithOneField(System.Boolean value) {
			if (value == this.OneField) {
				return this;
			}
		
			return this.With(oneField: Optional.For(value));
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		public NonEmptyBase With(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			return (NonEmptyBase)this.WithCore(
				oneField: oneField);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		protected virtual NonEmptyBase WithCore(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			var identity = default(ImmutableObjectGraph.Optional<System.Int32>);
			return this.WithFactory(
				oneField: Optional.For(oneField.GetValueOrDefault(this.OneField)),
				identity: Optional.For(identity.GetValueOrDefault(this.Identity)));
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		private NonEmptyBase WithFactory(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>),
			ImmutableObjectGraph.Optional<System.Int32> identity = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			if (
				(identity.IsDefined && identity.Value != this.Identity) || 
				(oneField.IsDefined && oneField.Value != this.OneField)) {
				return new NonEmptyBase(
					identity: identity.GetValueOrDefault(this.Identity),
					oneField: oneField.GetValueOrDefault(this.OneField));
			} else {
				return this;
			}
		}
	
		protected internal System.Int32 Identity {
			get { return this.identity; }
		}
	
		/// <summary>Returns a unique identity that may be assigned to a newly created instance.</summary>
		protected static System.Int32 NewIdentity() {
			return System.Threading.Interlocked.Increment(ref lastIdentityProduced);
		}
	
		/// <summary>Normalizes and/or validates all properties on this object.</summary>
		/// <exception type="ArgumentException">Thrown if any properties have disallowed values.</exception>
		partial void Validate();
	
		/// <summary>Provides defaults for fields.</summary>
		/// <param name="template">The struct to set default values on.</param>
		static partial void CreateDefaultTemplate(ref Template template);
	
		/// <summary>Returns a newly instantiated NonEmptyBase whose fields are initialized with default values.</summary>
		private static NonEmptyBase GetDefaultTemplate() {
			var template = new Template();
			CreateDefaultTemplate(ref template);
			return new NonEmptyBase(
				default(System.Int32), 
				template.OneField);
		}
	
		/// <summary>A struct with all the same fields as the containing type for use in describing default values for new instances of the class.</summary>
		private struct Template {
			internal System.Boolean OneField { get; set; }
		}
		
		internal static NonEmptyBase CreateWithIdentity(
				ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>),
				ImmutableObjectGraph.Optional<System.Int32> identity = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			if (!identity.IsDefined) {
				identity = NewIdentity();
			}
		
			return DefaultInstance.WithFactory(
					oneField: Optional.For(oneField.GetValueOrDefault(DefaultInstance.OneField)),
					identity: Optional.For(identity.GetValueOrDefault(DefaultInstance.Identity)));
		}
		
		public virtual EmptyDerivedFromNonEmptyBase ToEmptyDerivedFromNonEmptyBase() {
			EmptyDerivedFromNonEmptyBase that = this as EmptyDerivedFromNonEmptyBase;
			if (that != null && this.GetType().IsEquivalentTo(typeof(EmptyDerivedFromNonEmptyBase))) {
				return that;
			}
		
			return EmptyDerivedFromNonEmptyBase.CreateWithIdentity(
				oneField: Optional.For(this.OneField),
				identity: this.Identity);
		}
		
		public Builder ToBuilder() {
			return new Builder(this);
		}
		
		public partial class Builder {
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private NonEmptyBase immutable;
		
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			protected System.Boolean oneField;
		
			internal Builder(NonEmptyBase immutable) {
				this.immutable = immutable;
		
				this.oneField = immutable.OneField;
			}
		
			public System.Boolean OneField {
				get {
					return this.oneField;
				}
		
				set {
					this.oneField = value;
				}
			}
		
			public NonEmptyBase ToImmutable() {
				return this.immutable = this.immutable.With(
					ImmutableObjectGraph.Optional.For(this.OneField));
			}
		}
	}
	
	public partial class EmptyDerivedFromNonEmptyBase : NonEmptyBase {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static readonly EmptyDerivedFromNonEmptyBase DefaultInstance = GetDefaultTemplate();
	
		/// <summary>Initializes a new instance of the EmptyDerivedFromNonEmptyBase class.</summary>
		protected EmptyDerivedFromNonEmptyBase(
			System.Int32 identity,
			System.Boolean oneField)
			: base(
				identity: identity,
				oneField: oneField)
		{
			this.Validate();
		}
	
		public static EmptyDerivedFromNonEmptyBase Create(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			var identity = Optional.For(NewIdentity());
			return DefaultInstance.WithFactory(
				oneField: Optional.For(oneField.GetValueOrDefault(DefaultInstance.OneField)),
				identity: Optional.For(identity.GetValueOrDefault(DefaultInstance.Identity)));
		}
		
		/// <summary>Returns a new instance with the OneField property set to the specified value.</summary>
		public new EmptyDerivedFromNonEmptyBase WithOneField(System.Boolean value) {
			return (EmptyDerivedFromNonEmptyBase)base.WithOneField(value);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		protected override NonEmptyBase WithCore(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			return this.WithFactory(
				oneField: oneField);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		public EmptyDerivedFromNonEmptyBase With(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			return (EmptyDerivedFromNonEmptyBase)this.WithCore(
				oneField: oneField);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		private EmptyDerivedFromNonEmptyBase WithFactory(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>),
			ImmutableObjectGraph.Optional<System.Int32> identity = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			if (
				(identity.IsDefined && identity.Value != this.Identity) || 
				(oneField.IsDefined && oneField.Value != this.OneField)) {
				return new EmptyDerivedFromNonEmptyBase(
					identity: identity.GetValueOrDefault(this.Identity),
					oneField: oneField.GetValueOrDefault(this.OneField));
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
	
		/// <summary>Returns a newly instantiated EmptyDerivedFromNonEmptyBase whose fields are initialized with default values.</summary>
		private static EmptyDerivedFromNonEmptyBase GetDefaultTemplate() {
			var template = new Template();
			CreateDefaultTemplate(ref template);
			return new EmptyDerivedFromNonEmptyBase(
				default(System.Int32), 
				template.OneField);
		}
	
		/// <summary>A struct with all the same fields as the containing type for use in describing default values for new instances of the class.</summary>
		private struct Template {
			internal System.Boolean OneField { get; set; }
		}
		
		internal static EmptyDerivedFromNonEmptyBase CreateWithIdentity(
				ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>),
				ImmutableObjectGraph.Optional<System.Int32> identity = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			if (!identity.IsDefined) {
				identity = NewIdentity();
			}
		
			return DefaultInstance;
		}
		
		public NonEmptyBase ToNonEmptyBase() {
			return NonEmptyBase.CreateWithIdentity(
				oneField: Optional.For(this.OneField),
				identity: this.Identity);
		}
		
		public new Builder ToBuilder() {
			return new Builder(this);
		}
		
		public new partial class Builder : NonEmptyBase.Builder {
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private EmptyDerivedFromNonEmptyBase immutable;
		
			internal Builder(EmptyDerivedFromNonEmptyBase immutable) : base(immutable) {
				this.immutable = immutable;
		
			}
		
			public new EmptyDerivedFromNonEmptyBase ToImmutable() {
				return this.immutable = this.immutable;
			}
		}
	}
	
	public abstract partial class AbstractNonEmpty {
		
		/// <summary>The last identity assigned to a created instance.</summary>
		private static int lastIdentityProduced;
	
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly System.Boolean oneField;
	
		private readonly System.Int32 identity;
	
		/// <summary>Initializes a new instance of the AbstractNonEmpty class.</summary>
		protected AbstractNonEmpty(
			System.Int32 identity,
			System.Boolean oneField)
		{
			this.identity = identity;
			this.oneField = oneField;
		}
	
		public System.Boolean OneField {
			get { return this.oneField; }
		}
		
		/// <summary>Returns a new instance with the OneField property set to the specified value.</summary>
		public AbstractNonEmpty WithOneField(System.Boolean value) {
			if (value == this.OneField) {
				return this;
			}
		
			return this.With(oneField: Optional.For(value));
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		public AbstractNonEmpty With(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			return (AbstractNonEmpty)this.WithCore(
				oneField: oneField);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		protected abstract AbstractNonEmpty WithCore(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>));
	
		protected internal System.Int32 Identity {
			get { return this.identity; }
		}
	
		/// <summary>Returns a unique identity that may be assigned to a newly created instance.</summary>
		protected static System.Int32 NewIdentity() {
			return System.Threading.Interlocked.Increment(ref lastIdentityProduced);
		}
		
		public virtual EmptyDerivedFromAbstract ToEmptyDerivedFromAbstract() {
			EmptyDerivedFromAbstract that = this as EmptyDerivedFromAbstract;
			if (that != null && this.GetType().IsEquivalentTo(typeof(EmptyDerivedFromAbstract))) {
				return that;
			}
		
			return EmptyDerivedFromAbstract.CreateWithIdentity(
				oneField: Optional.For(this.OneField),
				identity: this.Identity);
		}
		
		public Builder ToBuilder() {
			return new Builder(this);
		}
		
		public partial class Builder {
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private AbstractNonEmpty immutable;
		
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			protected System.Boolean oneField;
		
			internal Builder(AbstractNonEmpty immutable) {
				this.immutable = immutable;
		
				this.oneField = immutable.OneField;
			}
		
			public System.Boolean OneField {
				get {
					return this.oneField;
				}
		
				set {
					this.oneField = value;
				}
			}
		
			public AbstractNonEmpty ToImmutable() {
				return this.immutable = this.immutable.With(
					ImmutableObjectGraph.Optional.For(this.OneField));
			}
		}
	}
	
	public partial class EmptyDerivedFromAbstract : AbstractNonEmpty {
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private static readonly EmptyDerivedFromAbstract DefaultInstance = GetDefaultTemplate();
	
		/// <summary>Initializes a new instance of the EmptyDerivedFromAbstract class.</summary>
		protected EmptyDerivedFromAbstract(
			System.Int32 identity,
			System.Boolean oneField)
			: base(
				identity: identity,
				oneField: oneField)
		{
			this.Validate();
		}
	
		public static EmptyDerivedFromAbstract Create(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			var identity = Optional.For(NewIdentity());
			return DefaultInstance.WithFactory(
				oneField: Optional.For(oneField.GetValueOrDefault(DefaultInstance.OneField)),
				identity: Optional.For(identity.GetValueOrDefault(DefaultInstance.Identity)));
		}
		
		/// <summary>Returns a new instance with the OneField property set to the specified value.</summary>
		public new EmptyDerivedFromAbstract WithOneField(System.Boolean value) {
			return (EmptyDerivedFromAbstract)base.WithOneField(value);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		protected override AbstractNonEmpty WithCore(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			return this.WithFactory(
				oneField: oneField);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		public EmptyDerivedFromAbstract With(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>)) {
			return (EmptyDerivedFromAbstract)this.WithCore(
				oneField: oneField);
		}
	
		/// <summary>Returns a new instance of this object with any number of properties changed.</summary>
		private EmptyDerivedFromAbstract WithFactory(
			ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>),
			ImmutableObjectGraph.Optional<System.Int32> identity = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			if (
				(identity.IsDefined && identity.Value != this.Identity) || 
				(oneField.IsDefined && oneField.Value != this.OneField)) {
				return new EmptyDerivedFromAbstract(
					identity: identity.GetValueOrDefault(this.Identity),
					oneField: oneField.GetValueOrDefault(this.OneField));
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
	
		/// <summary>Returns a newly instantiated EmptyDerivedFromAbstract whose fields are initialized with default values.</summary>
		private static EmptyDerivedFromAbstract GetDefaultTemplate() {
			var template = new Template();
			CreateDefaultTemplate(ref template);
			return new EmptyDerivedFromAbstract(
				default(System.Int32), 
				template.OneField);
		}
	
		/// <summary>A struct with all the same fields as the containing type for use in describing default values for new instances of the class.</summary>
		private struct Template {
			internal System.Boolean OneField { get; set; }
		}
		
		internal static EmptyDerivedFromAbstract CreateWithIdentity(
				ImmutableObjectGraph.Optional<System.Boolean> oneField = default(ImmutableObjectGraph.Optional<System.Boolean>),
				ImmutableObjectGraph.Optional<System.Int32> identity = default(ImmutableObjectGraph.Optional<System.Int32>)) {
			if (!identity.IsDefined) {
				identity = NewIdentity();
			}
		
			return DefaultInstance;
		}
		
		public new Builder ToBuilder() {
			return new Builder(this);
		}
		
		public new partial class Builder : AbstractNonEmpty.Builder {
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			private EmptyDerivedFromAbstract immutable;
		
			internal Builder(EmptyDerivedFromAbstract immutable) : base(immutable) {
				this.immutable = immutable;
		
			}
		
			public new EmptyDerivedFromAbstract ToImmutable() {
				return this.immutable = this.immutable;
			}
		}
	}
}


