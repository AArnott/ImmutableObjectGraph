namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Validation;

	public static class RecursiveTypeExtensions {
		/// <summary>Checks whether an object with the specified identity is among this object's descendents.</summary>
		public static bool HasDescendent(this IRecursiveParent parent, int identity) {
			Requires.NotNull(parent, "parent");

			IRecursiveType result;
			return parent.TryFind(identity, out result) && result != parent;
		}

		/// <summary>Checks whether a given object is among this object's descendents.</summary>
		public static bool HasDescendent(this IRecursiveParent parent, IRecursiveType possibleDescendent) {
			Requires.NotNull(parent, "parent");
			Requires.NotNull(possibleDescendent, "possibleDescendent");

			return HasDescendent(parent, possibleDescendent.Identity);
		}

		public static bool TryFind<TRecursiveParent, TRecursiveType>(this TRecursiveParent parent, int identity, out TRecursiveType value)
				where TRecursiveParent : class, IRecursiveParent, TRecursiveType
				where TRecursiveType : class, IRecursiveType {
			Requires.NotNullAllowStructs(parent, "parent");

			if (parent.Identity.Equals(identity)) {
				value = parent;
				return true;
			}

			var parentWithLookup = parent as IRecursiveParentWithFastLookup;
			if (parentWithLookup != null) {
				KeyValuePair<IRecursiveType, int> lookupValue;
				if (parentWithLookup.TryLookup(identity, out lookupValue)) {
					value = (TRecursiveType)lookupValue.Key;
					return lookupValue.Key != null;
				}
			}

			// No lookup table (or a failed lookup) means we have to exhaustively search each child and its descendents.
			foreach (var child in parent.Children) {
				var recursiveChild = child as TRecursiveParent;
				if (recursiveChild != null) {
					if (recursiveChild.TryFind(identity, out value)) {
						return true;
					}
				} else {
					if (child.Identity.Equals(identity)) {
						value = (TRecursiveType)child;
						return true;
					}
				}
			}

			value = null;
			return false;
		}

		public static bool TryFindImmediateChild<TRecursiveParent, TRecursiveType>(this TRecursiveParent parent, int identity, out TRecursiveType value)
			where TRecursiveParent : class, IRecursiveParent, TRecursiveType
			where TRecursiveType : class, IRecursiveType {
			Requires.NotNullAllowStructs(parent, "parent");

			var parentWithLookup = parent as IRecursiveParentWithFastLookup;
			if (parentWithLookup != null) {
				KeyValuePair<IRecursiveType, int> lookupValue;
				if (parentWithLookup.TryLookup(identity, out lookupValue)) {
					if (lookupValue.Value == parent.Identity) {
						value = (TRecursiveType)lookupValue.Key;
						return lookupValue.Key != null;
					} else {
						// It isn't an immediate child. 
						value = default(TRecursiveType);
						return false;
					}
				}
			}

			// No lookup table means we have to exhaustively search each child.
			foreach (var child in parent.Children) {
				if (child.Identity.Equals(identity)) {
					value = (TRecursiveType)child;
					return true;
				}
			}

			value = null;
			return false;
		}

		public static TRecursiveType Find<TRecursiveParent, TRecursiveType>(this TRecursiveParent parent, int identity)
			where TRecursiveParent : class, IRecursiveParent, TRecursiveType
			where TRecursiveType : class, IRecursiveType {
			Requires.NotNullAllowStructs(parent, "parent");

			TRecursiveType result;
			if (parent.TryFind(identity, out result)) {
				return result;
			}

			throw new KeyNotFoundException();
		}

		/// <summary>
		/// Returns a sequence starting with the given <paramref name="root"/>
		/// followed by its descendents in a depth-first search.
		/// </summary>
		/// <param name="root">The node at which to start enumeration.</param>
		/// <returns>A sequence of nodes beginning with <paramref name="root"/> and including all descendents.</returns>
		public static IEnumerable<TRecursiveType> GetSelfAndDescendents<TRecursiveType>(this IRecursiveParent<TRecursiveType> root)
			where TRecursiveType : IRecursiveType {
			return GetSelfAndDescendents<TRecursiveType>((TRecursiveType)root);
		}

		/// <summary>
		/// Returns a sequence starting with the given <paramref name="root"/>
		/// followed by its descendents in a depth-first search.
		/// </summary>
		/// <param name="root">The node at which to start enumeration.</param>
		/// <returns>A sequence of nodes beginning with <paramref name="root"/> and including all descendents.</returns>
		public static IEnumerable<TRecursiveType> GetSelfAndDescendents<TRecursiveType>(this TRecursiveType root)
			where TRecursiveType : IRecursiveType {
			yield return root;

			var rootAsParent = root as IRecursiveParent<TRecursiveType>;
			if (rootAsParent != null && rootAsParent.Children != null) {
				foreach (TRecursiveType child in rootAsParent.Children) {
					var childAsParent = child as IRecursiveParent<TRecursiveType>;
					foreach (var descendent in childAsParent.GetSelfAndDescendents()) {
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
		public static IEnumerable<TRecursiveType> GetSelfAndDescendentsBreadthFirst<TRecursiveType>(this IRecursiveParent<TRecursiveType> root)
			where TRecursiveType : IRecursiveType {
			var nodesToVisit = new Queue<TRecursiveType>();
			nodesToVisit.Enqueue((TRecursiveType)root);

			while (nodesToVisit.Count > 0) {
				var visiting = nodesToVisit.Dequeue();
				yield return visiting;

				var visitingAsParent = visiting as IRecursiveParent;
				if (visitingAsParent != null && visitingAsParent.Children != null) {
					foreach (TRecursiveType child in visitingAsParent.Children) {
						nodesToVisit.Enqueue(child);
					}
				}
			}
		}

		public static IEnumerable<ParentedRecursiveType<TRecursiveParent, TRecursiveType>> GetSelfAndDescendentsWithParents<TRecursiveParent, TRecursiveType>(this TRecursiveParent root, TRecursiveParent parent = default(TRecursiveParent))
			where TRecursiveParent : class, IRecursiveParent<TRecursiveType>
			where TRecursiveType : class, IRecursiveType {
			IRecursiveType rootAsRecursiveType = root;
			yield return new ParentedRecursiveType<TRecursiveParent, TRecursiveType>((TRecursiveType)rootAsRecursiveType, parent);

			var rootAsParent = root as TRecursiveParent;
			if (rootAsParent != null && rootAsParent.Children != null) {
				foreach (TRecursiveType child in rootAsParent.Children) {
					var childAsParent = child as TRecursiveParent;
					foreach (var descendent in childAsParent.GetSelfAndDescendentsWithParents<TRecursiveParent, TRecursiveType>(rootAsParent)) {
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

			var currentAsParent = current as IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>;
			var currentAsRecursiveType = (IRecursiveDiffingType<TPropertiesEnum, TDiffGram>)current;

			var before = new HashSet<ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
			var after = new HashSet<ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());

			var priorVersionAsParent = priorVersion as IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>;
			if (priorVersionAsParent != null) {
				before.UnionWith(priorVersionAsParent.GetSelfAndDescendentsWithParents<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
			} else {
				before.Add(priorVersion.WithParent());
			}

			if (currentAsParent != null) {
				after.UnionWith(currentAsParent.GetSelfAndDescendentsWithParents<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
			} else {
				after.Add(current.WithParent());
			}

			var added = new HashSet<ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
			var removed = new HashSet<ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());
			var changed = new Dictionary<ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>>(Comparers.Parented<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>());

			var descendentsOfAddOrRemove = new HashSet<IRecursiveType>(Comparers.Identity);

			foreach (var fromBefore in before) {
				if (after.Contains(fromBefore)) {
					ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> fromAfter;
					if (currentAsParent != null) {
						var parent = currentAsParent.GetParentedNode(fromBefore.Value.Identity);
						fromAfter = new ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>(
							(IRecursiveDiffingType<TPropertiesEnum, TDiffGram>)parent.Value,
							(IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>)parent.Parent);
					} else {
						fromAfter = new ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>(
							fromBefore.Value.Identity == current.Identity ? (IRecursiveDiffingType<TPropertiesEnum, TDiffGram>)current : null);
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
				var parent = (IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>)topLevelOperation.Value;
				descendentsOfAddOrRemove.UnionWith(parent.GetSelfAndDescendents().Skip(1));
			}

			var history = new List<TDiffGram>();
			history.AddRange(removed.Where(r => !descendentsOfAddOrRemove.Contains(r.Value)).Select(r => currentAsRecursiveType.Remove(r.Value)));

			foreach (var changedNode in changed) {
				var oldNode = changedNode.Key;
				var newNode = changedNode.Value;

				var diff = newNode.DiffProperties(oldNode);
				if (!currentAsRecursiveType.Equals(diff, default(TPropertiesEnum))) {
					history.Add(currentAsRecursiveType.Change(oldNode.Value, newNode.Value, diff));
				}
			}

			history.AddRange(added.Where(a => !descendentsOfAddOrRemove.Contains(a.Value)).Select(a => currentAsRecursiveType.Add(a.Value)));

			return history;
		}

		public static TPropertiesEnum DiffProperties<TPropertiesEnum, TDiffGram>(this ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> self, ParentedRecursiveType<IRecursiveParent<IRecursiveDiffingType<TPropertiesEnum, TDiffGram>>, IRecursiveDiffingType<TPropertiesEnum, TDiffGram>> other)
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
						var otherParentOrdered = (IRecursiveParentWithOrderedChildren)other.Parent;
						int beforeIndex = otherParentOrdered.IndexOf(other.Value);
						int afterIndex = selfParentOrdered.IndexOf(self.Value);

						// TODO: review (and add tests for) cases where items are inserted/removed from the parent,
						// causing all other items after it to apparently shift in position. We probably don't want
						// to consider that a change in PositionUnderParent, since the removal or add will take care of it.
						if (afterIndex != beforeIndex) {
							changes = self.Value.Union(changes, self.Value.PositionUnderParentProperty);
						}
					}
				}
			}

			return changes;
		}

		private static ParentedRecursiveType<IRecursiveParent<TRecursiveType>, TRecursiveType> WithParent<TRecursiveType>(this TRecursiveType value, IRecursiveParent<TRecursiveType> parent = null)
			where TRecursiveType : IRecursiveType {
			return new ParentedRecursiveType<IRecursiveParent<TRecursiveType>, TRecursiveType>(value, parent);
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
