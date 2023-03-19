using System.Collections;
using UnityEngine;

public class Piece : MonoBehaviour {
  public Animator anim;
  public PieceType type;
  public bool IsLight;
  public int x, y;
  public float MaxSpeed = 2;
  public float Accell = 1;
  public float MaxHealth = 1;
  public float Health = 1;
  public GameObject Weapon;
  public Transform StartWeapon;
  public Rigidbody rb;
  public Collider coll;
  public AIStatus status = AIStatus.Idle;
  Game game;
  [HideInInspector] public Piece inFight = null;



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
    return new(x * 1.5625f - 6.25f, 0.01f, y * 1.2375f - 4.95f);

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
    if (inFight == null) return (0, 0);
    Vector3 enemyP = inFight.transform.position;
    enemyP.y = 0;
    Vector3 myP = transform.position;
    myP.y = 0;
    Vector3 enemy = enemyP - myP;
    float angle = Mathf.Abs(Vector3.SignedAngle(transform.forward, enemy, Vector3.up));
    float dist = enemy.magnitude;
    return (angle, dist);
  }

  internal bool DoesShoot() {
    switch (type) {
      case PieceType.Pawn: 
      case PieceType.Goblin:
      case PieceType.Manticore:
      case PieceType.Valkyrie:
      case PieceType.Banshee:
        return false;

      case PieceType.Archer:
      case PieceType.Golem:
      case PieceType.Troll:
      case PieceType.Unicorn:
      case PieceType.Basilisk:
        return true;

      case PieceType.ShapeShifter:
        return DoesShoot();

      case PieceType.Djinni:
      case PieceType.Phoenix:
      case PieceType.Dragon:
      case PieceType.Wizard:
      case PieceType.Sorceress:

      default:
        return false; //FIXME
    }
  }

  #endregion Generic ^^^

  #region **************** Movement ********************************************************************************

  BoardTile destination;
  Vector3 srcPosition, dstPosition;
  public float speed = 0;


  internal void SetTargetBoardTile(BoardTile tile) {
    if (status == AIStatus.Dead) return;
    destination = tile;
    srcPosition = GetWorldPosition();
    dstPosition = tile.GetWorldPosition();
    if (tile.piece != null) dstPosition.x += IsLight ? -.4f : .4f;
    if (status != AIStatus.Walk) speed = 0;
    status = AIStatus.Walk;
  }

  internal void SetDestination(Vector3 dst) {
    if (status == AIStatus.Dead) return;
    dstPosition = FitPosInBattleArea(dst);
    if (status != AIStatus.Walk) {
      srcPosition = transform.position;
    }
    if (status != AIStatus.Slash) status = AIStatus.Walk;
    destination = null;
  }



  public void StepBack(Piece selected) {
    StartCoroutine(WalkBack(selected.transform.position));
  }

  IEnumerator WalkBack(Vector3 other) {
    anim.SetInteger("Status", 8);
    float time = 0;
    srcPosition = transform.position;
    dstPosition = transform.position + Vector3.right * (IsLight ? -.4f : .4f);
    float backSpeed = -1.8f * Mathf.Clamp(Vector3.Distance(transform.position, other), 1f, 6f) + 12f;
    anim.speed = backSpeed * 1.5f;
    while (time < 1) {
      time += Time.deltaTime * backSpeed;
      transform.position = Vector3.Lerp(srcPosition, dstPosition, time);
      yield return null;
    }
    anim.SetInteger("Status", 0);
    anim.speed = 1;
    status = AIStatus.Idle;
  }

  void WalkBack() {
    anim.SetInteger("Status", 8);
    float dist = Vector3.Distance(transform.position, dstPosition) / Vector3.Distance(srcPosition, dstPosition);
    if (dist < .1f) {
      anim.SetInteger("Status", 0);
      status = AIStatus.Idle;
      rb.velocity = Vector3.zero;
      anim.speed /= 2;
    }
    else {
      rb.MovePosition(transform.position + MaxSpeed * Time.deltaTime * (dstPosition - srcPosition).normalized);
    }
  }

  private void Update() {
    if (type == PieceType.Banshee && status == AIStatus.Slash) { // Check hitting the enemy
      DamageArea();
    }

    if (stop > 0) {
      stop -= Time.deltaTime;
      if (stop <= 0) stop = 0;
    }
    rb.velocity = Vector3.zero;

    if (inFight!=null && IsLight != Game.Player1IsLight) switch (type) {
        case PieceType.Pawn:
        case PieceType.Goblin: 
          AIPawnGoblin();
          break;
        case PieceType.Archer:
          break;
        case PieceType.Manticore:
          break;
        case PieceType.Valkyrie:
          break;
        case PieceType.Banshee:
          break;
        case PieceType.Golem:
          break;
        case PieceType.Troll:
          break;
        case PieceType.Unicorn:
          break;
        case PieceType.Basilisk:
          break;
        case PieceType.Djinni:
          break;
        case PieceType.ShapeShifter:
          break;
        case PieceType.Phoenix:
          break;
        case PieceType.Dragon:
          break;
        case PieceType.Wizard:
          break;
        case PieceType.Sorceress:
          break;
      }

    if (status == AIStatus.Walk)  WalkToDestination();
    if (status == AIStatus.WalkBack) WalkBack();
  }

  void WalkToDestination() {
    // We should follow some sort of path that is just a set of points, we should go at constant speed thru the points
    // If start from zero speed we should quickly increase the speed (acceleration)
    // When reacing the destination we should slow down the speed

    anim.SetInteger("Status", 1);
    float dist = Vector3.Distance(transform.position, dstPosition);
    if (dist < .1f) { // We reached the destination
      speed = 0;
      status = AIStatus.Idle;
      waitAction = 0;
      anim.SetInteger("Status", 0);
      if (inFight == null) {
        transform.SetPositionAndRotation(dstPosition, GetWorldRotation());
        Game.status = GameStatus.PlayerPickPiece;
        game.UpdateCell(destination, this);
      }
      else {
        rb.MovePosition(dstPosition);
        rb.velocity = Vector3.zero;
      }
    }
    else {
      float distSrc = Vector3.Distance(transform.position, srcPosition);
      if (distSrc < .5f) speed = MaxSpeed * (.9f - distSrc) * 2;
      else if (dist < .5f) speed = MaxSpeed * (.9f - dist) * 2;
      else speed = MaxSpeed;
      if (inFight != null) speed *= 1.5f;
      
      Vector3 pos = transform.position;
      pos += speed * Time.deltaTime * (dstPosition - pos).normalized;
      if (inFight != null) {
        rb.MovePosition(pos);
      }
      else {
        transform.position = pos;
      }

      pos = dstPosition - pos;
      Quaternion angle = Quaternion.Euler(0, Mathf.Atan2(pos.x, pos.z) * Mathf.Rad2Deg, 0);
      transform.rotation = Quaternion.Slerp(transform.rotation, angle, Time.deltaTime * 5);
    }
  }

  internal bool IsWalking() {
    return status == AIStatus.Walk || status == AIStatus.WalkBack;
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
    status = AIStatus.Idle;
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

  internal void SetFight(Piece enemy, MeshRenderer battle) {
    Accell = 5;
    inFight = enemy;
    anim.speed = 1;
    coll.enabled = true;
    transform.SetParent(battle.transform);
    battleArea = battle.transform;
    if (battleAreaBounds == default) battleAreaBounds = battle.bounds;
  }


  internal void SlashSingle() {
    if (status == AIStatus.Dead) return;
    anim.SetInteger("Status", 3);
    if (status == AIStatus.Slash) anim.Play("Slash");
    status = AIStatus.Slash;
  }
  internal void SlashArea(bool start) {
    if (start) {
      anim.SetInteger("Status", 4);
      status = AIStatus.Slash;
      StartAreaEffect(true);
    }
    else {
      anim.SetInteger("Status", 5);
      status = AIStatus.Idle;
      StartAreaEffect(false);
    }
  }

  internal bool IsMovable() {
    return status == AIStatus.Idle || status == AIStatus.Walk || status == AIStatus.WalkBack;
  }

  internal void Die() {
    status = AIStatus.Dead;
    inFight = null;
    anim.SetInteger("Status", 7);
  }

  internal void EndFight() {
    inFight = null;
    status = AIStatus.Idle;
    anim.speed = Random.Range(.8f, 1.2f);
    anim.SetInteger("Status", 0);
  }


  #endregion Fight ^^^


  #region **************** Hits ********************************************************************************

  public void HitEnd() {
    status = AIStatus.Idle;
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
    status = AIStatus.Idle;
    // We check if in front of us, not too much far away, there is the other piece. In case we will record the hit in the Game
    if (Physics.Raycast(transform.position + Vector3.up * .5f, transform.forward, out RaycastHit hit, 1.2f) &&
       hit.collider.TryGetComponent(out Piece enemy)) {
      game.Hit(enemy, damage);
    }
  }

  private void ShootArrow() {
    anim.SetInteger("Status", 0);
    status = AIStatus.Idle;
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
    status = AIStatus.Idle;
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
    if (EnemyAngle().dist < 4) {
      game.Hit(inFight, .95f * Time.deltaTime);
    }
  }

  void CheckRocksHit() {
    var (angle, dist) = EnemyAngle();
    if (dist < 4.5f && angle < 40) {
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




  #region **************** AI ********************************************************************************
  float delay = 0, waitAction = .5f;
  public float hitDist = .75f;
  void AIPawnGoblin() {
    if (inFight == null || status == AIStatus.Dead) return;



    var (angle, dist) = EnemyAngle();


    // If we are idle check how to attack the enemy or move away
    if (status == AIStatus.Idle) {

      if (delay < waitAction) { // If we have to wait, wait
        delay += Time.deltaTime;
        game.Dbg.text = $"         A{angle:f0}   D{dist:f2}   WAIT {status} {(delay < waitAction ? (waitAction - delay).ToString("F2") : "")}";
        return;
      }

      if (inFight != null && inFight.status != AIStatus.Dead) {
        // Are we close?
        if (dist < .71f) { // Move a little bit back
          status = AIStatus.WalkBack;
          srcPosition = transform.position;
          dstPosition = transform.position - transform.forward * Random.Range(1f, 3f);
          waitAction = 0;
          anim.speed *= 2;
          steps = "back";
        }
        else if (dist < 1f) {
          if (angle < 45) { // Attack
            status = AIStatus.Slash;
            delay = 0;
            waitAction = Random.Range(1f, 1.95f);
            SlashSingle();
            steps = "slash";
          }
          else { // Move a little bit back
            SetDestination(transform.position - (transform.position - inFight.transform.position).normalized * Random.Range(1f, 2f));
            status = AIStatus.Walk;
            waitAction = 0;
            steps = "liback";
          }
        }
        else if (dist < 4f) { // Go straight to the enemy
          SetDestination(inFight.transform.position + (transform.position - inFight.transform.position).normalized * .75f);
          status = AIStatus.Walk;
          waitAction = Random.Range(.1f, .2f);
          steps = "strgt";
        }
        else { // Find a random point beween me and the enemy and move there.
          float distH = dist * .5f;
          Vector3 pos = inFight.transform.position +
            distH * (transform.position - inFight.transform.position).normalized +
            Random.Range(-distH, distH) * .5f * transform.right;
          SetDestination(pos);
          status = AIStatus.Walk;
          waitAction = Random.Range(.1f, .2f);
          steps = "rndhalf";
          // FIXME  In case the enemy shoots and there are cover points try to use them

        }
      }


      game.Dbg.text = $"         A{angle:f0}   D{dist:f2}   {steps} {status} {(delay < waitAction ? (waitAction - delay).ToString("F2") : "")}";
      return;
    }

    // For the other actions we do nothing right now
  }
  string steps="";

  Transform battleArea;
  Bounds battleAreaBounds = default;

  public bool IsDead => status == AIStatus.Dead;

  private Vector3 FitPosInBattleArea(Vector3 pos) {
    if (pos.x < battleAreaBounds.min.x + .5f) { pos.x = battleAreaBounds.min.x + .5f; }
    if (pos.x > battleAreaBounds.max.x - .5f) { pos.x = battleAreaBounds.max.x - .5f; }
    if (pos.z < battleAreaBounds.min.z + .5f) { pos.z = battleAreaBounds.min.z + .5f; }
    if (pos.z > battleAreaBounds.max.z - .5f) { pos.z = battleAreaBounds.max.z - .5f; }
    return pos;
  }



  #endregion AI ^^^




}

public enum PieceType {
  Pawn, Goblin, Archer, Manticore, Valkyrie, Banshee, Golem, Troll, Unicorn, Basilisk, Djinni, ShapeShifter, Phoenix, Dragon, Wizard, Sorceress
}

public enum AIStatus {
  Idle, Walk, WalkBack, Slash, Dead,
}
