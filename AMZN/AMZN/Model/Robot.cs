using AMZN.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace AMZN.Model
{
    /// <summary>
    /// A robot irányát jelölő típus
    /// </summary>
    public enum Direction
    {
        NORTH,
        EAST,
        SOUTH,
        WEST
    }

    /// <summary>
    /// A robotokat reprezentáló osztály
    /// </summary>
    [DataContract]
    public class Robot
    {
        #region privát adattagok
        private Coords _coords;
        private int _id;
        private int _batteryUsed;
        private int _batteryLevel;
        private Pod _pod = null;
        private Direction _direction;
        private List<Coords> _coordsList;
        // DEPRECATED private bool _isBusy;
        private int _travelCost;
        private int _dockerCost;
        private Types _destType;
        private int _idle;
        private Coords _assignedPodCoords = null;
        #endregion

        #region propertyk
        /// <summary>
        /// A koordináták, ahol a robot leteheti és később újra felveheti az általa szállított podot,
        /// ha szállítás közben el kell mennie tölteni.
        /// </summary>
        /// <remarks>Csak akkor van értéke, ha le kell tennie / újra fel kell vennie egy podot, egyébként null.</remarks>
        public Coords AssignedPodCoords { get => _assignedPodCoords; set => _assignedPodCoords = value; }

        /// <summary>
        /// A robot koordinátái
        /// </summary>
        [DataMember]
        public Coords Coords { get => _coords; set => _coords = value; }

        /// <summary>
        /// A robot sorszáma
        /// </summary>
        [DataMember]
        public int Id { get => _id; set => _id = value; }

        /// <summary>
        /// A robot töltöttségi szintje
        /// </summary>
        [DataMember]
        public int BatteryLevel { get => _batteryLevel; set => _batteryLevel = value; }

        /// <summary>
        /// A robot által felhasznált összes energia
        /// </summary>
        public int BatteryUsed { get => _batteryUsed; set => _batteryUsed = value; }

        /// <summary>
        /// Az irány, amerre a robot éppen néz
        /// </summary>
        internal Direction Direction { get => _direction; set => _direction = value; }

        /// <summary>
        /// A robot útvonala (koordináták listájaként reprezentálva)
        /// </summary>
        public List<Coords> CoordsList { get => _coordsList; set => _coordsList = value; }
        
        /// <summary>
        /// Igaz-hamis érték, ami azt mutatja meg, hogy a robotnak van-e épp feladata
        /// </summary>
        public bool IsBusy { get => _coordsList.Count() > 0; }

        /// <summary>
        /// A polc, amit a robot szállít (értéke null, ha épp nem szállít polcot)
        /// </summary>
        public Pod Pod { get => _pod; set => _pod = value; }

        /// <summary>
        /// A robot útjának költsége
        /// </summary>
        public int TravelCost { get => _travelCost; set => _travelCost = value; }

        /// <summary>
        /// A robot töltőállomáshoz való útjának költsége
        /// </summary>
        public int DockerCost { get => _dockerCost; set => _dockerCost = value; }

        /// <summary>
        /// A robot célmezőjének típusa
        /// </summary>
        public Types DestType { get => _destType; set => _destType = value; }

        //public Coords Destination { get => _destination; set => _destination = value; }
        //public Coords NearestDocker { get => _nearestDocker; set => _nearestDocker = value; }
        //public Types DestType { get => _destType; set => _destType = value; }
        
        /// <summary>
        /// A körök száma, amíg a robotnak várakoznia kell (alapból 0)
        /// </summary>
        public int Idle { get => _idle; set => _idle = value; }
        #endregion

        #region eventek
        /// <summary>
        /// Event annak jelzésére, hogy a robot mozgott a táblán. Két koordinátát ad át: a kiindulási helyet (key) és a célt (value).
        /// </summary>
        public EventHandler<KeyValuePair<Coords, Coords>> RobotMoved;
        
        /// <summary>
        /// Event annak jelzésére, hogy a robot energiát használt fel.
        /// </summary>
        public EventHandler<EventArgs> RobotUsedEnergy;
        #endregion

        #region konstruktorok
        /// <summary>
        /// A robot konstruktora
        /// </summary>
        /// <param name="x">x-koordináta</param>
        /// <param name="y">y-koordináta</param>
        /// <param name="id">sorszám</param>
        public Robot(int x, int y, int id)
        {
            _coordsList = new List<Coords>();
            _coords = new Coords(x, y);
            _id = id;
            _batteryLevel = 100;
            // _isBusy = false;
            _idle = 0;
            _destType = Types.NULL;
        }

        /// <summary>
        /// A robot konstruktora
        /// </summary>
        /// <param name="coords">koordináták</param>
        /// <param name="id">sorszám</param>
        public Robot(Coords coords, int id)
        {
            _coordsList = new List<Coords>();
            _coords = coords;
            _id = id;
            _batteryLevel = 100;
            // _isBusy = false;
            _idle = 0;
            _destType = Types.NULL;
        }
        #endregion

        #region metódusok és függvények
        /// <summary>
        /// A robot léptetőmetódusa: ha megfelelő irányba néz, előrehalad, egyébként megfelelő irányba forog.
        /// </summary>
        public void Advance()
        {
            if (_coordsList.Count > 0 && _coords.Equals(_coordsList[0])) _coordsList.RemoveAt(0);
            if (_batteryLevel > 0 && _coordsList.Count > 0)
            {
                // A robot megfelelő irányba néz
                Direction _destDirection = _coords.NeighborDirection(_coordsList[0]);
                if (_direction == _destDirection)
                {
                    RobotMoved?.Invoke(this, new KeyValuePair<Coords, Coords>(_coords, _coordsList[0]));
                }
                else
                {
                    if (_direction == Direction.NORTH && _destDirection == Direction.EAST
                          || _direction == Direction.EAST && _destDirection == Direction.SOUTH
                          || _direction == Direction.SOUTH && _destDirection == Direction.WEST
                          || _direction == Direction.WEST && _destDirection == Direction.NORTH)
                    {
                        Debug.WriteLine("Direction is {0} when it should be {1}, turning right", _direction, _coords.NeighborDirection(_coordsList[0]));
                        TurnRight();
                    }
                    else
                    {
                        Debug.WriteLine("Direction is {0} when it should be {1}, turning left", _direction, _coords.NeighborDirection(_coordsList[0]));
                        TurnLeft();
                    }
                }
            }
        }

        /// <summary>
        /// Metódus, amivel a robot felvesz egy polcot, ha Pod mezőn áll.
        /// </summary>
        /// <param name="pod">A polc objektum, amit felvesz a robot</param>
        public void LiftPod(Pod pod) 
        {
            Pod = pod;
            --_batteryLevel;
            RobotUsedEnergy?.Invoke(this, null);
            Debug.WriteLine("--Battery [LIFTPOD]");
        }

        /// <summary>
        /// Metódus, amivel a robot leteszi a polcot, amit épp szállít, feltéve, hogy szállít polcot és üres mezőn áll.
        /// </summary>
        public void PlacePod() 
        {
            Pod = null;
            --_batteryLevel;
            RobotUsedEnergy?.Invoke(this, null);
            Debug.WriteLine("--Battery [PLACEPOD]");
        }

        /// <summary>
        /// Metódus, amivel a robot lead egy terméket a polcról, amit szállít.
        /// Csak akkor működik, ha olyan Station mezőn áll, aminek sorszáma megegyezik a leadandó termék sorszámával.
        /// </summary>
        /// <param name="id">A leadandó termék sorszáma</param>
        public void RemoveItem(int id)
        {
            Pod.Remove(id);
            --_batteryLevel;
            RobotUsedEnergy?.Invoke(this, null);
            Debug.WriteLine("--Battery [REMOVEITEM]");
        }

        /// <summary>
        /// Elforgatja a robotot 90 fokkal balra.
        /// </summary>
        private void TurnLeft()
        {
            _direction = _direction == Direction.NORTH ? Direction.WEST : --_direction;
            --_batteryLevel;
            RobotUsedEnergy?.Invoke(this, null);
            Debug.WriteLine("--Battery [TURNLEFT]");
        }

        /// <summary>
        /// Elforgatja a robotot 90 fokkal balra.
        /// </summary>
        private void TurnRight()
        {
            _direction = _direction == Direction.WEST ? Direction.NORTH : ++_direction;
            --_batteryLevel;
            RobotUsedEnergy?.Invoke(this, null);
            Debug.WriteLine("--Battery [TURNRIGHT]");
        }

        /// <summary>
        /// Szöveges reprezentációt ad a robotról.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return base.ToString();
        }

        /// <summary>
        /// Megadja, hogy két robot ellenkező irányba néz-e (frontális ütközés vizsgálatához).
        /// </summary>
        /// <param name="other">A másik Robot-objektum</param>
        /// <returns>Igaz, ha a robotok ellentétet irányba (egymással szembe) néznek; egyébként hamis.</returns>
        public bool IsFacingOpposite(Robot other)
        {
            if (_direction == Direction.NORTH && other.Direction == Direction.SOUTH ||
                _direction == Direction.SOUTH && other.Direction == Direction.NORTH ||
                _direction == Direction.WEST && other.Direction == Direction.EAST ||
                _direction == Direction.EAST && other.Direction == Direction.WEST)
                    return true;
            return false;
        }
        #endregion
    }
}

/* ez meg jol johet
 https://stackoverflow.com/questions/36372713/c-sharp-a-algorithm-gives-wrong-results
*/
