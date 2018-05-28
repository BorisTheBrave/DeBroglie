namespace DeBroglie
{
    public interface IWaveConstraint
    {
        CellStatus Check(WavePropagator wavePropagator);
    }
}
