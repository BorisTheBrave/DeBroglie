namespace DeBroglie
{
    public interface ITileConstraint<T>
    {
        CellStatus Init(TilePropagator<T> propagator);

        CellStatus Check(TilePropagator<T> propagator);
    }
}
