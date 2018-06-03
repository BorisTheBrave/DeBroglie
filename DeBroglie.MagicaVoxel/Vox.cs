using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.MagicaVoxel
{
    public class Vox
    {
        public int Version { get; set; }

        public List<Chunk> Chunks { get; set; }

    }

    public class Chunk
    {
    }

    public class Pack : Chunk
    {
        public int NumModels { get; set; }
    }
    public class Size : Chunk
    {
        public int SizeX { get; set; }
        public int SizeY { get; set; }
        public int SizeZ { get; set; }
    }

    public class Xyzi : Chunk
    {
        public List<Voxel> Voxels { get; set; }
    }

    public struct Voxel
    {
        public byte X { get; set; }
        public byte Y { get; set; }
        public byte Z { get; set; }
        public byte ColorIndex { get; set; }
    }

    public class Rgba : Chunk
    {
        public int[] Colors { get; set; }
    }

    public class Transform : Chunk
    {
        public int Id { get; set; }

        public Dictionary<string, byte[]> Attributes { get; set; }

        public int ChildId { get; set; }
        public int Reserved { get; set; }
        public int LayerId { get; set; }
        public List<Frame> Frames { get; set; }
}

    public class Frame
    {
        public Dictionary<string, byte[]> Attributes { get; set; }
        public byte Rotation { get; set; }
        public int TranslateX { get; set; }
        public int TranslateY { get; set; }
        public int TranslateZ { get; set; }
    }

    public class Group : Chunk
    {
        public int Id { get; set; }

        public Dictionary<string, byte[]> Attributes { get; set; }

        public List<int> Children { get; set; }
    }

    public class Shape : Chunk
    {
        public int Id { get; set; }
        public Dictionary<string, byte[]> Attributes { get; set; }

        public List<Model> Models { get; set; }
    }

    public class Model
    {
        public int Id { get; set; }
        public Dictionary<string, byte[]> Attributes { get; set; }
    }

    public class Material : Chunk
    {
        public int Id { get; set; }
        public Dictionary<string, byte[]> Attributes { get; set; }
    }

    public enum MaterialType
    {
        Diffuse = 0,
        Metal, 
        Glass,
        Emissive
    }

    [Flags]
    public enum PropertyFlags
    {
        Plastic     = 0x0001,
        Roughness   = 0x0002,
        Specular    = 0x0004,
        IOR         = 0x0008,
        Attenuation = 0x0010,
        Power       = 0x0020,
        Glow        = 0x0040,
        IsTotalPower= 0x0080,
    }

    public class Matt : Chunk
    {
        public int Id { get; set; }

        public MaterialType MaterialType { get; set; }

        public float MaterialWeight { get; set; }

        public float? Plastic { get; set; }
        public float? Roughness { get; set; }
        public float? Specular { get; set; }
        public float? IOR { get; set; }
        public float? Attenuation { get; set; }
        public float? Power { get; set; }
        public float? Glow { get; set; }
        public bool IsTotalPower { get; set; }
    }

    public class UnknownChunk : Chunk
    {
        public int ChunkId { get; set; }

        public string ChunkIdStr => "" +
            (char)(byte)(ChunkId >> 0) +
            (char)(byte)(ChunkId >> 8) +
            (char)(byte)(ChunkId >> 16) +
            (char)(byte)(ChunkId >> 24);

        public byte[] Content { get; set; }
    }
}
