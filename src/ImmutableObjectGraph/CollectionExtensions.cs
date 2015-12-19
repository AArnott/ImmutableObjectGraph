namespace ImmutableObjectGraph {
	using System.Collections.Generic;
	using System.Collections.Immutable;

	using System.Linq;

	public static class CollectionExtensions {
		public static ImmutableSortedSet<T> ResetContents<T>(this ImmutableSortedSet<T> set, IEnumerable<T> values) {
			return set.SetEquals(values) ? set : set.Clear().Union(values);
		}

		public static ImmutableHashSet<T> ResetContents<T>(this ImmutableHashSet<T> set, IEnumerable<T> values) {
			return set.SetEquals(values) ? set : set.Clear().Union(values);
		}

		public static ImmutableSortedSet<T> AddRange<T>(this ImmutableSortedSet<T> set, IEnumerable<T> values) {
			return set.Union(values);
		}

		public static ImmutableHashSet<T> AddRange<T>(this ImmutableHashSet<T> set, IEnumerable<T> values) {
			return set.Union(values);
		}

		public static ImmutableSortedSet<T> RemoveRange<T>(this ImmutableSortedSet<T> set, IEnumerable<T> values) {
			return set.Except(values);
		}

		public static ImmutableHashSet<T> RemoveRange<T>(this ImmutableHashSet<T> set, IEnumerable<T> values) {
			return set.Except(values);
		}

		public static ImmutableList<T> ResetContents<T>(this ImmutableList<T> list, IEnumerable<T> values) {
			return list.SequenceEqual(values) ? list : list.Clear().AddRange(values);
		}

		public static ImmutableArray<T> ResetContents<T>(this ImmutableArray<T> array, IEnumerable<T> values) {
			return array.SequenceEqual(values) ? array : array.Clear().AddRange(values);
		}

		public static ImmutableSortedSet<T> Replace<T>(this ImmutableSortedSet<T> set, T oldValue, T newValue) {
			var alteredSet = set.Remove(oldValue);
			return alteredSet != set ? alteredSet.Add(newValue) : set;
		}

		public static IEnumerable<T> Concat<T>(this T element, IEnumerable<T> sequence) {
			return new[] { element }.Concat(sequence);
		}

		public static IEnumerable<T> Concat<T>(this IEnumerable<T> sequence, T element) {
			return sequence.Concat(new[] { element });
		}
	}
}
