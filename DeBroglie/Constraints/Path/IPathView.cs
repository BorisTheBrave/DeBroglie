namespace DeBroglie.Constraints
{
    /// <summary>
    /// A view of the underlying TilePropagator used for various path constraints, such as ConnectedConstraint.
    /// 
    /// This is best understood as exposing two array of bool variables, called "path" and "relevant".
    /// These are variables in the constraint solver sense - that is, we track if they could be true/false,
    /// and those evolve over time.
    /// The path variable at a given index is possibly true if CouldBePath is true, and it's possibly false if MustBePath is false.
    /// IPathView is responsible for propagating those variables back to the source of truth, the TilePropagator.:
    ///  * Calling update propagates from TilePropagator to {Could/Must}Be{Path/Relevant}.
    ///  * Calling {Select/Ban}{Path/Relevant} propagates back.
    ///  
    /// The path constraint is thus free to work entirely on the path and relevant variables, and doesn't use TilePropagator at all.
    /// The mapping between these two variables and the TilePropagator can cause a variety of effects.
    /// 
    /// The arrays of path and relevant don't need to match the topology of the TilePropagator at all, they use the Graph variable for their topology.
    /// 
    /// The interpretation of path and relevant varies between the different path constraints, but generally the path=t
    /// </summary>
    public interface IPathView
    {

        PathConstraintUtils.SimpleGraph Graph { get; }

        bool[] CouldBePath { get; }
        bool[] MustBePath { get; }

        bool[] CouldBeRelevant { get; }
        bool[] MustBeRelevant { get; }
        
        /// <summary>
        /// Updates CouldBePath,MustBePath,CouldBeRelevant,MustBeRelevant, to reflect the current state of the TilePropagator
        /// </summary>
        void Update();

        /// <summary>
        /// Updates the TilePropagator such that CouldBePath[index] and MustBePath[index] become true.
        /// </summary>
        void SelectPath(int index);

        /// <summary>
        /// Updates the TilePropagator such that CouldBePath[index] and MustBePath[index] becomes false.
        /// </summary>
        void BanPath(int index);

        /// <summary>
        /// Updates the TilePropagator such that CouldBeRelevant[index] and MustBeRelevant[index] becomes false.
        /// </summary>
        void BanRelevant(int index);
    }

    public static class PathViewExtensions
    {
        public static void Init(this IPathView pathView)
        {
            pathView.Update();
            for (var i = 0; i < pathView.Graph.NodeCount; i++)
            {
                if (pathView.MustBeRelevant[i])
                {
                    pathView.SelectPath(i);
                }
            }
        }
    }
}
