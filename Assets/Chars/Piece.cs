using System.Collections;
using UnityEngine;

public class Piece : MonoBehaviour {
  public Animator anim;
  public PieceType type;
  public bool IsLight;
  public int x, y;
  public float Speed = 2;
  public float Accell = 1;
  public float MaxHealth = 1;
  public float Health = 1;
  public GameObject Weapon;
  public Transform StartWeapon;
  public Rigidbody rb;
  public Collider coll;
  public bool dead = false;

  Game game;
  Piece inFight = null;



  #region **************** Generic ********************************************************************************
  internal void Init(int px, int py, bool lightSide, Game g) {
    x = px;
    y = py;
    IsLight = lightSide;
    anim.speed = Random.Range(.8f, 1.2f);
    gameObject.name = $"{type} {x},{y}";
    transform.SetPositionAndRotation(GetWorldPosition(), GetWorldRotation());
    game = g;
  }

  internal Vector3 GetWorldPosition() {
    return new(x * 1.125f - 4.5f, 0.01f, y * 1.125f - 4.5f);
  }

  internal Quaternion GetWorldRotation() {
    return Quaternion.Euler(0, IsLight ? 90 : -90, 0);
  }

  internal BoardTile ValidTargetCell(int dx, int dy, BoardTile[] tiles) {
    int px = x + dx;
    int py = y + dy;
    if (px < 0 || px > 8 || py < 0 || py > 8) return null;
    var t = tiles[px + 9 * py];
    if (t.piece == null || t.piece.IsOpponent(this)) return t;
    return null;
  }

  internal bool IsOpponent(Piece other) {
    return IsLight != other.IsLight;
  }

  (float angle, float dist) EnemyAngle() {
    Vector3 enemyP = inFight.transform.position;
    enemyP.y = 0;
    Vector3 myP = transform.position;
    myP.y = 0;
    Vector3 enemy = enemyP - myP;
    float angle = Mathf.Abs(Vector3.SignedAngle(transform.forward, enemy, Vector3.up));
    float dist = enemy.magnitude;
    return (angle, dist);
  }

  #endregion Generic ^^^

  #region **************** Movement ********************************************************************************

  BoardTile destination;
  bool walking = false;
  Vector3 srcPosition, dstPosition;
  float dist = 0, step = 0, speed = 0;
  bool notCheckedArrival;
  Quaternion defaultDirection;


  internal void SetTarget(BoardTile tile) {
    destination = tile;
    srcPosition = GetWorldPosition();
    dstPosition = tile.GetWorldPosition();
    if (tile.piece != null) dstPosition.x += IsLight ? -.4f : .4f;
    dist = Vector3.Distance(srcPosition, dstPosition);
    walking = true;
    speed = 0;
    step = 0;
    anim.SetInteger("Status", 1);
    defaultDirection = GetWorldRotation();
    notCheckedArrival = true;
  }

  internal void SetDestination(Vector3 dst) {
    srcPosition = transform.position;
    dstPosition = dst;
    dist = Vector3.Distance(srcPosition, dstPosition);
    walking = true;
    speed = 0;
    step = 0;
    anim.SetInteger("Status", 1);
    notCheckedArrival = false;
    destination = null;
  }

  public void StepBack() {
    StartCoroutine(WalkBack());
  }

  IEnumerator WalkBack() {
    anim.speed = 2;
    anim.SetInteger("Status", 1);
    float time = 0;
    srcPosition = transform.position;
    dstPosition = transform.position + Vector3.right * (IsLight ? -.4f : .4f);
    while (time < 1) {
      time += Time.deltaTime * .8f;
      transform.position = Vector3.Lerp(srcPosition, dstPosition, time);
      yield return null;
    }
    anim.SetInteger("Status", 0);
    anim.speed = 1;
  }

