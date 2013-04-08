using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImmutableObjectGraph
{
    using System.Collections.Immutable;
    using System.Reflection;

    class Program
    {
        static void Main(string[] args)
        {
            var apple = Fruit.Default
                .With(color: "red", skinThickness: 3);
            var greenApple = apple.With(color: "green");
            var greenAppleWithDefaultThickness = greenApple.With(skinThickness: 0);

            ImmutableList<Fruit> immutableFruits = ImmutableList<Fruit>.Empty.Add(apple);
            IImmutableList<Fruit> fruits = immutableFruits;
            var numericFruitSize = new NumericFruitSize(5);
            IFruitSize size = numericFruitSize;

            var basket = Basket.Default.With(contents: immutableFruits, size: numericFruitSize);
            basket = Basket.Default.With(contents: Optional.For(fruits), size: Optional.For(size));
            basket = Basket.Default.WithContents(fruits).WithSize(size);

            var appleBuilder = apple.ToBuilder();
            appleBuilder.Color = "yellow";
            var yellowApple = appleBuilder.ToImmutable();
            Console.WriteLine("You have a {0} apple with {1} skin thickness", apple.Color, apple.SkinThickness);
        }
    }
}
