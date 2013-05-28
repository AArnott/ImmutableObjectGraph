namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using Validation;

	partial class ProjectTree {
		public bool Contains(int identity) {
			return this.Find(identity) != null;
		}

		/// <summary>
		/// Walks a project tree starting at a given node, using a breadth-first search pattern.
		/// </summary>
		/// <param name="root">The node at which to begin the search.</param>
		/// <returns>A sequence of the given node, and all its descendents.  The given node always comes first in the sequence.</returns>
		public IEnumerable<ProjectTree> GetSelfAndDescendentsBreadthFirst() {
			var nodesToVisit = new Queue<ProjectTree>();
			nodesToVisit.Enqueue(this);

			while (nodesToVisit.Count > 0) {
				var visiting = nodesToVisit.Dequeue();
				yield return visiting;
				foreach (var child in visiting.Children) {
					nodesToVisit.Enqueue(child);
				}
			}
		}

		public static IReadOnlyList<DiffGram<ProjectTree, ProjectTreeChangedProperties>> GetDelta(ProjectTree before, ProjectTree after) {
			return after.ChangesSince(before);
		}
	}
}
