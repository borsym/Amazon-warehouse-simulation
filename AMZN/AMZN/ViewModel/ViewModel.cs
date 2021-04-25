using AMZN.Model;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Linq;
using System.Diagnostics;
using AMZN.View;
using AMZN.Services;
using System.Windows.Input;
using AMZN.ServerCommands;
using Newtonsoft.Json;
using AMZN.Persistence;
using Newtonsoft.Json.Linq;
namespace AMZN.ViewModel
{
    /// <summary>
    /// A szimuláció modelljét és a felhasználói felületet összekapcsoló nézetmodell
    /// </summary>
    public class ViewModel : ViewModelBase
    {
        #region privát adattagok
        private Model.Model _model;
        private int _textSliderValue = 0;
        private int _speedMultiplier = 1000;
        private string _startPauseButtonText = "START";
        private string _onlineOrderText = "Enable Online Orders";
        private Dictionary<Coords, Types> _selectedFields;
        private CreateSimulation _createSimulationWindow;
        private AMZNServices _service;
        private StringBuilder _jsonstream;
        private bool _isUserMoving;
        private Coords _movingTopLeftCoords;
        private int _movingRightCoords;
        private bool _isEnabledPlaceStation;
        private Dictionary<int, Types> _movingPreviewList;
        private Dictionary<Coords, Robot> _movingRobots;
        private Dictionary<Coords, Pod> _movingPods;
        private Dictionary<Coords, Docker> _movingDockers;
        private Dictionary<Coords, Station> _movingStations;
        #endregion

        #region propertyk
        public ICommand OnlineLoadCommand { get; set; }
        public ICommand SendStartCommand { get; set; }
        public ICommand SetSliderValueCommand { get; set; }
        public ICommand SendOnlineOrderCommand { get; set; }
        public ObservableCollection<Field> Fields { get; set; }
        public int TableSizeX { get { return _model.Table.SizeX; } }
        public int TableSizeY { get { return _model.Table.SizeY; } }
        public int TextSliderValue { get { return _textSliderValue; } set { _textSliderValue = value; } }
        public int SpeedMultiplier
        {
            get { return SpeedMultiplier1; }
            set
            {
                SpeedMultiplier1 = value;
                ModifySimulationSpeed?.Invoke(this, SpeedMultiplier1);
                OnPropertyChanged("SpeedMultiplier1");
            }
        }
        public int TimerCount { get { return _model.TimerCount; } }
        public int AllBatteryUsed { get { return _model.AllBatteryUsed; } }
        public string StartPauseButtonText { get { return _startPauseButtonText; } set { _startPauseButtonText = value; } } //nem volt set
        public string OnlineOderText { get { return _onlineOrderText; } set { _onlineOrderText = value; } }
        public bool IsSimulationPaused { get { return _startPauseButtonText == "START"; } }
        public DelegateCommand NewSimulationCommand { get; set; }
        public DelegateCommand PlaceSimulationItemsCommand { get; set; }
        public DelegateCommand DeleteSimulationItemsCommand { get; set; }
        public DelegateCommand PlaceProductsCommand { get; set; }
        public DelegateCommand DeleteProductsCommand { get; set; }
        public DelegateCommand ClearSelectionsCommand { get; set; }
        public DelegateCommand StartPauseSimulationCommand { get; set; }
        public DelegateCommand OpenCreateSimulationWindowCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand LoadCommand { get; set; }
        public DelegateCommand ExportDataCommand { get; set; }
        public DelegateCommand ResetDataCommand { get; set; }
        public DelegateCommand EnableDisableMove { get; set; }
        public DelegateCommand OnlineOrderCommand { get; set; }
        public DelegateCommand ViewSendStartCommnad { get; set; }
        public AMZNServices Service { get => _service; set => _service = value; }
        public int SpeedMultiplier1 { get => _speedMultiplier; set => _speedMultiplier = value; }
        #endregion

        #region eventek
        public EventHandler StartSimulation;
        public EventHandler PauseSimulation;
        public EventHandler StartOnlineOrder;
        public EventHandler PauseOnlineOrder;
        public EventHandler SaveMap;
        public EventHandler LoadMap;
        public EventHandler ExportData;
        public EventHandler OnlineOrder;
        public EventHandler<int> ModifySimulationSpeed;
        public EventHandler<int> NotifyAppAboutSpeed;
        #endregion

