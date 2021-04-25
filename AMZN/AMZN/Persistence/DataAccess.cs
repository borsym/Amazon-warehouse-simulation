using AMZN.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AMZN.Persistence
{
    public class DataAccess : IDataAccess
    {
        public async Task<SaveLoadData> LoadAsync(String path)
        {
            SaveLoadData result = new SaveLoadData();
            result.pods = new Dictionary<Coords, Pod>();
            result.stations = new Dictionary<Coords, Station>();
            result.dockers = new Dictionary<Coords, Docker>();
            result.robots = new Dictionary<Coords, Robot>();
            try
            {
                using (StreamReader reader = new StreamReader(path)) // fájl megnyitása
                {
                    string allLines = "";
                    String line = reader.ReadLine();
                    allLines += line;
                    allLines += "\n";
                    String[] numbers = line.Split(' '); // beolvasunk egy sort, és a szóköz mentén széttöredezzük
                    Int32 tableSizeN = Int32.Parse(numbers[0]); // beolvassuk a tábla méretét
                    Int32 tableSizeM = Int32.Parse(numbers[1]);
                    result.table = new Table(tableSizeN, tableSizeM); // létrehozzuk a táblát
                    line = reader.ReadLine();
                    allLines += line;
                    allLines += "\n";
                    numbers = line.Split(' ');
                    result.allBatteryUsed = Convert.ToInt32(numbers[0].Trim());
                    result.time = Convert.ToInt32(numbers[1].Trim());

                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine().Trim();
                        allLines += line;
                        allLines += "\n";
                        String[] splitted = line.Split(' ');

                        if (splitted[0] == "R")
                        {
                            //R (x, y, id, akku)
                            for (int i = 1; i < splitted.Length; ++i)
                            {
                                //zárójelek törlése
                                splitted[i] = splitted[i].Remove(splitted[i].Length - 1);
                                splitted[i] = splitted[i].Remove(0, 1);

                                String[] data = splitted[i].Split(',');
                                int x = Convert.ToInt32(data[0].Trim());
                                int y = Convert.ToInt32(data[1].Trim());
                                int id = Convert.ToInt32(data[2].Trim());
                                int battery = Convert.ToInt32(data[3].Trim());
                                result.robots.Add(new Coords(x, y), new Robot(new Coords(x, y), result.robots.Count));
                                result.table.SetValue(x, y, Types.ROBOT);
                            }
                        }
                        else if (splitted[0] == "P")
                        {
                            for (var i = 1; i < splitted.Length; ++i)
                            {
                                //zárójelek törlése
                                splitted[i] = splitted[i].Remove(splitted[i].Length - 1);
                                splitted[i] = splitted[i].Remove(0, 1);

                                String[] data = splitted[i].Split(',');
                                int x = Convert.ToInt32(data[0].Trim());
                                int y = Convert.ToInt32(data[1].Trim());

                                String list = data[2].Trim();

                                //zárójelek elhagyása
                                list = list.Remove(list.Length - 1);
                                list = list.Remove(0, 1);

                                Pod pod = new Pod(x, y, new List<int>());
                                if(list != "")
                                {
                                    String[] listElements = list.Split(';');
					                foreach (var item in listElements)
                                	{
                                        pod.Add(Convert.ToInt32(item));
                                    }
                                }

                                result.pods.Add(new Coords(x, y), pod);
                                switch(result.table[x, y])
                                {
                                    case Types.ROBOT:
                                        result.table.SetValue(x, y, Types.ROBOT_UNDER_POD);
                                        break;
                                    default:
                                        result.table.SetValue(x, y, Types.POD);
                                        break;
                                }

                            }
                        }
                        else if (splitted[0] == "S")
                        {
                            //R (x, y, akku)
                            for (var i = 1; i < splitted.Length; ++i)
                            {
                                //zárójelek elhagyása
                                splitted[i] = splitted[i].Remove(splitted[i].Length - 1);
                                splitted[i] = splitted[i].Remove(0, 1);

                                String[] data = splitted[i].Split(',');
                                int x = Convert.ToInt32(data[0].Trim());
                                int y = Convert.ToInt32(data[1].Trim());
                                int id = Convert.ToInt32(data[2].Trim());

                                result.stations.Add(new Coords(x, y), new Station(new Coords(x, y), id));

                                switch(result.table[x, y])
                                {
                                    case Types.ROBOT:
                                        result.table.SetValue(x, y, Types.ROBOT_ON_STATION);
                                        break;
                                    default:
                                        result.table.SetValue(x, y, Types.STATION);
                                        break;
                                }
                            }
                        }
                        else if (splitted[0] == "D")
                        {
                            //R (x, y, akku)
                            for (var i = 1; i < splitted.Length; ++i)
                            {
                                //zárójelek elhagyása
                                splitted[i] = splitted[i].Remove(splitted[i].Length - 1);
                                splitted[i] = splitted[i].Remove(0, 1);

                                String[] data = splitted[i].Split(',');
                                int x = Convert.ToInt32(data[0].Trim());
                                int y = Convert.ToInt32(data[1].Trim());

                                result.dockers.Add(new Coords(x, y), new Docker(new Coords(x, y)));
                                switch(result.table[x, y])
                                {
                                    case Types.ROBOT:
                                        result.table.SetValue(x, y, Types.ROBOT_ON_DOCKER);
                                        break;
                                    default:
                                        result.table.SetValue(x, y, Types.DOCKER);
                                        break;
                                }
                            }
                        }
                    }
                    result.allLines = allLines;
                    // MessageBox.Show("TÉRKÉP BETÖLTVE");
                    return result;
                }
            }
            catch
            {
                throw new DataException();
            }
        }

        public SaveLoadData LoadFromText(string lines)
        {
            SaveLoadData result = new SaveLoadData();
            result.pods = new Dictionary<Coords, Pod>();
            result.stations = new Dictionary<Coords, Station>();
            result.dockers = new Dictionary<Coords, Docker>();
            result.robots = new Dictionary<Coords, Robot>();
            byte[] byteArray = Encoding.UTF8.GetBytes(lines);
            MemoryStream stream = new MemoryStream(byteArray);

            try
            {
                using (StreamReader reader = new StreamReader(stream)) // fájl megnyitása
                {
                    string allLines = "";
                    String line = reader.ReadLine();
                    allLines += line;
                    allLines += "\n";
                    String[] numbers = line.Split(' '); // beolvasunk egy sort, és a szóköz mentén széttöredezzük
                    Int32 tableSizeN = Int32.Parse(numbers[0]); // beolvassuk a tábla méretét
                    Int32 tableSizeM = Int32.Parse(numbers[1]);
                    result.table = new Table(tableSizeN, tableSizeM); // létrehozzuk a táblát
                    line = reader.ReadLine();
                    allLines += line;
                    allLines += "\n";
                    numbers = line.Split(' ');
                    result.allBatteryUsed = Convert.ToInt32(numbers[0].Trim());
                    result.time = Convert.ToInt32(numbers[1].Trim());

                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine().Trim();
                        allLines += line;
                        allLines += "\n";
                        String[] splitted = line.Split(' ');

                        if (splitted[0] == "R")
                        {
                            //R (x, y, id, akku)
                            for (int i = 1; i < splitted.Length; ++i)
                            {
                                //zárójelek törlése
                                splitted[i] = splitted[i].Remove(splitted[i].Length - 1);
                                splitted[i] = splitted[i].Remove(0, 1);

                                String[] data = splitted[i].Split(',');
                                int x = Convert.ToInt32(data[0].Trim());
                                int y = Convert.ToInt32(data[1].Trim());
                                int id = Convert.ToInt32(data[2].Trim());
                                int battery = Convert.ToInt32(data[3].Trim());
                                result.robots.Add(new Coords(x, y), new Robot(new Coords(x, y), result.robots.Count));
                                result.table.SetValue(x, y, Types.ROBOT);
                            }
                        }
                        else if (splitted[0] == "P")
                        {
                            for (var i = 1; i < splitted.Length; ++i)
                            {
                                //zárójelek törlése
                                splitted[i] = splitted[i].Remove(splitted[i].Length - 1);
                                splitted[i] = splitted[i].Remove(0, 1);

                                String[] data = splitted[i].Split(',');
                                int x = Convert.ToInt32(data[0].Trim());
                                int y = Convert.ToInt32(data[1].Trim());

                                String list = data[2].Trim();

                                //zárójelek elhagyása
                                list = list.Remove(list.Length - 1);
                                list = list.Remove(0, 1);

                                Pod pod = new Pod(x, y, new List<int>());
                                if (list != "")
                                {
                                    String[] listElements = list.Split(';');
                                    foreach (var item in listElements)
                                    {
                                        pod.Add(Convert.ToInt32(item));
                                    }
                                }

                                result.pods.Add(new Coords(x, y), pod);
                                switch (result.table[x, y])
                                {
                                    case Types.ROBOT:
                                        result.table.SetValue(x, y, Types.ROBOT_UNDER_POD);
                                        break;
                                    default:
                                        result.table.SetValue(x, y, Types.POD);
                                        break;
                                }

                            }
                        }
                        else if (splitted[0] == "S")
                        {
                            //R (x, y, akku)
                            for (var i = 1; i < splitted.Length; ++i)
                            {
                                //zárójelek elhagyása
                                splitted[i] = splitted[i].Remove(splitted[i].Length - 1);
                                splitted[i] = splitted[i].Remove(0, 1);

                                String[] data = splitted[i].Split(',');
                                int x = Convert.ToInt32(data[0].Trim());
                                int y = Convert.ToInt32(data[1].Trim());
                                int id = Convert.ToInt32(data[2].Trim());

                                result.stations.Add(new Coords(x, y), new Station(new Coords(x, y), id));

                                switch (result.table[x, y])
                                {
                                    case Types.ROBOT:
                                        result.table.SetValue(x, y, Types.ROBOT_ON_STATION);
                                        break;
                                    default:
                                        result.table.SetValue(x, y, Types.STATION);
                                        break;
                                }
                            }
                        }
                        else if (splitted[0] == "D")
                        {
                            //R (x, y, akku)
                            for (var i = 1; i < splitted.Length; ++i)
                            {
                                //zárójelek elhagyása
                                splitted[i] = splitted[i].Remove(splitted[i].Length - 1);
                                splitted[i] = splitted[i].Remove(0, 1);

                                String[] data = splitted[i].Split(',');
                                int x = Convert.ToInt32(data[0].Trim());
                                int y = Convert.ToInt32(data[1].Trim());

                                result.dockers.Add(new Coords(x, y), new Docker(new Coords(x, y)));
                                switch (result.table[x, y])
                                {
                                    case Types.ROBOT:
                                        result.table.SetValue(x, y, Types.ROBOT_ON_DOCKER);
                                        break;
                                    default:
                                        result.table.SetValue(x, y, Types.DOCKER);
                                        break;
                                }
                            }
                        }
                    }
                    result.allLines = allLines;
                    return result;
                }
            }
            catch
            {
                throw new DataException();
            }
        }

        void IDataAccess.Save(string path, SaveLoadData data)  // mondjuk nem vágom miért írtam konstruktorba a modelt xd
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(path)) // fájl megnyitása
                {
                    //tábla méret kiírása
                    writer.WriteLine(data.table.SizeX.ToString() + " " + data.table.SizeY.ToString());

                    //összes energia és idő kiírása
                    writer.WriteLine(data.allBatteryUsed.ToString() + " " + data.time.ToString());

                    writer.Write("R ");
                    foreach (var robot in data.robots)
                    {
                        writer.Write("(" +
                                            robot.Key.X.ToString() + "," +
                                            robot.Key.Y.ToString() + "," +
                                            robot.Value.Id.ToString() + "," +
                                            robot.Value.BatteryLevel.ToString() + ") ");
                    }
                    writer.Write("\n"); // end of writing robots


                    writer.Write("P ");
                    foreach(var pod in data.pods)
                    {
                        writer.Write("(" +
                                            pod.Key.X.ToString() + "," +
                                            pod.Key.Y.ToString() + ",[");
                        for(int i = 0; i < pod.Value.Items.Count; ++i)
                        {
                            if(i == pod.Value.Items.Count - 1)
                            {
                                writer.Write(pod.Value.Items[i].ToString() + "]) ");
                            }
                            else
                            {
                                writer.Write(pod.Value.Items[i].ToString() + ";");
                            }
                        }
			if(pod.Value.Items.Count == 0) writer.Write("]) ");
                    }
                    writer.Write("\n"); // end of writing pods


                    writer.Write("S ");
                    foreach (var station in data.stations)
                    {
                        writer.Write("(" +
                                            station.Key.X.ToString() + "," +
                                            station.Key.Y.ToString() + "," +
                                            station.Value.Id.ToString() + ") ");
                    }
                    writer.Write("\n"); // end of writing staions


                    writer.Write("D ");
                    foreach (var docker in data.dockers)
                    {
                        writer.Write("(" +
                                            docker.Key.X.ToString() + "," +
                                            docker.Key.Y.ToString() + ") ");
                    }
                    writer.Write("\n"); // end of writing staions

                }
            }
            catch
            {
                throw new DataException();
            }
        }
    }
}
