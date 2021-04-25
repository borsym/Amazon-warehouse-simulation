using System;
using System.Collections.Generic;
using System.Text;
using AMZN.Model; // a types osztálynak az elhelyezése lehet ide kellesz
namespace AMZN.Persistence
{
    
    public class Table   : ICloneable
    {
        private Types[,] _fieldValues;
        public int SizeX { get { return _fieldValues.GetLength(0); }  }
        public int SizeY { get { return _fieldValues.GetLength(1); }  }
        public Types this[int x, int y] { get { return GetValue(x, y); } }
        public Table() : this(8, 8) { }

        public Table(int tableSizeX, int tableSizeY)
        {
            if (tableSizeX * tableSizeY < 6)
                throw new ArgumentOutOfRangeException("The table size is less than 0.", "tableSize");
            _fieldValues = new Types[tableSizeX, tableSizeY];
        }

        public Boolean IsEmpty(Int32 x, Int32 y)
        {
            if (x < 0 || x >= _fieldValues.GetLength(0))
                throw new ArgumentOutOfRangeException("x", "The X coordinate is out of range.");
            if (y < 0 || y >= _fieldValues.GetLength(1))
                throw new ArgumentOutOfRangeException("y", "The Y coordinate is out of range.");

            return _fieldValues[x, y] == Types.EMPTY;
        }

        public Types GetValue(Int32 x, Int32 y)
        {
            if (x < 0 || x >= _fieldValues.GetLength(0))
                throw new ArgumentOutOfRangeException("x", "The X coordinate is out of range.");
            if (y < 0 || y >= _fieldValues.GetLength(1))
                throw new ArgumentOutOfRangeException("y", "The Y coordinate is out of range.");

            return _fieldValues[x, y];
        }

        public void SetValue(Int32 x, Int32 y, Types value)
        {
            if (x < 0 || x >= _fieldValues.GetLength(0))
                throw new ArgumentOutOfRangeException("x", "The X coordinate is out of range.");
            if (y < 0 || y >= _fieldValues.GetLength(1))
                throw new ArgumentOutOfRangeException("y", "The Y coordinate is out of range.");

            _fieldValues[x, y] = value;
        }

        public object Clone()
        {
            return _fieldValues.Clone();
        }

        public void ClearFields(List<Coords> makeThemEmpty)
        {
            foreach(var elem in makeThemEmpty)
            {
                _fieldValues[elem.X, elem.Y] = Types.EMPTY;
            }
        }
    }
}
