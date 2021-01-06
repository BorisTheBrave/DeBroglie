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
}
