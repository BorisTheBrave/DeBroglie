using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DeBroglie.MagicaVoxel
{
    public static class VoxSerializer
    {
        private const int VOX_ = ('V') + ('O' << 8) + ('X' << 16) + (' ' << 24);
        private const int MAIN = ('M') + ('A' << 8) + ('I' << 16) + ('N' << 24);
        private const int PACK = ('P') + ('A' << 8) + ('C' << 16) + ('K' << 24);
        private const int SIZE = ('S') + ('I' << 8) + ('Z' << 16) + ('E' << 24);
        private const int XYZI = ('X') + ('Y' << 8) + ('Z' << 16) + ('I' << 24);
        private const int RGBA = ('R') + ('G' << 8) + ('B' << 16) + ('A' << 24);
        private const int MATT = ('M') + ('A' << 8) + ('T' << 16) + ('T' << 24);
        private const int nTRN = ('n') + ('T' << 8) + ('R' << 16) + ('N' << 24);
        private const int nGRP = ('n') + ('G' << 8) + ('R' << 16) + ('P' << 24);
        private const int nSHP = ('n') + ('S' << 8) + ('H' << 16) + ('P' << 24);
        private const int MATL = ('M') + ('A' << 8) + ('T' << 16) + ('L' << 24);

        public static Vox Read(BinaryReader reader)
        {
            var magic = reader.ReadInt32();
            if (magic != VOX_)
                throw new Exception("Magic VOX string not found");
            var version = reader.ReadInt32();

            // Details for main chunk
            var chunkId = reader.ReadInt32();
            var contentSize = reader.ReadInt32();
            var childrenSize = reader.ReadInt32();
            var chunks = new List<Chunk>();
            while (reader.PeekChar() != -1)
            {
                chunks.Add(ReadChunk(reader));
            }

            return new Vox
            {
                Version = version,
                Chunks = chunks,
            };
        }

        private static Chunk ReadChunk(BinaryReader reader)
        {
            var chunkId = reader.ReadInt32();
            var contentSize = reader.ReadInt32();
            var childrenSize = reader.ReadInt32();
            if (childrenSize != 0)
                throw new Exception("Recursive chunks not supported");

            switch(chunkId)
            {
                case PACK:
                    return new Pack
                    {
                        NumModels = reader.ReadInt32(),
                    };
                case SIZE:
                    return new Size
                    {
                        SizeX = reader.ReadInt32(),
                        SizeY = reader.ReadInt32(),
                        SizeZ = reader.ReadInt32(),
                    };
                case XYZI:
                    var numVoxels = reader.ReadInt32();
                    return new Xyzi
                    {
                        Voxels = Enumerable.Range(0, numVoxels)
                            .Select(_ => new Voxel
                            {
                                X = reader.ReadByte(),
                                Y = reader.ReadByte(),
                                Z = reader.ReadByte(),
                                ColorIndex = reader.ReadByte(),
                            })
                            .ToList(),
                    };
                case RGBA:
                    return new Rgba
                    {
                        Colors = Enumerable.Range(0, 256).Select(_ => reader.ReadInt32()).ToArray(),
                    };
                case MATT:
                    var matt = new Matt();
                    matt.Id = reader.ReadInt32();
                    matt.MaterialType = (MaterialType)reader.ReadInt32();
                    matt.MaterialWeight = reader.ReadSingle();
                    var propertyFlags = (PropertyFlags)reader.ReadInt32();
                    if (propertyFlags.HasFlag(PropertyFlags.Plastic))
                        matt.Plastic = reader.ReadSingle();
                    if (propertyFlags.HasFlag(PropertyFlags.Roughness))
                        matt.Roughness = reader.ReadSingle();
                    if (propertyFlags.HasFlag(PropertyFlags.Specular))
                        matt.Specular = reader.ReadSingle();
                    if (propertyFlags.HasFlag(PropertyFlags.IOR))
                        matt.IOR = reader.ReadSingle();
                    if (propertyFlags.HasFlag(PropertyFlags.Attenuation))
                        matt.Attenuation = reader.ReadSingle();
                    if (propertyFlags.HasFlag(PropertyFlags.Power))
                        matt.Power = reader.ReadSingle();
                    if (propertyFlags.HasFlag(PropertyFlags.Glow))
                        matt.Glow = reader.ReadSingle();
                    matt.IsTotalPower = propertyFlags.HasFlag(PropertyFlags.IsTotalPower);
                    return matt;
                case nTRN:
                    var transform = new Transform();
                    transform.Id = reader.ReadInt32();
                    transform.Attributes = ReadAttributes(reader);
                    transform.ChildId = reader.ReadInt32();
                    transform.Reserved = reader.ReadInt32();
                    transform.LayerId = reader.ReadInt32();
                    transform.Frames = Enumerable.Range(0, reader.ReadInt32())
                        .Select(x => new Frame
                        {
                            Attributes = ReadAttributes(reader),
                        })
                        .ToList();
                    return transform;
                case nGRP:
                    return new Group
                    {
                        Id = reader.ReadInt32(),
                        Attributes = ReadAttributes(reader),
                        Children = Enumerable.Range(0, reader.ReadInt32())
                            .Select(_ => reader.ReadInt32())
                            .ToList(),
                    };
                case nSHP:
                    return new Shape
                    {
                        Id = reader.ReadInt32(),
                        Attributes = ReadAttributes(reader),
                        Models = Enumerable.Range(0, reader.ReadInt32())
                            .Select(_ => new Model
                            {
                                Id = reader.ReadInt32(),
                                Attributes = ReadAttributes(reader),
                            })
                            .ToList(),
                    };
                case MATL:
                    return new Material
                    {
                        Id = reader.ReadInt32(),
                        Attributes = ReadAttributes(reader),
                    };
                default:
                    return new UnknownChunk
                    {
                        ChunkId = chunkId,
                        Content = reader.ReadBytes(contentSize),
                    };
            }
        }

        private static string ReadString(BinaryReader reader)
        {
            return Encoding.ASCII.GetString(ReadByteString(reader));
        }

        private static byte[] ReadByteString(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            return reader.ReadBytes(len);
        }

        private static Dictionary<string, byte[]> ReadAttributes(BinaryReader reader)
        {
            var n = reader.ReadInt32();
            var d = new Dictionary<string, byte[]>();
            for (int i = 0; i < n; i++)
            {
                var key = ReadString(reader);
                var value = ReadByteString(reader);
                d[key] = value;
            }
            return d;
        }

        public static void Write(BinaryWriter writer, Vox vox)
        {
            writer.Write(VOX_);
            writer.Write(vox.Version);
            writer.Write(MAIN);
            writer.Write(0);
            var childrenSizePos = writer.Seek(0, SeekOrigin.Current);
            writer.Write(0); // Will be filled in in retrospect
            foreach(var chunk in vox.Chunks)
            {
                WriteChunk(writer, chunk);
            }
            var endPos = writer.Seek(0, SeekOrigin.Current);
            writer.Seek((int)childrenSizePos, SeekOrigin.Begin);
            writer.Write((int)(endPos - childrenSizePos - 4));
        }

        private static void WriteChunk(BinaryWriter writer, Chunk chunk)
        {
            int lengthPos = 0;
            if (chunk is Pack pack)
            {
                writer.Write(PACK);
                writer.Write(4);
                writer.Write(0);
                writer.Write(pack.NumModels);
            }
            else if (chunk is Size size)
            {
                writer.Write(SIZE);
                writer.Write(12);
                writer.Write(0);
                writer.Write(size.SizeX);
                writer.Write(size.SizeY);
                writer.Write(size.SizeZ);
            }
            else if (chunk is Xyzi xyzi)
            {
                writer.Write(XYZI);
                writer.Write(4 + 4 * xyzi.Voxels.Count);
                writer.Write(0);
                writer.Write(xyzi.Voxels.Count);
                foreach (var voxel in xyzi.Voxels)
                {
                    writer.Write(voxel.X);
                    writer.Write(voxel.Y);
                    writer.Write(voxel.Z);
                    writer.Write(voxel.ColorIndex);
                }
            }
            else if (chunk is Rgba rgba)
            {
                writer.Write(RGBA);
                writer.Write(4 * 256);
                writer.Write(0);
                foreach (var color in rgba.Colors)
                {
                    writer.Write(color);
                }
            }
            else if (chunk is Matt matt)
            {
                throw new Exception("Writting MATT chunks is not supported. Use MATL");
            }
            else if (chunk is Transform transform)
            {
                writer.Write(nTRN);
                lengthPos = (int)writer.Seek(0, SeekOrigin.Current);
                writer.Write(0);
                writer.Write(0);
                writer.Write(transform.Id);
                WriteAttributes(writer, transform.Attributes);
                writer.Write(transform.ChildId);
                writer.Write(transform.Reserved);
                writer.Write(transform.LayerId);
                writer.Write(transform.Frames.Count);
                foreach(var frame in transform.Frames)
                {
                    WriteAttributes(writer, frame.Attributes);
                }
            }
            else if (chunk is Group group)
            {
                writer.Write(nGRP);
                lengthPos = (int)writer.Seek(0, SeekOrigin.Current);
                writer.Write(0);
                writer.Write(0);
                writer.Write(group.Id);
                WriteAttributes(writer, group.Attributes);
                writer.Write(group.Children.Count);
                foreach(var child in group.Children)
                {
                    writer.Write(child);
                }
            }
            else if (chunk is Shape shape)
            {
                writer.Write(nSHP);
                lengthPos = (int)writer.Seek(0, SeekOrigin.Current);
                writer.Write(0);
                writer.Write(0);
                writer.Write(shape.Id);
                WriteAttributes(writer, shape.Attributes);
                writer.Write(shape.Models.Count);
                foreach (var model in shape.Models)
                {
                    writer.Write(model.Id);
                    WriteAttributes(writer, model.Attributes);
                }
            }
            else if (chunk is Material material)
            {
                writer.Write(MATL);
                lengthPos = (int)writer.Seek(0, SeekOrigin.Current);
                writer.Write(0);
                writer.Write(0);
                writer.Write(material.Id);
                WriteAttributes(writer, material.Attributes);
            }
            else if (chunk is UnknownChunk unknown)
            {
                writer.Write(unknown.ChunkId);
                writer.Write(unknown.Content.Length);
                writer.Write(0);
                writer.Write(unknown.Content);
            }
            else
            {
                throw new Exception("Unexpected chunk");
            }
            if(lengthPos != 0)
            {
                var endPos = writer.Seek(0, SeekOrigin.Current);
                writer.Seek((int)lengthPos, SeekOrigin.Begin);
                writer.Write((int)(endPos - lengthPos - 8));
                writer.Seek((int)endPos, SeekOrigin.Begin);
            }
        }

        private static void WriteString(BinaryWriter writer, string s)
        {
            WriteByteString(writer, Encoding.ASCII.GetBytes(s));
        }

        private static void WriteByteString(BinaryWriter writer, byte[] s)
        {
            writer.Write(s.Length);
            writer.Write(s);
        }

        private static void WriteAttributes(BinaryWriter writer, Dictionary<string, byte[]> attributes)
        {
            writer.Write(attributes.Count);
            foreach(var kv in attributes)
            {
                WriteString(writer, kv.Key);
                WriteByteString(writer, kv.Value);
            }
        }
    }
}
