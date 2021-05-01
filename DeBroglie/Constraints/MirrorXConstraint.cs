using DeBroglie.Rot;
using DeBroglie.Topo;
using DeBroglie.Trackers;

namespace DeBroglie.Constraints
{
    /// <summary>
    /// Maintain
    /// </summary>
    public class MirrorXConstraint : SymmetryConstraint
    {
        private readonly static Rotation reflectX = new Rotation(0, true);

        public TileRotation TileRotation { get; set; }

        public override void Init(TilePropagator propagator)
        {
            if (TileRotation == null)
            {
                throw new System.ArgumentNullException(nameof(TileRotation));
            }

            propagator.Topology.AsGridTopology();
            base.Init(propagator);
        }

        protected override bool TryMapIndex(TilePropagator propagator, int i, out int i2)
        {
            var topology = propagator.Topology;
            topology.GetCoord(i, out var x, out var y, out var z);
            var x2 = topology.Width - 1 - x;
            i2 = topology.GetIndex(x2, y, z);
            return topology.ContainsIndex(i2);
        }

        protected override bool TryMapTile(Tile tile, out Tile tile2)
        {
            return TileRotation.Rotate(tile, reflectX, out tile2);
        }
    }
}
