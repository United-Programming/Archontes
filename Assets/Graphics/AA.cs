using UnityEngine;
using UnityEditor;

public class AA : MonoBehaviour // FIXME remove
{
}


[CustomEditor(typeof(AA))]
public class AAE : Editor {
  public override void OnInspectorGUI() {
    DrawDefaultInspector();
    if (GUILayout.Button("Build Object")) {
      Transform t = ((AA)target).transform;
      for (int y = 0; y < 9; y++) {
        for (int x = 0; x < 9; x++) {
          BoardTile bt = t.GetChild(x + 9 * y).GetComponent<BoardTile>();
          bt.gameObject.name = "Tile " + x + " " + y;
          bt.x = x;
          bt.y = y;
        }
      }
    }
  }
}
