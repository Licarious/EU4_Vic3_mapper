

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
        bool removeWastelandBool = true; //should wastland be removed before comparing eu4 and vic3 provs
        bool manyToOne = false; //should multiple eu4 ids go to a singe vic3 id

        //stopwatch
        Stopwatch sw = Stopwatch.StartNew();
        
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
        void parseProvincesEU4(List<Prov> provList) {
            //open _Input/EU4/provinces.png
            Bitmap bmp = new Bitmap(localDir + "/_Input/EU4/provinces.png");

            Console.WriteLine("Compressing EU4 Prov Map");
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
            Console.WriteLine("Matching EU4 Prov Map");
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

        void matchCoords(List<Prov> provListEU4, List<State> stateListVic3) {
            int vic3Count = 0;
            //get number of provs in stateListVic3
            foreach(State s in stateListVic3) {
                vic3Count += s.provList.Count;
            }

            Console.WriteLine("Matching " + provListEU4.Count +" EU4 Provs to "+ vic3Count +" Vic3 Provs");
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
                //if overLapVic3 >0.5 and overLapEU4 >0.5 then add to bestMapper
                foreach (var group in grouped) {
                    Mapper m = group.OrderByDescending(x => x.overLapVic3).ThenByDescending(x => x.overLapEU4).First();
                    if (m.overLapVic3 > 0.5 && m.overLapEU4 > 0.5) {
                        bestMapper.Add(m);
                        //and remove all elemnets in mapperList with that vic3ID
                        mapperList.RemoveAll(x => x.vic3ID == m.vic3ID);
                    }
                }
                //add the largets overlap to bestMapper
                foreach (var group in grouped) {
                    Mapper m = group.OrderByDescending(x => x.overLapVic3).ThenByDescending(x => x.overLapEU4).First();
                    bestMapper.Add(m);
                    //and remove all elemnets in mapperList with that vic3ID
                    mapperList.RemoveAll(x => x.vic3ID == m.vic3ID);
                }
            }

            //double sort by eu4ID and where thoes are the same vic3ID
            bestMapper = bestMapper.OrderBy(x => x.eu4ID).ThenBy(x => x.vic3ID).ToList();

            //group by eu4ID
            var grouped2 = bestMapper.GroupBy(x => x.eu4ID);


            //write group2 to file
            using StreamWriter f = new StreamWriter(localDir + "/_Output/EU4 to Vic3 mapping.txt");
            using StreamWriter fp = new StreamWriter(localDir + "/_Output/EU4 to Vic3 poor matches.txt");
            List<string> lines = new List<string>();
            List<string> linesPoor = new List<string>();
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
                    foreach (Mapper m in group) {
                        //check if m.vic3ID is in manyToOneVic3IDList
                        if (manyToOneVic3IDList.Contains(m.vic3ID)) {
                            //write the eu4 id from mannyToOneEU4IDList from the same index as manyToOneVic3IDList to file
                            lines.Add(" eu4 = " + mannyToOneEU4IDList[manyToOneVic3IDList.IndexOf(m.vic3ID)].ToString());
                        }

                        lines.Add(" vic3 = " + m.vic3ID);
                        name = m.eu4Nmae + " " + m.eu4HexColor;
                        if (m.overLapVic3 < poorP) {
                            poorTemp.Add(" vic3 = " + m.vic3ID);
                            isPoor = true;
                        }
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
            using StreamWriter f2 = new StreamWriter(localDir + "/_Output/EU4 missing.txt");
            List<string> lines2 = new List<string>();
            lines2.Add("The following EU4 provs were not mapped to a Vic3 prov.\nLikely because their coordinates fell entirely within a water/wasteland prov in the Vic3 image,\nOr the Vic3 provs it shares coordinates with have larger coverage with other eu4 provs.\r\n");
            foreach (Prov p in provListEU4) {
                if (!bestMapper.Exists(x => x.eu4ID == p.provID)) {
                    lines2.Add(p.provID + "\t"+ p.getHexColor().Replace("x","")+"\t" + p.name + "\r\n");
                }
            }
            f2.Write(string.Join("", lines2));
            f2.Close();

            //check if any Vic3 provs are missing
            using StreamWriter f3 = new StreamWriter(localDir + "/_Output/Vic3 missing.txt");
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

        }


        String HexConverter(System.Drawing.Color c){
            return "x" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        List<State> stateListVic3 = new List<State>();
        List<Prov> provListEU4 = new List<Prov>();
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
        




        parseDefinitionEU4(provListEU4);
        parseDefaultMapEU4(provListEU4);
        removeWater(provListEU4);
        if(removeWastelandBool)
            removeWasteland(provListEU4);
        parseProvincesEU4(provListEU4);
        removeEmpty(provListEU4);
        removeLowCountProvs(provListEU4);
        //set hashset for each prov
        for (int i = 0; i < provListEU4.Count; i++) {
            provListEU4[i].setHashSet();
        }

        matchCoords(provListEU4, stateListVic3);

    }
}