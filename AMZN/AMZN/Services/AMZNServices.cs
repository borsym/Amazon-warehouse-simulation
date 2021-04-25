using AMZN.Model;
using AMZN.Persistence;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AMZN.Services
{
    public struct OnlineOrder
    {
        int _x;
        int _y;
        int _item;
        public OnlineOrder(int x, int y, int item)
        {
            _x = x;
            _y = y;
            _item = item;
        }

        public int Item { get => _item; set => _item = value; }
        public int Y { get => _y; set => _y = value; }
        public int X { get => _x; set => _x = value; }
    }
    public class AMZNServices
    {
        private readonly HubConnection _connection;

        public event Action<bool> StartActionReceived;
        public event Action<int> SliderValueReceived;
        public event Action<string> LoadMapReceived;
        public event Action<OnlineOrder> OnlineOrderReceived; // 3 dolgot akarok átpaszolni? vagy csak azt hogy bevan kapcsolva és akkor generálja
        public AMZNServices(HubConnection connection)
        {
            _connection = connection;

            _connection.On<bool>("ReceiveStart", (val) => StartActionReceived?.Invoke(val));
            _connection.On<int>("ReceiveSliderValue", (val) => SliderValueReceived?.Invoke(val));
            _connection.On<string>("ReceiveLoadMap", (val) => LoadMapReceived?.Invoke(val));
            _connection.On<OnlineOrder>("ReceiveOnlineOrder", (val) => OnlineOrderReceived?.Invoke(val));
        }

        public async Task Connect()
        {
            await _connection.StartAsync();
        }

        public async Task SendStart(bool val)
        {
            await _connection.SendAsync("SendStart", val);
        }
        public async Task SendOnlineOrder(OnlineOrder val)
        {
            await _connection.SendAsync("SendOnlineOrder",val); // elküldöm a kordinátákat és a productot?
        }

        public async Task SendSliderValue(int val)
        {
            await _connection.SendAsync("SendSliderValue", val);
        }

        public async Task LoadOnline(string val)
        {
            await _connection.SendAsync("LoadOnline", val);
        }
    }
}
