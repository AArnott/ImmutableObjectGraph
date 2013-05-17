namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	/// <summary>
	/// A double-ended queue.
	/// </summary>
	/// <typeparam name="T">The type of element stored by the deque.</typeparam>
	/// <remarks>
	/// A rather inefficient way to deliver on our lookup table fixup stack requirements.
	/// </remarks>
	public class ImmutableDeque<T> : IEnumerable<T> {
		internal static readonly ImmutableDeque<T> Empty = new ImmutableDeque<T>(ImmutableList.Create<T>());

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		private readonly ImmutableList<T> contents;

		internal ImmutableDeque(ImmutableList<T> contents) {
			this.contents = contents;
		}

		public ImmutableDeque<T> Clear() {
			return Empty;
		}

		public bool IsEmpty {
			get { return this.Count == 0; }
		}

		public int Count {
			get { return this.contents.Count; }
		}

		public T PeekHead() {
			return this.contents[0];
		}

		public T PeekTail() {
			return this.contents[this.Count - 1];
		}

		public ImmutableDeque<T> DequeueFront() {
			return new ImmutableDeque<T>(this.contents.RemoveAt(0));
		}

		public ImmutableDeque<T> DequeueBack() {
			return new ImmutableDeque<T>(this.contents.RemoveAt(this.Count - 1));
		}

		public ImmutableDeque<T> EnqueueFront(T value) {
			return new ImmutableDeque<T>(this.contents.Insert(0, value));
		}

		public ImmutableDeque<T> EnqueueBack(T value) {
			return new ImmutableDeque<T>(this.contents.Add(value));
		}

		public IEnumerator<T> GetEnumerator() {
			return this.contents.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return this.GetEnumerator();
		}
	}
}
