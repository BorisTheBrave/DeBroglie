namespace DeBroglie
{
    public interface IWaveConstraint
    {
        CellStatus Init(WavePropagator wavePropagator);

        CellStatus Check(WavePropagator wavePropagator);
    }
}
