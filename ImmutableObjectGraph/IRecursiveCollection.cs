namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Validation;

	public interface IRecursiveType {
		int Identity { get; }
	}

	public interface IRecursiveParent : IRecursiveType {
		IEnumerable<IRecursiveType> Children { get; }

		ParentedRecursiveType<IRecursiveParent, IRecursiveType> GetParentedNode(int identity);
	}

	public interface IRecursiveParentWithOrderedChildren : IRecursiveParent {
		int IndexOf(IRecursiveType value);
	}

	public interface IRecursiveParentWithSortedChildren : IRecursiveParentWithOrderedChildren {
		int Compare(IRecursiveType first, IRecursiveType second);
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
		where TRecursiveParent : IRecursiveParent
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
