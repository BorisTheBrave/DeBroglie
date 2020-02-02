namespace DeBroglie.Wfc
{
    internal interface IPickHeuristic
    {
        // Returns -1/-1 if all cells are decided
        void PickObservation(out int index, out int pattern);
    }
}
