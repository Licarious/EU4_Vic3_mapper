

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

internal class Program
{
    private static void Main(string[] args) {
        string localDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;

        int minCount = 20; //the minimum pixel size a prov has to be before it will be used when mathcing eu4 and vic3 provs
        float poorP = 0.1f; //how much small should the best bigest match between eu4 and vic3 provs be before it should be flaged
        bool removeWastelandBool = false; //should wastland be removed before comparing eu4 and vic3 provs
        bool manyToOne = false; //should multiple eu4 ids go to a singe vic3 id (not implamented for Vic3_HOI4)

        string converterFlag = "EU4_Vic3_HOI4"; //"EU4_Vic3" or "Vic3_HOI4"
        

        //stopwatch
        Stopwatch sw = Stopwatch.StartNew();

        String HexConverter(System.Drawing.Color c) {
            return "x" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        //method to parse state files
        void parseStateFilesVic3(List<State> stateList) {
            //read all files in localDir/_Input/state_regions
            string[] files = Directory.GetFiles(localDir + "/_Input/Vic3/state_regions");
            //for each file
            int count = 0;

            foreach (string file in files) {
                if (file.EndsWith(".txt")) {
                    //read file
                    string[] lines = File.ReadAllLines(file);
                    //for each line
                    //Console.WriteLine(file);
                    State s = new State();
                    bool traitsfound = false;
                    foreach (string l1 in lines) {
                        string line = l1.Replace("=", " = ").Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Replace("  ", " ").Trim();

                        //get STATE_NAME
                        if (line.StartsWith("STATE_")) {
                            //Console.WriteLine("\t"+line.Split()[0]);
                            s = new State(line.Split()[0]);

                            //incase people are orverriding states in latter files
                            //check if state with same name already exists in stateList and if so, delete it
                            foreach (State state in stateList) {
                                if (state.name == s.name) {
                                    stateList.Remove(state);
                                    break;
                                }
                            }

                            stateList.Add(s);
                        }
                        //get stateID
                        if (line.StartsWith("id")) {
                            s.stateID = int.Parse(line.Split()[2]);
                        }
                        if (line.StartsWith("subsistence_building")) {
                            s.subsistanceBuilding = line.Split("=")[1].Replace("\"", "").Trim();
                        }

                        //get provinces
                        if (line.TrimStart().StartsWith("provinces")) {
                            string[] l2 = line.Split("=")[1].Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                                    string n = l2[i].Replace("\"", "").Replace("x", "");
                                    s.provIDList.Add(n);
                                    s.provList.Add(new Prov(ColorTranslator.FromHtml("#" + n)));
                                }
                            }
                        }
                        //get impassable colors
                        if (line.TrimStart().StartsWith("impassable")) {
                            string[] l2 = line.Split("=")[1].Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                                    string n = l2[i].Replace("\"", "").Replace("x", "");
                                    Color c = ColorTranslator.FromHtml("#" + n);
                                    s.impassables.Add(c);

                                    //set isWastland for that prov color to ture
                                    foreach (Prov p in s.provList) {
                                        if (p.color == c) {
                                            p.isWasteland = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        //get prime_land colors
                        if (line.TrimStart().StartsWith("prime_land")) {
                            string[] l2 = line.Split("=")[1].Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"x") || l2[i].StartsWith("x")) {
                                    string n = l2[i].Replace("\"", "").Replace("x", "");
                                    Color c = ColorTranslator.FromHtml("#" + n);
                                    s.primeLand.Add(c);
                                }
                            }
                        }

                        //get traits
                        if (line.Trim().StartsWith("traits")) {
                            traitsfound = true;
                        }
                        if (traitsfound) {
                            string[] l2 = line.Split(' ');
                            for (int i = 0; i < l2.Length; i++) {
                                if (l2[i].StartsWith("\"")) {
                                    s.traits.Add(l2[i].Replace("\"", ""));
                                }
                            }
                        }

                        //get arable_land
                        if (line.TrimStart().StartsWith("arable_land")) {
                            s.arableLand = int.Parse(line.Split("=")[1].Trim());
                            count++;
                        }
                        //get naval id
                        if (line.TrimStart().StartsWith("naval_exit_id")) {
                            string[] l2 = line.Split("=");
                            s.navalID = int.Parse(l2[1].Trim());
                        }

                        //get city color
                        if (line.TrimStart().StartsWith("city") || line.TrimStart().StartsWith("port") || line.TrimStart().StartsWith("farm") || line.TrimStart().StartsWith("mine") || line.TrimStart().StartsWith("wood")) {
                            string[] l2 = line.Split("=");
                            s.hubs.Add((l2[0].Trim(), ColorTranslator.FromHtml("#" + l2[1].Replace("\"", "").Replace("x", "").Trim())));
                            s.color = s.hubs[0].color;
                        }
                        //reset cappedResourseFound and discoverableResourseFound
                        if (line.Trim().StartsWith("}")) {
                            traitsfound = false;
                        }

                    }
                }
            }
        }

        List<AreaEU4> parseAreaFileEU4(List<Prov> provList) {
            string[] lines = File.ReadAllLines(localDir + "/_Input/EU4/area.txt");

            List<AreaEU4> areaList = new List<AreaEU4>();
            AreaEU4 a = new AreaEU4();
            bool areaFound = false;
            foreach (string l1 in lines) {
                if (l1.Trim().StartsWith("#") || l1.Trim() == "") {
                    continue;
                }
                if (l1.Contains("=")) {
                    string areaName = l1.Split("=")[0].Trim();
                    if(areaName== "color") {
                        continue;
                    }
                    a = new AreaEU4(areaName);
                    areaFound = true;
                }
                if (areaFound) {
                    if (l1.Trim().StartsWith("color")) {
                        continue;
                    }
                    string[] l2 = l1.Replace("#", " # ").Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Trim().Split(" ");
                    for (int i = 0; i < l2.Length; i++) {
                        if (int.TryParse(l2[i], out int n)) {
                            foreach (Prov p in provList) {
                                if (p.provID == n) {
                                    a.provList.Add(p);
                                    break;
                                }
                            }
                        }
                        else if (l2[i].Contains("}")) {
                            areaFound = false;

                            //if area has no prov in it, dont add it
                            if (a.provList.Count == 0) {
                                break;
                            }

                            //set color of area to color of first prov
                            if (a.provList.Count > 0) {
                                a.color = a.provList[0].color;
                            }
                            
                            areaList.Add(a);
                            break;
                        }
                        else if (l2[i].Contains("#")) {
                            break;
                        }
                    }
                }
            }

            return areaList;
        }

