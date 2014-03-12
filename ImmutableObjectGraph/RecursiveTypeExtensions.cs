namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Validation;

	public static class RecursiveTypeExtensions {

		public static IEnumerable<IRecursiveType> GetSelfAndDescendents(this IRecursiveType root) {
			yield return root;

			var rootAsParent = root as IRecursiveParent;
			if (rootAsParent != null && rootAsParent.Children != null) {
				foreach (var child in rootAsParent.Children) {
					foreach (var descendent in child.GetSelfAndDescendents()) {
						yield return descendent;
					}
				}
			}
		}

		/// <summary>
		/// Walks a project tree starting at a given node, using a breadth-first search pattern.
		/// </summary>
		/// <param name="root">The node at which to begin the search.</param>
		/// <returns>A sequence of the given node, and all its descendents.  The given node always comes first in the sequence.</returns>
		public static IEnumerable<IRecursiveType> GetSelfAndDescendentsBreadthFirst(this IRecursiveType root) {
			var nodesToVisit = new Queue<IRecursiveType>();
			nodesToVisit.Enqueue(root);

			while (nodesToVisit.Count > 0) {
				var visiting = nodesToVisit.Dequeue();
				yield return visiting;

				var visitingAsParent = visiting as IRecursiveParent;
				if (visitingAsParent != null && visitingAsParent.Children != null) {
					foreach (var child in visitingAsParent.Children) {
						nodesToVisit.Enqueue(child);
					}
				}
			}
		}

		public static IEnumerable<ParentedRecursiveType<TRecursiveParent, TRecursiveType>> GetSelfAndDescendentsWithParents<TRecursiveParent, TRecursiveType>(this TRecursiveType root, TRecursiveParent parent = default(TRecursiveParent))
			where TRecursiveParent : class, IRecursiveParent
			where TRecursiveType : class, IRecursiveType {
			yield return new ParentedRecursiveType<TRecursiveParent, TRecursiveType>(root, parent);

			var rootAsParent = root as TRecursiveParent;
			if (rootAsParent != null && rootAsParent.Children != null) {
				foreach (TRecursiveType child in rootAsParent.Children) {
					foreach (var descendent in child.GetSelfAndDescendentsWithParents(rootAsParent)) {
						yield return descendent;
					}
				}
			}
		}

		public static IReadOnlyList<TDiffGram> ChangesSince<TPropertiesEnum, TDiffGram>(this IRecursiveDiffingType<TPropertiesEnum, TDiffGram> current, IRecursiveDiffingType<TPropertiesEnum, TDiffGram> priorVersion)
			where TPropertiesEnum : struct
			where TDiffGram : struct {
			Requires.NotNull(current, "current");
			Requires.NotNull(priorVersion, "priorVersion");

			if (current == priorVersion) {
				return System.Collections.Immutable.ImmutableList.Create<TDiffGram>();
			}

			if (priorVersion.Identity != current.Identity) {
				throw new System.ArgumentException("Not another version of the same node.", "priorVersion");
			}

			var currentAsParent = current as IRecursiveParent;

			var before = new System.Collections.Generic.HashSet<ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(priorVersion.GetSelfAndDescendentsWithParents<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>(), Comparers.Parented<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
			var after = new System.Collections.Generic.HashSet<ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(current.GetSelfAndDescendentsWithParents<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>(), Comparers.Parented<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());

			var added = new System.Collections.Generic.HashSet<ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
			var removed = new System.Collections.Generic.HashSet<ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
			var changed = new System.Collections.Generic.Dictionary<ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());

			var descendentsOfAddOrRemove = new System.Collections.Generic.HashSet<IRecursiveType>(Comparers.Identity);

			foreach (var fromBefore in before) {
				if (after.Contains(fromBefore)) {
					ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> fromAfter;
					if (currentAsParent != null) {
						var parent = currentAsParent.GetParentedNode(fromBefore.Value.Identity);
						fromAfter = new ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>((IRecursiveDiffingType<TPropertiesEnum, TDiffGram>)parent.Value, parent.Parent);
					} else {
						fromAfter = new ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>(fromBefore.Value.Identity == current.Identity ? current : null);
					}

					if (!object.ReferenceEquals(fromBefore.Value, fromAfter.Value) || fromBefore.Parent.Identity != fromAfter.Parent.Identity) {
						changed.Add(fromBefore, fromAfter);
					}
				} else {
					removed.Add(fromBefore);
				}
			}

			foreach (var fromAfter in after) {
				if (!before.Contains(fromAfter)) {
					added.Add(fromAfter);
				}
			}

			foreach (var topLevelOperation in added.Concat(removed)) {
				descendentsOfAddOrRemove.UnionWith(topLevelOperation.Value.GetSelfAndDescendents().Skip(1));
			}

			var history = new System.Collections.Generic.List<TDiffGram>();
			history.AddRange(removed.Where(r => !descendentsOfAddOrRemove.Contains(r.Value)).Select(r => current.Remove(r.Value)));

			foreach (var changedNode in changed) {
				var oldNode = changedNode.Key;
				var newNode = changedNode.Value;

				var diff = newNode.DiffProperties(oldNode);
				if (!current.Equals(diff, default(TPropertiesEnum))) {
					history.Add(current.Change(oldNode.Value, newNode.Value, diff));
				}
			}

			history.AddRange(added.Where(a => !descendentsOfAddOrRemove.Contains(a.Value)).Select(a => current.Add(a.Value)));

			return history;
		}

		public static TPropertiesEnum DiffProperties<TPropertiesEnum, TDiffGram>(this ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> self, ParentedRecursiveType<IRecursiveParent, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> other)
			where TPropertiesEnum : struct {
			TPropertiesEnum changes = self.Value.DiffProperties(other.Value);
			if ((self.Parent == null ^ other.Parent == null) || (self.Parent != null && other.Parent != null && self.Parent.Identity != other.Parent.Identity)) {
				changes = self.Value.Union(changes, self.Value.ParentProperty);
			} else if (self.Value != other.Value && self.Parent != null && other.Parent != null) {
				var selfParentOrdered = self.Parent as IRecursiveParentWithOrderedChildren;
				if (selfParentOrdered != null) {
					var selfParentSorted = selfParentOrdered as IRecursiveParentWithSortedChildren;
					if (selfParentSorted != null) {
						if (selfParentSorted.Compare(self.Value, other.Value) != 0) {
							// Calculate where the node was, and where it would go in the old tree.
							var otherParentSorted = (IRecursiveParentWithSortedChildren)other.Parent;
							int beforeIndex = otherParentSorted.IndexOf(other.Value);
							int afterIndex = ~otherParentSorted.IndexOf(self.Value);

							// If the indices are the same, the new one would come "just before" the old one.
							// If the new index is just 1 greater than the old index, the new one would come "just after" the old one.
							// In either of these cases, since the old one will be gone in the new tree, the position hasn't changed.
							if (afterIndex != beforeIndex && afterIndex != beforeIndex + 1) {
								changes = self.Value.Union(changes, self.Value.PositionUnderParentProperty);
							}
						}
					} else {
						// Calculate whether items were reordered without leveraging a sorting comparer.
						throw new NotImplementedException();
					}
				}
			}

			return changes;
		}

		public static void Write(this IRecursiveParent root, TextWriter writer) {
			Requires.NotNull(root, "root");
			Requires.NotNull(writer, "writer");
			const string Indent = "  ";

			writer.Write(root);

			writer.NewLine += Indent;
			foreach (var child in root.Children) {
				writer.WriteLine();
				var childAsParent = child as IRecursiveParent;
				if (childAsParent != null) {
					Write(childAsParent, writer);
				} else {
					writer.Write(child);
				}
			}

			writer.NewLine = writer.NewLine.Substring(0, writer.NewLine.Length - Indent.Length);
		}
	}
}
