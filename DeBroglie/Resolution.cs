namespace DeBroglie
{
    /// <summary>
    /// Indicates whether something has been fully resolved or not.
    /// This is the return code for many functions, but can also
    /// describe the state of individual locations in a generated output.
    /// </summary>
    public enum Resolution
    {
        /// <summary>
        /// The operation has successfully completed and a value is known.
        /// </summary>
        Decided = 0,
        /// <summary>
        /// The operation has not yet found a value
        /// </summary>
        Undecided = -1,
        /// <summary>
        /// It was not possible to find a successful value.
        /// </summary>
        Contradiction = -2,
    }
}