  private void Update() {
    if (type == PieceType.Banshee && slashing) { // Check hitting the enemy
      DamageArea();
    }

    if (stop > 0) {
      stop -= Time.deltaTime;
      if (stop <= 0) {
        stop = 0;
      }
    }
    rb.velocity = Vector3.zero;
    if (!walking) return;

    if (speed < Speed) speed += Time.deltaTime * Accell;
    step += speed * Time.deltaTime;
    Vector3 dstStep = Vector3.Lerp(srcPosition, dstPosition, step / dist);
    if (inFight) rb.MovePosition(dstStep);
    else transform.position = dstStep;
    Quaternion angle = Quaternion.Euler(0, Mathf.Atan2(dstPosition.x - transform.position.x, dstPosition.z - transform.position.z) * Mathf.Rad2Deg, 0);
    if (!inFight && dist - step < .5) angle = defaultDirection;
    transform.rotation = Quaternion.Slerp(transform.rotation, angle, Time.deltaTime * 3);

    if (dist - step < 1.5f && notCheckedArrival) {
      notCheckedArrival = false;
    }

    if (step >= dist) {
      walking = false;
      anim.SetInteger("Status", 0);
      if (!inFight) {
        transform.SetPositionAndRotation(dstPosition, GetWorldRotation());
        Game.status = GameStatus.PlayerPickPiece;
        game.UpdateCell(destination, this);
      }
      else rb.velocity = Vector3.zero;
    }
  }

  internal bool IsWalking() {
    return walking || anim.GetInteger("Status") != 0;
  }

  float stop = 0;
  private void OnCollisionEnter(Collision collision) {
    stop = .1f;
  }

  #endregion Movement ^^^

  #region **************** Anim events ********************************************************************************

  bool startingBattle = false;
  public void StartBattleAnim() {
    anim.speed = .9f;
    anim.SetInteger("Status", 2);
    startingBattle = true;
    slashing = false;
    walking = false;
  }
  public void ReadyToStart() {
    startingBattle = false;
    anim.SetInteger("Status", 0);
    anim.Play("Idle", 0);
  }

  public bool IsStartingBattle() {
    return startingBattle;
  }

  internal void DeathCompleted() {
    game.EndBattle(this);
  }

  #endregion Anim events ^^^

  #region **************** Fight ********************************************************************************

  internal void SetFight(Piece enemy) {
    Speed *= 1.2f;
    Accell = 5;
    inFight = enemy;
    anim.speed = 1;
    coll.enabled = true;
  }

  bool slashing = false;
  internal void SlashSingle() {
    anim.SetInteger("Status", 3);
    slashing = true;
    walking = false;
  }
  internal void SlashArea(bool start) {
    if (start) {
      anim.SetInteger("Status", 4);
      slashing = true;
      StartAreaEffect(true);
    }
    else {
      anim.SetInteger("Status", 5);
      slashing = false;
      StartAreaEffect(false);
    }
    walking = false;
  }

  internal bool IsSlashing() {
    return slashing;
  }

  internal void Die() {
    walking = false;
    inFight = null;
    anim.SetInteger("Status", 7);
  }

  internal void EndFight() {
    inFight = null;
    walking = false;
    slashing = false;
    anim.speed = Random.Range(.8f, 1.2f);
  }


  #endregion Fight ^^^


  #region **************** Hits ********************************************************************************

  public void HitEnd() {
    anim.SetInteger("Status", 0);
    slashing = false;
  }
  public void HitStart() {
    if (inFight == null) return;

    // Do the actual action depending on the type of piece
    switch (type) {
      case PieceType.Pawn:
      case PieceType.Goblin:
        CheckSwordHit(1);
        break;
      case PieceType.Archer:
      case PieceType.Troll:
        ShootArrow();
        break;
      case PieceType.Manticore:
        CheckTailHit();
        break;
      case PieceType.Valkyrie:
        CheckSwordHit(2);
        break;
      case PieceType.Banshee:
        break;
      case PieceType.Golem:
        CheckRocksHit();
        break;
      case PieceType.Unicorn:
        ShootRay();
        break;
    }
  }

