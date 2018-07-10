namespace DeBroglie
{
    internal class TileConstraintAdaptor<T> : IWaveConstraint
    {
        private readonly ITileConstraint<T> underlying;
        private readonly TilePropagator<T> propagator;

        public TileConstraintAdaptor(ITileConstraint<T> underlying, TilePropagator<T> propagator)
        {
            this.underlying = underlying;
            this.propagator = propagator;
        }

        public CellStatus Check(WavePropagator wavePropagator)
        {
            return underlying.Check(propagator);
        }

        public CellStatus Init(WavePropagator wavePropagator)
        {
            return underlying.Init(propagator);
        }
    }
}
