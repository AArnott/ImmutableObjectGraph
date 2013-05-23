namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	/// <summary>
	/// A description of a change made to an instance of an immutable object.
	/// </summary>
	public struct DiffGram<TPropertiesEnum> {
		public DiffGram(int identity, ChangeKind kind, TPropertiesEnum changes)
			: this() {
			this.Identity = identity;
			this.Kind = kind;
			this.Changes = changes;
		}

		/// <summary>
		/// Gets the leaf node impacted by this change.
		/// </summary>
		public int Identity { get; private set; }

		/// <summary>
		/// Gets the kind of change made to the alterered node.
		/// </summary>
		public ChangeKind Kind { get; private set; }

		/// <summary>
		/// Gets the kinds of changes made to node if <see cref="Kind"/> is <see cref="ChangeKind.Replaced"/>.
		/// </summary>
		public TPropertiesEnum Changes { get; private set; }
	}
}
