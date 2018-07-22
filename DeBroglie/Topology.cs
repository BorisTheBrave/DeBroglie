namespace DeBroglie
{
    public class Topology
    {
        public Topology(int width, int height, bool periodic, bool[] mask = null)
            : this(Directions.Cartesian2d, width, height, 1, periodic, mask)
        {
        }

        public Topology(int width, int height, int depth, bool periodic, bool[] mask = null)
            : this(Directions.Cartesian3d, width, height, depth, periodic, mask)
        {
        }

        public Topology(Directions directions, int width, int height, bool periodic, bool[] mask = null)
            :this(directions, width, height, 1, periodic, mask)
        {
        }

        public Topology(Directions directions, int width, int height, int depth, bool periodic, bool[] mask = null)
        {
            Directions = directions;
            Width = width;
            Height = height;
            Depth = depth;
            Periodic = periodic;
            Mask = mask;
        }

        public Topology WithMask(bool[] mask)
        {
            return new Topology(Directions, Width, Height, Periodic, mask);
        }

        public Directions Directions { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Depth { get; set; }

        public bool Periodic { get; set; }

        public bool[] Mask { get; set; }

        public bool ContainsIndex(int index)
        {
            return Mask == null || Mask[index];
        }

        public int GetIndex(int x, int y, int z)
        {
            return x + y * Width + z * Width * Height;
        }

        public void GetCoord(int index, out int x, out int y, out int z)
        {
            x = index % Width;
            var i = index / Width;
            y = i % Height;
            z = i / Height;
        }

        public bool TryMove(int index, int direction, out int dest)
        {
            int x, y, z;
            GetCoord(index, out x, out y, out z);
            return TryMove(x, y, z, direction, out dest);
        }

        public bool TryMove(int x, int y, int z, int direction, out int dest)
        {
            if (TryMove(x, y, z, direction, out x, out y, out z))
            {
                dest = GetIndex(x, y, z);
                return true;
            }
            else
            {
                dest = -1;
                return false;
            }
        }

        public bool TryMove(int x, int y, int z, int direction, out int destx, out int desty, out int destz)
        {
            x += Directions.DX[direction];
            y += Directions.DY[direction];
            z += Directions.DZ[direction];
            if (Periodic)
            {
                if (x < 0) x += Width;
                if (x >= Width) x -= Width;
                if (y < 0) y += Height;
                if (y >= Height) y -= Height;
                if (z < 0) z += Depth;
                if (z >= Depth) z -= Depth;
            }
            else
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
                {
                    destx = -1;
                    desty = -1;
                    destz = -1;
                    return false;
                }
            }
            destx = x;
            desty = y;
            destz = z;
            if (Mask != null)
            {
                var index2 = GetIndex(x, y, z);
                return Mask[index2];
            }
            else
            {
                return true;
            }
        }
    }
}
