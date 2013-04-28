namespace ImmutableObjectGraph {
	using System.Diagnostics;

	/// <summary>
	/// A wrapper around optional parameters to capture whether they were specified or omitted.
	/// An implicit operator is defined so no one has to explicitly create this struct.
	/// </summary>
	[DebuggerDisplay("{IsDefined ? Value.ToString() : \"<missing>\",nq}")]
	public struct Optional<T> {
		private readonly T value;
		private readonly bool isDefined;

		[DebuggerStepThrough]
		public Optional(T value) {
			this.isDefined = true;
			this.value = value;
		}

		public bool IsDefined {
			[DebuggerStepThrough]
			get { return this.isDefined; }
		}

		public T Value {
			[DebuggerStepThrough]
			get { return this.value; }
		}

		[DebuggerStepThrough]
		public static implicit operator Optional<T>(T value) {
			return new Optional<T>(value);
		}

		public T GetValueOrDefault(T defaultValue) {
			return this.IsDefined ? this.value : defaultValue;
		}
	}
}
