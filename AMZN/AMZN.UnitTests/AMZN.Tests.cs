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
        private Mock<IDataAccess> _mock; // az adatel�r�s mock-ja
        private Model.Model _model;

        [TestInitialize]
        public void Initialize() // teszt inicializ�l�sa
        {
            _mock = new Mock<IDataAccess>();

            _mock.Setup(mock => mock.LoadAsync(It.IsAny<String>()));
            _model = new Model.Model(_mock.Object); // p�ld�nyos�tjuk a modellt a mock objektummal
            
        }
        
        [TestMethod]
        public void ModelConstructorTest() // egys�gteszt m�velet
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
                    Assert.AreEqual(Types.EMPTY, _model.Table[i, j]); // valamennyi mez� �res
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
            Assert.IsTrue(eventRaised); // kiv�ltottuk-e az esem�nyt

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
            Assert.IsTrue(eventRaised); // kiv�ltottuk-e az esem�nyt

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
         
            Assert.AreEqual(new Coords(1,1), _model.Robots.Values.FirstOrDefault(i => i.Id == 0).Coords); // megkezdte a kiker�l�st
            Assert.AreEqual(new Coords(0,3), _model.Robots.Values.FirstOrDefault(i => i.Id == 1).Coords); // �t v�rakoztatjuk (mivel docker a destypeja nagyobb a priorit�sa) 

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
            // Ha nincs el�rhet� pod null-t adunk vissza 
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
            Assert.AreEqual(0, _model.Robots.Values.FirstOrDefault(i => i.Id == 0).Pod.Items.Count); // leadtuk a term�ket
        }


        [TestMethod]
        public void ModelNearestPod()
        {
            _model.GenerateFields();  // we clean the map
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(4, 4), new Pod(4, 4, "1,2"));
          
            //NearestPod count rotations majd n�zd meg
            // 1 podn�l nem kell eld�nteni mihez megy
            Assert.AreEqual(new Coords(4,4), _model.NearestPod(_model.Robots[new Coords(0, 0)]));
            
            _model.Pods.Add(new Coords(2, 4), new Pod(2, 4, "1,2"));
           
            // 2 pod k�z�l a k�zelebbi v�lasztjuk 
            Assert.AreEqual(new Coords(2, 4), _model.NearestPod(_model.Robots[new Coords(0, 0)]));

            _model.Pods.Add(new Coords(0, 1), new Pod(0, 1, new List<int>()));
        
            // Azt a podot v�lasszuk amelyik a legk�zelebb van a robothoz �s nem �res 
            Assert.AreEqual(new Coords(2, 4), _model.NearestPod(_model.Robots[new Coords(0, 0)]));
        }

        [TestMethod]
        public void ModelFindNearestPod()
        {
            _model.GenerateFields();  // palya tiszt�t�sa
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Pods.Add(new Coords(4, 4), new Pod(4, 4, "1,2"));
         
            Assert.AreEqual(new Coords(4, 4), _model.NearestPod(_model.Robots[new Coords(0, 0)])); // megn�zz�k hogy hol van a legk�zelebbi pod
            foreach (var item in _model.Robots.Values)
            {
                _model.FindNearestPod(item);
            }
            Assert.AreEqual(9, _model.Robots[new Coords(0, 0)].CoordsList.Count);  // megn�zz�k hogy t�nyleg annyi l�p�st fog tenni mint sz�ks�ges
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
            
            // legk�zelebbi pod megkeres�se a legt�bb k�z�l
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
            // id alapj�n lefoglalja az els� a legk�zelebbi PODot amin van term�k, ez�rt csak az 1 ik robot lesz busy
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
            // id alapj�n lefoglalja az els� a legk�zelebbi PODot amin van term�k
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
                    if ((i != 0 && j != 0) )  // a 0,0 foglalt azon k�v�l mindennek �resnek kell lennie
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
                    if (!((i == 0 && j == 0) || (i == 4 && j== 4)))  // station felrak�sa ut�n az a mez� is foglalt
                        Assert.AreEqual(Types.EMPTY, _model.Table[i, j]);
                }
            }
            _model.Stations.Add(new Coords(2, 2), new Station(2, 2, 1));
            Assert.AreEqual(2, _model.SearchStations(new Coords(0, 0)));  // megsz�moljuk milyen messze van de a forg�sokat itt m�g nem adjuk hozz�
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
            // pod beallitasa �s felvetele innentol tud tal�lni stationt a megfelelo podhoz, mivel m�g nem k�t�nk r� a mozg�sra semmit
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
            _model.Robots[new Coords(0, 0)].LiftPod(_model.Pods[new Coords(0, 0)]); // mivel m�g nincs l�ptet�s hozz�rendelve ez�rt k�zzel v�gzem el a m�veletet
             Assert.AreEqual(0,_model.Robots[new Coords(0, 0)].CoordsList.Count); // mivel m�g nincs hozz� rendelve semmi ez�rt 0
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
                _model.Step(); // am�g el nem jutunk a podig addig l�peget�nk
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
          
            Assert.AreEqual(new Coords(5,7) ,_model.NearestDocker(_model.Robots[new Coords(0, 0)])); // legk�zelebbi docker kodin�t�i

            _model.Dockers.Add(new Coords(2, 7), new Docker(2, 7));
            _model.Dockers.Add(new Coords(7, 7), new Docker(7, 7));
            _model.Dockers.Add(new Coords(5, 2), new Docker(5, 2));
            Assert.AreEqual(new Coords(5, 2), _model.NearestDocker(_model.Robots[new Coords(0, 0)])); // legk�zelebbi docker kodin�t�i

        }
        [TestMethod]
        public void ModelFindNearestDocker()
        {
            _model.GenerateFields();
            _model.Robots.Add(new Coords(0, 0), new Robot(0, 0, 0));
            _model.Dockers.Add(new Coords(5, 7), new Docker(5, 7));
            
            _model.FindNearestDocker(_model.Robots[new Coords(0, 0)]);
            Assert.AreEqual(0,_model.Robots[new Coords(0,0)].CoordsList.Count); // mivel a t�lt�tts�gi szintje nem alacsony ez�rt nem k�ldj�k t�lteni
            
            _model.Robots[new Coords(0, 0)].BatteryLevel = 20;
            _model.FindNearestDocker(_model.Robots[new Coords(0, 0)]);
            Assert.AreEqual(13, _model.Robots[new Coords(0, 0)].CoordsList.Count);  // megn�zz�k merre van a legk�zelebbi docker, forg�sokkal belesz�m�tva

            // t�bb docker k�z�l legk�zelebbihez visszuk
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
                Assert.AreEqual(new Coords(0, i), _model.Robots[new Coords(0, 0)].CoordsList[i]);   // a kordin�ta list�ban az utvonalat a tengely ment�n raktam ez�rt megtudjuk tekinteni hogy helyes e az �tvonalterv
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
            // robot l�ptet�se a megfelel� helyre az�rt megy�nk [0-6[ mivel az elej�n egy forg�st is el kell v�geznie amit mint l�that� t�k�letesen v�grehajt
            for (int i = 0; i < 6; i++)
            {
                _model.Step();
               
                Assert.AreEqual(100-i-1, _model.Robots[new Coords(0, i)].BatteryLevel); // l�ptet�s sor�n a t�lt�tts�gi szintet is ellen�rzikk�k hogy cs�kken e
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
            // robot l�ptet�se a megfelel� helyre az�rt megy�nk [0-6[ mivel az elej�n egy forg�st is el kell v�geznie amit mint l�that� t�k�letesen v�grehajt
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
            while (!_model.Robots.ContainsKey(new Coords(1, 5)))  // mivel ez van a legt�volabb ez�rt ez legyen a kil�p�si felt�tel
            {
                _model.Step();
                count++;
            }
            Assert.AreEqual(5, count);
            // l�that� a t�lt�tts�gi szint alapj�n hogy a robotok egym�st kiker�lt�k.
            Assert.AreEqual(2, _model.Robots[new Coords(1, 5)].Id);
            Assert.AreEqual(95, _model.Robots[new Coords(1, 5)].BatteryLevel);

            Assert.AreEqual(1, _model.Robots[new Coords(1, 1)].Id);
            Assert.AreEqual(97, _model.Robots[new Coords(1, 1)].BatteryLevel);  // �t v�rakoztattuk ez�rt 97 es a t�lt�tts�gi szintje

            Assert.AreEqual(0, _model.Robots[new Coords(1, 0)].Id);
            Assert.AreEqual(95, _model.Robots[new Coords(1, 0)].BatteryLevel);  // � volt aki ker�lt


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
                robot.Value.RobotMoved += _model.OnRobotMoved;  // 0,0 megy 0,4 hez | 0,1 megy 0,5 h�z
            
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
            // �s itt m�r �tk�z�s lenne mivel mind a kett� elfordult jobbra de a 0,0 id ja kisebb ez�rt � hamarabb l�pne de a 0,1 es robot az �tj�ba van ez�rt ki kellesz ker�lnie
            _model.Step();
            Assert.AreEqual(99, _model.Robots[new Coords(0, 0)].BatteryLevel);
            Assert.AreEqual(99, _model.Robots[new Coords(0, 1)].BatteryLevel);

            _model.Step();
            Assert.AreEqual(98, _model.Robots[new Coords(0, 0)].BatteryLevel); // elindul a m�sik pedig addig v�r 1 l�p�st
            Assert.AreEqual(99, _model.Robots[new Coords(0, 1)].BatteryLevel);
          
        }

    }
}