        #region konstruktorok
        /// <summary>
        /// A nézetmodell konstruktora.
        /// </summary>
        /// <param name="model">A szimuláció modellje</param>
        public ViewModel(Model.Model model, AMZNServices service)
        {
            _service = service;
            _model = model;
            _model.TableCreated += new EventHandler<Model.EventArgs>(Model_TableCreated);
            _model.SimulationEnded += Model_SimulationEnded;

            PlaceSimulationItemsCommand = new DelegateCommand(param => PlaceSimulationItems((Types)Convert.ToInt32(param)));
            DeleteSimulationItemsCommand = new DelegateCommand(_ => DeleteSimulationItems());
            PlaceProductsCommand = new DelegateCommand(param => PlaceProducts(Convert.ToInt32(param)));
            DeleteProductsCommand = new DelegateCommand(param => DeleteProducts(Convert.ToInt32(param)));
            StartPauseSimulationCommand = new DelegateCommand(param => StartPauseSimulation());

            OnlineOrderCommand = new DelegateCommand(param => OnloneOrderTextChanger()); // itt hal le szerintem

            Fields = new ObservableCollection<Field>();
            for (int i = 0; i < 8; i++) // inicializáljuk a mezőket
            {
                for (int j = 0; j < 8; j++)
                {
                    Fields.Add(new Field
                    {
                        Text = String.Empty,
                        X = i,
                        Y = j,
                        Number = i * 8 + j,
                        Type = Types.EMPTY,
                        StepCommand = new DelegateCommand(param => HandleSelection(Convert.ToInt32(param))),  // teszteles miatt adtam hozza
                        HoverCommand = new DelegateCommand((param) => ShowMovingPreview(Convert.ToInt32(param)))
                    });
                }
            }
            RefreshTable();

            _selectedFields = new Dictionary<Coords, Types>();
            _jsonstream = new StringBuilder();

            _createSimulationWindow = new CreateSimulation();
            OpenCreateSimulationWindowCommand = new DelegateCommand((_) => PopupSizeChanger());
            _createSimulationWindow.updateTableSite += ChangeTableSize;
            ClearSelectionsCommand = new DelegateCommand((_) => ClearSelections());

            SaveCommand = new DelegateCommand((_) => OnSaveMap());
            LoadCommand = new DelegateCommand((_) => OnLoadMap());
            ExportDataCommand = new DelegateCommand((_) => OnExportData());
            ResetDataCommand = new DelegateCommand((_) => OnResetData());
            OnlineOrderCommand = new DelegateCommand((_) => OnloneOrderTextChanger());

            SendStartCommand = new SendStartCommand(this, service);
            SendOnlineOrderCommand = new SendOnlineOrderCommand(this, service);
            SetSliderValueCommand = new SetSliderValueCommand(this, service);
            OnlineLoadCommand = new OnlineLoadCommand(this, service);

            EnableDisableMove = new DelegateCommand((_) => EnableMoving());
            _movingRobots = new Dictionary<Coords, Robot>();
            _movingPods = new Dictionary<Coords, Pod>();
            _movingDockers = new Dictionary<Coords, Docker>();
            _movingStations = new Dictionary<Coords, Station>();
            _movingPreviewList = new Dictionary<int, Types>();

            ViewSendStartCommnad = new DelegateCommand((_) => StartTheSimulation());

            service.StartActionReceived += Service_StartActionReceived;
            service.SliderValueReceived += Service_SliderValueReceived;
            service.LoadMapReceived += Service_LoadMapReceived;

            service.OnlineOrderReceived += Service_OnlineOrderReceived;
        }

        private void StartTheSimulation()
        {
            if(_service == null)
            {
                StartPauseSimulation();
            }
            else
            {
                SendStartCommand.Execute(true);
            }
        }

        private void EnableMoving()
        {
            if (_isUserMoving)
            {
                _isUserMoving = false;
                _movingPreviewList.Clear();
                RefreshTable();
            }
            else
            {
                if ((_selectedFields.Values.Distinct().Count() == 1
                        && _selectedFields.Values.First() == Types.EMPTY) || _selectedFields.Count == 0)
                {
                    MessageBox.Show("Please select not empty fields!");
                    return;
                }
                _isUserMoving = true;
                int _minTop = _model.Table.SizeX;
                int _minLeft = _model.Table.SizeY;
                int _maxRight = 0;
                foreach (var _field in _selectedFields)
                {
                    if (_field.Key.X < _minTop)
                    {
                        _minTop = _field.Key.X;
                    }

                    if (_field.Key.Y < _minLeft)
                    {
                        _minLeft = _field.Key.Y;
                    }
                    if (_field.Key.Y > _maxRight)
                    {
                        _maxRight = _field.Key.Y;
                    }
                }
                _movingTopLeftCoords = new Coords(_minTop, _minLeft);
                _movingRightCoords = _maxRight;
                //MessageBox.Show(_maxRight.ToString());
            }
        }

