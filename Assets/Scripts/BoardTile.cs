using UnityEngine;

public class BoardTile : MonoBehaviour {
  MeshRenderer mr;
  public int x, y;
  public Piece piece;
  public bool possible;
  public Material OverMaterial;
  public Material PossiblePickMaterial;
  private void Start() {
    mr = GetComponent<MeshRenderer>();
    mr.enabled = false;
  }

  public void SetAsPossible(bool ok) {
    possible = ok;
    if (ok) {
      mr.material = PossiblePickMaterial;
      mr.enabled = true;
    }
    else {
      mr.enabled = false;
    }
  }

  private void OnMouseOver() {
    if (Game.status == GameStatus.PlayerPickPiece && piece != null /* FIXME && piece.IsPlayer1 */) {
      mr.enabled = true;
      mr.material = OverMaterial;
    }
    else if (Game.status == GameStatus.PlayerSelectDestination && possible) {
      mr.enabled = true;
      mr.material = OverMaterial;
    }
  }
  private void OnMouseExit() {
    if (possible) {
      mr.material = PossiblePickMaterial;
      mr.enabled = true;
    }
    else mr.enabled = false;
  }

  internal Vector3 GetWorldPosition() {
    return new(x * 1.125f - 4.5f, 0.01f, y * 1.125f - 4.5f);
  }

}