        //method to parse definition.csv
        void parseDefinitionEU4(List<Prov> provList) {
            //open _Input/EU4/definition.csv
            string[] lines = File.ReadAllLines(localDir + "/_Input/EU4/definition.csv");

            //for each line
            foreach (string l1 in lines) {
                if (l1.Contains(";")) {
                    string[] l2 = l1.Split(";");
                    //if l2[0] is an int
                    if (int.TryParse(l2[0], out int n)) {
                        Prov p = new Prov(Color.FromArgb(int.Parse(l2[1]), int.Parse(l2[2]), int.Parse(l2[3])));
                        p.provID = n;
                        p.name = l2[4];

                        provList.Add(p);
                    }
                }
            }

            string[] lines2 = File.ReadAllLines(localDir + "/_Input/EU4/climate.txt");
            bool foundImpassable = false;
            foreach (string l1 in lines2) {
                string l2 = l1.Replace("=", " = ").Replace("{", " { ").Replace("}", " } ").Replace(";", " ; ").Replace("\"", " \" ").Replace("  ", " ").Trim();
                if (l2.StartsWith("impassable")) {
                    foundImpassable = true;
                }
                if (foundImpassable) {
                    //check if each word in l2 is an int
                    string[] l3 = l2.Split(' ');
                    foreach (string l4 in l3) {
                        if (int.TryParse(l4, out int n)) {
                            foreach (Prov p in provList) {
                                if (p.provID == n) {
                                    p.isWasteland = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (l2.Contains("}")) {
                    foundImpassable = false;
                }
            }
        }

        void parseDefinitionHOI4(List<Prov> provList) {
            string[] lines = File.ReadAllLines(localDir + "/_Input/HOI4/definition.csv");

            //for each line
            foreach (string l1 in lines) {
                if (l1.Contains(";")) {
                    string[] l2 = l1.Split(";");
                    //if l2[0] is an int
                    if (int.TryParse(l2[0], out int n)) {
                        Prov p = new Prov(Color.FromArgb(int.Parse(l2[1]), int.Parse(l2[2]), int.Parse(l2[3])));
                        p.provID = n;
                        p.isWater = (l2[4] == "sea" || l2[4] == "lake");

                        provList.Add(p);
                    }
                }
            }
        }

        //parse default.map file
        void parseDefaultMapEU4(List<Prov> provList) {
            //open _Input/EU4/default.map
            string[] lines = File.ReadAllLines(localDir + "/_Input/EU4/default.map");

            bool waterFound = false;
            //for each line
            foreach (string l1 in lines) {
                string line = l1.Replace("=", " = ").Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Replace("  ", " ").Trim();

                if (line.StartsWith("sea_starts") || line.StartsWith("lakes")) {
                    waterFound = true;
                }

                if (waterFound) {

                    //split line into array
                    string[] l2 = line.Split(' ');
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("#")) {
                            break;
                        }
                        if (int.TryParse(l2[i], out int n)) {
                            //set isWater to true for provList with provID = n
                            foreach (Prov p in provList) {
                                if (p.provID == n) {
                                    p.isWater = true;
                                }
                            }
                        }
                    }

                    if (line.Contains("}")) {
                        waterFound = false;
                    }
                }

            }
        }

        void parseDefaultMapVic3(List<State> stateList) {
            string[] lines = File.ReadAllLines(localDir + "/_Input/Vic3/default.map");

            bool waterFound = false;
            //for each line
            foreach (string l1 in lines) {
                string line = l1.Replace("=", " = ").Replace("{", " { ").Replace("}", " } ").Replace("#", " # ").Replace("  ", " ").Trim();

                if (line.StartsWith("sea_starts") || line.StartsWith("lakes")) {
                    waterFound = true;
                }

                if (waterFound) {

                    //split line into array
                    string[] l2 = line.Split(' ');
                    for (int i = 0; i < l2.Length; i++) {
                        if (l2[i].StartsWith("#")) {
                            break;
                        }
                        if (l2[i].StartsWith("x")) {
                            Color c = ColorTranslator.FromHtml("#" + l2[i].Replace("x", ""));
                            //set isWater to true for provList with provID = n
                            foreach (State s in stateList) {
                                foreach (Prov p in s.provList) {
                                    if (p.color == c) {
                                        p.isWater = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (line.Contains("}")) {
                        waterFound = false;
                    }
                }
            }
        }

        //parse provinces.png file
        void parseProvincesPreV3(List<Prov> provList, string path) {
            //open _Input/EU4/provinces.png
            Bitmap bmp = new Bitmap(localDir + path);

            Console.WriteLine("Compressing " + path.Split("/")[2] + " Prov Map");
            //compress image into 2D colorList and 2D lengthList
            List<List<Color>> colorList = new List<List<Color>>();
            List<List<int>> lengthList = new List<List<int>>();
            for (int i = 0; i < bmp.Width; i++) {
                if (i % 512 == 0) {
                    Console.WriteLine("\t" + i * 100 / bmp.Width + "%");
                }

                colorList.Add(new List<Color>());
                lengthList.Add(new List<int>());

                colorList[i].Add(bmp.GetPixel(i, 0));
                int tmpLength = 0;
                int tx = 0;

                for (int j = 0; j < bmp.Height; j++) {
                    //check if pixel is the same as current last one in colorList
                    if (bmp.GetPixel(i, j) == colorList[i][colorList[i].Count - 1]) {
                        tmpLength++;
                    }
                    else {
                        colorList[i].Add(bmp.GetPixel(i, j));
                        lengthList[i].Add(tmpLength);
                        tx += tmpLength;
                        tmpLength = 1;
                    }
                }
                lengthList[i].Add(tmpLength);
            }

            //match provList color to colorList color and add the coords to prov coords
            Console.WriteLine("Matching " + path.Split("/")[2] + " Prov Map");
            for (int i = 0; i < colorList.Count; i++) {
                if (i % 512 == 0) {
                    Console.WriteLine("\t" + i * 100 / bmp.Width + "%");
                }

                int tx = 0;
                for (int j = 0; j < colorList[i].Count; j++) {
                    tx += lengthList[i][j];
                    if (tx >= bmp.Height) {
                        break;
                    }
                    //if alpha is 0, skip
                    if (colorList[i][j].A == 0) {
                        continue;
                    }

                    foreach (Prov p in provList) {
                        if (p.color == colorList[i][j]) {
                            for (int k = 0; k < lengthList[i][j]; k++) {
                                p.coords.Add((i, tx - k - 1));
                            }
                            goto stateCheckExit;
                        }
                    }

                    stateCheckExit:
                    int ignoreMe = 0;
                }
            }
        }

        void parseProvincesVic3(List<State> stateList) {
            //open _Input/Vic3/provinces.png
            Bitmap bmp = new Bitmap(localDir + "/_Input/Vic3/provinces.png");

            Console.WriteLine("Compressing Vic3 Prov Map");
            //compress image into 2D colorList and 2D lengthList
            List<List<Color>> colorList = new List<List<Color>>();
            List<List<int>> lengthList = new List<List<int>>();
            for (int i = 0; i < bmp.Width; i++) {
                if (i % 512 == 0) {
                    Console.WriteLine("\t" + i * 100 / bmp.Width + "%");
                }

                colorList.Add(new List<Color>());
                lengthList.Add(new List<int>());

                colorList[i].Add(bmp.GetPixel(i, 0));
                int tmpLength = 0;
                int tx = 0;

                for (int j = 0; j < bmp.Height; j++) {
                    //check if pixel is the same as current last one in colorList
                    if (bmp.GetPixel(i, j) == colorList[i][colorList[i].Count - 1]) {
                        tmpLength++;
                    }
                    else {
                        colorList[i].Add(bmp.GetPixel(i, j));
                        lengthList[i].Add(tmpLength);
                        tx += tmpLength;
                        tmpLength = 1;
                    }
                }
                lengthList[i].Add(tmpLength);
            }

            //match provList color to colorList color and add the coords to prov coords
            Console.WriteLine("Matching Vic3 Prov Map");
            for (int i = 0; i < colorList.Count; i++) {
                if (i % 512 == 0) {
                    Console.WriteLine("\t" + i * 100 / bmp.Width + "%");
                }

                int tx = 0;
                for (int j = 0; j < colorList[i].Count; j++) {
                    tx += lengthList[i][j];
                    if (tx >= bmp.Height) {
                        break;
                    }
                    //if alpha is 0, skip
                    if (colorList[i][j].A == 0) {
                        continue;
                    }

                    foreach (State s in stateList) {
                        foreach (Prov p in s.provList) {
                            if (p.color == colorList[i][j]) {
                                for (int k = 0; k < lengthList[i][j]; k++) {
                                    p.coords.Add((i, tx - k - 1));
                                }
                                goto stateCheckExit;
                            }
                        }
                    }

                    stateCheckExit:
                    int ignoreMe = 0;
                }
            }
        }

        //remove water and provs from list
        void removeWater(List<Prov> provList) {
            for (int i = 0; i < provList.Count; i++) {
                if (provList[i].isWater) {
                    provList.RemoveAt(i);
                    i--;
                }
            }
        }
        //remove wasteland provs from list
        void removeWasteland(List<Prov> provList) {
            for (int i = 0; i < provList.Count; i++) {
                if (provList[i].isWasteland) {
                    provList.RemoveAt(i);
                    i--;
                }
            }
        }
        //remove empty prov from list
        void removeEmpty(List<Prov> provList) {
            for (int i = 0; i < provList.Count; i++) {
                if (provList[i].coords.Count == 0) {
                    provList.RemoveAt(i);
                    i--;
                }
            }
        }

        //remove empty state from list
        void removeEmptyState(List<State> stateList) {
            for (int i = 0; i < stateList.Count; i++) {
                if (stateList[i].provList.Count == 0) {
                    stateList.RemoveAt(i);
                    i--;
                }
            }
        }

        //remove low count provs from list
        void removeLowCountProvs(List<Prov> provList) {

            for (int i = 0; i < provList.Count; i++) {
                if (provList[i].coords.Count < minCount) {
                    provList.RemoveAt(i);
                    i--;
                }
            }
        }

        void dubugDrawMap(List<Mapper> bm, string path) {
            Bitmap bmp = null;

            if (path.Contains("EU4") && path.Contains("VIC3")) {
                //open _Input/EU4/provinces.png
                bmp = new Bitmap(localDir + "/_Input/Vic3/provinces.png");
            }
            else if (path.Contains("VIC3") && path.Contains("HOI4")) {
                bmp = new Bitmap(localDir + "/_Input/HOI4/provinces.png");
            }

            //set bmp to be fully transparent
            for (int i = 0; i < bmp.Width; i++) {
                for (int j = 0; j < bmp.Height; j++) {
                    bmp.SetPixel(i, j, Color.FromArgb(0, 0, 0, 0));
                }
            }

            //for each mapper in bm set coords for the to as color of the from
            foreach (Mapper m in bm) {
                if (path.Contains("EU4") && path.Contains("VIC3")) {
                    //set eu4HexColor to each vic3Coords on bmp
                    foreach ((int x, int y) in m.vic3Coords) {
                        bmp.SetPixel(x, y, m.getEu4Color());
                    }
                }
                else if (path.Contains("VIC3") && path.Contains("HOI4")) {
                    //set vic3Color to each eu4Coords on bmp
                    foreach ((int x, int y) in m.eu4Coords) {
                        bmp.SetPixel(x, y, ColorTranslator.FromHtml(m.vic3ID.Replace("x", "#")));
                    }
                }
            }


            //save map
            bmp.Save(localDir + path);
        }

        void dubugDrawMapAS(List<MapperAreaState> bm) {
            Bitmap bmp = new Bitmap(localDir + "/_Input/Vic3/provinces.png");
            
            //set bmp to be fully transparent
            for (int i = 0; i < bmp.Width; i++) {
                for (int j = 0; j < bmp.Height; j++) {
                    bmp.SetPixel(i, j, Color.FromArgb(0, 0, 0, 0));
                }
            }

            var groupByState = bm.GroupBy(x => x.stateName).OrderByDescending(x => x.Max(y => y.overLapArea));

            //if a state has only one area then draw it as a single color
            foreach (var state in groupByState) {
                if (state.Count() == 1) {
                    foreach ((int x, int y) in state.First().stateCoords) {
                        bmp.SetPixel(x, y, state.First().areaColor);
                    }
                }
            }
            //if a state has more than one area then draw rotating through the areas every 10% of the state
            foreach (var state in groupByState) {
                if (state.Count() > 1) {
                    //sort the stateCoords by first then second
                    state.First().stateCoords = state.First().stateCoords.OrderBy(x => x.Item1).ThenBy(x => x.Item2).ToList();


                    int areaCount = state.Count();
                    int areaIndex = 0;
                    int areaSize = state.First().stateCoords.Count / areaCount;
                    int areaSizeCount = 0;
                    foreach ((int x, int y) in state.First().stateCoords) {
                        bmp.SetPixel(x, y, state.ElementAt(areaIndex).areaColor);
                        areaSizeCount++;
                        if (areaSizeCount >= areaSize) {
                            areaSizeCount = 0;
                            areaIndex++;
                            if (areaIndex >= areaCount) {
                                areaIndex = 0;
                            }
                        }
                    }
                }
            }




            /*
            //for each mapper in bm set coords for the to as color of the from
            foreach (MapperAreaState m in bm) {
                //set eu4HexColor to each vic3Coords on bmp
                foreach ((int x, int y) in m.stateCoords) {
                    bmp.SetPixel(x, y, m.areaColor);
                }
            }
            */
            //save map
            bmp.Save(localDir + "/_Output/EU4_VIC3/areaStateMap.png");


        }

        //match provs based on coords
        List<Mapper> matchCoordsEU4_Vic3(List<Prov> provListEU4, List<State> stateListVic3) {
            //check if _Output/EU4_VIC3 folder exists
            if (!Directory.Exists(localDir + "/_Output/EU4_VIC3")) {
                Directory.CreateDirectory(localDir + "/_Output/EU4_VIC3");
            }

            int vic3Count = 0;
            //get number of provs in stateListVic3
            foreach (State s in stateListVic3) {
                vic3Count += s.provList.Count;
            }

            Console.WriteLine("Matching " + provListEU4.Count + " EU4 Provs to " + vic3Count + " Vic3 Provs");
            List<Mapper> mapperList = new List<Mapper>();
            List<(string, int)> index = new List<(string, int)>();


            //parallel for loop
            Parallel.For(0, provListEU4.Count, i => {

                for (int j = 0; j < stateListVic3.Count; j++) {
                    foreach (Prov p in stateListVic3[j].provList) {
                        if (!index.Contains((HexConverter(p.color), provListEU4[i].provID))) {
                            //check if hashset has at least 1 match in provListEU4[i].coords and p.coords
                            if (provListEU4[i].coords.Intersect(p.coords).Any()) {

                                index.Add((HexConverter(p.color), provListEU4[i].provID));
                                Mapper m = new Mapper(HexConverter(p.color), provListEU4[i].provID);
                                m.vic3CoordCount = p.coords.Count;
                                m.eu4CoordCount = provListEU4[i].coords.Count;
                                m.eu4Nmae = provListEU4[i].name;
                                m.eu4HexColor = provListEU4[i].getHexColor().Replace("x", "");
                                m.sharedCoords = provListEU4[i].coords.Intersect(p.coords).Count();
                                m.vic3Coords = p.coords;

                                m.getOverLap();
                                mapperList.Add(m);




                            }
                        }

                    }
                }

                //1 tab if provListEU4[i].name is long, 2 if it is short
                if (provListEU4[i].name.Length < 8) {
                    Console.WriteLine(provListEU4[i].name + "\t\t" + provListEU4[i].coords.Count + "px\t" + "\t" + sw.Elapsed + "s");
                }
                else {
                    Console.WriteLine(provListEU4[i].name + "\t" + provListEU4[i].coords.Count + "px\t" + "\t" + sw.Elapsed + "s");
                }
            }
            );

            //group by vic3ID
            var grouped = mapperList.GroupBy(x => x.vic3ID);
            //move the highest overlap to a new list
            List<Mapper> bestMapper = new List<Mapper>();
            List<(List<int> eu4IDs, List<string> vic3IDs)> bestMapper2 = new List<(List<int> eu4IDs, List<string> vic3IDs)>();
            List<string> manyToOneVic3IDList = new List<string>();
            List<int> mannyToOneEU4IDList = new List<int>();


            if (manyToOne) {
                //add the largets overlap to bestMapper and remove all elemnets in mapperList with that vic3ID
                foreach (var group in grouped) {
                    Mapper m = group.OrderByDescending(x => x.overLapEU4).ThenByDescending(x => x.overLapVic3).First();
                    bestMapper.Add(m);
                }

                List<int> skipedEU4IDs = new List<int>();
                foreach (Prov p in provListEU4) {
                    if (!bestMapper.Exists(x => x.eu4ID == p.provID)) {
                        skipedEU4IDs.Add(p.provID);
                    }
                }

                List<Mapper> redoList = new List<Mapper>();
                //add every mapperList element.eu4ID is in skipedEU4IDs id to redoList
                foreach (Mapper m in mapperList) {
                    if (skipedEU4IDs.Contains(m.eu4ID)) {
                        redoList.Add(m);
                    }
                }

                List<(int e4, List<string> v3)> tmpMapperList = new List<(int, List<string>)>();
                //for each mapper in bestMapper if eu4ID is in tmpMapperList append vic3ID to v3 list else add new tuple to tmpMapperList
                foreach (Mapper m in bestMapper) {
                    if (tmpMapperList.Exists(x => x.e4 == m.eu4ID)) {
                        tmpMapperList.Find(x => x.e4 == m.eu4ID).v3.Add(m.vic3ID);
                    }
                    else {
                        tmpMapperList.Add((m.eu4ID, new List<string> { m.vic3ID }));
                    }
                }


                //for each mapper in redoList check if there is a tmpMapperList element.v3 contains mapper.vic3ID and that element.v3 only contains one string if so add mapper to bestMapper
                foreach (Mapper m in redoList) {
                    if (tmpMapperList.Exists(x => x.v3.Contains(m.vic3ID) && x.v3.Count == 1 && !manyToOneVic3IDList.Contains(m.vic3ID) && !mannyToOneEU4IDList.Contains(m.eu4ID))) {
                        bestMapper.Add(m);
                        manyToOneVic3IDList.Add(m.vic3ID);
                        mannyToOneEU4IDList.Add(m.eu4ID);
                    }
                }
            }
            else {
                //move the highest overlap to a new list
                foreach (var group in grouped) {
                    Mapper m = group.OrderByDescending(x => x.overLapEU4).ThenByDescending(x => x.overLapVic3).First();
                    bestMapper.Add(m);
                }
            }

            //double sort by eu4ID and where thoes are the same vic3ID
            bestMapper = bestMapper.OrderBy(x => x.eu4ID).ThenBy(x => x.vic3ID).ToList();

            //group by eu4ID
            var grouped2 = bestMapper.GroupBy(x => x.eu4ID);


            //write group2 to file
            using StreamWriter f = new StreamWriter(localDir + "/_Output/EU4_Vic3/EU4 to Vic3 mapping.txt");
            using StreamWriter fp = new StreamWriter(localDir + "/_Output/EU4_Vic3/EU4 to Vic3 poor matches.txt");
            List<string> lines = new List<string>();
            List<string> linesPoor = new List<string>();

            int Vic3MatchCount = 0;
            int EU4MatchCount = 0;
            linesPoor.Add("The following mapping have a low overlap, and should probably be checked manually.\r\n");
            foreach (var group in grouped2) {
                //check if key is in manyToOneEU4IDList
                if (mannyToOneEU4IDList.Contains(group.Key)) {
                    continue;
                }
                else {
                    List<String> poorTemp = new List<string>();
                    bool isPoor = false;
                    lines.Add("\tlink = { eu4 = " + group.Key.ToString());
                    poorTemp.Add("\tlink = { eu4 = " + group.Key.ToString());
                    string name = "";
                    EU4MatchCount++;
                    foreach (Mapper m in group) {
                        //check if m.vic3ID is in manyToOneVic3IDList
                        if (manyToOneVic3IDList.Contains(m.vic3ID)) {
                            //write the eu4 id from mannyToOneEU4IDList from the same index as manyToOneVic3IDList to file
                            lines.Add(" eu4 = " + mannyToOneEU4IDList[manyToOneVic3IDList.IndexOf(m.vic3ID)].ToString());
                            EU4MatchCount++;
                        }

                        lines.Add(" vic3 = 0x" + m.vic3ID);
                        name = m.eu4Nmae + " " + m.eu4HexColor;
                        if (m.overLapVic3 < poorP) {
                            poorTemp.Add(" vic3 = 0x" + m.vic3ID);
                            isPoor = true;
                        }
                        Vic3MatchCount++;
                    }
                    lines.Add(" } # " + name + " : ");
                    poorTemp.Add(" } # " + name + " : ");

                    foreach (Mapper m in group) {
                        //m.overlap as a 2 decimal point presentage
                        lines[lines.Count - 1] += " " + Math.Round(m.overLapVic3 * 100, 2) + "%, ";
                        if (m.overLapVic3 < poorP) {
                            poorTemp[poorTemp.Count - 1] += " " + Math.Round(m.overLapVic3 * 100, 2) + "%, ";
                        }

                    }
                    lines.Add("\r\n");
                    poorTemp.Add("\r\n");
                    if (isPoor) {
                        linesPoor.AddRange(poorTemp);
                    }
                }
            }
            f.Write(string.Join("", lines));
            f.Close();
            fp.Write(string.Join("", linesPoor));
            fp.Close();

            //write eu4 missing list
            using StreamWriter f2 = new StreamWriter(localDir + "/_Output/EU4_Vic3/EU4 missing.txt");
            List<string> lines2 = new List<string>();
            lines2.Add("The following EU4 provs were not mapped to a Vic3 prov.\nLikely because their coordinates fell entirely within a water/wasteland prov in the Vic3 image,\nOr the Vic3 provs it shares coordinates with have larger coverage with other eu4 provs.\r\n");
            foreach (Prov p in provListEU4) {
                if (!bestMapper.Exists(x => x.eu4ID == p.provID)) {
                    lines2.Add(p.provID + "\t" + p.getHexColor().Replace("x", "") + "\t" + p.name + "\r\n");
                }
            }
            f2.Write(string.Join("", lines2));
            f2.Close();

            //check if any Vic3 provs are missing
            using StreamWriter f3 = new StreamWriter(localDir + "/_Output/EU4_Vic3/Vic3 missing.txt");
            List<string> missingList = new List<string>();
            missingList.Add("The following Vic3 provs were not mapped to a EU4 prov.\nLikely because their coordinates fell entirely within a water/wasteland prov in the EU4 image.\r\n");
            foreach (State s in stateListVic3) {
                foreach (Prov p in s.provList) {
                    bool found = false;
                    for (int i = 0; i < bestMapper.Count; i++) {
                        if (bestMapper[i].vic3ID == HexConverter(p.color)) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        missingList.Add(HexConverter(p.color) + "\r\n");
                    }
                }
            }

            f3.Write(string.Join("", missingList));
            f3.Close();

            //print presentage of vic3 provs mapped to hoi4 provs
            Console.WriteLine("\n" + Math.Round((double)(vic3Count - Vic3MatchCount) / vic3Count * 100, 2) + "% of Vic3 provs were not mapped to EU4 provs");
            Console.WriteLine("\n" + Math.Round((double)(provListEU4.Count - EU4MatchCount) / provListEU4.Count * 100, 2) + "% of EU4 provs were not mapped to Vic3 provs");


            return bestMapper;
        }

        List<MapperAreaState> matchCoordsEU4_Vic3_AS(List<AreaEU4> areaListEU4, List<State> stateListVic3) {
            //check if _Output/EU4_VIC3 folder exists
            if (!Directory.Exists(localDir + "/_Output/EU4_VIC3")) {
                Directory.CreateDirectory(localDir + "/_Output/EU4_VIC3");
            }
            int vic3Count = 0;
            //get number of provs in stateListVic3
            foreach (State s in stateListVic3) {
                vic3Count += s.provList.Count;
                s.setHashSet();
            }
            int eu4Count = 0;
            //get number of provs in areaListEU4
            foreach (AreaEU4 a in areaListEU4) {
                eu4Count += a.provList.Count;
                a.setHashSet();
            }

            Console.WriteLine("Matching " + eu4Count + " EU4 Provs to " + vic3Count + " Vic3 Provs");

            int count = 0;
            List<MapperAreaState> mapperASList = new List<MapperAreaState>();
            //parralel loop through areaListEU4 and stateListVic3 to find all overlapping coordSet and add them to mapperASList
            Parallel.ForEach(areaListEU4, area => {
                foreach (State state in stateListVic3) {
                    if (area.coordSet.Overlaps(state.coordSet)) {

                        //check if mapperASList already contains a mapperAS with the same area and state names
                        if (mapperASList.Exists(x => x.areaName == area.name && x.stateName == state.name)) {
                            continue;
                        }

                        MapperAreaState mapperAS = new MapperAreaState(state.name, area.name);
                        mapperAS.stateCoords = state.coordSet.ToList();
                        mapperAS.areaCoords = area.coordSet.ToList();
                        mapperAS.stateColor = state.color;
                        mapperAS.areaColor = area.color;
                        mapperAS.sharedCoordsCount = area.coordSet.Intersect(state.coordSet).Count();
                        mapperAS.stateCoordCount = state.coordSet.Count;
                        mapperAS.areaCoordCount = area.coordSet.Count;
                        mapperAS.getOverLap();


                        mapperASList.Add(mapperAS);

                        //Console.WriteLine(mapperAS.toString());
                    }
                }
                count++;
                //print progress every 10 areas
                if (count % 10 == 0) {
                    Console.WriteLine("Progress: " + count + "/" + areaListEU4.Count);
                }
            });

            //group by areaName
            var groupByArea = mapperASList.GroupBy(x => x.areaName);

            //move the highest overlap to a new list
            List<MapperAreaState> bestMapperAS = new List<MapperAreaState>();

            //move any mapperAS with overLapState > 0.5 or overLapArea > 0.5 to bestMapperAS
            foreach (var group in groupByArea) {
                foreach (MapperAreaState m in group) {
                    if (m.overLapState > 0.5 || m.overLapArea > 0.5) {
                        //add to bestMapperAS if instance with same areaName and stateName does not already exist
                        if (!bestMapperAS.Exists(x => x.areaName == m.areaName && x.stateName == m.stateName)) {
                            bestMapperAS.Add(m);
                        }
                        //remove from mapperASList
                        //mapperASList.Remove(m);
                    }
                }
            }


            //print time elapsed
            Console.WriteLine(sw.Elapsed);



            List<string> stateMany = new List<string>();
            List<string> areaMany = new List<string>();
            //print to file any states with more than one area
            using StreamWriter f = new StreamWriter(localDir + "/_Output/EU4_Vic3/States with more than one area.txt");
            List<string> lines = new List<string>();
            lines.Add("The following states have more than one area mapped to them.\r\n");
            //group by area bestMapper orver by overlapstate
            groupByArea = bestMapperAS.GroupBy(x => x.areaName).OrderByDescending(x => x.Max(y => y.overLapState));
            groupByArea = groupByArea.OrderByDescending(x => x.Max(y => y.overLapState));
            foreach (var group in groupByArea) {
                if (group.Count() > 1) {
                    lines.Add(group.Key + "\r\n");
                    stateMany.Add(group.Key);
                    foreach (MapperAreaState m in group) {
                        //with overlap as a percentage instead of a decimal value (for easier reading) and rounded to 2 decimal places (for easier reading)
                        lines.Add("\t" + m.stateName + "\t" + Math.Round(m. overLapState * 100, 2) + "%\t"+m.areaCoordCount + "\t" + Math.Round(m.overLapArea * 100, 2) + "\r\n");

                    }
                }
            }
            lines.Add("\r\n\n\nThe following areas have more than one state mapped to them.\r\n");
            var groupByState = bestMapperAS.GroupBy(x => x.stateName).OrderByDescending(x => x.Max(y => y.overLapArea));
            //sort groupByState by overLapState
            groupByState = groupByState.OrderByDescending(x => x.First().overLapArea);
            foreach (var group in groupByState) {
                if (group.Count() > 1) {
                    lines.Add(group.Key + "\r\n");
                    areaMany.Add(group.Key);
                    foreach (MapperAreaState m in group) {
                        lines.Add("\t" + m.areaName + "\t" + Math.Round(m.overLapArea * 100, 2) + "%\t" + m.stateCoordCount + "\t" + Math.Round(m.overLapState * 100, 2) + "%\r\n");
                    }
                }
            }

            //write out all states and areas that are maped to 1
            lines.Add("\r\n\n\nThe following states and areas are mapped one to one with eachother.\r\n");
            //write out all states pairs areas that do not have members in stateMany or areaMany
            foreach (MapperAreaState m in bestMapperAS) {
                if (!stateMany.Contains(m.areaName) && !areaMany.Contains(m.stateName)) {
                    lines.Add(m.areaName + "\t<->\t" + m.stateName + "\t\t" + Math.Round(m.overLapState * 100, 2) + "%\t" + Math.Round(m.overLapArea * 100, 2) + "%\r\n");
                }
            }

            f.Write(string.Join("", lines));
            f.Close();

            List<string> skipedStates = new List<string>();
            //print any states/areas that are not in bestMapperAS
            using StreamWriter f2 = new StreamWriter(localDir + "/_Output/EU4_Vic3/States and areas not mapped.txt");
            List<string> lines2 = new List<string>();
            lines2.Add("The following states are not mapped to any areas.\r\n");
            foreach (State state in stateListVic3) {
                if (!bestMapperAS.Exists(x => x.stateName == state.name)) {
                    lines2.Add(state.name + "\r\n");
                    skipedStates.Add(state.name);
                }
            }
            List<string> skipedAreas = new List<string>();
            lines2.Add("\r\n\n\nThe following areas are not mapped to any states.\r\n");
            foreach (AreaEU4 area in areaListEU4) {
                if (!bestMapperAS.Exists(x => x.areaName == area.name)) {
                    lines2.Add(area.name + "\r\n");
                    skipedAreas.Add(area.name);
                }
            }
            f2.Write(string.Join("", lines2));
            f2.Close();
            











            return bestMapperAS;
        }

        List<Mapper> matchCoordsVic3_HOI4(List<State> stateListVic3, List<Prov> provListHOI4) {
            //check if _Output/VIC3_HOI4 folder exists
            if (!Directory.Exists(localDir + "/_Output/VIC3_HOI4")) {
                Directory.CreateDirectory(localDir + "/_Output/VIC3_HOI4");
            }

            int vic3Count = 0;
            //get number of provs in stateListVic3
            foreach (State s in stateListVic3) {
                vic3Count += s.provList.Count;
            }

            Console.WriteLine("Matching " + vic3Count + " Vic3 Provs to " +provListHOI4.Count + " HOI4 Provs ");
            List<Mapper> mapperList = new List<Mapper>();
            List<(string, int)> index = new List<(string, int)>();

            int count = 0;
            //parallel for loop
            Parallel.For(0, provListHOI4.Count, i => {
                
                for (int j = 0; j < stateListVic3.Count; j++) {
                    foreach (Prov p in stateListVic3[j].provList) {
                        
                        if (!index.Contains((HexConverter(p.color), provListHOI4[i].provID))) {
                            //check if hashset has at least 1 match in provListEU4[i].coords and p.coords
                            if (provListHOI4[i].coords.Intersect(p.coords).Any()) {

                                index.Add((HexConverter(p.color), provListHOI4[i].provID));
                                Mapper m = new Mapper(HexConverter(p.color), provListHOI4[i].provID);
                                m.vic3CoordCount = p.coords.Count;
                                m.eu4CoordCount = provListHOI4[i].coords.Count;
                                m.eu4Nmae = provListHOI4[i].name;
                                m.eu4HexColor = provListHOI4[i].getHexColor().Replace("x", "");
                                m.sharedCoords = provListHOI4[i].coords.Intersect(p.coords).Count();
                                m.vic3Coords = p.coords;
                                m.eu4Coords = provListHOI4[i].coords;

                                m.getOverLap();
                                mapperList.Add(m);

                            }
                        }

                    }
                }
                count++;
                if (count % 50 == 0) {
                    //count / provListHOI4.count as a presentage with 2 points of pressision
                    Console.WriteLine("\r" + Math.Round((double)count / provListHOI4.Count * 100, 2) + "%\t" + sw.Elapsed);
                }

            }
            );

            //group by vic3ID
            var grouped = mapperList.GroupBy(x => x.vic3ID);

            //move the highest overlap to a new list
            List<Mapper> bestMapper = new List<Mapper>();
            /*
            foreach (var group in grouped) {
                bestMapper.Add(group.OrderByDescending(x => x.overLapVic3).First());
            }
            */
            //move the highest overlap to a new list
            foreach (var group in grouped) {
                Mapper m = group.OrderByDescending(x => x.overLapEU4).ThenByDescending(x => x.overLapVic3).First();
                bestMapper.Add(m);
            }

            //double sort by eu4ID and where thoes are the same vic3ID
            bestMapper = bestMapper.OrderBy(x => x.eu4ID).ThenBy(x => x.vic3ID).ToList();

            //group by eu4ID
            var grouped2 = bestMapper.GroupBy(x => x.eu4ID);

            //write to file
            using StreamWriter f = new StreamWriter(localDir + "/_Output/Vic3_HOI4/Vic3 to HOI4.txt");
            using StreamWriter fp = new StreamWriter(localDir + "/_Output/Vic3_HOI4/Vic3 to HOI4 poor matches.txt");


            int Vic3MatchCount = 0;
            int HOI4MatchCount = 0;
            List<string> lines = new List<string>();
            List<string> linesPoor = new List<string>();
            linesPoor.Add("The following mapping have a low overlap, and should probably be checked manually.\r\n");
            foreach(var group in grouped2) {
                List<String> poorTemp = new List<string>();
                bool isPoor = false;

                
                lines.Add("\t link = {");
                poorTemp.Add("\t link = {");
                foreach (Mapper m in group) {
                    if (m.overLapVic3 < poorP) {
                        isPoor = true;
                        poorTemp.Add(" vic3 = 0" + m.vic3ID);
                    }
                    lines.Add(" vic3 = 0" + m.vic3ID);
                    Vic3MatchCount++;
                }
                lines.Add(" hoi4 = " + group.Key + " } # ");
                poorTemp.Add(" hoi4 = " + group.Key + " } # ");
                HOI4MatchCount++;

                foreach (Mapper m in group) {
                    //m.overlap as a 2 decimal point presentage
                    lines.Add(" " + Math.Round(m.overLapVic3 * 100, 2) + "%, ");
                    if (m.overLapVic3 < poorP) {
                        poorTemp.Add(" " + Math.Round(m.overLapVic3 * 100, 2) + "%, ");
                    }
                }

                lines.Add(" : " + group.First().eu4HexColor+"\r\n");
                if (isPoor) {
                    poorTemp.Add(" : " + group.First().eu4HexColor + "\r\n");
                    linesPoor.AddRange(poorTemp);
                }
                
            }
            f.Write(string.Join("", lines));
            f.Close();
            fp.Write(string.Join("", linesPoor));
            fp.Close();



            //write eu4 missing list
            using StreamWriter f2 = new StreamWriter(localDir + "/_Output/Vic3_HOI4/HOI4 missing.txt");
            List<string> lines2 = new List<string>();
            lines2.Add("The following HOI4 provs were not mapped to a Vic3 prov.\nLikely because their coordinates fell entirely within a water/wasteland prov in the Vic3 image,\nOr the Vic3 provs it shares coordinates with have larger coverage with other HOI4 provs.\r\n");
            foreach (Prov p in provListHOI4) {
                if (!bestMapper.Exists(x => x.eu4ID == p.provID)) {
                    lines2.Add(p.provID + "\t" + p.getHexColor().Replace("x", "") + "\t" + p.name + "\r\n");
                }
            }
            f2.Write(string.Join("", lines2));
            f2.Close();

            //check if any Vic3 provs are missing
            using StreamWriter f3 = new StreamWriter(localDir + "/_Output/Vic3_HOI4/Vic3 missing.txt");
            List<string> missingList = new List<string>();
            missingList.Add("The following Vic3 provs were not mapped to a HOI4 prov.\nLikely because their coordinates fell entirely within a water/wasteland prov in the HOI4 image.\nOr the HOI4 provs it shares coordinates with have larger coverage with other Vic3 provs.\r\n");
            foreach (State s in stateListVic3) {
                foreach (Prov p in s.provList) {
                    bool found = false;
                    for (int i = 0; i < bestMapper.Count; i++) {
                        if (bestMapper[i].vic3ID == HexConverter(p.color)) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) {
                        missingList.Add(HexConverter(p.color) + "\r\n");
                    }
                }
            }

            f3.Write(string.Join("", missingList));
            f3.Close();

            //print presentage of vic3 provs mapped to hoi4 provs
            Console.WriteLine("\n" + Math.Round((double)(vic3Count - Vic3MatchCount) / vic3Count * 100, 2) + "% of Vic3 provs were not mapped to HOI4 rovs");
            Console.WriteLine("\n" + Math.Round((double)(provListHOI4.Count - HOI4MatchCount) / provListHOI4.Count * 100, 2) + "% of HOI4 provs were not mapped to Vic3 provs");


            return bestMapper;
        }
        



        List<State> stateListVic3 = new List<State>();
        List<Prov> provListEU4 = new List<Prov>();

        if (converterFlag.Contains("Vic3")) {
            parseStateFilesVic3(stateListVic3);
            parseDefaultMapVic3(stateListVic3);
            for (int i = 0; i < stateListVic3.Count; i++) {
                removeWater(stateListVic3[i].provList);                
                if (removeWastelandBool)
                    removeWasteland(stateListVic3[i].provList);                
            }
            parseProvincesVic3(stateListVic3);
            for (int i = 0; i < stateListVic3.Count; i++) {
                removeEmpty(stateListVic3[i].provList);
                removeLowCountProvs(stateListVic3[i].provList);
            }
            removeEmptyState(stateListVic3);
            //set hashset for each state prov
            for (int i = 0; i < stateListVic3.Count; i++) {
                for (int j = 0; j < stateListVic3[i].provList.Count; j++) {
                    stateListVic3[i].provList[j].setHashSet();
                }
            }
        }



        List<AreaEU4> areaEU4List = new List<AreaEU4>();
        if (converterFlag.Contains("EU4")) {
            parseDefinitionEU4(provListEU4);
            parseDefaultMapEU4(provListEU4);
            removeWater(provListEU4);
            if (removeWastelandBool)
                removeWasteland(provListEU4);
            parseProvincesPreV3(provListEU4, "/_Input/EU4/provinces.png");
            removeEmpty(provListEU4);
            removeLowCountProvs(provListEU4);
            //set hashset for each prov
            for (int i = 0; i < provListEU4.Count; i++) {
                provListEU4[i].setHashSet();
            }
            areaEU4List = parseAreaFileEU4(provListEU4);

            //remove empty areas whos provs dont exist in the map
            for (int i = 0; i < areaEU4List.Count; i++) {
                if (areaEU4List[i].provList.Count == 0) {
                    areaEU4List.RemoveAt(i);
                    i--;
                }
            }



        }

        bool useAreaState = false;
        if (converterFlag.Contains("Vic3") && converterFlag.Contains("EU4")) {
            if (useAreaState) {
                List<MapperAreaState> bm = matchCoordsEU4_Vic3_AS(areaEU4List, stateListVic3);
                dubugDrawMapAS(bm);
            }
            else {
                List<Mapper> bm = matchCoordsEU4_Vic3(provListEU4, stateListVic3);
                dubugDrawMap(bm, "/_Output/EU4_VIC3/ProvMap.png");
            }
        }
        

        List<Prov> provListHOI4 = new List<Prov>();
        if (converterFlag.Contains("HOI4")) {
            parseDefinitionHOI4(provListHOI4);
            removeWater(provListHOI4);
            parseProvincesPreV3(provListHOI4, "/_Input/HOI4/provinces.png");
            removeEmpty(provListHOI4);
            removeLowCountProvs(provListHOI4);
            //set hashset for each prov
            for (int i = 0; i < provListHOI4.Count; i++) {
                provListHOI4[i].setHashSet();
            }
            
        }

        
        if (converterFlag.Contains("Vic3") && converterFlag.Contains("HOI4")) {
            List<Mapper> bm = matchCoordsVic3_HOI4(stateListVic3, provListHOI4);
            dubugDrawMap(bm, "/_Output/VIC3_HOI4/ProvMap.png");
        }




    }
}