namespace DeBroglie.Topo
{
    public class Topology
    {
        public Topology(int width, int height, bool periodic)
            : this(Directions.Cartesian2d, width, height, 1, periodic, periodic, periodic)
        {
        }

        public Topology(int width, int height, int depth, bool periodic)
            : this(Directions.Cartesian3d, width, height, depth, periodic, periodic, periodic)
        {
        }

        public Topology(Directions directions, int width, int height, bool periodicX, bool periodicY, bool[] mask = null)
            :this(directions, width, height, 1, periodicX, periodicY, false, mask)
        {
        }

        public Topology(Directions directions, int width, int height, int depth, bool periodicX, bool periodicY, bool periodicZ, bool[] mask = null)
        {
            Directions = directions;
            Width = width;
            Height = height;
            Depth = depth;
            PeriodicX = periodicX;
            PeriodicY = periodicY;
            PeriodicZ = periodicZ;
            Mask = mask;
        }

        public Topology WithMask(bool[] mask)
        {
            return new Topology(Directions, Width, Height, Depth, PeriodicX, PeriodicY, PeriodicZ, mask);
        }

        public Topology WithSize(int width, int height, int depth = 1)
        {
            return new Topology(Directions, width, height, depth, PeriodicX, PeriodicY, PeriodicZ);
        }

        public Directions Directions { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Depth { get; set; }

        public bool PeriodicX { get; set; }

        public bool PeriodicY { get; set; }

        public bool PeriodicZ { get; set; }

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
            if (PeriodicX)
            {
                if (x < 0) x += Width;
                if (x >= Width) x -= Width;
            }
            else if (x < 0 || x >= Width)
            {
                destx = -1;
                desty = -1;
                destz = -1;
                return false;
            }
            if (PeriodicY)
            {
                if (y < 0) y += Height;
                if (y >= Height) y -= Height;
            }
            else if (y < 0 || y >= Height)
            {
                destx = -1;
                desty = -1;
                destz = -1;
                return false;
            }
            if (PeriodicZ)
            {
                if (z < 0) z += Depth;
                if (z >= Depth) z -= Depth;
            }
            else if (z < 0 || z >= Depth)
            {
                destx = -1;
                desty = -1;
                destz = -1;
                return false;
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
