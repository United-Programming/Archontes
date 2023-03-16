using System.Collections;
using UnityEngine;

public class Arrow : MonoBehaviour {
  public Rigidbody rb;
  public float Power = 3;
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
    if (collision.collider.TryGetComponent(out Piece piece)) piece.HitFromProjectile(Power);
    StartCoroutine(Remove());
  }

  private void OnTriggerEnter(Collider other) {
    if (alreadyHit) return;
    alreadyHit = true;
    StartCoroutine(Remove());
  }

  IEnumerator Remove() {
    yield return new WaitForSeconds(.1f);
    if (alreadyDestroyed) yield break;
    alreadyDestroyed = true;
    Destroy(gameObject);
  }
}