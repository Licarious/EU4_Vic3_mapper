using System.Drawing;

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

    public List<(int, int)> vic3Coords = new List<(int, int)>();
    public List<(int, int)> eu4Coords = new List<(int, int)>();

    public Mapper(string vic3ID, int eu4ID) {
        this.vic3ID = vic3ID;
        this.eu4ID = eu4ID;
    }
    public Mapper() {    }

    public void getOverLap() {
        overLapVic3 = sharedCoords / vic3CoordCount;
        overLapEU4 = sharedCoords / eu4CoordCount;
    }

    //convert eu4HexColor from string to Color
    public Color getEu4Color() {
        return ColorTranslator.FromHtml("#"+eu4HexColor);
    }

    //tostring
    public string toString() {

        string s = "Vic3ID: " + vic3ID + " EU4ID: " + eu4ID + " EU4Name: " + eu4Nmae + " EU4Color: " + eu4HexColor + " sharedCoords: " + sharedCoords + " vic3CoordCount: " + vic3CoordCount + " eu4CoordCount: " + eu4CoordCount + " overLapVic3: " + overLapVic3 + " overLapEU4: " + overLapEU4;
        return s;
    }
}
