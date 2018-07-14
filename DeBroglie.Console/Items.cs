using System.Collections.Generic;
using System.Xml.Serialization;

namespace DeBroglie.Console
{

    [XmlRoot("items")]
    public class Items
    {
        [XmlElement(Type = typeof(Overlapping), ElementName = "overlapping")]
        [XmlElement(Type = typeof(Adjacent), ElementName = "adjacent")]
        [XmlElement(Type = typeof(SimpleTiled), ElementName = "simpletiled")]
        public List<Item> AllItems { get; set; }
    }

    public class Item
    {
        [XmlAttribute("dest")]
        public string Dest { get; set; }

        [XmlAttribute("src")]
        public string Src { get; set; }

        [XmlAttribute("periodicInput")]
        public string PeriodicInput { get; set; } = "true";

        public bool IsPeriodicInput => PeriodicInput.ToLower() == "true";

        [XmlAttribute("periodic")]
        public string Periodic { get; set; } = "false";

        public bool IsPeriodic => Periodic.ToLower() == "true";

        [XmlAttribute("width")]
        public int Width { get; set; } = 48;

        [XmlAttribute("height")]
        public int Height { get; set; } = 48;

        [XmlAttribute("depth")]
        public int Depth { get; set; } = 48;

        [XmlAttribute("backtrack")]
        public bool Backtrack { get; set; }

        [XmlArray("tiles")]
        [XmlArrayItem("tile")]
        public List<TileData> Tiles { get; set; }

        [XmlArray("constraints")]
        [XmlArrayItem(Type = typeof(PathData), ElementName = "path")]
        public List<ConstraintData> Constraints { get; set; }
    }

    public class Overlapping : Item
    {
        [XmlAttribute("N")]
        public int N { get; set; } = 2;

        [XmlAttribute("symmetry")]
        public int Symmetry { get; set; } = 8;

        [XmlAttribute("ground")]
        public int Ground { get; set; }
    }

    public class Adjacent : Item
    {

    }

    public class SimpleTiled : Item
    {

    }

    public class TileData
    {
        [XmlAttribute("value")]
        public string Value { get; set; }

        [XmlAttribute("change-frequency")]
        public string ChangeFrequency { get; set; }
    }

    public class ConstraintData
    {

    }

    public class PathData : ConstraintData
    {
        [XmlArray("path-tiles")]
        [XmlArrayItem("path-tile")]
        public string[] PathTiles { get; set; }
    }
}
