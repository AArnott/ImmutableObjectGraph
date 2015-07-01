namespace ImmutableObjectGraph {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public class RecursiveChildNotUniqueException : ArgumentException {
		public RecursiveChildNotUniqueException(object nonUniqueChildIdentifier) {
			this.NonUniqueChildIdentifier = nonUniqueChildIdentifier;
		}

		public object NonUniqueChildIdentifier { get; private set; }
	}
}
