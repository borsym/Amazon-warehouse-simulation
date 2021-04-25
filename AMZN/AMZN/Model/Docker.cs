using System;
using System.Collections.Generic;
using System.Text;

namespace AMZN.Model
{
    /// <summary>
    /// A töltőállomásokat reprezentáló osztály
    /// </summary>
    public class Docker
    {
        #region privát adattagok
        private Coords _coords;
        private bool _isEmpty;
        #endregion

        #region propertyk
        /// <summary>
        /// Igaz-hamis érték, ami megmutatja, hogy épp használatban van-e a töltőállomás
        /// </summary>
        public bool IsEmpty { get => _isEmpty; set => _isEmpty = value; }

        /// <summary>
        /// A töltőállomás koordinátái
        /// </summary>
        public Coords Coords { get => _coords; set => _coords = value; }
        #endregion

        #region konstruktorok
        /// <summary>
        /// A töltőállomás konstruktora
        /// </summary>
        /// <param name="x">x-koordináta</param>
        /// <param name="y">y-koordináta</param>
        public Docker(int x, int y)
        {
            _coords = new Coords(x, y);
            _isEmpty = true;
        }

        /// <summary>
        /// A töltőállomás konstruktora
        /// </summary>
        /// <param name="coords">A töltőállomás koordinátái</param>
        public Docker(Coords coords)
        {
            Coords = coords;
            _isEmpty = true;
        }
        #endregion

        /* TODO Docker.Charge() metódus
         * 
         * egyelőre nem működik ezzel a metódussal a töltés,
         * szerintem azért, mert a modell a robot másolatát adja át,
         * jobb tippem nincs
         * -B.
         * 
        /// <summary>
        /// Metódus, amivel a töltőállomás feltölti a rajta álló robot energiaszintjét 100%-ra, és 5 körig várakoztatja.
        /// </summary>
        /// <param name="r">A robot objektum, ami a töltőállomáson áll</param>
        public void Charge(Robot r)
        {
            _isEmpty = false;
            if (r.Coords == this.Coords)
            {
                r.BatteryLevel = 100;
                r.Idle = 5;
            }
            _isEmpty = true;
        }
        */
    }
}
