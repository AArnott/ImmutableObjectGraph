namespace ImmutableObjectGraph
{
    public class Tree : IPlant
    {
        public string Name { get; private set; }

        public Tree(string name)
        {
            Name = name;
        }
    }
}