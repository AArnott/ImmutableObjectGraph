namespace ImmutableObjectGraph.Tests {
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	partial class Family {
		static partial void CreateDefaultTemplate(ref Template template) {
			template.Members = ImmutableList.Create<Person>();
		}
	}
}
