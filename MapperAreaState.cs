using System.Drawing;

public class MapperAreaState{
    public string stateName = "";
    public string areaName = "";

    public Color stateColor = Color.FromArgb(0, 0, 0, 0);
    public Color areaColor = Color.FromArgb(0, 0, 0, 0);

    public double sharedCoordsCount = 0;
    public double stateCoordCount = 0;
    public double areaCoordCount = 0;

    public double overLapState = 0;
    public double overLapArea = 0;

    public List<(int, int)> stateCoords = new List<(int, int)>();
    public List<(int, int)> areaCoords = new List<(int, int)>();

    public MapperAreaState(string stateName, string areaName) {
        this.stateName = stateName;
        this.areaName = areaName;
    }

    public MapperAreaState() { }

    public void getOverLap() {
        overLapState = sharedCoordsCount / stateCoordCount;
        overLapArea = sharedCoordsCount / areaCoordCount;
    }

    //print stateName, areaName, overLapState, overLapArea
    public string toString() {
        //with overLapState and overLapArea as percentage values to 2 decimal places
        return stateName + " \t " + areaName + " \t " + overLapState.ToString("P2") + " \t " + overLapArea.ToString("P2");
        
    }


}

