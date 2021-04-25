using AMZN.Persistence;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AMZN.Model
{

    /// <summary>
    /// A szimuláció modellje
    /// </summary>
    public class Model
    {
        static int _maxStationCount = 4;

        #region privát adattagok
        private IDataAccess _dataAccess;
        private Table _table;
        private int _allBatteryUsed = 0;
        private int _allSteps = 0;
        private int _timerCount;
        private static int _stationID = 0;
        private SortedSet<int> _reusableStationIDs;
        private Dictionary<Coords, Robot> _robots;
        private Dictionary<Coords, Docker> _dockers;
        private Dictionary<Coords, Station> _stations;
        private Dictionary<Coords, Pod> _pods;
        private AStar _astar;
        private List<Coords> _previousSteps;
        private bool _isGameOver;
        #endregion

        #region eventek
        /// <summary>
        /// A szimuláció befejeztét jelző event
        /// </summary>
        public event EventHandler<EventArgs> SimulationEnded;

        /// <summary>
        /// Új tábla elkészültét jelző event
        /// </summary>
        public event EventHandler<EventArgs> TableCreated;

        /* DEPRECATED
        /// <summary>
        /// A táblán történő változást jelző event
        /// </summary>
        public event EventHandler<EventArgs> TableRefresh;

        /// <summary>
        /// Az idő múlását jelző event
        /// </summary>
        public event EventHandler<EventArgs> AdvanceTime;
        */
        #endregion


        #region propertyk
        /// <summary>
        /// A robotok összes lépésének (előrehaladásának) számlálója (cum.)
        /// </summary>
        public int AllSteps { get => _allSteps; set => _allSteps = value; }

        /// <summary>
        /// A robotok által összesen elhasznált energia (cum.)
        /// </summary>
        public int AllBatteryUsed { get => _allBatteryUsed; set => _allBatteryUsed = value; }

        /// <summary>
        /// A robotok listája, koordinátáik alapján O(0) időben elérhető módon tárolva
        /// </summary>
        public Dictionary<Coords, Robot> Robots { get => _robots; set => _robots = value; }

        /// <summary>
        /// A töltőállomások listája, koordinátáik alapján O(0) időben elérhető módon tárolva
        /// </summary>
        public Dictionary<Coords, Docker> Dockers { get => _dockers; set => _dockers = value; }

        /// <summary>
        /// A célállomások listája, koordinátáik alapján O(0) időben elérhető módon tárolva
        /// </summary>
        public Dictionary<Coords, Station> Stations { get => _stations; set => _stations = value; }

        /// <summary>
        /// A polcok listája, koordinátáik alapján O(0) időben elérhető módon tárolva
        /// </summary>
        public Dictionary<Coords, Pod> Pods { get => _pods; set => _pods = value; }

        /// <summary>
        /// Az A* algoritmust implementáló osztály példánya
        /// </summary>
        public AStar Astar { get => _astar; set => _astar = value; }

        /// <summary>
        /// A szimuláció táblája
        /// </summary>
        public Table Table { get => _table; set => _table = value; }

        /// <summary>
        /// A szimuláció kezdetete óta megtett lépések száma (tick)  
        /// </summary>
        public int TimerCount { get => _timerCount; set => _timerCount = value; }

        /// <summary>
        /// Igaz-hamis érték, ami megmutatja, hogy véget ért-e a szimuláció
        /// </summary>
        public bool IsGameOver { get => _isGameOver; set => _isGameOver = value; }

        public IDataAccess DataAccess { get { return _dataAccess; } set { _dataAccess = value; } }
        #endregion


        /// <summary>
        /// Segédmetódus debugoláshoz
        /// </summary>
        public void SayHi()
        {
            Debug.Write("\n ranyomott a gombra köszönök\n");
        }


        #region konstruktorok
        /// <summary>
        /// A modell konstruktora
        /// </summary>
        /// <param name="dataAccess">perzisztencia</param>
        public Model(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
            Table = new Table();
            _robots = new Dictionary<Coords, Robot>();
            _dockers = new Dictionary<Coords, Docker>();
            _pods = new Dictionary<Coords, Pod>();
            _stations = new Dictionary<Coords, Station>();
            _astar = new AStar(this);
        }

        /// <summary>
        /// A modell konstruktora
        /// </summary>
        /// <remarks>(egyelőre beégetett értékekkel)</remarks>
        public Model()
        {
            Table = new Table();
            _robots = new Dictionary<Coords, Robot>();
            _dockers = new Dictionary<Coords, Docker>();
            _pods = new Dictionary<Coords, Pod>();
            _stations = new Dictionary<Coords,Station>();
            _previousSteps = new List<Coords>();
            _reusableStationIDs = new SortedSet<int>();
            _isGameOver = false;
            GenerateFields();

            foreach (Robot robot in _robots.Values)
                robot.RobotMoved += OnRobotMoved;

            _astar = new AStar(this);
            Debug.Assert(_astar != null);

            

            /*_pods.Add(new Coords(3, 2), new Pod(3, 2, "1,2,3"));// new List<int> { 1 }
            _pods.Add(new Coords(7, 7), new Pod(7, 7, "1,2"));
            _pods.Add(new Coords(0, 7), new Pod(0, 7, "1"));
            _pods.Add(new Coords(6, 0), new Pod(6, 0, "2"));
            _pods.Add(new Coords(7, 1), new Pod(7, 1, "1,2"));

            _stations.Add(new Coords(2, 0), new Station(2, 0, 3));
            _stations.Add(new Coords(7, 4), new Station(7, 4, 2));
            _stations.Add(new Coords(3, 6), new Station(3, 6, 1));

            _dockers.Add(new Coords(3, 1), new Docker(3, 1));
            _dockers.Add(new Coords(5, 6), new Docker(5, 6));

            _robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _robots.Add(new Coords(5, 5), new Robot(5, 5, 1));
            _robots.Add(new Coords(0, 3), new Robot(0, 3, 2));
            Table.SetValue(0, 0, Types.ROBOT);
            Table.SetValue(5, 5, Types.ROBOT);
            Table.SetValue(0, 3, Types.ROBOT);

            Table.SetValue(0, 7, Types.POD);
            Table.SetValue(3, 1, Types.DOCKER);
            Table.SetValue(2, 0, Types.STATION);
            Table.SetValue(7, 4, Types.STATION);
            // _table.SetValue(7, 6, Types.POD);
            Table.SetValue(3, 2, Types.POD);
            Table.SetValue(7, 7, Types.POD);
            Table.SetValue(0, 1, Types.POD);
            // barikadolas során a quarter megtalálja/ ezen át tud még gázolni a pathfinding 
            Table.SetValue(3, 6, Types.STATION);
            Table.SetValue(6, 0, Types.POD);
            Table.SetValue(7, 1, Types.POD);
            _table.SetValue(5, 6, Types.DOCKER);

            */
        }
        #endregion

        #region a tábla inicializálásáért felelős metódusok
        /// <summary>
        /// Üresre állítja a tábla összes mezőjét
        /// </summary>
        public void GenerateFields()
        {
            for (int i = 0; i < Table.SizeX; i++)
            {
                for (int j = 0; j < Table.SizeY; j++)
                {
                    Table.SetValue(i, j, Types.EMPTY);
                }
            }
            TableCreated?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// A táblát validáló függvény.
        /// </summary>
        /// <remarks>
        /// A táblát negyedekre osztja, és megvizsgálja,
        /// hogy mindegyik célállomás elérhető-e a negyedek
        /// legalább egy üres mezőjéből.
        /// </remarks>
        /// <returns>
        /// Hamis, ha legalább egy negyedben egy olyan üres mező sincs,
        /// ahonnan nem érhető el mindegyik célállomás, egyébként igaz.
        /// </returns>
        public bool CheckQuarters()
        {
            // valahogy meg kell nézni hogyha nem talált a quarterben szabad helyet
            bool q1, q2, q3, q4;
            q1 = q2 = q3 = q4 = false;
            for (int i = 0; i < Table.SizeX / 2; i++)
            {
                for (int j = 0; j < Table.SizeY / 2; j++)
                {
                    if (!q1 && Table[i, j] == Types.EMPTY)
                    {
                        q1 = SearchStations(new Coords(i, j)) == _stations.Count;
                    }
                    if (!q2 && Table[i + Table.SizeX / 2, j] == Types.EMPTY)
                    {
                        q2 = SearchStations(new Coords(i + Table.SizeX / 2, j)) == _stations.Count;
                    }
                    if (!q3 && Table[i, j + Table.SizeY / 2] == Types.EMPTY)
                    {
                        q3 = SearchStations(new Coords(i, j + Table.SizeY / 2)) == _stations.Count;
                    }
                    if (!q4 && Table[i + Table.SizeX / 2, j + Table.SizeX / 2] == Types.EMPTY)
                    {
                        q4 = SearchStations(new Coords(i + Table.SizeY / 2, j + Table.SizeY / 2)) == _stations.Count;
                    }
                }
            }
            return q1 && q2 && q3 && q4;
        }

        /// <summary>
        /// Megszámolja a megadott koordinátákból elérhető célállomásokat.
        /// </summary>
        /// <param name="startCoords">A kiindulási koordináták</param>
        /// <returns>Az elérhető célállomások száma (nemnegatív egész szám)</returns>
        public int SearchStations(Coords startCoords) //, Types[,] table)
        {
            int count = 0;
            foreach (Coords stationCoords in _stations.Keys)
            {
                PathData pathdata = _astar.AStarSearch((Types[,])Table.Clone(), startCoords, stationCoords, true);
                if (pathdata.isValidPath) ++count;
            }
            return count;
        }
        #endregion

        #region objektumok elhelyezéséért/törléséért felelős metódusok
        /// <summary>
        /// Elhelyez egy új példányt a megadott típusból a megadott koordinátákon.
        /// </summary>
        /// <param name="coords">Az új példány koordinátái</param>
        /// <param name="type">Az új példány típusa</param>
        /// <returns>Igaz, ha az elhelyezés sikeres, egyébként hamis.</returns>
        //  A visszatérési értékkel nagyrészt nem kell foglalkoznunk, viszont így
        //  nem kellett teleírnom breakekkel a függvényt
        public bool AddObject(Coords coords, Types type)
        {
            int x = coords.X;
            int y = coords.Y;

            switch (type)
            {
                case Types.ROBOT:
                    switch (Table[x, y])
                    {
                        case Types.EMPTY:
                            Table.SetValue(x, y, Types.ROBOT);
                            break;
                        case Types.POD:
                            Table.SetValue(x, y, Types.ROBOT_UNDER_POD);
                            break;
                        case Types.STATION:
                            Table.SetValue(x, y, Types.ROBOT_ON_STATION);
                            break;
                        case Types.DOCKER:
                            Table.SetValue(x, y, Types.ROBOT_ON_DOCKER);
                            break;
                        default:
                            Debug.WriteLine("[!] Failed to add ROBOT at <{0};{1}>", x, y);
                            return false;
                    }
                    _robots.Add(coords, new Robot(coords, _robots.Count));
                    _robots[coords].RobotMoved += OnRobotMoved;
                    _robots[coords].RobotUsedEnergy += OnRobotUsedEnergy;
                    return true;

                case Types.POD:
                    switch (Table[x, y])
                    {
                        case Types.EMPTY:
                            Table.SetValue(x, y, Types.POD);
                            break;
                        case Types.ROBOT:
                            Table.SetValue(x, y, Types.ROBOT_UNDER_POD);
                            break;
                        default:
                            Debug.WriteLine("[!] Failed to add POD at <{0};{1}>", x, y);
                            return false;
                    }
                    _pods.Add(coords, new Pod(coords));
                    return true;

                case Types.STATION:
                    if (_stations.Count < _maxStationCount)
                    {
                        int id;
                        switch (Table[x, y])
                        {
                            case Types.EMPTY:
                                Table.SetValue(x, y, Types.STATION);
                                break;
                            case Types.ROBOT:
                                Table.SetValue(x, y, Types.ROBOT_ON_STATION);
                                break;
                            case Types.ROBOT_WITH_POD:
                                Table.SetValue(x, y, Types.ROBOT_WITH_POD_ON_STATION);
                                break;
                            default:
                                Debug.WriteLine("[!] Failed to add STATION at <{0};{1}>", x, y);
                                return false;
                        }
                        if (_reusableStationIDs.Count > 0)
                        {
                            id = _reusableStationIDs.Min();
                            _reusableStationIDs.Remove(id);
                        }
                        else id = ++_stationID;
                        _stations.Add(coords, new Station(coords, id));
                        return true;
                    }
                    return false;

                case Types.DOCKER:
                    switch (Table[x, y])
                    {
                        case Types.EMPTY:
                            Table.SetValue(x, y, Types.DOCKER);
                            break;
                        case Types.ROBOT:
                            Table.SetValue(x, y, Types.ROBOT_ON_DOCKER);
                            break;
                        default:
                            Debug.WriteLine("[!] Failed to add DOCKER at <{0};{1}>", x, y);
                            return false;
                    }
                    _dockers.Add(coords, new Docker(coords));
                    return true;

                default:
                    Debug.WriteLine("[!] Failed to add UNHANDLED TYPE ({0}) at <{1};{2}>", type, x, y);
                    return false;
            }
        }

        /// <summary>
        /// Elhelyez egy új példányt a megadott típusból a megadott koordinátákon.
        /// </summary>
        /// <param name="coords">Az új példány x-koordinátája</param>
        /// <param name="coords">Az új példány y-koordinátája</param>
        /// <param name="type">Az új példány típusa</param>
        /// <returns>Igaz, ha az elhelyezés sikeres, egyébként hamis.</returns>
        //  Coords példány nélküli overload
        public bool AddObject(int x, int y, Types type) => AddObject(new Coords(x, y), type);

        /// <summary>
        /// Törli az összes objektumot a megadott koordinátákon.
        /// </summary>
        /// <param name="coords">Koordináták (akár üres mező koordinátái is lehetnek)</param>
        /// <returns>Hamis, ha a törlés meghiúsul (a koordináták kezdettől fogva üres mezőre mutattak), egyébként igaz.</returns>
        public bool DelObject(Coords coords)
        {
            int x = coords.X;
            int y = coords.Y;

            switch (Table[x, y])
            {
                case Types.ROBOT:
                    _robots.Remove(coords);
                    break;

                case Types.ROBOT_UNDER_POD:
                case Types.ROBOT_WITH_POD:
                    _robots.Remove(coords);
                    _pods.Remove(coords);
                    break;

                case Types.ROBOT_WITH_POD_ON_STATION:
                    _robots.Remove(coords);
                    _pods.Remove(coords);
                    _reusableStationIDs.Add(_stations[coords].Id);
                    _stations.Remove(coords);
                    break;

                case Types.ROBOT_ON_STATION:
                    _robots.Remove(coords);
                    _reusableStationIDs.Add(_stations[coords].Id);
                    _stations.Remove(coords);
                    break;

                case Types.ROBOT_ON_DOCKER:
                    _robots.Remove(coords);
                    _dockers.Remove(coords);
                    break;

                case Types.STATION:
                    _reusableStationIDs.Add(_stations[coords].Id);
                    _stations.Remove(coords);
                    break;

                case Types.DOCKER:
                    _dockers.Remove(coords);
                    break;

                case Types.POD:
                    _pods.Remove(coords);
                    break;

                default:
                    Debug.WriteLine("[!] Failed to delete object at <{0};{1}>", coords.X, coords.Y);
                    return false;
            }
            Table.SetValue(coords.X, coords.Y, Types.EMPTY);
            return true;
        }

        /// <summary>
        /// Törli az összes objektumot a megadott koordinátákon.
        /// </summary>
        /// <param name="x">A mező X-koordinátája</param>
        /// <param name="y">A mező Y-koordinátája</param>
        /// <returns>Hamis, ha a törlés meghiúsul (a koordináták kezdettől fogva üres mezőre mutattak), egyébként igaz.</returns>
        //  Coords példány nélküli overload
        public bool DelObject(int x, int y) => DelObject(new Coords(x, y));
        #endregion

        #region termékek elhelyezéséért/törléséért felelős metódusok
        /// <summary>
        /// Felteszi a kiválasztott terméket a megadott koordinátákon lévő polcra.
        /// </summary>
        /// <param name="coords">A polc koordinátái</param>
        /// <param name="id">Termékazonosító</param>
        /// <returns>Igaz, ha a kiválasztott koordinátákon polc van, amin még nincs fent a kiválasztott termék, egyébként hamis.</returns>
        //  Itt sem kell mindig foglalkoznunk a termékek elhelyezésének sikerességével, de jól jöhet
        public bool AddProduct(Coords coords, int id)
        {
            if (_pods.ContainsKey(coords) && !_pods[coords].Items.Contains(id))
            {
                _pods[coords].Add(id);
                return true;
            }
            else if (!_pods.ContainsKey(coords) && Table[coords.X, coords.Y] == Types.POD)
            {
                _pods.Add(coords, new Pod(coords, id.ToString()));
                return true;
            }
            Debug.WriteLine("[!] Failed to add PRODUCT#{0} ({0}) at <{1};{2}>", id, coords.X, coords.Y);
            return false;
        }

        /// <summary>
        /// Felteszi a kiválasztott terméket a megadott koordinátákon lévő polcra.
        /// </summary>
        /// <param name="x">A polc X-koordinátája</param>
        /// <param name="y">A polc Y-koordinátája</param>
        /// <param name="id">Termékazonosító</param>
        /// <returns>Igaz, ha a kiválasztott koordinátákon polc van, amin még nincs fent a kiválasztott termék, egyébként hamis.</returns>
        //  Coords példány nélküli overload
        public bool AddProduct(int x, int y, int id) => AddProduct(new Coords(x, y), id);

        /// <summary>
        /// Törli a kiválasztott terméket a megadott koordinátákon lévő polcról.
        /// </summary>
        /// <param name="coords">A polc koordinátái</param>
        /// <param name="id">Termékazonosító</param>
        /// <returns>Hamis, ha a koordináták nem polcra mutatnak, vagy a polcon nincs fent a kiválasztott termék, egyébként igaz.</returns>
        public bool DelProduct(Coords coords, int id)
        {
            if (_pods.ContainsKey(coords) && _pods[coords].Items.Contains(id))
            {
                _pods[coords].Remove(id);
                return true;
            }
            Debug.WriteLine("[!] Failed to delete PRODUCT#{0} ({0}) at <{1};{2}>", id, coords.X, coords.Y);
            return false;
        }

        /// <summary>
        /// Törli a kiválasztott terméket a megadott koordinátákon lévő polcról.
        /// </summary>
        /// <param name="x">A polc X-koordinátája</param>
        /// <param name="y">A polc Y-koordinátája</param>
        /// <param name="id">Termékazonosító</param>
        /// <returns>Hamis, ha a koordináták nem polcra mutatnak, vagy a polcon nincs fent a kiválasztott termék, egyébként igaz.</returns>
        //  Coords példány nélküli overload
        public bool DelProduct(int x, int y, int id) => DelProduct(new Coords(x, y), id);
        #endregion

        
        /// <summary>
        /// A szimuláció léptetéséért felelős metódus.
        /// </summary>
        public void Step()
        {
            // megvizsgáljuk, hogy van-e még szállításra alkalmas pod;
            // ha nincs, véget ért a szimuláció
            if (_pods.Values.Where(pod => !pod.IsEmpty || !pod.Coords.Equals(pod.BaseCoords)).Count() == 0)
            {
                SimulationEnded?.Invoke(this, new EventArgs());
                return;
            }

            // készítünk egy másolatot a robotok kulcsairól, és ezeken iterálunk végig,
            // mert csak így lehet menetközben módosítani a dictionaryn
            List<Coords> robotKeys = new List<Coords>(_robots.Keys);

            foreach (Coords coords in robotKeys)
            {
                // ha a robotnak nem kell várakoznia
                if (_robots[coords].Idle == 0)
                {
                    // és van legenerált útvonala
                    if (_robots[coords].IsBusy)
                    {
                        // LÉPÉS

                        // először megnézzük, hogy docker mezőn áll-e (ahol eddig töltött)
                        // ha igen, felszabadítjuk a dockert
                        if (_dockers.ContainsKey(_robots[coords].Coords))
                            _dockers[_robots[coords].Coords].IsEmpty = true;

                        // ha nem docker felé tart és nem docker mezőn áll,
                        // utat generálunk a legközelebbi dockerhez, de csak akkor küldjük ide,
                        // ha a töltöttségi szintje mínusz az út költsége közelít a nullához 
                        if (_robots[coords].DestType != Types.DOCKER
                            && Table[coords.X, coords.Y] != Types.ROBOT_ON_DOCKER)
                                FindNearest(Types.DOCKER, _robots[coords]);

                        // megvizsgáljuk, hogy elérte-e azokat a koordinátákat, ahol le kell tennie a podot;
                        // ha igen, akkor letesszük a podot, és befejezzük az iterációt
                        if (_robots[coords].Pod != null && _robots[coords].AssignedPodCoords != null
                            && coords.Equals(_robots[coords].AssignedPodCoords))
                        {
                            _pods[coords] = _robots[coords].Pod;
                            _robots[coords].PlacePod();
                            Table.SetValue(coords.X, coords.Y, Types.ROBOT_UNDER_POD);
                            break;
                        }

                        _robots[coords].Advance(); // és lépünk
                    }

                    else // ha nincs legenerált útvonala -> elérte a célját
                    {
                        switch (_robots[coords].DestType)
                        {
                            // ha a cél pod volt, felvesszük a podot, és a podon lévő
                            // termékek függvényében üres mezőre vagy stationre állítjuk a célt
                            case Types.POD:
                                _robots[coords].LiftPod(_pods[coords]);
                                _pods[coords].IsBusy = true;
                                Table.SetValue(coords.X, coords.Y, Types.ROBOT_WITH_POD);
                                _robots[coords].DestType = _robots[coords].Pod.IsEmpty ? Types.EMPTY : Types.STATION;
                                break;

                            // ha a cél station volt, leadjuk az adott terméket, és a podon lévő
                            // termékek függvényében üres mezőre vagy stationre állítjuk a célt
                            case Types.STATION:
                                _robots[coords].RemoveItem(_stations[coords].Id);
                                _robots[coords].DestType = _robots[coords].Pod.IsEmpty ? Types.EMPTY : Types.STATION;
                                break;

                            // ha a cél üres volt, visszaértünk a pod kiindulási helyére;
                            // letesszük a podot és podra állítjuk a célt
                            case Types.EMPTY:
                                _pods[coords] = _robots[coords].Pod;
                                _pods[coords].IsBusy = false;
                                _robots[coords].PlacePod();
                                Table.SetValue(coords.X, coords.Y, Types.ROBOT_UNDER_POD);
                                _robots[coords].DestType = _pods.Values.Count(p => !p.IsBusy && (!p.IsEmpty || !p.Coords.Equals(p.BaseCoords))) == 0 ? Types.NULL : Types.POD;
                                break;

                            // ha a cél docker volt, feltöltjük a robotot,
                            // a célt pedig podra állítjuk
                            case Types.DOCKER:
                                // _dockers[coords].Charge(_robots[coords]);
                                // a docker metódusa nem tölti fel, gondolom másolatot ad át a modell, nem az eredetit
                                _robots[coords].BatteryLevel = 100;
                                _robots[coords].Idle = 5;
                                _robots[coords].DestType = Types.POD;
                                break;

                            // NULL - tétlen
                            // megvizsgáljuk, hogy történt-e változás a táblán
                            // (felhasználó / online rendelések belepiszkált-e)
                            default:
                                if (_pods.Values.Count(p => p.IsBusy == false) > 0)
                                    _robots[coords].DestType = Types.POD;
                                break;
                        }

                        // mindegyik ág végén beállítottuk az új célmező típusát, 
                        // már csak utat kell generálni odáig:
                        FindNearest(_robots[coords].DestType, _robots[coords]);
                    }
                }

                // ha a robotnak várakoznia kell, csökkentünk a várakozási időn,
                // és továbblépünk a következő robotra
                else --_robots[coords].Idle;
            }
        }

        public void Init(SaveLoadData data)
        {
            TimerCount = data.time;
            Table = data.table;
            AllBatteryUsed = data.allBatteryUsed;
            Robots = data.robots;
            Pods = data.pods;
            Stations = data.stations;
            Dockers = data.dockers;
            foreach (var robot in _robots.Values)
            {
                 robot.RobotMoved += OnRobotMoved;
            }
        }


        #region az utak generálásáért felelős metódusok és segédfüggvényeik
        /// <summary>
        /// A robot útvonala során bekövetkező fordulatokat számolja meg.
        /// </summary>
        /// <param name="path">A robot útvonala</param>
        /// <returns>Nemnegatív egész szám</returns>
        private int CountRotations(List<Coords> path)
        {
            int turns = 0;  //nézzük átlósan
            for (int i = 1; i < path.Count - 1; i++)
            {
                if (path[i - 1].X != path[i + 1].X && path[i - 1].Y != path[i + 1].Y)
                {
                    //Debug.WriteLine("növelem: {0} {1} -- {2} {3}\n", path[i - 1].X, path[i - 1].Y, path[i + 1].X, path[i + 1].Y);
                    turns++;
                }
            }
            return turns;
        }

        /// <summary>
        /// Megkeresi a megadott robothoz legközelebbi példányt a megadott típusból.
        /// </summary>
        /// <param name="type">A keresendő objektum típusa</param>
        /// <param name="r">A robot objektum</param>
        private void FindNearest(Types type, Robot r)
        {
            switch (type)
            {
                case Types.DOCKER:
                    FindNearestDocker(r);
                    break;
                case Types.STATION:
                  //  Debug.WriteLine(_robots[item.Key].Pod.Items.Count);
                    FindNearestStation(r);
                    break;
                case Types.POD:
                    FindNearestPod(r);
                    break;
                case Types.EMPTY:
                    ReturnPod(r);
                    break;
                default: // NULL - tétlen
                    break;
            }
        }
      
        

        /// <summary>
        /// Megkeresi a robothoz legközelebbi polcot, amit el kell szállítani.
        /// </summary>
        /// <param name="r">A robot objektum</param>
        /// <returns>Null, ha nincs olyan polc, ami nemüres vagy nincs az eredeti helyén, egyébként egy ilyen polc koordinátái.</returns>
        public Coords NearestPod(Robot r)
        {
            Coords result = null;

            if (r.AssignedPodCoords != null)
            {
                result = r.AssignedPodCoords;
                r.AssignedPodCoords = null;
                return result;
            }

            double minDist = double.MaxValue;
            foreach (var pod in _pods.Values)
            {
                if (!pod.IsBusy && (!pod.IsEmpty || !pod.Coords.Equals(pod.BaseCoords)))
                {
                    double tmp = r.Coords.DistanceFrom(pod.Coords);
                    if (minDist > tmp)
                    {
                        result = pod.Coords;
                        minDist = tmp;
                        Debug.WriteLine(result.X + " " + result.Y + " " + minDist + "\n");
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Utat generál a robottól a hozzá legközelebbi szállításra alkalmas polcig.
        /// </summary>
        /// <param name="r">A robot objektum</param>
        public void FindNearestPod(Robot r)
        {
            // _pods = _pods.OrderByDescending(i => i.Value.Items.Count).ToDictionary(x => x.Key, x => x.Value); // lerendezzük csökkenő sorrendbe tárgyak szerint [why?]
            Coords nearestPodCoords = NearestPod(r);
            if (nearestPodCoords == null)
            {
                r.DestType = Types.NULL;
                return;
            }

            Debug.WriteLine("---------------------------- legközelebbi pod: " + nearestPodCoords.X + " " + nearestPodCoords.Y);

            PathData pathdata = _astar.AStarSearch((Types[,])Table.Clone(), r.Coords, nearestPodCoords, false); // ha podot keresel, akkor biztosan nincs rajtad pod
            if (!pathdata.isValidPath) return; // TODO: hiba kiváltása, ha nem találta meg a podot

            r.CoordsList = pathdata.path;
            r.TravelCost = CountRotations(r.CoordsList) + r.CoordsList.Count;
            _pods[nearestPodCoords].IsBusy = true;

            // Debug.WriteLine("\n\n FORDULÁSOKKAL MENNYI " + r.TravelCost + " ut hossza: " + r.CoordsList.Count + "\n\n");
            // Debug.WriteLine(r.Pod.IsBusy + " " + r.Pod.Items.Count + " " + r.Pod.Coords.X + " " + r.Pod.Coords.Y);
            // Debug.WriteLine(_pods[nearestPodCoords].IsBusy);
            
        }

        /// <summary>
        /// Utat generál a robottól a szállított polc kiindulási helyéig.
        /// </summary>
        /// <param name="r">A robot objektum</param>
        private void ReturnPod(Robot r)
        {
            // Debug.WriteLine(nearest.X + " " + nearest.Y +"\n");
            _astar.result.path = new List<Coords>();  // valahogy ki kell törölni az előző utat, DE EZ MÉG HASZNOS LEHET, HOGY MEGVAN EGYBEN!!!!
            PathData path = _astar.AStarSearch((Types[,])Table.Clone(), r.Coords, r.Pod.BaseCoords, true);
            if (!path.isValidPath) return; // TODO: hiba kiváltása, ha nem találta meg a pod kiindulási helyét

            r.CoordsList = path.path;
            r.TravelCost = CountRotations(r.CoordsList) + r.CoordsList.Count;
        }



        /// <summary>
        /// Kiválasztja a robothoz legközelebbi célállomást, ahova tud terméket szállítani.
        /// </summary>
        /// <param name="r">A robot objektum</param>
        /// <returns>Ha a polc (valamilyen bug folytán) üres, akkor null, egyébként egy alkalmas célállomás koordinátái</returns>
        public Coords NearestStation(Robot r)
        {
            Coords result = null;
            double minDist = double.MaxValue;
            foreach (Station station in _stations.Values)
            {
                if(r.Pod.HasItem(station.Id))
                {
                    double tmp = r.Coords.DistanceFrom(station.Coords);
                    if (minDist > tmp)
                    {
                        result = station.Coords;
                        minDist = tmp;
                        Debug.WriteLine("station: "+result.X + " " + result.Y + " " + minDist + "\n");
                    }
                }
            }
            return result;
        }
       
        /// <summary>
        /// Utat generál a robottól a legközelebbi alkalmas célállomásig.
        /// </summary>
        /// <param name="r">A robot objektum</param>
        public void FindNearestStation(Robot r)
        {
            //Debug.WriteLine("elemek szama: " + r.Pod.Items.Count);
            //Debug.WriteLine("Robot helye: : " + r.Coords.X + " " + r.Coords.Y);
            Debug.WriteLine("hanyszor leptem be");

            Coords nearestStationCoords = NearestStation(r);
            //Debug.WriteLine(nearest.X + " " + nearest.Y +"\n");
            _astar.result.path = new List<Coords>();  // valahogy ki kell törölni az előző utat, DE EZ MÉG HASZNOS LEHET, HOGY MEGVAN EGYBEN!!!!
                                                      // [...how? miért tárolnád el, hogy hogyan jutottál el a podhoz?]
            if (nearestStationCoords == null) ReturnPod(r); // elvileg erre sosem kerülhetne sor, max valami bug következményeként

            PathData pathdata = _astar.AStarSearch((Types[,])Table.Clone(), r.Coords, nearestStationCoords, true);
            if (!pathdata.isValidPath) return; // TODO: hiba kiváltása, ha nem találta meg a Stationt

            r.CoordsList = pathdata.path;
            r.TravelCost = CountRotations(r.CoordsList) + r.CoordsList.Count;
        }



        /// <summary>
        /// Kiválasztja a robothoz legközelebbi üres töltőállomást.
        /// </summary>
        /// <param name="r">A robot objektum</param>
        /// <returns>Ha nincs üres docker, akkor null, egyébként a legközelebbi töltőállomás koordinátái</returns>
        public Coords NearestDocker(Robot r)
        {
            // TODO: egyelőre nem veszi figyelembe, hogy a docker foglalt-e vagy sem
            Coords result = null;
            double minDist = double.MaxValue;
            foreach (Docker docker in _dockers.Values.Where(docker => docker.IsEmpty))
            {
                double tmp = r.Coords.DistanceFrom(docker.Coords);
                if (minDist > tmp)
                {
                    result = docker.Coords;
                    minDist = tmp;
                    Debug.WriteLine("station: " + result.X + " " + result.Y + " " + minDist + "\n");
                }
            }
            return result;
        }

        /// <summary>
        /// Visszaadja a megadott útvonal első üres mezőjét.
        /// </summary>
        /// <param name="path">Az útvonal (koordinátapárok listája)</param>
        /// <returns>Ha nincs üres mező az útvonalon, akkor null, egyébként az első üres mező koordinátái</returns>
        private Coords FindFirstEmptyCellInPath(List<Coords> path)
        {
            Coords result = null;
            foreach (Coords coords in path)
            {
                if (Table[coords.X, coords.Y] == Types.EMPTY)
                {
                    result = coords;
                    break;
                }
            }
            return result;
        }

        /// <summary>
        /// Utat generál a robottól a legközelebbi töltőállomásig, és elküldi oda (felülírva az eddigi utat), ha a töltöttségi szintje túl alacsony
        /// </summary>
        /// <param name="r">A robot objektum</param>
        public void FindNearestDocker(Robot r) 
        {
            // lekérjük a legközelebbi docker koordinátáit
            Debug.WriteLine("Finding nearest docker...");
            Coords nearestDockerCoords = NearestDocker(r);
            if (nearestDockerCoords == null) return;
            // TODO: hiba kiváltása, ha nincs docker (ez is csak bug miatt történhet meg)

            List<Coords> path = null;
            Coords firstEmptyCell = null;

            // legeneráljuk az útvonalat a dockerhez
            _astar.result.path = new List<Coords>();
            PathData pathdata = _astar.AStarSearch((Types[,])Table.Clone(), r.Coords, nearestDockerCoords, r.Pod != null);
            if (!pathdata.isValidPath || pathdata.path.Count == 0) return;
            // TODO: hiba kiváltása, ha nem találta meg a Dockert - ezen optimalizálni kell, mert lehet hogy csak pillanatnyilag blokkolt a docker

            // ha a roboton van pod...
            if (r.Pod != null)
            {
                // megkeressük az első üres cellát a robot és a docker között
                firstEmptyCell = FindFirstEmptyCellInPath(pathdata.path);
                if (firstEmptyCell != null)
                {
                    // ezt a koordinátát kimentjük (itt kell majd letennie a robotnak a podot),
                    // és ettől a koordinátától új utat generálunk a dockerhez, innentől pod nélkül
                    List<Coords> pathToFirstEmptyCell = pathdata.path.GetRange(0, pathdata.path.FindIndex(x => x.Equals(firstEmptyCell)) + 1);
                    pathdata = _astar.AStarSearch((Types[,])Table.Clone(), firstEmptyCell, nearestDockerCoords, false);
                    path = pathToFirstEmptyCell.Concat(pathdata.path).ToList();
                }
                // TODO: kezelni kell, hogyha stationről közvetlenül mozogna docker mezőre poddal, mert nem tudja letenni útközben a podot
                else path = pathdata.path;
            }
            // ha nincs rajta pod, az útvonal változatlan
            else path = pathdata.path;

            // csak akkor küldjük el tölteni, ha a töltöttségi szint mínusz a legenerált út költsége elér egy kritikus határt
            if (CountRotations(path) + path.Count() + 10 >= r.BatteryLevel) // TODO: hagytam helyet hibának (5), ezen még lehet optimalizálni
            {
                Debug.WriteLine("SENDING ROBOT TO DOCKER");

                r.CoordsList = path;
                r.DestType = Types.DOCKER;
                r.TravelCost = CountRotations(path) + path.Count;
                _dockers[nearestDockerCoords].IsEmpty = false;

                r.AssignedPodCoords = firstEmptyCell;
            }
        }
        #endregion

        #region eventhandlerek
        /// <summary>
        /// A robotok töltöttségi szintjének változására reagáló eventhandler metódus,
        /// ami növeli az összes felhasznált energia számlálóját eggyel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRobotUsedEnergy(object sender, EventArgs e) { ++_allBatteryUsed; }

        /// <summary>
        /// A robot lépés-eventjét kezelő metódus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="kvp"></param>
        public void OnRobotMoved(object sender, KeyValuePair<Coords, Coords> kvp)
        {
            Coords src = kvp.Key;
            Types srcType = Table.GetValue(src.X, src.Y);

            Coords dest = kvp.Value;
            Types destType = Table.GetValue(dest.X, dest.Y);

            // ha a célmezőn robot áll, egy körig várunk
            if (destType == Types.ROBOT
                || destType == Types.ROBOT_UNDER_POD
                || destType == Types.ROBOT_WITH_POD
                || destType == Types.ROBOT_WITH_POD_ON_STATION
                || destType == Types.ROBOT_ON_STATION
                || destType == Types.ROBOT_ON_DOCKER)
            {
                // frontális ütközés
                if (_robots[src].IsFacingOpposite(_robots[dest])
                    || _robots[dest].DestType == Types.NULL)
                {
                    HandleFrontalCollision(src, dest);
                }

                // oldalsó ütközés
                else
                {
                    _robots[src].Idle = 1;
                }

                return;
            }

            Robot tmp = _robots[src];

            // átállítjuk a célmező értékét a táblán,
            // frissítjük a mozgó objektumok koordinátáit
            if (destType == Types.EMPTY)
            {
                if (_robots[src].Pod != null)
                {
                    Table.SetValue(dest.X, dest.Y, Types.ROBOT_WITH_POD);
                    _pods.Add(dest, tmp.Pod);
                    _robots.Add(dest, tmp);
                    _pods.Remove(src);
                    _robots.Remove(src);
                }
                else
                {
                    Table.SetValue(dest.X, dest.Y, Types.ROBOT);
                    _robots.Add(dest, tmp);
                    _robots.Remove(src);
                }
            }

            else if (destType == Types.POD)
            {
                if (_robots[src].Pod != null)
                {
                    // a célmező pod, de a robot épp podot szállít
                    // -> egy robot eltorlaszolta az utat egy poddal

                    // új utat generálunk
                    FindNearest(_robots[src].DestType, _robots[src]);
                    return;
                }
                Table.SetValue(dest.X, dest.Y, Types.ROBOT_UNDER_POD);
                _robots.Add(dest, tmp);
                _robots.Remove(src);
            }

            else if (destType == Types.DOCKER)
            {
                Table.SetValue(dest.X, dest.Y, Types.ROBOT_ON_DOCKER);
                _robots.Add(dest, tmp);
                _robots.Remove(src);
            }

            else if (destType == Types.STATION)
            {
                if (_robots[src].Pod != null)
                {
                    Table.SetValue(dest.X, dest.Y, Types.ROBOT_WITH_POD_ON_STATION);
                    _robots.Add(dest, tmp);
                    _pods.Add(dest, tmp.Pod);
                    _robots.Remove(src);
                    _pods.Remove(src);
                }
                else
                {
                    Table.SetValue(dest.X, dest.Y, Types.ROBOT_ON_STATION);
                    _robots.Add(dest, tmp);
                    _robots.Remove(src);
                }
            }

            // frissítjük a robot (és a szállított pod) koordinátáit
            _robots[dest].Coords = dest;
            _robots[dest].CoordsList.RemoveAt(0);
            if (_robots[dest].Pod != null) _robots[dest].Pod.Coords = dest;

            // átállítjuk a kiindulási hely típusát a táblán
            switch (srcType)
            {
                case Types.ROBOT_ON_DOCKER:
                    Table.SetValue(src.X, src.Y, Types.DOCKER);
                    break;
                case Types.ROBOT_ON_STATION:
                case Types.ROBOT_WITH_POD_ON_STATION:
                    Table.SetValue(src.X, src.Y, Types.STATION);
                    break;
                case Types.ROBOT_UNDER_POD:
                    Table.SetValue(src.X, src.Y, Types.POD);
                    break;
                default:
                    Table.SetValue(src.X, src.Y, Types.EMPTY);
                    break;
            }

            // csökkentjük a robot töltöttségi szintjét, és növeljük az összesen felhasznált energiát
            --_robots[dest].BatteryLevel;
            ++_robots[dest].BatteryUsed;
            ++_allBatteryUsed;
            ++_allSteps;
            Debug.WriteLine("ROBOT MOVED FROM <{0};{1}> TO <{2};{3}>", src.X, src.Y, dest.X, dest.Y);
        }

        /// <summary>
        /// Megadja egy robot útjának a prioritását a frontális ütközés kezeléséhez.
        /// A nagyobb prioritású robot fog várakozni, míg a másik kikerüli.
        /// </summary>
        /// <remarks>
        /// A tétlen robotok prioritása 0.
        /// </remarks>
        /// <param name="r">A robot objektum</param>
        /// <returns>Egy természetes számot, ami az út fontosságát képviseli</returns>
        private int RoutePriority(Robot r)
        {
            if (r.DestType == Types.NULL) return 0;
            int result = 0;
            if (r.Pod != null) result += 1;
            if (r.DestType == Types.DOCKER) result += 2;
            return result;
        }

        /// <summary>
        /// A robotok frontális ütközését kezelő metódus:
        /// az egyik robotot várakoztatja, a másikat kerülőútra küldi.
        /// A szerepek kiosztását a robotok célja befolyásolja, a töltöttségi szintjük,
        /// valamint az, hogy melyikük hordoz polcot.
        /// </summary>
        /// <remarks>
        /// Ezt a metódust használjuk abban az esetben is, ha egy tétlen robot állja el
        /// egy még dolgozó robot útját, akkor is, ha nem ellentétes irányba néznek.
        /// </remarks>
        /// <param name="first">Egy robot objektum</param>
        /// <param name="second">Egy másik robot objektum, ami az előzővel frontálisan ütközik</param>
        public void HandleFrontalCollision(Coords first, Coords second)
        {
            Coords waiter; // ő fog várni, amíg a másik kikerüli
            Coords mover; // ő fog elmenni az útból

            // kiválasztjuk, melyik robot fog mozogni, és melyik várakozik
            if (RoutePriority(_robots[first]) > RoutePriority(_robots[second])
                || (RoutePriority(_robots[first]) == RoutePriority(_robots[second])
                    && _robots[first].BatteryLevel < _robots[second].BatteryLevel))
            {
                waiter = first;
                mover = second;
            }
            else
            {
                waiter = second;
                mover = first;
            }

            bool moverHasPod = _robots[mover].Pod != null; // ez a lépésre alkalmas mezők szűréséhez kell
            Coords diff = waiter - mover; // ez az esetleges hátramozgatáshoz kell
            Coords sidestep = null; // ide fogjuk eltéríteni a mozgatandó robotot
            
            // bevezettem egy változót a ciklushoz
            //  - ha nulla, az azt jelenti, hogy az egyik robotot sikeresen eltérítettük
            //  - minden iterációban, ahol nem sikerül eltéríteni a mozgatandó robotot,
            //    mert minden oldalról blokkolt, a változó értéke eggyel nő
            // (a végtelen ciklus elkerülése miatt csináltam így, de a tényleges kezelés még nincs implementálva)
            int tries = 1;

            while (tries > 0)
            {
                if (diff.X != 0) // a vízszintes tengely mentén próbál mozogni
                { 
                    // megpróbáljuk:

                    // 1) egy mezővel felfelé mozgatni
                    if (mover.Y + 1 < Table.SizeY && 
                       ((moverHasPod && new Types[] { Types.EMPTY, Types.STATION }.Contains(Table.GetValue(mover.X, mover.Y + 1))) ||
                       (!moverHasPod && new Types[] { Types.EMPTY, Types.STATION,
                                                      Types.POD,   Types.DOCKER }.Contains(Table.GetValue(mover.X, mover.Y + 1)))))
                    {
                        sidestep = new Coords(mover.X, mover.Y + 1);
                        tries = 0;
                    }
                    
                    // 2) egy mezővel lefelé mozgatni
                    else if (mover.Y - 1 >= 0 &&
                            ((moverHasPod && new Types[] { Types.EMPTY, Types.STATION }.Contains(Table.GetValue(mover.X, mover.Y - 1))) ||
                            (!moverHasPod && new Types[] { Types.EMPTY, Types.STATION,
                                                           Types.POD,   Types.DOCKER }.Contains(Table.GetValue(mover.X, mover.Y - 1)))))
                    {
                        sidestep = new Coords(mover.X, mover.Y - 1);
                        tries = 0;
                    }

                    // 3) egy mezővel hátrafelé mozgatni
                    else if (mover.X - diff.X >= 0 &&
                            ((moverHasPod && new Types[] { Types.EMPTY, Types.STATION }.Contains(Table.GetValue(mover.X - diff.X, mover.Y))) ||
                            (!moverHasPod && new Types[] { Types.EMPTY, Types.STATION,
                                                           Types.POD,   Types.DOCKER }.Contains(Table.GetValue(mover.X - diff.X, mover.Y)))))
                    {
                        sidestep = new Coords(mover.X - diff.X, mover.Y);
                        tries = 0;
                    }

                    // 4) minden irányból blokkolt; a másik robotot próbáljuk mozgatni
                    else
                    {
                        Debug.WriteLine("Robot at <{0};{1}> can't move out of the way (obstructed from all directions);\n" + 
                                        "Trying other robot...", mover.X, mover.Y);
                        Coords temp = mover;
                        mover = waiter;
                        waiter = temp;
                        ++tries;
                    }
                }
                else // diff.Y != 0 -> a függőleges tengely mentén próbál mozogni
                { 
                    // megpróbáljuk:

                    // 1) egy mezővel jobbra mozgatni
                    if (mover.X + 1 < Table.SizeX && 
                       ((moverHasPod && new Types[] { Types.EMPTY, Types.STATION }.Contains(Table.GetValue(mover.X + 1, mover.Y))) ||
                       (!moverHasPod && new Types[] { Types.EMPTY, Types.STATION,
                                                      Types.POD,   Types.DOCKER }.Contains(Table.GetValue(mover.X + 1, mover.Y)))))
                    {
                        sidestep = new Coords(mover.X + 1, mover.Y);
                        tries = 0;
                    }

                    // 2) egy mezővel balra mozgatni
                    else if (mover.X - 1 >= 0 &&
                            ((moverHasPod && new Types[] { Types.EMPTY, Types.STATION }.Contains(Table.GetValue(mover.X - 1, mover.Y))) ||
                            (!moverHasPod && new Types[] { Types.EMPTY, Types.STATION,
                                                           Types.POD,   Types.DOCKER }.Contains(Table.GetValue(mover.X - 1, mover.Y)))))
                    {
                        sidestep = new Coords(mover.X - 1, mover.Y);
                        tries = 0;
                    }

                    // 3) egy mezővel hátrafelé mozgatni
                    else if (mover.Y - diff.Y >= 0 &&
                            ((moverHasPod && new Types[] { Types.EMPTY, Types.STATION }.Contains(Table.GetValue(mover.X, mover.Y - diff.Y))) ||
                            (!moverHasPod && new Types[] { Types.EMPTY, Types.STATION,
                                                           Types.POD,   Types.DOCKER }.Contains(Table.GetValue(mover.X, mover.Y - diff.Y)))))
                    {
                        sidestep = new Coords(mover.X, mover.Y - diff.Y);
                        tries = 0;
                    }

                    // 4) minden irányból blokkolt; a másik robotot próbáljuk mozgatni
                    else
                    {
                        Debug.WriteLine("Robot at <{0};{1}> can't move out of the way (obstructed from all directions);\n" +
                                        "Trying other robot...", mover.X, mover.Y);
                        Coords temp = mover;
                        mover = waiter;
                        waiter = temp;
                        ++tries;
                    }
                }

                if (tries > 5) 
                {
                    // TODO: HIBA - mindkét robot be van falazva (végtelen ciklus)
                }
            }

            Debug.WriteLine("MOVER:    <{0};{1}>\n" +
                            "SIDESTEP: <{2};{3}>\n" +
                            "WAITER:   <{4};{5}>\n" +
                            "DIFF:     <{6};{7}>", 
                            mover.X,    mover.Y, 
                            sidestep.X, sidestep.Y, 
                            waiter.X,   waiter.Y, 
                            diff.X,     diff.Y);

            _robots[mover].CoordsList.Insert(0, sidestep); // megtörténik az eltérítés: módosítjuk a mozgatandó robot útját
            _robots[waiter].Idle = 2;                      // a másik robotot addig várakoztatjuk

            // ha a robot tétlen, manuálisan kell mozgatnunk (a step() metódus kihagyja a tétlen robotokat)
            if (_robots[mover].DestType == Types.NULL)
                _robots[mover].Advance();

            // ha nem volt tétlen, vissza kell mozgatnunk a kiindulási helyére, különben lenne egy "lyuk" az útjában
            else _robots[mover].CoordsList.Insert(1, mover); 
            // TODO: vajon elegánsabb megoldás lenne, ha a kiindulási helyre küldés helyett az új pozícióból új utat generálnánk?
        }

        #endregion
    }
}
