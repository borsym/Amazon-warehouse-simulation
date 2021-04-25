using AMZN.Model;
using AMZN.Persistence;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace AMZN.UnitTests
{
    
    [TestClass]
    public class AMZNTests
    {
        private Mock<IDataAccess> _mock; // az adatelérés mock-ja
        private Model.Model _model;

        [TestInitialize]
        public void Initialize() // teszt inicializálása
        {
            _mock = new Mock<IDataAccess>();

            _mock.Setup(mock => mock.LoadAsync(It.IsAny<String>()));
            _model = new Model.Model(_mock.Object); // példányosítjuk a modellt a mock objektummal
            
        }
        
        [TestMethod]
        public void ModelConstructorTest() // egységteszt mûvelet
        {
            Assert.AreEqual(0, _model.Robots.Count);
            Assert.AreEqual(0, _model.Pods.Count);
            Assert.AreEqual(0, _model.Stations.Count);
            Assert.AreEqual(0, _model.Dockers.Count);
            Assert.AreEqual(8, _model.Table.SizeX);
            Assert.AreEqual(8, _model.Table.SizeY);
            
            Assert.IsTrue(_model.CheckQuarters());

            for (Int32 i = 0; i < _model.Table.SizeX; i++)
                for (Int32 j = 0; j < _model.Table.SizeY; j++)
                    Assert.AreEqual(Types.EMPTY, _model.Table[i, j]); // valamennyi mezõ üres
        }
        [TestMethod]
        public void ModelNewGameEvent()
        {
            bool eventRaised = false;
            _model.TableCreated += delegate (object sender, Model.EventArgs e)
            {
                eventRaised = true;
            };
            _model.GenerateFields();
            Assert.IsTrue(eventRaised); // kiváltottuk-e az eseményt

        }

        [TestMethod]
        public void ModelSimulationOverEvent()
        {
            bool eventRaised = false;
            _model.SimulationEnded += delegate (object sender, Model.EventArgs e)
            {
                eventRaised = true;
            };
            _model.GenerateFields();
            _model.Step();
            Assert.IsTrue(eventRaised); // kiváltottuk-e az eseményt

        }

        [TestMethod]  
        public void RobotPickUpPod()
        {
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(0, 1), new Pod(0, 1, "1"));
            _model.Stations.Add(new Coords(0, 4), new Station(0,4,1));

            foreach (var robot in _model.Robots)
                robot.Value.RobotMoved += _model.OnRobotMoved;

            var _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            var _pod = _model.Pods[new Coords(0,1)];

            _model.Step();
            Assert.AreEqual(Types.POD, _robot.DestType);

            _model.Step();
            _model.Step();

            _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            Assert.AreEqual(Types.POD, _robot.DestType);
            Assert.AreEqual(98, _robot.BatteryLevel);

            Assert.AreEqual(Types.POD, _robot.DestType);
            Assert.AreEqual(0, _robot.Coords.X);
            Assert.AreEqual(1, _robot.Coords.Y);

            Assert.AreEqual(_robot.Coords, _pod.Coords);

            _model.Step();
            _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            Assert.IsNotNull(_robot.Pod);
        }
   
        [TestMethod]
        public void ModelHandleFrontalCollision()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 1), new Robot(0, 1, 0));
            _model.Robots.Add(new Coords(0, 3), new Robot(0, 3, 1));

            _model.Dockers.Add(new Coords(0, 0), new Docker(0, 0));
            _model.Pods.Add(new Coords(0, 4), new Pod(0, 4, "1"));

            _model.Robots[new Coords(0, 3)].DestType = Types.DOCKER;
            _model.Robots[new Coords(0, 1)].DestType = Types.POD;

            foreach (var robot in _model.Robots)
                robot.Value.RobotMoved += _model.OnRobotMoved;

            _model.HandleFrontalCollision(new Coords(0, 1), new Coords(0, 3));
            _model.Step();
            _model.Step();
            _model.Step();
         
            Assert.AreEqual(new Coords(1,1), _model.Robots.Values.FirstOrDefault(i => i.Id == 0).Coords); // megkezdte a kikerülést
            Assert.AreEqual(new Coords(0,3), _model.Robots.Values.FirstOrDefault(i => i.Id == 1).Coords); // õt várakoztatjuk (mivel docker a destypeja nagyobb a prioritása) 

        }

        [TestMethod]
        public void RobotTurn90()
        {
            _model.GenerateFields();  //palya tisztitasa
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(4, 4), new Pod(4, 4, "1,2"));
            //robot leptetes osszekapcsolasa
            foreach (var robot in _model.Robots)
                robot.Value.RobotMoved += _model.OnRobotMoved;

            var _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);  // robot id alapjan kikerese
            _model.FindNearestPod(_robot);  // algoritmus hivasa robotra
            _model.Step();  // leptetjuk a robotot, elfordul 
            Assert.AreEqual(99, _robot.BatteryLevel);  // forgas 1 energiat fogyaszt
            Assert.AreEqual(new Coords(0, 0), _robot.Coords);  // helyen maradt
        }

        [TestMethod]
        public void RobotTurn180()
        {
            _model.GenerateFields();  //palya tisztitasa
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(1, 0), new Pod(1, 0, "1,2"));
            //robot leptetes osszekapcsolasa
            foreach (var robot in _model.Robots)
                robot.Value.RobotMoved += _model.OnRobotMoved;

            var _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);  // robot id alapjan kikerese
            _model.FindNearestPod(_robot);  // algoritmus hivasa robotra
            Assert.AreEqual(2, _robot.CoordsList.Count);
            _model.Step();                  // leptetjuk a robotot, elfordul 
            _model.Step();                  // fordul megint
            _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            Assert.AreEqual(98, _robot.BatteryLevel);  // forgas 1 energiat fogyaszt
            Assert.AreEqual(new Coords(0, 0), _robot.Coords);  // helyen maradt
        }

        // -------- POD TESTS ----------- TODO parbeginzezni
        [TestMethod]
        public void ModelNearestPodEveryPodEmpty()
        {
            _model.GenerateFields();  // we clean the map
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(0, 1), new Pod(0, 1, new List<int>()));
            _model.Pods.Add(new Coords(0, 2), new Pod(0, 2, new List<int>()));
            _model.Pods.Add(new Coords(7, 7), new Pod(7, 7, new List<int>()));
            _model.Pods.Add(new Coords(5, 5), new Pod(5, 5, new List<int>()));
            _model.Table.SetValue(0, 0, Types.ROBOT);
            _model.Table.SetValue(0, 1, Types.POD);
            _model.Table.SetValue(0, 2, Types.POD);
            _model.Table.SetValue(7, 7, Types.POD);
            _model.Table.SetValue(5, 5, Types.POD);
            // Ha nincs elérhetõ pod null-t adunk vissza 
            Assert.IsNull(_model.NearestPod(_model.Robots[new Coords(0, 0)]));
        }


        [TestMethod] 
        public void PodPlaceItemAtStation()
        {
            _model.GenerateFields();  //palya tisztitasa
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(1, 1), new Pod(1, 1, "2"));
            _model.Pods.Add(new Coords(1, 3), new Pod(1, 3, "1"));
            _model.Stations.Add(new Coords(1, 0), new Station(1, 0, 1));
            _model.Stations.Add(new Coords(1, 2), new Station(1, 2, 2));
            _model.Table.SetValue(0, 0, Types.ROBOT);
            _model.Table.SetValue(1, 1, Types.POD);
            _model.Table.SetValue(1, 3, Types.POD);
            _model.Table.SetValue(1, 0, Types.STATION);
            _model.Table.SetValue(1, 2, Types.STATION);

            var _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            //_robot.LiftPod(_model.Pods[new Coords(0, 0)]);

            //robot leptetes osszekapcsolasa
            foreach (var robot in _model.Robots)
                robot.Value.RobotMoved += _model.OnRobotMoved;
            _model.Step();

            _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            Assert.AreEqual(Types.POD,_robot.DestType);
           
            _model.Step();
            _model.Step();
            _model.Step();
            _model.Step();
            _model.Step();
            _model.Step();

            _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            Assert.AreEqual(94, _model.Robots.Values.FirstOrDefault(i => i.Id == 0).BatteryLevel);
            Assert.IsNotNull(_robot.Pod);

            _model.Step();
            Assert.AreEqual(_robot.DestType, Types.STATION);
            _model.Step();
            _model.Step();
            _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            Assert.AreEqual(91, _model.Robots.Values.FirstOrDefault(i => i.Id == 0).BatteryLevel);
            Assert.AreEqual(0, _model.Robots.Values.FirstOrDefault(i => i.Id == 0).Pod.Items.Count); // leadtuk a terméket
        }


        [TestMethod]
        public void ModelNearestPod()
        {
            _model.GenerateFields();  // we clean the map
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(4, 4), new Pod(4, 4, "1,2"));
          
            //NearestPod count rotations majd nézd meg
            // 1 podnál nem kell eldönteni mihez megy
            Assert.AreEqual(new Coords(4,4), _model.NearestPod(_model.Robots[new Coords(0, 0)]));
            
            _model.Pods.Add(new Coords(2, 4), new Pod(2, 4, "1,2"));
           
            // 2 pod közül a közelebbi választjuk 
            Assert.AreEqual(new Coords(2, 4), _model.NearestPod(_model.Robots[new Coords(0, 0)]));

            _model.Pods.Add(new Coords(0, 1), new Pod(0, 1, new List<int>()));
        
            // Azt a podot válasszuk amelyik a legközelebb van a robothoz és nem üres 
            Assert.AreEqual(new Coords(2, 4), _model.NearestPod(_model.Robots[new Coords(0, 0)]));
        }

        [TestMethod]
        public void ModelFindNearestPod()
        {
            _model.GenerateFields();  // palya tisztítása
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(4, 4), new Pod(4, 4, "1,2"));
         
            Assert.AreEqual(new Coords(4, 4), _model.NearestPod(_model.Robots[new Coords(0, 0)])); // megnézzük hogy hol van a legközelebbi pod
            foreach (var item in _model.Robots.Values)
            {
                _model.FindNearestPod(item);
            }
            Assert.AreEqual(9, _model.Robots[new Coords(0, 0)].CoordsList.Count);  // megnézzük hogy tényleg annyi lépést fog tenni mint szükséges
        }

        [TestMethod]
        public void ModelFindNearestPodFromMultiplePods()
        {
            _model.GenerateFields();  // we clean the map
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(4, 4), new Pod(1, 1, new List<int>()));
            _model.Pods.Add(new Coords(0, 7), new Pod(0, 7, "2"));
            _model.Pods.Add(new Coords(5, 5), new Pod(5, 5, "1"));
            _model.Pods.Add(new Coords(6, 1), new Pod(6, 1, "1,2,3"));
            _model.Pods.Add(new Coords(7, 3), new Pod(7, 3, new List<int>()));
            _model.Pods.Add(new Coords(7, 2), new Pod(7, 2, "1"));
            
            // legközelebbi pod megkeresése a legtöbb közül
            Assert.AreEqual(new Coords(6, 1), _model.NearestPod(_model.Robots[new Coords(0, 0)]));
            _model.FindNearestPod(_model.Robots[new Coords(0, 0)]);
            Assert.AreEqual(8, _model.Robots[new Coords(0, 0)].CoordsList.Count);
        }

        [TestMethod]
        public void ModelRobotsBookingPod()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Robots.Add(new Coords(0, 1), new Robot(0, 0, 1));
            _model.Pods.Add(new Coords(0, 5), new Pod(0, 5, "1,2"));

            _model.Step();
            // id alapján lefoglalja az elsõ a legközelebbi PODot amin van termék, ezért csak az 1 ik robot lesz busy
            Assert.AreEqual(0, _model.Robots[new Coords(0, 1)].CoordsList.Count);
            Assert.AreEqual(6, _model.Robots[new Coords(0, 0)].CoordsList.Count);
            Assert.IsTrue(_model.Robots[new Coords(0, 0)].IsBusy);
            Assert.IsFalse(_model.Robots[new Coords(0, 1)].IsBusy);
        }

        [TestMethod]
        public void ModelRobotsMultipleBookingPods()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Robots.Add(new Coords(0, 1), new Robot(0, 0, 1));
            _model.Pods.Add(new Coords(0, 5), new Pod(0, 5, "1,2"));
            _model.Pods.Add(new Coords(7, 5), new Pod(7, 5, "1,2"));
            _model.Pods.Add(new Coords(2, 5), new Pod(2, 5, "1,2"));

            _model.Step();
            // id alapján lefoglalja az elsõ a legközelebbi PODot amin van termék
            Assert.AreEqual(8, _model.Robots[new Coords(0, 1)].CoordsList.Count);
            Assert.AreEqual(6, _model.Robots[new Coords(0, 0)].CoordsList.Count);
        }


        // -------- STATION TESTS ----------- TODO parbeginzezni
        [TestMethod]
        public void ModelSearchStationCount()
        {
            _model.GenerateFields();  // we clean the map
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Stations.Add(new Coords(4, 4), new Station(4,4,0));
            _model.Table.SetValue(0, 0, Types.ROBOT);
            // after we put our robot into the field, check if anything else happend
            for (int i = 0; i < _model.Table.SizeX; i++)
            {
                for (int j = 0; j < _model.Table.SizeY; j++)
                {
                    if ((i != 0 && j != 0) )  // a 0,0 foglalt azon kívûl mindennek üresnek kell lennie
                        Assert.AreEqual(Types.EMPTY, _model.Table[i, j]);
                }
            }
            _model.Table.SetValue(4, 4, Types.STATION);
            Assert.AreEqual(Types.ROBOT, _model.Table[0, 0]); // is the faild ROBOT
            Assert.AreEqual(Types.STATION, _model.Table[4, 4]); // is the faild STATION
            Assert.AreEqual(1, _model.SearchStations(new Coords(0, 0)));  // search for a station
            // after we put our station into the field, check if anything else happend
            for (int i = 0; i < _model.Table.SizeX; i++)
            {
                for (int j = 0; j < _model.Table.SizeY; j++)
                {
                    if (!((i == 0 && j == 0) || (i == 4 && j== 4)))  // station felrakása után az a mezõ is foglalt
                        Assert.AreEqual(Types.EMPTY, _model.Table[i, j]);
                }
            }
            _model.Stations.Add(new Coords(2, 2), new Station(2, 2, 1));
            Assert.AreEqual(2, _model.SearchStations(new Coords(0, 0)));  // megszámoljuk milyen messze van de a forgásokat itt még nem adjuk hozzá
        }

        [TestMethod] 
        public void ModelSearchStationBlocked()
        {
            _model.GenerateFields();  
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Stations.Add(new Coords(7,7), new Station(7, 7, 0));
            _model.Dockers.Add(new Coords(6, 7), new Docker(6, 7));
            _model.Dockers.Add(new Coords(7, 6), new Docker(7, 6));

            _model.Table.SetValue(0, 0, Types.ROBOT);
            _model.Table.SetValue(7, 7, Types.STATION);
            _model.Table.SetValue(6, 7, Types.DOCKER);
            _model.Table.SetValue(7, 6, Types.DOCKER);

            // station blocked
            Assert.AreEqual(0, _model.SearchStations(new Coords(0, 0)));
            // another blocked station
            _model.Stations.Add(new Coords(0, 7), new Station(0, 7, 1));
            _model.Dockers.Add(new Coords(0, 6), new Docker(0, 6));
            _model.Dockers.Add(new Coords(1, 7), new Docker(1, 7));
            _model.Table.SetValue(0, 7, Types.STATION);
            _model.Table.SetValue(0, 6, Types.DOCKER);
            _model.Table.SetValue(1, 7, Types.DOCKER);

            Assert.AreEqual(0, _model.SearchStations(new Coords(0, 0)));
            _model.Table.SetValue(0, 4, Types.STATION);
            //aviable station
            _model.Stations.Add(new Coords(0, 4), new Station(0, 4, 2));
            Assert.AreEqual(1, _model.SearchStations(new Coords(0, 0)));
        }

        [TestMethod] 
        public void ModelFindNearestStation()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(2, 2), new Robot(2, 2, 0));
            _model.Stations.Add(new Coords(7, 7), new Station(7, 7, 0));
            try // mivel a roboton nincs pod ezert exceptiont kell dobnunk 
            {
                _model.NearestStation(_model.Robots[new Coords(2, 2)]);
                Assert.Fail();
            }
            catch(NullReferenceException) { }
            
            _model.Pods.Add(new Coords(2, 2), new Pod(2, 2, "0"));
            // pod beallitasa és felvetele innentol tud találni stationt a megfelelo podhoz, mivel még nem kötünk rá a mozgásra semmit
            _model.Robots[new Coords(2, 2)].LiftPod(_model.Pods[new Coords(2, 2)]);
            Assert.AreEqual(new Coords(7,7),_model.NearestStation(_model.Robots[new Coords(2, 2)]));


            _model.Pods.Add(new Coords(3, 3), new Pod(3, 3, "1"));
            _model.Robots.Add(new Coords(3, 3), new Robot(3, 3, 1));
            _model.Stations.Add(new Coords(7, 5), new Station(7, 5, 1));
            _model.Robots[new Coords(3, 3)].LiftPod(_model.Pods[new Coords(3, 3)]);
            Assert.AreEqual(new Coords(7, 5), _model.NearestStation(_model.Robots[new Coords(3, 3)]));

        }

        [TestMethod] 
        public void ModelStationPath()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 1));
            _model.Pods.Add(new Coords(0, 0), new Pod(0, 0, "1"));
            _model.Stations.Add(new Coords(0, 7), new Station(0, 7, 1));
            _model.Robots[new Coords(0, 0)].LiftPod(_model.Pods[new Coords(0, 0)]); // mivel még nincs léptetés hozzárendelve ezért kézzel végzem el a mûveletet
             Assert.AreEqual(0,_model.Robots[new Coords(0, 0)].CoordsList.Count); // mivel még nincs hozzá rendelve semmi ezért 0
            _model.FindNearestStation(_model.Robots[new Coords(0, 0)]);
            Assert.AreEqual(8, _model.Robots[new Coords(0, 0)].CoordsList.Count);
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(new Coords(0, i), _model.Robots[new Coords(0, 0)].CoordsList[i]);
            }
        }


        [TestMethod]
        public void ModelAdvanceRobotToStation()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(0, 5), new Pod(0, 5, "1,2"));
            _model.Stations.Add(new Coords(4, 4), new Station(4, 4, 1));


            foreach (var robot in _model.Robots)
                robot.Value.RobotMoved += _model.OnRobotMoved;

            foreach (var robot in _model.Robots)
                _model.FindNearestPod(robot.Value);


            while (!_model.Robots.ContainsKey(new Coords(0, 5)))
            {
                _model.Step(); // amíg el nem jutunk a podig addig lépegetünk
            }

            var _pod = _model.Pods.Values.FirstOrDefault(i => i.Coords.Equals(new Coords(0, 5)));
            var _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            Assert.AreEqual(_pod.Coords, _robot.Coords);
            _model.Robots[new Coords(0, 5)].LiftPod(_model.Pods[new Coords(0, 5)]);

            foreach (var robot in _model.Robots)
                _model.FindNearestStation(robot.Value);

            while (!_model.Robots.ContainsKey(new Coords(4, 4)))
            {
                _model.Step();
            }

            _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            Assert.AreEqual(_model.Stations[new Coords(4, 4)].Coords, _robot.Coords);

        }

        // ------------ DOCKER TESTS------
        [TestMethod]
        public void ModelNearestDocker()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            Assert.IsNull(_model.NearestDocker(_model.Robots[new Coords(0, 0)]));
            _model.Dockers.Add(new Coords(5, 7), new Docker(5, 7));
          
            Assert.AreEqual(new Coords(5,7) ,_model.NearestDocker(_model.Robots[new Coords(0, 0)])); // legközelebbi docker kodinátái

            _model.Dockers.Add(new Coords(2, 7), new Docker(2, 7));
            _model.Dockers.Add(new Coords(7, 7), new Docker(7, 7));
            _model.Dockers.Add(new Coords(5, 2), new Docker(5, 2));
            Assert.AreEqual(new Coords(5, 2), _model.NearestDocker(_model.Robots[new Coords(0, 0)])); // legközelebbi docker kodinátái

        }
        [TestMethod]
        public void ModelFindNearestDocker()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Dockers.Add(new Coords(5, 7), new Docker(5, 7));
            
            _model.FindNearestDocker(_model.Robots[new Coords(0, 0)]);
            Assert.AreEqual(0,_model.Robots[new Coords(0,0)].CoordsList.Count); // mivel a töltöttségi szintje nem alacsony ezért nem küldjök tölteni
            
            _model.Robots[new Coords(0, 0)].BatteryLevel = 20;
            _model.FindNearestDocker(_model.Robots[new Coords(0, 0)]);
            Assert.AreEqual(13, _model.Robots[new Coords(0, 0)].CoordsList.Count);  // megnézzük merre van a legközelebbi docker, forgásokkal beleszámítva

            // több docker közül legközelebbihez visszuk
            _model.Dockers.Add(new Coords(2, 7), new Docker(2, 7));
            _model.Dockers.Add(new Coords(7, 7), new Docker(7, 7));
            _model.Dockers.Add(new Coords(5, 2), new Docker(5, 2));
            Assert.AreEqual(13, _model.Robots[new Coords(0, 0)].CoordsList.Count);

        }

        [TestMethod]
        public void ModelDockerPath()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Dockers.Add(new Coords(0, 7), new Docker(0, 7));
            _model.Robots[new Coords(0, 0)].BatteryLevel = 10;
            _model.FindNearestDocker(_model.Robots[new Coords(0, 0)]);
            Assert.AreEqual(8, _model.Robots[new Coords(0, 0)].CoordsList.Count);
            for(int i = 0; i < 8; i++)
            {
                Assert.AreEqual(new Coords(0, i), _model.Robots[new Coords(0, 0)].CoordsList[i]);   // a kordináta listában az utvonalat a tengely mentén raktam ezért megtudjuk tekinteni hogy helyes e az útvonalterv
            }

        }

       

        [TestMethod]
        public void ModelAdvanceRobot()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(0, 5), new Pod(0, 5, "1,2"));

            foreach (var robot in _model.Robots)
                robot.Value.RobotMoved += _model.OnRobotMoved;
            
            _model.FindNearestPod(_model.Robots[new Coords(0, 0)]);
            Assert.AreEqual(100, _model.Robots[new Coords(0, 0)].BatteryLevel);
            // robot léptetése a megfelelõ helyre azért megyünk [0-6[ mivel az elején egy forgást is el kell végeznie amit mint látható tökéletesen végrehajt
            for (int i = 0; i < 6; i++)
            {
                _model.Step();
               
                Assert.AreEqual(100-i-1, _model.Robots[new Coords(0, i)].BatteryLevel); // léptetés során a töltöttségi szintet is ellenõrzikkük hogy csökken e
            }
            var _pod = _model.Pods.Values.FirstOrDefault(i => i.Items.Contains(1));
            var _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            Assert.AreEqual(_pod.Coords, _robot.Coords); // robot eljutott a podhoz

        }
        [TestMethod]
        public void ModelAdvanceRobots()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Robots.Add(new Coords(1, 0), new Robot(1, 0, 1));
            _model.Pods.Add(new Coords(0, 5), new Pod(0, 5, "1,2"));
            _model.Pods.Add(new Coords(1, 5), new Pod(1, 5, "1,2"));

            foreach (var robot in _model.Robots)
                robot.Value.RobotMoved += _model.OnRobotMoved;

            foreach (var robot in _model.Robots)
                _model.FindNearestPod(robot.Value);
           
            Assert.AreEqual(100, _model.Robots[new Coords(0, 0)].BatteryLevel);
            Assert.AreEqual(100, _model.Robots[new Coords(1, 0)].BatteryLevel);
            // robot léptetése a megfelelõ helyre azért megyünk [0-6[ mivel az elején egy forgást is el kell végeznie amit mint látható tökéletesen végrehajt
            for (int i = 0; i < 6; i++)
            {
                _model.Step();

                Assert.AreEqual(100 - i - 1, _model.Robots[new Coords(0, i)].BatteryLevel);
                Assert.AreEqual(100 - i - 1, _model.Robots[new Coords(1, i)].BatteryLevel);
            }
            var _pod = _model.Pods.Values.FirstOrDefault(i => i.Coords.Equals(new Coords(0,5)));
            var _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 0);
            Assert.AreEqual(_pod.Coords, _robot.Coords); // robot eljutott a podhoz

            _pod = _model.Pods.Values.FirstOrDefault(i => i.Coords.Equals(new Coords(1, 5)));
            _robot = _model.Robots.Values.FirstOrDefault(i => i.Id == 1);
            Assert.AreEqual(_pod.Coords, _robot.Coords); // robot eljutott a podhoz
        }
      
        [TestMethod]
        public void ModelAdvanceRobotToDocker()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Dockers.Add(new Coords(0, 5), new Docker(0, 5));
            
            foreach (var robot in _model.Robots) { 
                robot.Value.RobotMoved += _model.OnRobotMoved;
                robot.Value.BatteryLevel = 10;
            }

            foreach (var robot in _model.Robots)
                _model.FindNearestDocker(robot.Value);

            Assert.AreEqual(Types.DOCKER, _model.Robots[new Coords(0, 0)].DestType);
            

        }
        // Robot carsh 

        [TestMethod]
        public void ModelAdvanceRobotsCrashAvoid3Robots()  
        {
            _model.GenerateFields();

            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Robots.Add(new Coords(0, 1), new Robot(0, 1, 1));
            _model.Robots.Add(new Coords(1, 1), new Robot(1, 1, 2));

            _model.Pods.Add(new Coords(0, 5), new Pod(0, 5, "1,2"));
            _model.Pods.Add(new Coords(0, 4), new Pod(0, 4, "1,2"));
            _model.Pods.Add(new Coords(1, 5), new Pod(1, 5, "1,2"));
            _model.Pods.Add(new Coords(1, 4), new Pod(1, 4, "1,2"));

            foreach (var robot in _model.Robots)
                robot.Value.RobotMoved += _model.OnRobotMoved;  

            _model.Table.SetValue(0, 0, Types.ROBOT);
            _model.Table.SetValue(0, 1, Types.ROBOT);
            _model.Table.SetValue(1, 1, Types.ROBOT);

            _model.Table.SetValue(0, 5, Types.POD);
            _model.Table.SetValue(0, 4, Types.POD);
            _model.Table.SetValue(1, 5, Types.POD);
            _model.Table.SetValue(1, 4, Types.POD);


            foreach (var robot in _model.Robots)
                _model.FindNearestPod(robot.Value);
            int count = 0;
            while (!_model.Robots.ContainsKey(new Coords(1, 5)))  // mivel ez van a legtávolabb ezért ez legyen a kilépési feltétel
            {
                _model.Step();
                count++;
            }
            Assert.AreEqual(5, count);
            // látható a töltöttségi szint alapján hogy a robotok egymást kikerülték.
            Assert.AreEqual(2, _model.Robots[new Coords(1, 5)].Id);
            Assert.AreEqual(95, _model.Robots[new Coords(1, 5)].BatteryLevel);

            Assert.AreEqual(1, _model.Robots[new Coords(1, 1)].Id);
            Assert.AreEqual(97, _model.Robots[new Coords(1, 1)].BatteryLevel);  // õt várakoztattuk ezért 97 es a töltöttségi szintje

            Assert.AreEqual(0, _model.Robots[new Coords(1, 0)].Id);
            Assert.AreEqual(95, _model.Robots[new Coords(1, 0)].BatteryLevel);  // õ volt aki került


            _model.Step();
            // neki fordulnia kellett
            Assert.AreEqual(1, _model.Robots[new Coords(1, 1)].Id);
            Assert.AreEqual(96, _model.Robots[new Coords(1, 1)].BatteryLevel);
           

            _model.Step();
            Assert.AreEqual(0, _model.Robots[new Coords(0, 1)].Id);
            Assert.AreEqual(93, _model.Robots[new Coords(0, 1)].BatteryLevel);

        }

        [TestMethod]  
        public void ModelAdvanceRobotsCrashAvoid2()  
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Robots.Add(new Coords(0, 1), new Robot(0, 0, 1));
            _model.Pods.Add(new Coords(0, 5), new Pod(0, 5, "1,2"));
            _model.Pods.Add(new Coords(0, 4), new Pod(0, 4, "1,2"));

            foreach (var robot in _model.Robots)
                robot.Value.RobotMoved += _model.OnRobotMoved;  // 0,0 megy 0,4 hez | 0,1 megy 0,5 höz
            
            _model.Table.SetValue(0, 0, Types.ROBOT);
            _model.Table.SetValue(0, 1, Types.ROBOT);
            _model.Table.SetValue(0, 5, Types.POD);
            _model.Table.SetValue(0, 4, Types.POD);

            foreach (var robot in _model.Robots)
                _model.FindNearestPod(robot.Value);

            Assert.AreEqual(100, _model.Robots[new Coords(0, 0)].BatteryLevel);
            Assert.AreEqual(100, _model.Robots[new Coords(0, 1)].BatteryLevel);
            Assert.AreEqual(5, _model.Robots[new Coords(0, 0)].CoordsList.Count);
            Assert.AreEqual(6, _model.Robots[new Coords(0, 1)].CoordsList.Count);
            // és itt már ütközés lenne mivel mind a kettõ elfordult jobbra de a 0,0 id ja kisebb ezért õ hamarabb lépne de a 0,1 es robot az útjába van ezért ki kellesz kerülnie
            _model.Step();
            Assert.AreEqual(99, _model.Robots[new Coords(0, 0)].BatteryLevel);
            Assert.AreEqual(99, _model.Robots[new Coords(0, 1)].BatteryLevel);

            _model.Step();
            Assert.AreEqual(98, _model.Robots[new Coords(0, 0)].BatteryLevel); // elindul a másik pedig addig vár 1 lépést
            Assert.AreEqual(99, _model.Robots[new Coords(0, 1)].BatteryLevel);
          
        }

    }
}
