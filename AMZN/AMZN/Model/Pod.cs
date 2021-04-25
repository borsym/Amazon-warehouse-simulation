using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace AMZN.Model
{
    /// <summary>
    /// A polcokat reprezentáló osztály
    /// </summary>
    public class Pod
    {
        #region privát adattagok
        private List<int> _items;
        private Coords _coords;
        private bool _isBusy;
        private Coords _baseCoords;
        #endregion

        #region propertyk
        /// <summary>
        /// Igaz-hamis érték, ami megmutatja, hogy üres-e a polc (nincs rajta leszállítandó termék)
        /// </summary>
        public bool IsEmpty { get => _items.Count == 0; }

        /// <summary>
        /// A polcon lévő leszállítandó termékek listája (a termékeket a sorszámukkal jelöljük)
        /// </summary>
        public List<int> Items { get => _items; }

        /// <summary>
        /// A polc koordinátái
        /// </summary>
        public Coords Coords { get => _coords; set => _coords = value; }

        /// <summary>
        /// Igaz-hamis érték, ami megmutatja, hogy elfoglalt-e a polc (épp szállítják-e vagy tart-e felé robot)
        /// </summary>
        public bool IsBusy { get => _isBusy; set => _isBusy = value; }

        /// <summary>
        /// A polc kiindulási koordinátái, ahova a termékek leadása után vissza kell vinni
        /// </summary>
        public Coords BaseCoords { get => _baseCoords; set => _baseCoords = value; }
        #endregion

        #region konstruktorok
        /// <summary>
        /// A polc konstruktora.
        /// </summary>
        /// <param name="coords">A polc koordinátái</param>
        public Pod(Coords coords)
        {
            BaseCoords = coords;
            _coords = coords;
            _items = new List<int>();
            _isBusy = false;
        }
        
        /// <summary>
        /// A polc konstruktora.
        /// </summary>
        /// <param name="x">A polc x-koordinátája</param>
        /// <param name="y">A polc y-koordinátája</param>
        public Pod(int x, int y) => new Pod(new Coords(x, y));
        
        /// <summary>
        /// A polc konstruktora
        /// </summary>
        /// <param name="x">x-koordináta</param>
        /// <param name="y">y-koordináta</param>
        /// <param name="itemList">a polcon lévő termékek, sorszámuk által meghatározva, szövegesen reprezentálva, vesszővel elválasztva</param>
        public Pod(int x, int y, string itemList)
        {
            BaseCoords = new Coords(x, y);
            _coords = new Coords(x, y);
            _items = itemList.Split(',')
                .Select(Int32.Parse)
                .ToList();
            _isBusy = false;
        }

        /// <summary>
        /// A polc konstruktora
        /// </summary>
        /// <param name="coords">a polc koordinátái</param>
        /// <param name="itemList">a polcon lévő termékek, sorszámuk által meghatározva, szövegesen reprezentálva, vesszővel elválasztva</param>
        public Pod(Coords coords, string itemList)
        {
            BaseCoords = coords;
            _coords = coords;
            _items = itemList.Split(',')
                .Select(Int32.Parse)
                .ToList();
            _isBusy = false;
        }

        /// <summary>
        /// A polc konstruktora
        /// </summary>
        /// <param name="x">x-koordináta</param>
        /// <param name="y">y-koordináta</param>
        /// <param name="itemList">a polcon lévő termékek, sorszámuk által meghatározva</param>
        public Pod(int x, int y, List<int> itemList)
        {
            BaseCoords = new Coords(x, y);
            _coords = new Coords(x, y);
            _items = itemList;
            _isBusy = false;
        }

        /// <summary>
        /// A polc konstruktora
        /// </summary>
        /// <param name="coords">a polc koordinátái</param>
        /// <param name="itemList">a polcon lévő termékek, sorszámuk által meghatározva</param>
        public Pod(Coords coords, List<int> itemList)
        {
            BaseCoords = coords;
            _coords = coords;
            _items = itemList;
            _isBusy = false;
        }
        #endregion

        #region metódusok és függvények
        /// <summary>
        /// Metódus, amivel több új terméket helyezhetünk el egyszerre a polcon.
        /// </summary>
        /// <param name="newOnes">Az új termékek, sorszámuk által meghatározva</param>
        public void Add(List<int> newOnes) { 
            _items.AddRange(newOnes);
        }

        /// <summary>
        /// Metódus, amivel egy új terméket helyezhetünk el a polcon.
        /// </summary>
        /// <param name="newOne">az új termék sorszáma</param>
        public void Add(int newOne) { 
            _items.Add(newOne);
            //MessageBox.Show(string.Join(";", _items));
        }

        /// <summary>
        /// Metódus, amivel eltávolítunk egy terméket a polcról.
        /// </summary>
        /// <param name="item">az eltávolítandó termék sorszáma</param>
        public void Remove(int item) { 
            _items.Remove(item);
            //MessageBox.Show(string.Join(";", _items));
        }

        /// <summary>
        /// Eldönti, hogy egy termék rajta van-e a polcon.
        /// </summary>
        /// <param name="id">A termék sorszáma</param>
        /// <returns>Igaz, ha a termék fent van a polcon, egyébként hamis.</returns>
        public bool HasItem(int id) => _items.Contains(id);
        #endregion
    }

}
