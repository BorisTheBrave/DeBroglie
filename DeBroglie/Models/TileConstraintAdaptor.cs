using DeBroglie.Constraints;
using DeBroglie.Wfc;

namespace DeBroglie.Models
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

        public Resolution Check(WavePropagator wavePropagator)
        {
            return underlying.Check(propagator);
        }

        public Resolution Init(WavePropagator wavePropagator)
        {
            return underlying.Init(propagator);
        }
    }
}
