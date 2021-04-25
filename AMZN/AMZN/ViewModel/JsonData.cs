using System;
using System.Collections.Generic;
using System.Text;

namespace AMZN.ViewModel
{
    class JsonData
    {
        public int AllBatteryUsed { get; set; }
        public int CountOfProductsToDeliver { get; set; }
        public List<JsonRobot> Robots { get; set; }
    }

    class JsonRobot
    {
        public int Id { get; set; }
        public int BatteryLevel { get; set; }
        public int BatteryUsed { get; set; }
    }
}
