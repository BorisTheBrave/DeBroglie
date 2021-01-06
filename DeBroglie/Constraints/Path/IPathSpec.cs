namespace DeBroglie.Constraints
{

    public interface IPathSpec
    {
        IPathView MakeView(TilePropagator tilePropagator);
    }
}
