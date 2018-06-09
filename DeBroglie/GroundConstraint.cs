namespace DeBroglie
{

    public class GroundConstraint : IWaveConstraint
    {
        private readonly int groundPattern;

        public GroundConstraint(int groundPattern)
        {
            this.groundPattern = groundPattern;
        }

        public CellStatus Check(WavePropagator wavePropagator)
        {
            return CellStatus.Undecided;

        }

        public CellStatus Init(WavePropagator wp)
        {
            for (var x = 0; x < wp.Width; x++)
            {
                for (var y = 0; y < wp.Height; y++)
                {
                    for (var z = 0; z < wp.Depth; z++)
                    {
                        if (y == wp.Height - 1)
                        {
                            wp.Select(x, y, z, groundPattern);
                        }
                        else
                        {
                            wp.Ban(x, y, z, groundPattern);
                        }
                    }
                }
            }
            return CellStatus.Undecided;
        }
    }
}
