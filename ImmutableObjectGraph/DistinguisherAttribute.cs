namespace ImmutableObjectGraph {
	using System;

	[System.AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public sealed class DistinguisherAttribute : Attribute {
		public DistinguisherAttribute() {
		}

		public string CollectionModifierMethodSuffix { get; set; }
	}
}
