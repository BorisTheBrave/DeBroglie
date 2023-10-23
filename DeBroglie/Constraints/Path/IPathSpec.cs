namespace DeBroglie.Constraints
{

    /// <summary>
    /// Factory for an IPathView
    /// </summary>
    public interface IPathSpec
    {
        IPathView MakeView(TilePropagator tilePropagator);
    }
}
