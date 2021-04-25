using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using AMZN.ViewModel;
using AMZN.Model;
using System.Windows.Threading;
using AMZN.Persistence;
using Microsoft.Win32;
using Microsoft.AspNetCore.SignalR.Client;
using AMZN.Services;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace AMZN
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ViewModel.ViewModel _viewModel; // itt valamiért össze akad valamivel azért írtam ki a pontosabb névteret
        private IDataAccess _dataAccess;
        private MainWindow _view;
        private Model.Model _model;
        private DispatcherTimer _timer;
        // private bool _startStop;
        public App()
        {
            Startup += new StartupEventHandler(App_Startup);
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            HubConnection connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/AMZN")
                .Build();

            _model = new Model.Model();
            _model.SimulationEnded += _model_SimulationEnded;

            _viewModel = ViewModel.ViewModel.CreatedConnectedViewModel(_model, new AMZNServices(connection));
            _viewModel.StartSimulation += new EventHandler(ViewModel_Start);    
            _viewModel.PauseSimulation += new EventHandler(ViewModel_Pause);
            
            
            _viewModel.StartOnlineOrder += new EventHandler(ViewModel_StartOnlineOrder);

            _viewModel.ModifySimulationSpeed += OnSimulationSpeedChanged;
            _viewModel.SaveMap += new EventHandler(ViewModel_Save);
            _viewModel.LoadMap += new EventHandler(ViewModel_Load);
            _viewModel.ExportData += new EventHandler(ViewModel_ExportData);
            _viewModel.NotifyAppAboutSpeed += new EventHandler<int>(ViewModel_SpeedNotifier);
            _dataAccess = new DataAccess();
            _model.DataAccess = _dataAccess;

            _view = new MainWindow();
            _view.SliderDragged += new EventHandler<int>(View_SliderDragged);
            _view.DataContext = _viewModel;
            // ide szerintem kelleni fog egy esemény az online orderre

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(500);
            _timer.Tick += new EventHandler(Timer_Tick);

            _view.Show();

            _view.Closed += OnViewClosed;
        }

        private void ViewModel_SpeedNotifier(object sender, int e)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(1501 - e);
        }

        private void View_SliderDragged(object sender, int e)
        {
            _viewModel.SetSliderValueCommand.Execute(e);
        }

        private async void ViewModel_Load(object sender, System.EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "AMZN tábla betöltése";
                openFileDialog.Filter = "AMZN tábla|*.txt";
                if (openFileDialog.ShowDialog() == true)
                {
                    SaveLoadData data = await _dataAccess.LoadAsync(openFileDialog.FileName);
                    _viewModel.ChangeTableSize(this, new Coords(data.table.SizeX, data.table.SizeY));
                    _model.Init(data);
                    _viewModel.RefreshTable();
                    if(_viewModel.Service != null)
                    {
                        _viewModel.OnlineLoadCommand.Execute(data.allLines);
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message.ToString());
                MessageBox.Show("Failed to load from file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewModel_Save(object sender, System.EventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "AMZN tábla mentése";
                saveFileDialog.Filter = "AMZN tábla|*.txt";
                if (saveFileDialog.ShowDialog() == true)
                {
                    _dataAccess.Save(saveFileDialog.FileName, new SaveLoadData(
                        _model.Table, _model.Robots, _model.Pods, _model.Stations, _model.Dockers, _model.AllBatteryUsed, _model.TimerCount, ""));
                }
            }
            catch
            {
                MessageBox.Show("Failed to save to file!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewModel_ExportData(object sender, System.EventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "AMZN adatok exportálása";
                saveFileDialog.Filter = "JSON|*.json";
                if (saveFileDialog.ShowDialog() == true)
                {
                    _viewModel.ExportDataToFile(saveFileDialog.FileName);
                }
            }
            catch
            {
                MessageBox.Show("Failed to export data!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void _model_SimulationEnded(object sender, Model.EventArgs e)
        {
            _timer.Stop();
            MessageBox.Show("The simulation is over.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Timer_Tick(object sender, System.EventArgs e)
        {
            _model.TimerCount++;
            _viewModel.StepSimulation();
           
            _viewModel.EnableDisableOnlineOrder();
            
           
            /* if (_model.TimerCount % 15 == 0)
             {
                 Random rd = new Random();
                 int random = rd.Next(0, _model.Pods.Values.Count);  // kiveszem az indexet amire rakni akarok
                 if (_model.Pods.Values.Count != 0 && _model.Pods.Values.ElementAt(random).Items.Count < 4)
                 {
                     int item = rd.Next(1, 5);
                     while (!_model.Pods.Values.ElementAt(random).Items.Contains(item))
                     {
                         _model.Pods.Values.ElementAt(random).Items.Add(item);
                     }
                 }
             }*/

            // ez ide valószínűleg nem kéne, melyik passzolja tovább az értékeket ez a kérdés
         
        }
        // ez a ketto felesleges ide
        private void ViewModel_PauseOnlineOrder(object sender, System.EventArgs e)
        {
            
            //_viewModel.SendOnlineOrderCommand.Execute(e); // nemtudom hogy így kell e? de amikor itt volt lehalt tőle
        }

        private void ViewModel_StartOnlineOrder(object sender, System.EventArgs e)
        {
            _viewModel.EnableDisableOnlineOrder();
        }

        private void ViewModel_Start(object sender, System.EventArgs e)
        {
            if (!_model.CheckQuarters())
            {
                MessageBox.Show("Blocked Station!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            _timer.Start();
        }
        private void ViewModel_Pause(object sender, System.EventArgs e) => _timer.Stop();

        private void OnSimulationSpeedChanged(object sender, int value)
        {
            if(_viewModel.Service != null)
            {
                _viewModel.SetSliderValueCommand.Execute(value);
            }
            _timer.Interval = TimeSpan.FromMilliseconds(1501 - value);
        }

        private void OnViewClosed(object sender, System.EventArgs e)
        {
            Current.Shutdown();
        }
    }
}
