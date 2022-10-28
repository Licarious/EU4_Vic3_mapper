using System.Drawing;

public class Prov
{
    public string name = "";
    public int provID = -1;
    public Color color = new Color();
    public List<(int, int)> coords = new List<(int, int)>();
    public bool isWater = false;
    public bool isWasteland = false;

    //hash set of coords
    public HashSet<(int, int)> coordSet = new HashSet<(int, int)>();
    

    public Prov(Color c) {
            color = c;
    }
    public Prov() {
    }

    //Get HexColor
    public string getHexColor() {
        return ColorTranslator.ToHtml(color).Replace("#", "x");
    }

    //set HashSet
    public void setHashSet() {
        foreach ((int, int) coord in coords) {
            coordSet.Add(coord);
        }
    }

}
