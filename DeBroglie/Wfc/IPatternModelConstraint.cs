namespace DeBroglie.Wfc
{
    /// <summary>
    /// Similar to IWaveConstraint, this listens to changes in the Wave and makes appropriate changes to the propagator for the constraint.
    /// It's special mostly for historical reasons, and is used for the adjacent pattern constraint specified by the model.
    /// </summary>
    internal interface IPatternModelConstraint
    {
        void DoBan(int index, int pattern);

        void UndoBan(int index, int pattern);

        void DoSelect(int index, int pattern);

        void Propagate();

        void Clear();
    }
}
