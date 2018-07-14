namespace DeBroglie
{
    internal class TileConstraintAdaptor : IWaveConstraint
    {
        private readonly ITileConstraint underlying;
        private readonly TilePropagator propagator;

        public TileConstraintAdaptor(ITileConstraint underlying, TilePropagator propagator)
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