        private void ShowMovingPreview(int number)
        {
            if (!_isUserMoving) return;

            int x = number / _model.Table.SizeY;
            int y = number % _model.Table.SizeY;
            // kiszedem az üres mezőket, mert azokat nem mozgatjuk
            foreach (var _field in _selectedFields)
            {
                if (_field.Value == Types.EMPTY)
                {
                    _selectedFields.Remove(_field.Key);
                }
            }
            RefreshTable();

            foreach (var _item in _movingPreviewList)
            {
                Fields[_item.Key].Type = _model.Table[_item.Key / _model.Table.SizeY, _item.Key % _model.Table.SizeX];
            }
            _movingPreviewList.Clear();
            _movingRobots.Clear();
            _movingStations.Clear();
            _movingDockers.Clear();
            _movingPods.Clear();

            int minusValue = _movingTopLeftCoords.X * _model.Table.SizeY + _movingTopLeftCoords.Y
                - number;
            Debug.WriteLine("FIELDS COUNT: " + Fields.Count.ToString() + "\n");
            foreach (var _field in _selectedFields)
            {
                Debug.Write(_field.Key.X * _model.Table.SizeY + _field.Key.Y);
                if (_field.Key.X * _model.Table.SizeY + _field.Key.Y - minusValue < Fields.Count
                    && _field.Key.X * _model.Table.SizeY + _field.Key.Y - minusValue >= 0)
                {
                    int newIdx = _field.Key.X * _model.Table.SizeY + _field.Key.Y - minusValue;
                    if (y + _movingRightCoords - _movingTopLeftCoords.Y < _model.Table.SizeY)
                    {
                        _movingPreviewList.Add(_field.Key.X * _model.Table.SizeY + _field.Key.Y - minusValue,
                                _model.Table[_field.Key.X, _field.Key.Y]);
                        //MessageBox.Show(_model.Table[_field.Key.X, _field.Key.Y].ToString());
                        int newy = newIdx % _model.Table.SizeY;
                        int newx = newIdx / _model.Table.SizeY;
                        switch (Fields[_field.Key.X * _model.Table.SizeY + _field.Key.Y].Type)
                        {
                            case Types.ROBOT:
                                {
                                    _movingRobots.Add(new Coords(newx, newy), _model.Robots[_field.Key]);
                                    break;
                                }
                            case Types.DOCKER:
                                {
                                    _movingDockers.Add(new Coords(newx, newy), _model.Dockers[_field.Key]);
                                    break;
                                }
                            case Types.STATION:
                                {
                                    _movingStations.Add(new Coords(newx, newy), _model.Stations[_field.Key]);
                                    break;
                                }
                            case Types.POD:
                                {
                                    _movingPods.Add(new Coords(newx, newy), _model.Pods[_field.Key]);
                                    break;
                                }
                            case Types.ROBOT_ON_DOCKER:
                                {
                                    _movingRobots.Add(new Coords(newx, newy), _model.Robots[_field.Key]);
                                    _movingDockers.Add(new Coords(newx, newy), _model.Dockers[_field.Key]);
                                    break;
                                }
                            case Types.ROBOT_ON_STATION:
                                {
                                    _movingRobots.Add(new Coords(newx, newy), _model.Robots[_field.Key]);
                                    _movingStations.Add(new Coords(newx, newy), _model.Stations[_field.Key]);
                                    break;
                                }
                            case Types.ROBOT_UNDER_POD:
                                {
                                    _movingRobots.Add(new Coords(newx, newy), _model.Robots[_field.Key]);
                                    _movingPods.Add(new Coords(newx, newy), _model.Pods[_field.Key]);
                                    break;
                                }
                            default:
                                {
                                    break;
                                }
                        }
                        Fields[_field.Key.X * _model.Table.SizeY + _field.Key.Y - minusValue].Type = Types.MOVING;
                    }
                    else
                    {
                        _movingPreviewList.Clear();
                        _movingRobots.Clear();
                    }
                }
            }
            OnPropertyChanged("Fields");
        }

