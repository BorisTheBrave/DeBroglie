using DeBroglie.Models;
using DeBroglie.Topo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeBroglie.Test.Constraints
{
    public class PathModel
    {
        public Tile Empty = new Tile(" ");
        public Tile Straight1 = new Tile("║");
        public Tile Straight2 = new Tile("═");
        public Tile Corner1 = new Tile("╚");
        public Tile Corner2 = new Tile("╔");
        public Tile Corner3 = new Tile("╗");
        public Tile Corner4 = new Tile("╝");
        public Tile Fork1 = new Tile("╠");
        public Tile Fork2 = new Tile("╦");
        public Tile Fork3 = new Tile("╣");
        public Tile Fork4 = new Tile("╩");
        
        public AdjacentModel Model = new AdjacentModel(DirectionSet.Cartesian2d);

        public Dictionary<Tile, ISet<Direction>> Exits;

        public PathModel(bool forks = true)
        {
            bool Filter(Tile t)
            {
                if (!forks && new[] { Fork1, Fork2, Fork3, Fork4 }.Contains(t)) return false;
                return true;
            }

            Model.AddAdjacency(
                new[] { Empty, Straight1, Corner3, Corner4, Fork3 }.Where(Filter).ToList(),
                new[] { Empty, Straight1, Corner1, Corner2, Fork1 }.Where(Filter).ToList(),
                Direction.XPlus);

            Model.AddAdjacency(
                new[] { Straight2, Corner1, Corner2, Fork1, Fork2, Fork4 }.Where(Filter).ToList(),
                new[] { Straight2, Corner3, Corner4, Fork2, Fork3, Fork4 }.Where(Filter).ToList(),
                Direction.XPlus);

            Model.AddAdjacency(
                new[] { Empty, Straight2, Corner1, Corner4, Fork4 }.Where(Filter).ToList(),
                new[] { Empty, Straight2, Corner2, Corner3, Fork2 }.Where(Filter).ToList(),
                Direction.YPlus);

            Model.AddAdjacency(
                new[] { Straight1, Corner2, Corner3, Fork1, Fork2, Fork3 }.Where(Filter).ToList(),
                new[] { Straight1, Corner1, Corner4, Fork1, Fork3, Fork4 }.Where(Filter).ToList(),
                Direction.YPlus);

            Model.SetUniformFrequency();

            Exits = new Dictionary<Tile, ISet<Direction>>
            {
                {Straight1, new []{Direction.YMinus, Direction.YPlus}.ToHashSet() },
                {Straight2, new []{Direction.XMinus, Direction.XPlus}.ToHashSet() },
                {Corner1, new []{Direction.YMinus, Direction.XPlus}.ToHashSet() },
                {Corner2, new []{Direction.YPlus, Direction.XPlus}.ToHashSet() },
                {Corner3, new []{Direction.YPlus, Direction.XMinus}.ToHashSet() },
                {Corner4, new []{Direction.YMinus, Direction.XMinus}.ToHashSet() },
                {Fork1, new []{ Direction.YMinus, Direction.XPlus, Direction.YPlus}.ToHashSet() },
                {Fork2, new []{ Direction.XPlus, Direction.YPlus, Direction.XMinus}.ToHashSet() },
                {Fork3, new []{ Direction.YPlus, Direction.XMinus, Direction.YMinus}.ToHashSet() },
                {Fork4, new []{ Direction.XMinus, Direction.YMinus, Direction.XPlus}.ToHashSet() },
            };
        }
    }
}
