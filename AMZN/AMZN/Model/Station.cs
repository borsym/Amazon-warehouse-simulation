using System;
using System.Collections.Generic;
using System.Text;

namespace AMZN.Model
{
    /// <summary>
    /// A célállomásokat reprezentáló osztály
    /// </summary>
    public class Station
    {
        #region privát adattagok
        private Coords _coords;
        private int _id;
        private bool _isBusy;
        #endregion

        #region propertyk
        /// <summary>
        /// A célállomás sorszáma (ez megegyezik az itt leadható termék sorszámával)
        /// </summary>
        public int Id { get => _id; set => _id = value; }

        /// <summary>
        /// Igaz-hamis érték, ami azt jelöli, hogy épp használatban van-e a célállomás
        /// </summary>
        public bool IsBusy { get => _isBusy; set => _isBusy = value; }

        /// <summary>
        /// A célállomás koordinátái
        /// </summary>
        public Coords Coords { get => _coords; set => _coords = value; }
        #endregion

        #region konstruktorok
        /// <summary>
        /// A célállomás konstruktora
        /// </summary>
        /// <param name="x">x-koordináta</param>
        /// <param name="y">y-koordináta</param>
        /// <param name="id">sorszám</param>
        public Station(int x, int y, int id)
        {
            _isBusy = false;
            _coords = new Coords(x, y);
            _id = id;
        }

        /// <summary>
        /// A célállomás konstruktora
        /// </summary>
        /// <param name="coords">koordináták</param>
        /// <param name="id">sorszám</param>
        public Station(Coords coords, int id)
        {
            _isBusy = false;
            _coords = coords;
            _id = id;
        }
        #endregion
    }
}