        private void moveAllFromPreview(int movedX, int movedY)
        {
            int conflictedFields = 0;
            foreach (Types type in _movingPreviewList.Values.Distinct())
            {
                _selectedFields.Clear();
                foreach (var item in _movingPreviewList)
                {
                    if (item.Value == type)
                    {
                        _selectedFields.Add(new Coords(item.Key / _model.Table.SizeY, item.Key % _model.Table.SizeY), item.Value);
                    }
                }
                switch (type)
                {
                    case Types.DOCKER:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.ROBOT)
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, Types.ROBOT_ON_DOCKER);
                                    _model.Dockers.Add(_selectedField.Key,
                                        _movingDockers[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                                else
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, type);
                                    _model.Dockers.Add(_selectedField.Key,
                                        _movingDockers[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                            }
                            break;
                        }
                    case Types.ROBOT:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.POD)
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, Types.ROBOT_UNDER_POD);
                                    _model.Robots.Add(_selectedField.Key,
                                        _movingRobots[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.DOCKER)
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, Types.ROBOT_ON_DOCKER);
                                    _model.Robots.Add(_selectedField.Key,
                                        _movingRobots[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.STATION)
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, Types.ROBOT_ON_STATION);
                                    _model.Robots.Add(_selectedField.Key,
                                        _movingRobots[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                                else
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, type);
                                    _model.Robots.Add(_selectedField.Key,
                                        _movingRobots[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                            }
                            break;
                        }
                    case Types.STATION:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Stations.Count >= 4)
                                {
                                    conflictedFields++;
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.ROBOT)
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, Types.ROBOT_ON_STATION);
                                    _model.Stations.Add(_selectedField.Key,
                                        _movingStations[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                                else
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, type);
                                    _model.Stations.Add(_selectedField.Key,
                                        _movingStations[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                            }
                            if (_model.Stations.Count == 4)
                            {
                                _isEnabledPlaceStation = false;
                            }
                            OnPropertyChanged("IsEnabledPlaceStation");
                            break;
                        }
                    case Types.POD:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.ROBOT)
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, Types.ROBOT_UNDER_POD);
                                    _model.Pods.Add(_selectedField.Key,
                                        _movingPods[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                                else
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, type);
                                    _model.Pods.Add(_selectedField.Key,
                                        _movingPods[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                            }
                            break;
                        }
                    case Types.ROBOT_ON_DOCKER:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                                else
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, type);
                                    _model.Dockers.Add(_selectedField.Key,
                                        _movingDockers[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                    _model.Robots.Add(_selectedField.Key,
                                        _movingRobots[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                            }
                            break;
                        }
                    case Types.ROBOT_UNDER_POD:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                                else
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, type);
                                    _model.Pods.Add(_selectedField.Key,
                                        _movingPods[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                    _model.Robots.Add(_selectedField.Key,
                                        _movingRobots[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                            }
                            break;
                        }
                    case Types.ROBOT_ON_STATION:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                                else
                                {
                                    _selectedFields.Remove(_selectedField.Key);
                                    _model.Table.SetValue(_selectedField.Key.X, _selectedField.Key.Y, type);
                                    _model.Stations.Add(_selectedField.Key,
                                        _movingStations[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                    _model.Robots.Add(_selectedField.Key,
                                        _movingRobots[new Coords(_selectedField.Key.X, _selectedField.Key.Y)]);
                                }
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
        }
        private bool checkAllMove()
        {
            int conflictedFields = 0;
            foreach (Types type in _movingPreviewList.Values.Distinct())
            {
                _selectedFields.Clear();
                foreach (var item in _movingPreviewList)
                {
                    if (item.Value == type)
                    {
                        _selectedFields.Add(new Coords(item.Key / _model.Table.SizeY, item.Key % _model.Table.SizeY), item.Value);
                    }
                }
                switch (type)
                {
                    case Types.DOCKER:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.ROBOT)
                                {
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                            }
                            break;
                        }
                    case Types.ROBOT:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.POD)
                                {
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.DOCKER)
                                {
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.STATION)
                                {
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                            }
                            break;
                        }
                    case Types.STATION:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Stations.Count >= 4)
                                {
                                    conflictedFields++;
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.ROBOT)
                                {
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    //MessageBox.Show("Robot nem üresre!");
                                    conflictedFields++;
                                }
                            }
                            if (_model.Stations.Count == 4)
                            {
                                _isEnabledPlaceStation = false;
                            }
                            OnPropertyChanged("IsEnabledPlaceStation");
                            break;
                        }
                    case Types.POD:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] == Types.ROBOT)
                                {
                                }
                                else if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                            }
                            break;
                        }
                    case Types.ROBOT_ON_DOCKER:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                            }
                            break;
                        }
                    case Types.ROBOT_UNDER_POD:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                            }
                            break;
                        }
                    case Types.ROBOT_ON_STATION:
                        {
                            foreach (var _selectedField in _selectedFields)
                            {
                                if (_model.Table[_selectedField.Key.X, _selectedField.Key.Y] != Types.EMPTY)
                                {
                                    conflictedFields++;
                                }
                            }
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            return conflictedFields == 0;
        }
        private void OnloneOrderTextChanger()
        {
            if (IsSimulationPaused) return; // ha megy a szimuláció csak akkor lehet bekapcsolni
            if (_onlineOrderText != "Disable Online Orders")
            {
                _onlineOrderText = "Disable Online Orders";
            } 
            else
            {
                _onlineOrderText = "Enable Online Orders";
            }

            OnPropertyChanged("OnlineOderText");
            
        }

        private void OnOnlineOrder()
        {
            
        }

        private void Service_RobotReceived(List<int> obj)
        {
            foreach(var o in obj)
            {
                MessageBox.Show(o.ToString());
            }
        }

        private void Service_LoadMapReceived(string obj)
        {
            SaveLoadData data = _model.DataAccess.LoadFromText(obj);
            ChangeTableSize(this, new Coords(data.table.SizeX, data.table.SizeY));
            _model.Init(data);
            RefreshTable();
        }

        private void Service_SliderValueReceived(int obj)
        {
            SpeedMultiplier1 = obj;
            if(NotifyAppAboutSpeed != null)
            {
                NotifyAppAboutSpeed(this, obj);
            }
            OnPropertyChanged("SpeedMultiplier1");
            OnPropertyChanged("SpeedMultiplier");
        }

        public static ViewModel CreatedConnectedViewModel(Model.Model model, AMZNServices service)
        {
            ViewModel vm = new ViewModel(model, service);

            service.Connect().ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    MessageBox.Show("Unable to reach server!");
                    vm._service = null;
                }
            });

            return vm;
        }
        private void Service_OnlineOrderReceived(OnlineOrder obj)
        {
            // és akkor itt kell meghívni az objt az enablebe valószínűleg
            _model.Pods[new Coords(obj.X, obj.Y)].Items.Add(obj.Item);
            RefreshTable();
        }
     

        private void Service_StartActionReceived(bool obj)
        {
            StartPauseSimulation();
        }
        #endregion

        #region metódusok és függvények
        private void OnSaveMap()
        {
            if (!IsSimulationPaused)
            {
                MessageBox.Show("You can only do that when the simulation is paused!",
                                "Simulation is running", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (SaveMap != null)
            {
                SaveMap(this, System.EventArgs.Empty);
            }
        }
        
        private void OnLoadMap()
        {
            if (!IsSimulationPaused)
            {
                MessageBox.Show("You can only do that when the simulation is paused!",
                                "Simulation is running", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (LoadMap != null)
            {
                LoadMap(this, System.EventArgs.Empty);
            }
        }

        private void OnExportData()
        {
            if (!IsSimulationPaused)
            {
                MessageBox.Show("You can only do that when the simulation is paused!",
                                "Simulation is running", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (ExportData != null)
            {
                ExportData(this, System.EventArgs.Empty);
            }
        }
        public void ExportDataToFile(string path)
        {
            File.WriteAllTextAsync(path, "{" + _jsonstream.ToString() + "}\n");
        }

        private void OnResetData()
        {
            _model.TimerCount = 0;
            _model.AllBatteryUsed = 0;
            foreach (Coords coords in _model.Robots.Keys)
            {
                _model.Robots[coords].BatteryUsed = 0;
            }
            _jsonstream.Clear();
        }

        /// <summary>
        /// Megjeleníti a dialógusablakot, ahol beállíthatjuk a tábla méretét.
        /// </summary>
        private void PopupSizeChanger()
        {
            if (!IsSimulationPaused)
            {
                MessageBox.Show("You can only do that when the simulation is paused!",
                                "Simulation is running", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _createSimulationWindow.ShowDialog();
            _createSimulationWindow = new CreateSimulation();
            _createSimulationWindow.updateTableSite += ChangeTableSize;
        }

        /// <summary>
        /// Frissíti a felhasználói felületet.
        /// </summary>
        public void RefreshTable()
        {
            foreach (Field field in Fields) // inicializálni kell a mezőket is
            {
                if ((_model.Table[field.X, field.Y] == Types.POD
                        || _model.Table[field.X, field.Y] == Types.ROBOT_UNDER_POD
                        || _model.Table[field.X, field.Y] == Types.ROBOT_WITH_POD
                        || _model.Table[field.X, field.Y] == Types.ROBOT_WITH_POD_ON_STATION)
                        && _model.Pods.ContainsKey(new Coords(field.X, field.Y))
                        && !_model.Pods[new Coords(field.X, field.Y)].IsEmpty)
                    field.Text = string.Join(" ", _model.Pods[new Coords(field.X, field.Y)].Items);
                else if (_model.Table[field.X, field.Y] == Types.ROBOT
                    || _model.Table[field.X, field.Y] == Types.ROBOT_ON_STATION
                    || _model.Table[field.X, field.Y] == Types.ROBOT_ON_DOCKER
                    || _model.Table[field.X, field.Y] == Types.ROBOT_UNDER_POD
                    || _model.Table[field.X, field.Y] == Types.ROBOT_WITH_POD
                    || _model.Table[field.X, field.Y] == Types.ROBOT_WITH_POD_ON_STATION)
                    field.Text = _model.Robots[new Coords(field.X, field.Y)].BatteryLevel + "%";
                else if (_model.Table[field.X, field.Y] == Types.STATION)
                    field.Text = _model.Stations[new Coords(field.X, field.Y)].Id + "";
                else
                    field.Text = "";

                field.Type = _model.Table[field.X, field.Y];
            }
            OnPropertyChanged("TimerCount");
            OnPropertyChanged("AllBatteryUsed");
            Console.WriteLine("itt\n\n");
        }

        // felhejezi a termékeket a podokra
        public void EnableDisableOnlineOrder()  // és akkor itt kéne megkapnia az értéket és beírnia?
        {
          if(_onlineOrderText == "Disable Online Orders")
          {
                if (_model.TimerCount % 5 == 0)
                {
                    
                    List<Pod> tmp = _model.Pods.Values.ToList().FindAll(i => i.IsBusy == false);
                   // MessageBox.Show(tmp.Count + " " + _model.Pods.Values.Count);
                    if(tmp.Count > 0)
                    {
                        Random rd = new Random();
                        int random = rd.Next(0, tmp.Count);  // kiveszem az indexet amire rakni akarok
                        int random_item = rd.Next(1, 5);
                       // MessageBox.Show(tmp[random].Coords.X + " " + tmp[random].Coords.Y);
                        //MessageBox.Show(_model.Pods[tmp[random].Coords].Coords.X + " " + _model.Pods[tmp[random].Coords].Coords.Y);
                        if (_model.Pods[tmp[random].Coords] != null && !_model.Pods[tmp[random].Coords].Items.Contains(random_item))
                        {
                            _model.Pods[tmp[random].Coords].Items.Add(random_item);
                            OnlineOrder new_order = new OnlineOrder(_model.Pods[tmp[random].Coords].Coords.X, _model.Pods[tmp[random].Coords].Coords.Y, random_item);
                            if (_service != null) SendOnlineOrderCommand.Execute(new_order);
                            //MessageBox.Show("raraktuk" + tmp[random].Coords.X + " " + tmp[random].Coords.Y);
                        }
                    }
                    
                }
            }
            

            RefreshTable();

        }
        /// <summary>
        /// Elindítja / megállítja a szimulációt.
        /// </summary>
        private void StartPauseSimulation()
        {
            if (_startPauseButtonText == "START")
            {
                if(_model.Stations.Count < 4 || _model.Dockers.Count == 0 || _model.Robots.Count == 0)
                {
                    MessageBox.Show("There must be exactly 4 stations, at least 1 docker and robot on the map!",
                                    "Not enough elements on the map!");
                    return;
                }
                StartSimulation?.Invoke(this, null);
                _startPauseButtonText = "STOP";
                OnPropertyChanged("StartPauseButtonText");
                OnPropertyChanged("IsSimulationPaused");
            }
            else
            {
                PauseSimulation?.Invoke(this, null);
                _startPauseButtonText = "START";
                OnPropertyChanged("StartPauseButtonText");
                OnPropertyChanged("IsSimulationPaused");
            }
        }

        /// <summary>
        /// Lépteti a szimulációt.
        /// </summary>
        public void StepSimulation()
        {
            _model.Step();
            //// ezt kell kirakni egy eseménybe hogy mindegyiken megjelenjenek
            //if (_model.TimerCount % 5 == 0 && _onlineOrderText == "Disable Online Orders")
            //{
            //    Random rd = new Random();
            //    int random = rd.Next(0, _model.Pods.Values.Count);  // kiveszem az indexet amire rakni akarok
            //    if (_model.Pods.Values.Count != 0 && _model.Pods.Values.ElementAt(random).Items.Count < 4)
            //    {
            //        int item = rd.Next(1, 5);
            //        //while(!_model.Pods.Values.ElementAt(random).Items.Contains(item))
            //        //{
            //        _model.Pods.Values.ElementAt(random).Items.Add(item);
            //        //}
            //    }
            //}

            RefreshTable();

            // adatok logolása json formátumban:
            JsonData jsondata = new JsonData()
            {
                AllBatteryUsed = AllBatteryUsed,
                CountOfProductsToDeliver = _model.Pods.Sum(x => x.Value.Items.Count),
                Robots = new List<JsonRobot>()
            };
            foreach (Robot r in _model.Robots.Values)
                jsondata.Robots.Add(
                    new JsonRobot()
                    {
                        BatteryLevel = r.BatteryLevel,
                        BatteryUsed = r.BatteryUsed,
                        Id = r.Id
                    });
            _jsonstream.Append("\"" + TimerCount + "\":" + JsonConvert.SerializeObject(jsondata, Formatting.None) + ",\n");
        }


        /// <summary>
        /// Megjeleníti a kijelölést
        /// </summary>
        private void ShowSelectedFields()
        {
            foreach (var _field in _selectedFields)
                Fields[_field.Key.X * _model.Table.SizeY + _field.Key.Y].Type = Types.SELECTED;
        }

        /// <summary>
        /// Törli a kijelölést
        /// </summary>
        private void ClearSelections()
        {
            _selectedFields.Clear();
            RefreshTable();
        }

        private void HandleSelection(int idx)
        {
            if (!IsSimulationPaused) return;
            if (_isUserMoving)
            {
                if (_movingPreviewList.Count == 0)
                {
                    MessageBox.Show("Can't move at the time!");
                    return;
                }
                DeleteSimulationItems();
                int x = idx / _model.Table.SizeY;
                int y = idx % _model.Table.SizeY;

                int movedX = _movingTopLeftCoords.X - x;
                int movedY = _movingTopLeftCoords.Y - y;
                if (checkAllMove())
                {
                    moveAllFromPreview(movedX, movedY);
                }
                else
                {
                    MessageBox.Show("Can't move at the time!");
                    _selectedFields.Clear();

                    foreach (var r in _movingRobots)
                    {
                        _selectedFields.Add(new Coords(r.Key.X + movedX, r.Key.Y + movedY),
                            _model.Table[r.Key.X + movedX, r.Key.Y + movedY]);
                    }
                    PlaceSimulationItems(Types.ROBOT);

                    foreach (var r in _movingPods)
                    {
                        _selectedFields.Add(new Coords(r.Key.X + movedX, r.Key.Y + movedY),
                            _model.Table[r.Key.X + movedX, r.Key.Y + movedY]);
                    }
                    PlaceSimulationItems(Types.POD);

                    foreach (var r in _movingDockers)
                    {
                        _selectedFields.Add(new Coords(r.Key.X + movedX, r.Key.Y + movedY),
                            _model.Table[r.Key.X + movedX, r.Key.Y + movedY]);
                    }
                    PlaceSimulationItems(Types.DOCKER);

                    foreach (var r in _movingStations)
                    {
                        _selectedFields.Add(new Coords(r.Key.X + movedX, r.Key.Y + movedY),
                            _model.Table[r.Key.X + movedX, r.Key.Y + movedY]);
                    }
                    PlaceSimulationItems(Types.STATION);
                }
                RefreshTable();

                _selectedFields.Clear();
                _isUserMoving = false;
                return;
            }
            else
            {
                if (Fields[idx].Type != Types.SELECTED)
                {
                    _selectedFields[new Coords(idx / _model.Table.SizeY, idx % _model.Table.SizeY)] = Fields[idx].Type; //saving field type for later, if we delete the selection
                    Fields[idx].Type = Types.SELECTED;
                }
                else
                {
                    //MessageBox.Show((idx / _model.Table.SizeY).ToString() + " " + (idx % _model.Table.SizeY).ToString());
                    //MessageBox.Show(_selectedFields.ContainsKey(new Coords(idx / _model.Table.SizeY, idx % _model.Table.SizeY)).ToString());
                    Fields[idx].Type = _selectedFields[new Coords(idx / _model.Table.SizeY, idx % _model.Table.SizeY)];
                    _selectedFields.Remove(new Coords(idx / _model.Table.SizeY, idx % _model.Table.SizeY));
                }
            }
        }


        /// <summary>
        /// Elhelyezi a kiválasztott típusú objektumot a kijelölt mezőkön.
        /// </summary>
        /// <param name="type">Az objektum típusa</param>
        //simple items ~ robot, docker, station - no extra data
        private void PlaceSimulationItems(Types type)
        {
            int conflictedFields = 0;
            foreach (var _selectedField in _selectedFields)
            {
                if (!_model.AddObject(_selectedField.Key, type)) ++conflictedFields;
                else _selectedFields.Remove(_selectedField.Key);
            }

            if (conflictedFields > 0)
            {
                MessageBox.Show("There were conflictions on " + conflictedFields.ToString() + " fields!"
                                + Environment.NewLine + ("Unchanged fields are still selected!"),
                                "Failed to add all objects", MessageBoxButton.OK, MessageBoxImage.Error);
                RefreshTable();
                ShowSelectedFields();
            }
            else
            {
                _selectedFields.Clear();
                RefreshTable();
            }
        }

        /// <summary>
        /// Törli a szimuláció objektumait a kijelölt mezőkről.
        /// </summary>
        private void DeleteSimulationItems()
        {
            foreach(var _selectedField in _selectedFields)
            {
                _model.DelObject(_selectedField.Key);
            }
            _model.Table.ClearFields(_selectedFields.Keys.ToList());
            _selectedFields.Clear();
            RefreshTable();
        }

        /// <summary>
        /// Elhelyezi a kiválasztott terméket a kijelölt polcokon.
        /// </summary>
        /// <param name="itemID">Termékszám</param>
        private void PlaceProducts(int itemID)
        {
            foreach (var _selectedField in _selectedFields)
                if (_model.AddProduct(_selectedField.Key, itemID)) _selectedFields.Remove(_selectedField.Key);
            if (_selectedFields.Count() > 0)
                MessageBox.Show("You can place products only on pods and only once!" + Environment.NewLine +
                                "There were conflictions on " + _selectedFields.Count.ToString() + " fields!",
                                "Failed to add products", MessageBoxButton.OK, MessageBoxImage.Error);
            RefreshTable();
            ShowSelectedFields();
        }

        /// <summary>
        /// Törli a kiválasztott terméket a kijelölt polcokról.
        /// </summary>
        /// <param name="itemID">Termékszám</param>
        private void DeleteProducts(int itemID)
        {
            foreach (var _selectedField in _selectedFields.Where(x => x.Value == Types.POD || x.Value == Types.ROBOT_UNDER_POD))
            {
                _model.Pods[_selectedField.Key].Remove(itemID);
                _selectedFields.Remove(_selectedField.Key);
            }
            if (_selectedFields.Count() > 0)
            {
                MessageBox.Show("You can remove products only from pods!" + Environment.NewLine +
                                "There were conflictions on " + _selectedFields.Where(x => x.Value != Types.POD).Count().ToString() + " fields!",
                                "Failed to remove products", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            RefreshTable();
            ShowSelectedFields();
        }
        #endregion

        #region eventhandlerek
        /// <summary>
        /// Új táblát hoz létre a megadott méretek szerint.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="newSize"></param>
        public void ChangeTableSize(object sender, Coords newSize)
        {
            _model.Table = new Persistence.Table(newSize.X, newSize.Y);
            Fields = new ObservableCollection<Field>();
            for (int i = 0; i < _model.Table.SizeX; i++) // inicializáljuk a mezőket
            {
                for (int j = 0; j < _model.Table.SizeY; j++)
                {
                    Fields.Add(new Field
                    {
                        Text = String.Empty,
                        X = i,
                        Y = j,
                        Number = i * _model.Table.SizeY + j,
                        Type = Types.EMPTY,
                        StepCommand = new DelegateCommand(param => HandleSelection(Convert.ToInt32(param))),  // teszteles miatt adtam hozza
                        HoverCommand = new DelegateCommand((param) => ShowMovingPreview(Convert.ToInt32(param)))
                    });
                }
            }
            for (int i = 0; i < _model.Table.SizeX; i++)
            {
                for (int j = 0; j < _model.Table.SizeY; j++)
                {
                    _model.Table.SetValue(i, j, Types.EMPTY);
                }
            }
            OnPropertyChanged("Fields");
            OnPropertyChanged("TableSizeX");
            OnPropertyChanged("TableSizeY");
            OnPropertyChanged("TimerCount");
            OnPropertyChanged("AllBatteryUsed");
            RefreshTable();
        }

        /// <summary>
        /// Frissíti a nézetet, ha a modellben új tábla jön létre.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Model_TableCreated(object sender, Model.EventArgs e)
        {
            PauseSimulation?.Invoke(this, null);
            RefreshTable();
        }

        /// <summary>
        /// A szimuláció végét jelző eventet kezelő metódus.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Model_SimulationEnded(object sender, Model.EventArgs e)
        {
            PauseSimulation?.Invoke(this, null);
            _startPauseButtonText = "START";
            OnPropertyChanged("StartPauseButtonText");
            OnPropertyChanged("IsSimulationPaused");
        }

        /// <summary>
        /// Jelez az alkalmazásnak, ha a felhasználó állít a sebességi csúszkán.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSpeedSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<int> e)
        {
            MessageBox.Show("Állítva");
            SetSliderValueCommand.Execute(Convert.ToInt32(e.NewValue));
            ModifySimulationSpeed?.Invoke(this, Convert.ToInt32(e.NewValue));
        }
        #endregion
    }
}
