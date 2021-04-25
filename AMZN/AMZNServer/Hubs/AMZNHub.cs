using AMZN.Persistence;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AMZNServer.Hubs
{// ezt bele kellett rakni a fő projectbe mert nem látta sehogy se
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
    public class AMZNHub : Hub
    {
        public async Task SendStart(bool val)
        {
            await Clients.All.SendAsync("ReceiveStart", val);
        }
        public async Task SendOnlineOrder(OnlineOrder val)
        {
            await Clients.Others.SendAsync("ReceiveOnlineOrder", val);
            Debug.WriteLine(val.Item + " " + val.X);
        }

        public async Task SendSliderValue(int val)
        {
            await Clients.Others.SendAsync("ReceiveSliderValue", val);
        }


        public async Task LoadOnline(string val)
        {
            await Clients.Others.SendAsync("ReceiveLoadMap", val);
            Debug.WriteLine("ADAT MEGÉRKEZETT");
            Debug.WriteLine(val);
        }
    }
}
