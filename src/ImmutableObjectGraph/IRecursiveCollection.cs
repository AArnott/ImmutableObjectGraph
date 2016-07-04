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
		IReadOnlyCollection<IRecursiveType> Children { get; }

		ParentedRecursiveType<IRecursiveParent<IRecursiveType>, IRecursiveType> GetParentedNode(IdentityFieldType identity);
	}

	public interface IRecursiveParent<out TRecursiveType> : IRecursiveParent
		where TRecursiveType : IRecursiveType {
		new IReadOnlyCollection<TRecursiveType> Children { get; }
	}

    public interface IRecursiveParentWithChildReplacement<TRecursiveType> : IRecursiveParent
        where TRecursiveType : IRecursiveType
    {
        IRecursiveParent<TRecursiveType> ReplaceChild(ImmutableStack<TRecursiveType> oldSpine, ImmutableStack<TRecursiveType> newSpine);
    }

	public interface IRecursiveParentWithOrderedChildren : IRecursiveParent {
        new IReadOnlyList<IRecursiveType> Children { get; }

        int IndexOf(IRecursiveType value);
	}

	public interface IRecursiveParentWithSortedChildren : IRecursiveParentWithOrderedChildren {
        int Compare(IRecursiveType first, IRecursiveType second);
	}

    public interface IRecursiveParentWithLookupTable<TRecursiveType> : IRecursiveParent<TRecursiveType>
        where TRecursiveType : IRecursiveType
    {
        uint InefficiencyLoad { get; }

        new IReadOnlyCollection<TRecursiveType> Children { get; }

        ImmutableDictionary<IdentityFieldType, KeyValuePair<TRecursiveType, IdentityFieldType>> LookupTable { get; }
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
