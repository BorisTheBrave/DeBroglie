namespace DeBroglie.Constraints
{
    public interface IPathView
    {

        PathConstraintUtils.SimpleGraph Graph { get; }

        bool[] CouldBePath { get; }
        bool[] MustBePath { get; }

        bool[] CouldBeRelevant { get; }
        bool[] MustBeRelevant { get; }
        
        void Update();

        void SelectPath(int index);
        void BanPath(int index);
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
