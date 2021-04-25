using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AMZN.Model;

namespace AMZN.Persistence
{
    public class SaveLoadData
    {
        public Table table { get; set; }
        public Dictionary<Coords, Robot> robots { get; set; }
        public Dictionary<Coords, Pod> pods { get; set; }
        public Dictionary<Coords, Station> stations { get; set; }
        public Dictionary<Coords, Docker> dockers { get; set; }
        public int allBatteryUsed { get; set; }
        public int time { get; set; }
        public string allLines { get; set; }
        public SaveLoadData(Table _table, Dictionary<Coords, Robot> _robots, Dictionary<Coords, Pod> _pods, Dictionary<Coords, Station> _stations,
            Dictionary<Coords, Docker> _dockers, int _allBatteryUsed, int _time, string _allLines)
        {
            table = _table;
            robots = _robots;
            pods = _pods;
            stations = _stations;
            dockers = _dockers;
            allBatteryUsed = _allBatteryUsed;
            time = _time;
            allLines = _allLines;
        }

        public SaveLoadData() { }
    }
    public interface IDataAccess
    {
        Task<SaveLoadData> LoadAsync(String path);
        SaveLoadData LoadFromText(string lines);
        void Save(string path, SaveLoadData data);
    }
}
