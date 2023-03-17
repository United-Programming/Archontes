using UnityEditor;
using UnityEngine;

public class BoneGizmo : MonoBehaviour {
  public Transform parent;
  public bool root;
  public Color color = Color.red;

  private void OnDrawGizmos() {
    if (root) return;
    if (parent == null) parent = transform.parent;
    Vector3 endPos = parent==null ? transform.position-transform.forward : parent.position;
    Handles.DrawBezier(transform.position, endPos, transform.position, endPos, color, null, 5);
  }

  private void OnDrawGizmosSelected() {
    if (root) return;
    if (parent == null) parent = transform.parent;
    Vector3 endPos = parent == null ? transform.position - transform.forward : parent.position;
    Handles.DrawBezier(transform.position, endPos, transform.position, endPos, Color.white, null, 8);
  }
}
