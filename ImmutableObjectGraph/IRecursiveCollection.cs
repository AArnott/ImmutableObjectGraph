namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Validation;
    using IdentityFieldType = System.UInt32;

	public interface IRecursiveType {
        IdentityFieldType Identity { get; }
	}

	public interface IRecursiveParent : IRecursiveType {
		IEnumerable<IRecursiveType> Children { get; }

		ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType> GetParentedNode(IdentityFieldType identity);
	}

	public interface IRecursiveParent<out TRecursiveType> : IRecursiveParent
		where TRecursiveType : IRecursiveType {
		new IEnumerable<TRecursiveType> Children { get; }
	}

	public interface IRecursiveParentWithOrderedChildren : IRecursiveParent {
		int IndexOf(IRecursiveType value);
	}

	public interface IRecursiveParentWithSortedChildren : IRecursiveParentWithOrderedChildren {
		int Compare(IRecursiveType first, IRecursiveType second);
	}

	public interface IRecursiveParentWithFastLookup : IRecursiveParent {
		/// <summary>
		/// Tries to lookup a descendent node by its identity in a fast lookup table.
		/// </summary>
		/// <param name="identity">The identity of the descendent node to find.</param>
		/// <param name="result">Receives a reference to the sought object and the identity of its immediate parent, if the lookup table exists and the entry is found.</param>
		/// <returns><c>true</c> if the lookup table exists; <c>false</c> otherwise.</returns>
		/// <remarks>
		/// Note that a return value of <c>false</c> does not mean a matching descendent does not exist.
		/// It merely means that no fast lookup table has been initialized.
		/// If the return value is <c>true</c>, then the lookup table exists and the <paramref name="result"/>
		/// will either be empty or non-empty based on the presence of the descendent.
		/// </remarks>
		bool TryLookup(IdentityFieldType identity, out KeyValuePair<IRecursiveType, IdentityFieldType> result);
	}

	public interface IRecursiveDiffingType<TPropertiesEnum, TDiffGram> : IRecursiveType {
		TPropertiesEnum ParentProperty { get; }

		TPropertiesEnum PositionUnderParentProperty { get; }

		TPropertiesEnum DiffProperties(IRecursiveType other);

		TDiffGram Change(IRecursiveType before, IRecursiveType after, TPropertiesEnum diff);

		TDiffGram Add(IRecursiveType after);

		TDiffGram Remove(IRecursiveType before);

		bool Equals(TPropertiesEnum first, TPropertiesEnum second);

		TPropertiesEnum Union(TPropertiesEnum first, TPropertiesEnum second);
	}

	public struct ParentedRecursiveType<TRecursiveParent, TRecursiveType>
		where TRecursiveParent : IRecursiveParent<TRecursiveType>
		where TRecursiveType : IRecursiveType {
		public ParentedRecursiveType(TRecursiveType value, TRecursiveParent parent = default(TRecursiveParent))
			: this() {
			this.Value = value;
			this.Parent = parent;
		}

		public TRecursiveType Value { get; private set; }

		public TRecursiveParent Parent { get; private set; }
	}
}