  internal void HitFromProjectile(float power) {
    game.Hit(this, power);
  }


  void CheckSwordHit(float damage) {
    anim.SetInteger("Status", 0);
    slashing = false;
    // We check if in front of us, not too much far away, there is the other piece. In case we will record the hit in the Game
    if (Physics.Raycast(transform.position + Vector3.up * .5f, transform.forward, out RaycastHit hit, 1.2f) &&
       hit.collider.TryGetComponent(out Piece enemy)) {
      game.Hit(enemy, damage);
    }
  }

  private void ShootArrow() {
    anim.SetInteger("Status", 0);
    slashing = false;
    var arrow = Instantiate(Weapon, transform.parent);
    arrow.transform.SetPositionAndRotation(StartWeapon.position, transform.rotation);
    if (arrow.TryGetComponent(out Rigidbody arb)) {
      arb.velocity = type switch {
        PieceType.Archer => transform.forward * 10,
        PieceType.Troll => transform.forward * 12 + Vector3.up,
        _ => transform.forward
      };
      var angleDist = EnemyAngle();
      if (type == PieceType.Archer && angleDist.angle < 15) {
        float vert = (inFight.transform.position - StartWeapon.position).normalized.y * 5;
        Vector3 vel = arb.velocity;
        vel.y = vert;
        arb.velocity = vel;
      }
    }
  }

  void CheckTailHit() {
    anim.SetInteger("Status", 0);
    slashing = false;
    var angleDist = EnemyAngle();
    if (angleDist.angle < 30 && angleDist.dist < 1.5f) { // Bite
      game.Hit(inFight, 2);
    }
    else if (angleDist.angle > 140 && angleDist.dist < 1.8f) { // Tail
      game.Hit(inFight, 10);
    }
  }

  void StartAreaEffect(bool enable) { // FIXME
    Weapon.SetActive(enable);
  }

  void DamageArea() {
    game.Hit(this, .15f * Time.deltaTime);
    var angleDist = EnemyAngle();
    if (angleDist.dist < 4) {
      game.Hit(inFight, .95f * Time.deltaTime);
    }
  }

  void CheckRocksHit() {
    var angleDist = EnemyAngle();
    if (angleDist.dist < 4.5f && angleDist.angle < 40) {
      game.Hit(inFight, 10);
    }
  }

  void ShootRay() {
    LineRenderer lr = Weapon.GetComponent<LineRenderer>();
    Vector3[] positions = new Vector3[2];
    positions[0] = Weapon.transform.position;
    positions[1] = Vector3.zero;
    lr.SetPositions(positions);
    lr.positionCount = 2;
    lr.enabled = true;
    if (Physics.Raycast(transform.position + Vector3.up, transform.forward, out RaycastHit hit) && hit.collider.TryGetComponent(out Piece _)) {
      StartCoroutine(BeamFire(lr, positions, hit.point, true));
    }
    else {
      StartCoroutine(BeamFire(lr, positions, Vector3.zero, false));
    }
  }

  IEnumerator BeamFire(LineRenderer lr, Vector3[] positions, Vector3 target, bool hit) {
    float time = 0;
    while (time < 1) {
      positions[1] = positions[0] + 10 * time * transform.forward;
      lr.positionCount = 2;
      lr.SetPositions(positions);
      if (hit && Vector3.Distance(target + transform.forward, positions[1]) < 1f) {
        game.Hit(inFight, 5);
        yield return new WaitForSeconds(.2f);
        lr.enabled = false;
        yield break;
      }
      time += 2.5f * Time.deltaTime;
      yield return null;
    }
    yield return new WaitForSeconds(.2f);
    lr.enabled = false;
  }

  #endregion Hits ^^^













}

public enum PieceType {
  Pawn, Goblin, Archer, Manticore, Valkyrie, Banshee, Golem, Troll, Unicorn, Bsilisk, Djinni, ShapeShifter, Phoenix, Dragon, Wizard, Sorceress
  
}
