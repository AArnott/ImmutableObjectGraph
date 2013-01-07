using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConsoleApplication9 {
	using System.Collections.Immutable;
	using System.Reflection;

	class Program {
		static void Main(string[] args) {
			var apple = new Fruit().WithColor("red").WithSkinThickness(3);
			var basket = new Basket().WithContents(ImmutableList<Fruit>.Empty.Add(apple)).WithSize(5);
			Console.WriteLine("You have a {0} apple with {1} skin thickness", apple.Color, apple.SkinThickness);
		}
	}
}
