namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	/// <summary>
	/// A description of a change made to an instance of an immutable object.
	/// </summary>
	public struct DiffGram<T, TPropertiesEnum> {
		private DiffGram(T before, T after, ChangeKind kind, TPropertiesEnum changes)
			: this() {
			this.Before = before;
			this.After = after;
			this.Kind = kind;
			this.Changes = changes;
		}

		public static DiffGram<T, TPropertiesEnum> Change(T before, T after, TPropertiesEnum changes) {
			return new DiffGram<T, TPropertiesEnum>(before, after, ChangeKind.Replaced, changes);
		}

		public static DiffGram<T, TPropertiesEnum> Add(T value) {
			return new DiffGram<T, TPropertiesEnum>(default(T), value, ChangeKind.Added, default(TPropertiesEnum));
		}

		public static DiffGram<T, TPropertiesEnum> Remove(T value) {
			return new DiffGram<T, TPropertiesEnum>(value, default(T), ChangeKind.Removed, default(TPropertiesEnum));
		}

		/// <summary>
		/// Gets the leaf node before the change.
		/// </summary>
		public T Before { get; private set; }

		/// <summary>
		/// Gets the leaf node after the change.
		/// </summary>
		public T After { get; private set; }

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
