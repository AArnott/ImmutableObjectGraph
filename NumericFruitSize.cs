namespace ImmutableObjectGraph
{
    public class NumericFruitSize : IFruitSize
    {
        public int Value { get; private set; }

        public NumericFruitSize(int value)
        {
            Value = value;
        }
    }
}