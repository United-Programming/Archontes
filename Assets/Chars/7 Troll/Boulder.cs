using System.Collections;
using UnityEngine;

public class Boulder : MonoBehaviour {
  public Rigidbody rb;
  public float Power = 10;
  bool alreadyHit = false;
  bool alreadyDestroyed = false;

  private IEnumerator Start() {
    yield return new WaitForSeconds(5);
    if (!alreadyDestroyed) {
      alreadyDestroyed = true;
      Destroy(gameObject);
    }
  }

  private void OnCollisionEnter(Collision collision) {
    rb.velocity = Vector3.zero;
    if (alreadyHit) return;
    alreadyHit = true;
    if (collision.collider.TryGetComponent(out Piece piece)) {
      piece.HitFromProjectile(Power);
      StartCoroutine(Remove(.1f));
    }
    else StartCoroutine(Remove(3f));
  }

  private void OnTriggerEnter(Collider other) {
    if (alreadyHit) return;
    alreadyHit = true;
    StartCoroutine(Remove(1f));
  }

  IEnumerator Remove(float time) {
    yield return new WaitForSeconds(time);
    if (alreadyDestroyed) yield break;
    alreadyDestroyed = true;
    Destroy(gameObject);
  }
}