using DeBroglie.Rot;
using DeBroglie.Topo;

namespace DeBroglie.Constraints
{
    public class MirrorYConstraint : SymmetryConstraint
    {
        private readonly static Rotation reflectY = new Rotation(180, true);

        public TileRotation TileRotation { get; set; }

        public override void Init(TilePropagator propagator)
        {
            if (TileRotation == null)
            {
                throw new System.ArgumentNullException(nameof(TileRotation));
            }

            var directionsType = propagator.Topology.AsGridTopology().Directions.Type;
            if (directionsType != DirectionSetType.Cartesian2d && directionsType != DirectionSetType.Cartesian3d)
            {
                throw new System.Exception($"MirrorYConstraint not supported on {directionsType}");
            }
            base.Init(propagator);
        }

        protected override bool TryMapIndex(TilePropagator propagator, int i, out int i2)
        {
            var topology = propagator.Topology;
            topology.GetCoord(i, out var x, out var y, out var z);
            var y2 = topology.Height - 1 - y;
            i2 = topology.GetIndex(x, y2, z);
            return topology.ContainsIndex(i2);
        }

        protected override bool TryMapTile(Tile tile, out Tile tile2)
        {
            return TileRotation.Rotate(tile, reflectY, out tile2);
        }
    }
}
