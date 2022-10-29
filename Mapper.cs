using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Mapper
{
    public string vic3ID = "";
    public int eu4ID = 0;
    public string eu4Nmae = "";
    public string eu4HexColor = "";

    public double sharedCoords = 0;
    public double vic3CoordCount = 0;
    public double eu4CoordCount = 0;
    public double overLapVic3 = 0;
    public double overLapEU4 = 0;

    public Mapper(string vic3ID, int eu4ID) {
        this.vic3ID = vic3ID;
        this.eu4ID = eu4ID;
    }
    public Mapper() {
        
    }

    public void getOverLap() {
        overLapVic3 = sharedCoords / vic3CoordCount;
        overLapEU4 = sharedCoords / eu4CoordCount;
    }

}
