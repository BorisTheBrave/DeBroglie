namespace DeBroglie
{
    public interface ITileConstraint
    {
        CellStatus Init(TilePropagator propagator);

        CellStatus Check(TilePropagator propagator);
    }
}
