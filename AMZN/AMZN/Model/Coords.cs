using System;
using System.Collections.Generic;
using System.Text;

namespace AMZN.Model
{
    /// <summary>
    /// A koordinátapárokat reprezentáló osztály
    /// </summary>
    public class Coords
    {
        #region privát adattagok
        private int _x;
        private int _y;
        // A*-hoz kellenek
        #endregion

        #region propertyk
        /// <summary>
        /// Koordináta a vízszintes tengely mentén
        /// </summary>
        public int X { get => _x; set => _x = value; }

        /// <summary>
        /// Koordináta a függőleges tengely mentén
        /// </summary>
        public int Y { get => _y; set => _y = value; }
        #endregion

        #region konstruktorok
        /// <summary>
        /// A koordinátapár konstruktora
        /// </summary>
        /// <param name="x">x-koordináta</param>
        /// <param name="y">y-koordináta</param>
        public Coords(int x, int y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// A koordinátapár másolókonstruktora
        /// </summary>
        /// <param name="coords">a másolandó koordináták</param>
        public Coords(Coords coords) => new Coords(coords.X, coords.Y);

        /* TODO - paraméter nélküli konstruktor (szükséges?)
         * 
         * sehol sincs használva elvileg, de egyelőre csak kikommentezem
         * -B.
         * 
        /// <summary>
        /// A koordinátapár üres konstruktora? ez kell bárhova?
        /// </summary>
        public Coords() {}
        */
        #endregion

        #region metódusok és függvények
        /// <summary>
        /// Megadja, hogy a jelenlegi koordinátapárhoz képest melyik irányba van egy szomszédos koordinátapár.
        /// </summary>
        /// <param name="other">A szomszédos koordinátapár</param>
        /// <returns>Egy Direction típusú értéket</returns>
        public Direction NeighborDirection(Coords neighbor)
        {
            Coords diff = this - neighbor;
            if (diff.X == 0 && diff.Y == 1) return Direction.EAST; 
            if (diff.X == -1 && diff.Y == 0) return Direction.SOUTH;
            if (diff.X == 0 && diff.Y == -1) return Direction.WEST;
            return Direction.NORTH;
        }

        /// <summary>
        /// Kiszámolja a távolságot két mező között
        /// </summary>
        /// <param name="other">A másik mező koordinátái</param>
        /// <returns>A két mező koordinátáinak mértani közepe</returns>
        public double DistanceFrom(Coords other)
            => ((double)Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y)));
        #endregion

        #region műveletdefiníciók
        /// <summary>
        /// A koordinátapár hashcodeját adja meg összehasonlítás céljából
        /// </summary>
        public override int GetHashCode() => _x.GetHashCode() ^ _y.GetHashCode();
        // ez előfeltétele az equals overrideolásának, még ha nem is használjuk

        /// <summary>
        /// Két koordinátapárt hasonlít össze
        /// </summary>
        /// <param name="other">A koordinátapár, amivel össze szeretnénk hasonlítani</param>
        /// <returns>Igaz, ha a koordináták rendre megegyeznek, egyébként hamis.</returns>
        public override bool Equals(object other) => (other as Coords).X == X && (other as Coords).Y == Y;

        /// <summary>
        /// A különbségoperátor koordinátapárokon való értelmezése
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>egy koordinátát, ahol az X- és Y értékek rendre a két koordináta értékeinek különbségei</returns>
        public static Coords operator -(Coords a, Coords b) => new Coords(a.X - b.X, a.Y - b.Y);
        #endregion
    }
}
