using System;
using System.Collections.Generic;
using System.Text;

namespace DeBroglie.MagicaVoxel
{
    public static class VoxUtils
    {
        public static ITopArray<byte> Load(Vox vox)
        {
            Size size = null;
            foreach (var chunk in vox.Chunks)
            {
                if (chunk is Size)
                {
                    size = (Size)chunk;
                }
                else if(chunk is Xyzi xyzi)
                {
                    return Load(size, xyzi);
                }
            }
            throw new Exception();
        }

        private static ITopArray<byte> Load(Size size, Xyzi xyzi)
        {
            var data = new byte[size.SizeX, size.SizeY, size.SizeZ];
            foreach(var voxel in xyzi.Voxels)
            {
                data[voxel.X, voxel.Y, voxel.Z] = voxel.ColorIndex;
            }
            return new TopArray3D<byte>(data, false);
        }

        private static void Save(Size size, Xyzi xyzi, ITopArray<byte> topArray)
        {
            // TODO
            size.SizeX = topArray.Topology.Width;
            size.SizeY = topArray.Topology.Height;
            size.SizeZ = topArray.Topology.Depth;
            xyzi.Voxels = new List<Voxel>();

            for (var z = 0; z < size.SizeZ; z++)
            {
                for (var y = 0; y < size.SizeY; y++)
                {
                    for (var x = 0; x < size.SizeX; x++)
                    {
                        var colorIndex = topArray.Get(x, y, z);
                        if (colorIndex != 0)
                            xyzi.Voxels.Add(new Voxel
                            {
                                X = (byte)x,
                                Y = (byte)y,
                                Z = (byte)z,
                                ColorIndex = colorIndex,
                            });
                    }
                }
            }
        }

        public static void Save(Vox vox, ITopArray<byte> topArray)
        {
            Size size = null;
            foreach (var chunk in vox.Chunks)
            {
                if (chunk is Size)
                {
                    size = (Size)chunk;
                }
                else if (chunk is Xyzi xyzi)
                {
                    Save(size, xyzi, topArray);
                    return;
                }
            }
        }
    }
}
