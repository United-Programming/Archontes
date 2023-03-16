using UnityEngine;

public class AnimEventsHandler : MonoBehaviour {
  public Piece parent;
  public void HitStart() {
    parent.HitStart();
  }
  public void HitEnd() {
    parent.HitEnd();
  }
  public void ReadyToStart() {
    parent.ReadyToStart();
  }
  public void DeathCompleted() {
    parent.DeathCompleted();
  }
}
