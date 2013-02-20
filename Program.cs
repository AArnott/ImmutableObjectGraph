using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImmutableObjectGraph {
	using System.Collections.Immutable;
	using System.Reflection;

	class Program {
		static void Main(string[] args) {
			var apple = Fruit.Default
				.With(color: "red", skinThickness: 3);
			var greenApple = apple.With(color: "green");
			var greenAppleWithDefaultThickness = greenApple.With(skinThickness: 0);
			var basket = Basket.Default.WithContents(ImmutableList<Fruit>.Empty.Add(apple)).WithSize(5);
			var appleBuilder = apple.ToBuilder();
			appleBuilder.Color = "yellow";
			var yellowApple = appleBuilder.ToImmutable();
			Console.WriteLine("You have a {0} apple with {1} skin thickness", apple.Color, apple.SkinThickness);
		}
	}
}
