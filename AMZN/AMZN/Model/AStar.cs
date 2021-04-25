using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AMZN.Model
{
    /* TODO
     * - ha podot visz/ ha nem visz podot ezeket csekkoljam le
     * - kerülje ne kerüljünk dolgokat
     */

    /// <summary>
    /// Az útvonal kirajzolásához szükséges cellák típusa.
    /// </summary>
    public struct Cell
    {
        #region privát adattagok
        private int _parent_i;
        private int _parent_j;
        private double f, g, h;
        #endregion

        #region propertyk
        /// <summary>
        /// A cella sor szerinti szülője; innen vezet út ebbe a cellába.
        /// </summary>
        public int Parent_I { get => _parent_i; set => _parent_i = value; }

        /// <summary>
        /// A cella oszlop szerinti szülője; innen vezet út ebbe a cellába.
        /// </summary>
        public int Parent_J { get => _parent_j; set => _parent_j = value; }

        /// <summary>
        /// A kiindulási cellából idáig mért költség (G) és a becsült hátralévő költség (H) összege.
        /// </summary>
        public double F { get => f; set => f = value; }
        
        /// <summary>
        /// A kiindulási cellából idáig mért költség.
        /// </summary>
        public double G { get => g; set => g = value; }

        /// <summary>
        /// A becsült hátralévő költség.
        /// </summary>
        public double H { get => h; set => h = value; }
        #endregion

        #region konstruktorok
        /// <summary>
        /// A cella típus konstruktora.
        /// </summary>
        /// <param name="F">A kiindulási cellából idáig mért költség (G) és a becsült hátralévő költség (H) összege</param>
        /// <param name="G">A kiindulási cellából idáig mért költség</param>
        /// <param name="H">A becsült hátralévő költség</param>
        /// <param name="parentI">A cella sor szerinti szülője; ebből az indexből vezet út ebbe a cellába</param>
        /// <param name="parentJ">A cella oszlop szerinti szülője; ebből az indexből vezet út ebbe a cellába]</param>
        public Cell(double F, double G, double H, int parentI, int parentJ)
        {
            f = F;
            g = G;
            h = H;
            _parent_i = parentI;
            _parent_j = parentJ;
        }
        #endregion
    }

    /// <summary>
    /// Az útvonalat reprezentáló típus
    /// </summary>
    public struct PathData
    {
        #region publikus adattagok
        /// <summary>
        /// Igaz-hamis érték, ami azt jelöli, hogy az útvonal a célmezőbe vezet-e.
        /// </summary>
        public bool isValidPath; // ezt szerintem majd nevezzük át isCorrectPath-ra vagy ilyesmire hogy beszédesebb legyen
        /// <summary>
        /// Az útvonalat felépítő mezők (azaz koordináták) listája.
        /// </summary>
        public List<Coords> path;
        #endregion

        #region konstruktorok
        /// <summary>
        /// Az útvonal típus konstruktora.
        /// </summary>
        /// <param name="ispath">Igaz-hamis érték, ami azt jelöli, hogy az útvonal a célmezőbe vezet-e.</param>
        public PathData(bool isvalidpath)
        {
            path = new List<Coords>();
            isValidPath = isvalidpath;
        }
        #endregion
    }

    /// <summary>
    /// Az A* algoritmust implementáló osztály
    /// </summary>
    public class AStar
    {
        #region privát adattagok
        private Model _model;
        private Coords _dest;
        #endregion

        #region propertyk
        /// <summary>
        /// a generált útvonal
        /// </summary>
        public PathData result;
        #endregion

        #region konstruktorok
        /// <summary>
        /// Az A* algoritmust implementáló osztály konstruktora.
        /// </summary>
        /// <param name="model">a szimuláció modellje</param>
        public AStar(Model model)
        {
            _model = model;
            result = new PathData(false);
        }
        #endregion

        #region metódusok és függvények
        /// <summary>
        /// Segédmetódus debugoláshoz
        /// </summary>
        public void SayHi()
        {
            Debug.WriteLine("SZIA\n\n");
        }
        /// <summary>
        /// Validálja a megadott koordinátákat.
        /// </summary>
        /// <param name="row">sorindex</param>
        /// <param name="col">oszlopindex</param>
        /// <returns>Igaz, ha a koordináták a táblán belülre esnek, egyébként hamis.</returns>
        public bool IsValid(int row, int col) => (row >= 0) && (row < _model.Table.SizeX) && (col >= 0) && (col < _model.Table.SizeY);

        /// <summary>
        /// Megvizsgálja, hogy a tábla adott mezője blokkolt-e.
        /// </summary>
        /// <param name="table">a szimulációs tábla</param>
        /// <param name="row">sorindex</param>
        /// <param name="col">oszlopindex</param>
        /// <param name="isCarryingPod">a robot éppen szállít-e podot</param>
        /// <returns>Igaz, ha a mezőn olyan objektum van, amin a robot át tud haladni, egyébként hamis.</returns>
        public bool IsUnblocked(Types[,] table, int row, int col, bool isCarryingPod) 
        {
            // IDE KELL MAJD FELTÉTEL VALÓSZÍNŰLEG
            if (_dest.Equals(new Coords(row, col))) return true;
            // itt mindenen átgázolunk, valami javítás kelleni fog
            if (isCarryingPod) // ez így nem jó
            {
                return table[row, col] == Types.EMPTY
                    || table[row, col] == Types.ROBOT
                    || table[row, col] == Types.STATION
                    || table[row, col] == Types.ROBOT_UNDER_POD
                    || table[row, col] == Types.ROBOT_WITH_POD
                    || table[row, col] == Types.ROBOT_ON_STATION
                    || table[row, col] == Types.ROBOT_WITH_POD_ON_STATION
                    || table[row, col] == Types.ROBOT_ON_DOCKER;
                // el kell fogadni a robotot mert a kiindulási pontja ez és blokkoltnak érzi,
                // itt baj lesz mert ha találkozik más robottal nem fogja érzékelni
            }
            else
            {
                return table[row, col] == Types.EMPTY
                    || table[row, col] == Types.ROBOT
                    || table[row, col] == Types.DOCKER
                    || table[row, col] == Types.POD
                    || table[row, col] == Types.STATION
                    || table[row, col] == Types.ROBOT_UNDER_POD
                    || table[row, col] == Types.ROBOT_WITH_POD
                    || table[row, col] == Types.ROBOT_ON_STATION
                    || table[row, col] == Types.ROBOT_WITH_POD_ON_STATION
                    || table[row, col] == Types.ROBOT_ON_DOCKER;
                // azért soroltam fel itt az összes típust, mert ha valamin nem szeretnénk hogy átmenjen, 
                // könnyebb kivenni, de ha átmehet mindenen átírhatjuk != types.VISITED-re és akkor mindenen átgázol
            }
        }

        /// <summary>
        /// Megvizsgálja, hogy a megadott koordináták a célmezőt reprezentálják-e.
        /// </summary>
        /// <param name="row">sorindex</param>
        /// <param name="col">oszlopindex</param>
        /// <param name="destination">a célmező koordinátái</param>
        /// <returns>Igaz, ha a koordináták egyeznek a célmező koordinátáival, egyébként hamis.</returns>
        public bool IsDestination(int row, int col, Coords destination)
        {
            return (row == destination.X && col == destination.Y) ? true : false;
        }

        /// <summary>
        /// Segédfüggvény a 'h' heurisztikus érték kiszámolására,
        /// ami a megadott koordináták és a célmező közötti legolcsóbb út költségének becslése.
        /// </summary>
        /// <param name="row">sorindex</param>
        /// <param name="col">oszlopindex</param>
        /// <param name="destination">a célmező koordinátái</param>
        /// <returns>Valós szám; a koordináták mértani átlaga.</returns>
        public double CalculateHValue(int row, int col, Coords destination)
        {
            return ((double)Math.Sqrt( (row - destination.X) * (row - destination.X) + (col - destination.Y) * (col - destination.Y)));
        }

        /// <summary>
        /// Az útvonal "kirajzolásáért" felelős függvény.
        /// </summary>
        /// <param name="cellDetails">azon cellák kétdimenziós tömbje, amelyeken át utat keresünk</param>
        /// <param name="dest">a célmező koordinátái</param>
        /// <returns></returns>
        public PathData TracePath(Cell[,] cellDetails, Coords dest)
       {
            int row = dest.X;
            int col = dest.Y;
            // Coords current = new Coords(row, col);
            Stack<Coords> Path = new Stack<Coords>();
            // át kell írni
            while (!(cellDetails[row,col].Parent_I == row && cellDetails[row,col].Parent_J == col))
            {
                Path.Push(new Coords(row, col));
                int temp_row = cellDetails[row,col].Parent_I;
                int temp_col = cellDetails[row,col].Parent_J;
                row = temp_row;
                col = temp_col;
            }

            Path.Push(new Coords(row, col));
            result.path = new List<Coords>();
            Debug.WriteLine("Path: ");
            while (Path.Count > 0)
            {
                Coords p = Path.Peek();
                Path.Pop();
                result.path.Add(p);
            }
            result.isValidPath = true;
            // Debug.WriteLine(string.Join(" -> ", result.path.ForEach(delegate(ToString())));
            return result;
        }

        // A Function to find the shortest path between
        // a given source cell to a destination cell according
        // to A* Search Algorithm
        /// <summary>
        /// Az A* algoritmus, ami megkeresi a legrövidebb utat a kiindulási mező és a célmező között.
        /// </summary>
        /// <param name="table">a szimulációs tábla</param>
        /// <param name="src">a kiindulási mező koordinátái</param>
        /// <param name="dest">a célmező koordinátái</param>
        /// <param name="isCarryingPod">a robot éppen szállít-e podot</param>
        /// <returns>Egy PathData típusú útvonalat a két mező között.</returns>
        public PathData AStarSearch(Types[,] table, Coords src, Coords dest, bool isCarryingPod)
        {
            Debug.WriteLine("[ A* STARTS ]");
            _dest = dest;

            // A kiindulási mező validálása
            if (!IsValid(src.X, src.Y))
            {
                Debug.WriteLine("Source at <{0};{1}> is invalid; stopping...\n", src.X, src.Y);
                return result;
            }

            // A célmező validálása
            if (!IsValid(dest.X, dest.Y))
            {
                Debug.WriteLine("Destination at <{0};{1}> is invalid; stopping...\n", dest.X, dest.Y);
                return result;
            }

            // Blokkolva van-e a kiindulási mező vagy a célmező
            // Debug.WriteLine("Source: " + (IsUnblocked(table, src.X, src.Y, isCarryingPod) ? "nem blokkolt" : "BLOKKOLT"));
            // Debug.WriteLine("Destination: " + (IsUnblocked(table, dest.X, dest.Y, isCarryingPod) ? "nem blokkolt" : "BLOKKOLT"));
            if (!IsUnblocked(table, src.X, src.Y, isCarryingPod)
                || !IsUnblocked(table, dest.X, dest.Y, isCarryingPod))  
            {
                Debug.WriteLine("Source or the destination is blocked; stopping...\n");
                return result;
            }

            // A kiindulási mező és a célmező megegyezik
            if (IsDestination(src.X, src.Y, dest))
            {
                Debug.WriteLine("Source at <{0};{1}> is equal to destination at <{2}:{3}>; stopping...\n", src.X, src.Y, dest.X, dest.Y);
                return result;
            }

            // closedList: kétdimenziós bool-tömb; egy értéke azt mutatja, hogy azon koordinátákat meglátogatta-e már a robot
            bool[,] closedList = new bool[_model.Table.SizeX,_model.Table.SizeY];

            // Kétdimenziós tömb, amiben a cellák részletes adatait tároljuk (F, G, H, szülők)
            Cell[,] cellDetails = new Cell[_model.Table.SizeX, _model.Table.SizeY];
            
            int i, j;

            // A kétdimenziós tömbök inicializálása
            for (i = 0; i < _model.Table.SizeX; i++)
            {
                for (j = 0; j < _model.Table.SizeY; j++)
                {
                    closedList[i, j]  = false;
                    cellDetails[i, j] = new Cell(float.MaxValue, float.MaxValue, float.MaxValue, -1, -1);
                }
            }

            // A kiindulási node paramétereinek inicializálása
            i = src.X;
            j = src.Y;
            cellDetails[i, j].F = 0.0;
            cellDetails[i, j].G = 0.0;
            cellDetails[i, j].H = 0.0;
            cellDetails[i, j].Parent_I = i;
            cellDetails[i, j].Parent_J = j;

            // openList: Kulcs-érték párok listája
            //  - kulcs: költség (f = g + h)
            //  - érték: koordináták
            List<KeyValuePair<double, Coords>> openList = new List<KeyValuePair<double, Coords>>();  // ITT LEHET HIBA LESZ

            // Ebbe a listába felvesszük a kiindulási cellát 0 költséggel
            openList.Add(new KeyValuePair<double, Coords>(0.0, new Coords(i,j)));

            // Felveszünk egy bool értéket, ami azt jelöli, elértük-e a célt
            bool foundDest = false;

            while (openList.Count > 0)
            {
                var p = openList.First();
                // Debug.WriteLine("\nCost: {0} - Coords: <{1};{2}>", p.Key, p.Value.X, p.Value.Y);

                // A koordinátát eltávolítjuk az openListről...
                openList.Remove(openList.First());

                // ...míg a closedListen jelöljük, hogy ezt a koordinátát meglátogattuk
                i = p.Value.X;
                j = p.Value.Y;
                closedList[i,j] = true;
                // Debug.WriteLine("Checking neighbors of <{0};{1}>...", i, j);

                /* Legeneráljuk a cella leszármazottait
                 *
                 *         N
                 *         |   
                 *         |  
                 *   W----Cell----E
                 *         | 
                 *         |  
                 *         S   
                 *
                 * Cell --> vizsgált cella [i,   j]
                 *    N --> North (észak)  [i-1, j]
                 *    S --> South (dél)    [i+1, j]
                 *    E --> East (kelet)   [i,   j+1]
                 *    W --> Wes  (nyugat)  [i,   j-1]
                 */

                // Felveszünk változókat a 4 leszármazott adatainak tárolására
                double gNew, hNew, fNew;

                //----------- Leszármazott #1 (North) ------------

                // Debug.WriteLine("NORTH: <{0};{1}>", i-1, j);

                // Csak akkor dolgozzuk fel a cellát, ha a táblán belül van
                if (IsValid(i - 1, j))
                {
                    // Debug.WriteLine(" - Coords are valid.");
                    // Ha a cél megegyezik ezzel a leszármazottal
                    if (IsDestination(i - 1, j, dest))
                    {
                        // Beállítjuk a cél szülő-adattagjait
                        cellDetails[i - 1, j].Parent_I = i;
                        cellDetails[i - 1, j].Parent_J = j;
                        Debug.WriteLine("\nFound destination cell at <{0};{1}>", i-1, j);
                        result = TracePath(cellDetails, dest);
                        foundDest = true;
                        return result;
                    }
                    // Ha a leszármazott már a closedListen van (korábban bejártuk)
                    // vagy blokkolt, ignoráljuk.
                    // Egyéb esetben a következőt tesszük:
                    else if (!closedList[i - 1, j]
                            && IsUnblocked(table, i - 1, j, isCarryingPod))
                    {
                        gNew = cellDetails[i,j].G + 1;
                        hNew = CalculateHValue(i - 1, j, dest);
                        fNew = gNew + hNew;

                        // Ha nincs a openListen, hozzáadjuk.
                        // A jelenleg vizsgált cella (Cell) lesz
                        // a szülője. Eltároljuk az F, G, H értékeket.
                        //                  VAGY
                        // Ha már az openListen van, megvizsgáljuk,
                        // hogy ez az út kisebb költségű-e (f).
                        if (cellDetails[i - 1, j].F == float.MaxValue || cellDetails[i - 1, j].F > fNew)
                        {
                            // openList.Add(make_pair(fNew, make_pair(i - 1, j)));
                            openList.Add(new KeyValuePair<double, Coords>(fNew, new Coords(i - 1, j)));

                            // frissítjük a cella adatait
                            cellDetails[i - 1, j].F = fNew;
                            cellDetails[i - 1, j].G = gNew;
                            cellDetails[i - 1, j].H = hNew;
                            cellDetails[i - 1, j].Parent_I= i;
                            cellDetails[i - 1, j].Parent_J = j;
                            // Debug.WriteLine("-1 {0} {1} {2} szulok: {3} {4}", cellDetails[i - 1, j].F, cellDetails[i - 1, j].G, cellDetails[i - 1, j].H,
                            //    cellDetails[i - 1, j].Parent_I, cellDetails[i - 1, j].Parent_J);
                        }
                    }
                }

                //----------- Leszármazott #2 (South) ------------

                // Csak akkor dolgozzuk fel a cellát, ha a táblán belül van
                if (IsValid(i + 1, j))
                {
                    // Ha a cél megegyezik ezzel a leszármazottal
                    if (IsDestination(i + 1, j, dest))
                    {
                        // Beállítjuk a cél szülő-adattagjait
                        cellDetails[i + 1, j].Parent_I = i;
                        cellDetails[i + 1, j].Parent_J = j;
                        Debug.WriteLine("Found destination cell at <{0};{1}>\n", i + 1, j);
                        result = TracePath(cellDetails, dest);
                        foundDest = true;
                        return result;
                    }
                    // Ha a leszármazott már a closedListen van (korábban bejártuk)
                    // vagy blokkolt, ignoráljuk.
                    // Egyéb esetben a következőt tesszük:
                    else if (!closedList[i + 1, j]
                            && IsUnblocked(table, i + 1, j, isCarryingPod))
                    {
                        gNew = cellDetails[i, j].G + 1;
                        hNew = CalculateHValue(i + 1, j, dest);
                        fNew = gNew + hNew;

                        // Ha nincs a openListen, hozzáadjuk.
                        // A jelenleg vizsgált cella (Cell) lesz
                        // a szülője. Eltároljuk az F, G, H értékeket.
                        //                  VAGY
                        // Ha már az openListen van, megvizsgáljuk,
                        // hogy ez az út kisebb költségű-e (f).
                        if (cellDetails[i + 1, j].F == float.MaxValue || cellDetails[i + 1, j].F > fNew)
                        {
                            // openList.Add(make_pair(fNew, make_pair(i + 1, j)));
                            openList.Add(new KeyValuePair<double, Coords>(fNew, new Coords(i + 1, j)));

                            // frissítjük a cella adatait
                            cellDetails[i + 1, j].F = fNew;
                            cellDetails[i + 1, j].G = gNew;
                            cellDetails[i + 1, j].H = hNew;
                            cellDetails[i + 1, j].Parent_I = i;
                            cellDetails[i + 1, j].Parent_J = j;
                            // Debug.WriteLine("-1 {0} {1} {2} szulok: {3} {4}", cellDetails[i + 1, j].F, cellDetails[i + 1, j].G, cellDetails[i + 1, j].H,
                            //     cellDetails[i + 1, j].Parent_I, cellDetails[i + 1, j].Parent_J);
                        }
                    }
                }

                //----------- Leszármazott #3 (East) ------------

                // Csak akkor dolgozzuk fel a cellát, ha a táblán belül van
                if (IsValid(i, j + 1))
                {
                    // Ha a cél megegyezik ezzel a leszármazottal
                    if (IsDestination(i, j + 1, dest))
                    {
                        // Beállítjuk a cél szülő-adattagjait
                        cellDetails[i, j + 1].Parent_I = i;
                        cellDetails[i, j + 1].Parent_J = j;
                        Debug.WriteLine("Found destination cell at <{0};{1}>\n", i, j + 1);
                        result = TracePath(cellDetails, dest);
                        foundDest = true;
                        return result;
                    }
                    // Ha a leszármazott már a closedListen van (korábban bejártuk)
                    // vagy blokkolt, ignoráljuk.
                    // Egyéb esetben a következőt tesszük:
                    else if (!closedList[i, j + 1]
                            && IsUnblocked(table, i, j + 1, isCarryingPod))
                    {
                        gNew = cellDetails[i, j].G + 1;
                        hNew = CalculateHValue(i, j + 1, dest);
                        fNew = gNew + hNew;

                        // Ha nincs a openListen, hozzáadjuk.
                        // A jelenleg vizsgált cella (Cell) lesz
                        // a szülője. Eltároljuk az F, G, H értékeket.
                        //                  VAGY
                        // Ha már az openListen van, megvizsgáljuk,
                        // hogy ez az út kisebb költségű-e (f).
                        if (cellDetails[i, j + 1].F == float.MaxValue || cellDetails[i, j + 1].F > fNew) 
                        {
                            // openList.Add(make_pair(fNew, make_pair(i - 1, j)));
                            openList.Add(new KeyValuePair<double, Coords>(fNew, new Coords(i, j + 1)));

                            // frissítjük a cella adatait
                            cellDetails[i, j + 1].F = fNew;
                            cellDetails[i, j + 1].G = gNew;
                            cellDetails[i, j + 1].H = hNew;
                            cellDetails[i, j + 1].Parent_I = i;
                            cellDetails[i, j + 1].Parent_J = j;
                            // Debug.WriteLine("-1 {0} {1} {2} szulok: {3} {4}", cellDetails[i, j + 1].F, cellDetails[i, j + 1].G, cellDetails[i, j + 1].H,
                            //     cellDetails[i, j + 1].Parent_I, cellDetails[i, j + 1].Parent_J);
                        }
                    }
                }

                //----------- Leszármazott #4 (West) ------------

                // Csak akkor dolgozzuk fel a cellát, ha a táblán belül van
                if (IsValid(i, j - 1))
                {
                    // Ha a cél megegyezik ezzel a leszármazottal
                    if (IsDestination(i, j - 1, dest))
                    {
                        // Beállítjuk a cél szülő-adattagjait
                        cellDetails[i, j - 1].Parent_I = i;
                        cellDetails[i, j - 1].Parent_J = j;
                        Debug.WriteLine("Found destination cell at <{0};{1}>\n", i, j - 1);
                        result = TracePath(cellDetails, dest);
                        foundDest = true;
                        return result;
                    }
                    // Ha a leszármazott már a closedListen van (korábban bejártuk)
                    // vagy blokkolt, ignoráljuk.
                    // Egyéb esetben a következőt tesszük:
                    else if (!closedList[i, j - 1]
                            && IsUnblocked(table, i, j - 1, isCarryingPod))
                    {
                        gNew = cellDetails[i, j].G + 1;
                        hNew = CalculateHValue(i, j - 1, dest);
                        fNew = gNew + hNew;

                        // Ha nincs a openListen, hozzáadjuk.
                        // A jelenleg vizsgált cella (Cell) lesz
                        // a szülője. Eltároljuk az F, G, H értékeket.
                        //                  VAGY
                        // Ha már az openListen van, megvizsgáljuk,
                        // hogy ez az út kisebb költségű-e (f).
                        if (cellDetails[i, j - 1].F == float.MaxValue || cellDetails[i, j - 1].F > fNew)
                        {
                            // openList.Add(make_pair(fNew, make_pair(i - 1, j)));
                            openList.Add(new KeyValuePair<double, Coords>(fNew, new Coords(i, j - 1)));

                            // frissítjük a cella adatait
                            cellDetails[i, j - 1].F = fNew;
                            cellDetails[i, j - 1].G = gNew;
                            cellDetails[i, j - 1].H = hNew;
                            cellDetails[i, j - 1].Parent_I = i;
                            cellDetails[i, j - 1].Parent_J = j;
                            // Debug.WriteLine("-1 {0} {1} {2} szulok: {3} {4}", cellDetails[i, j - 1].F, cellDetails[i, j - 1].G, cellDetails[i, j - 1].H,
                            //     cellDetails[i, j - 1].Parent_I, cellDetails[i, j - 1].Parent_J);
                        }
                    }
                }
            }

            // Ha nem érjük el a célt és üres az openList (nincs több bejárható cella),
            // belátjuk hogy a célt nem lehet elérni. Ez akkor történhet meg,
            // ha a célmezőt minden irányból blokkolja valami.
            if (!foundDest)
                Debug.WriteLine("Failed to find the destination cell.\n");
            
            return result;
        }
        #endregion
    }
}
