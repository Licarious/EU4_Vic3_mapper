using System.Drawing;


public class AreaEU4
{
    public string name = "";
    public List<Prov> provList = new List<Prov>();
    public Color color = Color.FromArgb(0, 0, 0, 0);
    public List<(int, int)> coords = new List<(int, int)>();

    //hash set of coords
    public HashSet<(int, int)> coordSet = new HashSet<(int, int)>();

    public AreaEU4(string name) {
        this.name = name;
    }
    public AreaEU4() { }

    //set HashSet
    public void setHashSet() {
        foreach (Prov p in provList) {
            foreach ((int, int) coord in p.coords) {
                coordSet.Add(coord);
            }
        }
    }

    public string toString() {
        return name + " \t " + provList.Count + " \t " + color.ToString() + " \t " + coords.Count;
    }

}

