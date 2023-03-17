using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Game : MonoBehaviour {
  public Piece[] pieces;
  public Camera cam;
  public float zoom = 30f; // FOV is between 20 and 40
  public float height = 8f; // from 1 to 20
  bool panning = false;
  Vector3 origPan;
  Vector3 halfStep = Vector3.one * .5f;
  Vector3 camDefault = new(0, 8, -12.5f);
  Quaternion camDefRotation = Quaternion.Euler(33, 0, 0);
  public BoardTile[] tiles;
  public static GameStatus status = GameStatus.Intro;
  public Canvas canvas;
  public LayerMask TilesMask;
  public Transform Pieces;
  public Transform Board;
  public MeshRenderer Battle;
  public Canvas battleCanvas;
  public TextMeshProUGUI Dbg;

  Material battleMat;

  private void Start() {
    battleMat = Battle.material;
    Battle.material = battleMat;
    Battle.gameObject.SetActive(false);
    battleCanvas.enabled = false;
    Piece p;
    for (int y = 1; y < 8; y++) {
      p = Instantiate(pieces[0], Pieces);
      p.Init(1, y, true, this);
      tiles[1 + y * 9].piece = p;
    }
    p = Instantiate(pieces[2], Pieces);
    p.Init(1, 0, true, this);
    tiles[1 + 0 * 9].piece = p;
    p = Instantiate(pieces[2], Pieces);
    p.Init(1, 8, true, this);
    tiles[1 + 8 * 9].piece = p;
    p = Instantiate(pieces[4], Pieces);
    p.Init(0, 0, true, this);
    tiles[0 + 0 * 9].piece = p;
    p = Instantiate(pieces[4], Pieces);
    p.Init(0, 8, true, this);
    tiles[0 + 8 * 9].piece = p;
    p = Instantiate(pieces[6], Pieces);
    p.Init(0, 1, true, this);
    tiles[0 + 1 * 9].piece = p;
    p = Instantiate(pieces[6], Pieces);
    p.Init(0, 7, true, this);
    tiles[0 + 7 * 9].piece = p;
    p = Instantiate(pieces[8], Pieces);
    p.Init(0, 2, true, this);
    tiles[0 + 2 * 9].piece = p;
    p = Instantiate(pieces[8], Pieces);
    p.Init(0, 6, true, this);
    tiles[0 + 6 * 9].piece = p;


    for (int y = 1; y < 8; y++) {
      p = Instantiate(pieces[1], Pieces);
      p.Init(7, y, false, this);
      tiles[7 + y * 9].piece = p;
    }
    p = Instantiate(pieces[3], Pieces);
    p.Init(7, 0, false, this);
    tiles[7 + 0 * 9].piece = p;
    p = Instantiate(pieces[3], Pieces);
    p.Init(7, 8, false, this);
    tiles[7 + 8 * 9].piece = p;
    p = Instantiate(pieces[5], Pieces);
    p.Init(8, 0, false, this);
    tiles[8 + 0 * 9].piece = p;
    p = Instantiate(pieces[5], Pieces);
    p.Init(8, 8, false, this);
    tiles[8 + 8 * 9].piece = p;
    p = Instantiate(pieces[7], Pieces);
    p.Init(8, 1, false, this);
    tiles[8 + 1 * 9].piece = p;
    p = Instantiate(pieces[7], Pieces);
    p.Init(8, 7, false, this);
    tiles[8 + 7 * 9].piece = p;

    p = Instantiate(pieces[9], Pieces);
    p.Init(8, 2, false, this);
    tiles[8 + 2 * 9].piece = p;
    p = Instantiate(pieces[9], Pieces);
    p.Init(8, 6, false, this);
    tiles[8 + 6 * 9].piece = p;

  }

  Piece selected;
  float clickTime = 0;
  private void Update() {
    if (status == GameStatus.Fight) {
      Fight();
      return;
    }
    PanAndZoom();
    if (Input.GetMouseButtonDown(0)) {
      RaycastHit hit;
      BoardTile bt;
      switch (status) {
        case GameStatus.Intro:
          canvas.enabled = false;
          status = GameStatus.PlayerPickPiece;
          foreach (var tile in tiles) tile.SetAsPossible(false);
          break;

        case GameStatus.PlayerPickPiece:
          if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, TilesMask) &&
            hit.collider.TryGetComponent(out bt)) {
            if (
            bt.piece != null /* FIXME && bt.piece.IsPlayer1 */) {
              selected = bt.piece;
              Dbg.text = selected.name;
              // Calculate the possible movement cells
              if (CalculatePossibleCells(selected)) status = GameStatus.PlayerSelectDestination;
            }
          }
          break;

        case GameStatus.PlayerSelectDestination:
          if (Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out hit, TilesMask) &&
            hit.collider.TryGetComponent(out bt) && bt.possible) {
            // Clean all the highlights
            foreach (var tile in tiles) tile.SetAsPossible(false);
            // Make the piece to walk to the destination. When reaced cehck if we need a battle
            status = GameStatus.WalkToDestination;
            selected.SetTarget(bt);
            if (bt.piece != null) bt.piece.StepBack();
          }
          else {
            foreach (var tile in tiles) tile.SetAsPossible(false);
            status = GameStatus.PlayerPickPiece;
          }
          break;
      }
      // If we are over a piece select it (show on the side?)
      // If a piece was already selected show only the cells valid for the piece to move
    }
  }

  private bool CalculatePossibleCells(Piece p) {
    foreach (var tile in tiles) tile.SetAsPossible(false);
    BoardTile t;
    bool doneOne = false;
    for (int x = -8; x <= 8; x++) { // FIXME
      for (int y = -8; y <= 8; y++) {
        if (x == 0 && y == 0) continue;
        if (x * x + y * y > 65) continue;
        t = p.ValidTargetCell(x, y, tiles);
        if (t != null) { doneOne = true; t.SetAsPossible(true); }
      }
    }
    return doneOne;


    switch (p.type) {
      case PieceType.Pawn:
        for (int x = -1; x <= 1; x++) {
          for (int y = -1; y <= 1; y++) {
            if (x == 0 && y == 0) continue;
            t = p.ValidTargetCell(x, y, tiles);
            if (t != null) { doneOne = true; t.SetAsPossible(true); }
          }
        }
        break;
      case PieceType.Goblin:
        for (int x = -2; x <= 2; x++) {
          for (int y = -2; y <= 2; y++) {
            if (x == 0 && y == 0) continue;
            t = p.ValidTargetCell(x, y, tiles);
            if (t != null) { doneOne = true; t.SetAsPossible(true); }
          }
        }
        break;
      case PieceType.Archer:
        for (int x = -1; x <= 2; x++) {
          for (int y = -1; y <= 1; y++) {
            if (x == 0 && y == 0) continue;
            t = p.ValidTargetCell(x, y, tiles);
            if (t != null) { doneOne = true; t.SetAsPossible(true); }
          }
        }
        break;
      case PieceType.Manticore:
        for (int x = -1; x <= 1; x++) {
          for (int y = -1; y <= 1; y++) {
            if (x == 0 && y == 0) continue;
            t = p.ValidTargetCell(x, y, tiles);
            if (t != null) { doneOne = true; t.SetAsPossible(true); }
          }
        }
        break;
      case PieceType.Valkyrie:
        for (int x = -3; x <= 3; x++) {
          for (int y = -3; y <= 3; y++) {
            if (x == 0 && y == 0) continue;
            t = p.ValidTargetCell(x, y, tiles);
            if (t != null) { doneOne = true; t.SetAsPossible(true); }
          }
        }
        break;
      case PieceType.Banshee:
        for (int x = -4; x <= 4; x++) {
          for (int y = -4; y <= 4; y++) {
            if (x == 0 && y == 0) continue;
            if (x * x + y * y > 10) continue;
            t = p.ValidTargetCell(x, y, tiles);
            if (t != null) { doneOne = true; t.SetAsPossible(true); }
          }
        }
        break;
      case PieceType.Golem:
        for (int xy = -3; xy <= 3; xy++) {
          if (xy == 0) continue;
          t = p.ValidTargetCell(xy, 0, tiles);
          if (t != null) { doneOne = true; t.SetAsPossible(true); }
          t = p.ValidTargetCell(0, xy, tiles);
          if (t != null) { doneOne = true; t.SetAsPossible(true); }
        }
        break;
    }
    return doneOne;
  }

  void PanAndZoom() {
    if (status == GameStatus.Intro) return;

    // Camera zoom
    zoom -= Input.mouseScrollDelta.y * 4f;
    if (zoom < 20) zoom = 20;
    if (zoom > 40) zoom = 40;
    cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, zoom, Time.deltaTime * 6);

    if (Input.GetMouseButtonDown(1)) {
      panning = true;
      origPan = cam.ScreenToViewportPoint(Input.mousePosition) - halfStep;
      if (Time.time - clickTime < .25f) {
        height = 8;
        zoom = 30;
        StartCoroutine(ResetCamera());
      }
      clickTime = Time.time;
    }
    if (Input.GetMouseButtonUp(1)) {
      panning = false;
    }

    if (panning) {
      Vector3 current = cam.ScreenToViewportPoint(Input.mousePosition) - halfStep;
      if (current.x != origPan.x) {
        float angle = (current.x - origPan.x) * 90;
        origPan.x = current.x;
        cam.transform.RotateAround(Vector3.up, Vector3.up, angle);
      }
      if (current.y != origPan.y) {
        height += (origPan.y - current.y) * .25f;
        height = Mathf.Clamp(height, 1, 20);
      }
      Vector3 cp = cam.transform.position;
      cp.y = Mathf.Lerp(cp.y, height, Time.deltaTime * 8);
      cam.transform.position = cp;
      cam.transform.LookAt(Vector3.zero);
    }
  }

  private IEnumerator ResetCamera() {
    float time = 0;
    while (time < .2f) {
      time += Time.deltaTime;
      cam.transform.SetPositionAndRotation(
        Vector3.Lerp(cam.transform.position, camDefault, time * 5), 
        Quaternion.Slerp(cam.transform.rotation, camDefRotation, time * 5));
      cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, zoom, time * 5);
      yield return null;
    }
    cam.transform.SetPositionAndRotation(camDefault, camDefRotation);
    cam.fieldOfView = zoom;
  }

  internal void UpdateCell(BoardTile destination, Piece piece) {
    Piece opponent = destination.piece;
    tiles[piece.x + piece.y * 9].piece = null;
    destination.piece = piece;
    piece.x = destination.x;
    piece.y = destination.y;
    if (opponent != null) { // Start a battle
      status = GameStatus.Fight;
      StartCoroutine(StartFight(piece, opponent));
    }
  }

  Vector3 camStart;
  readonly Vector3 camEnd = new(0, 12, -12.5f); // cam pos 0, 12, -12.5
  Quaternion camRStart;
  readonly Quaternion camREnd = Quaternion.Euler(45, 0, 0); // rot 45 0 0
  float fovStart;
  readonly float fovEnd = 30; // FOV 30
  BoardTile battleTile;

  IEnumerator StartFight(Piece piece, Piece opponent) {
    battleTile = tiles[piece.x + 9 * piece.y];
    while (piece.IsWalking() || opponent.IsWalking()) {
      yield return null;
    }
    opponent.StartBattleAnim();
    piece.StartBattleAnim();
    while (piece.IsStartingBattle() || opponent.IsStartingBattle()) {
      yield return null;
    }
    yield return new WaitForSeconds(.1f);
    LPiece = piece.IsLight ? piece : opponent;
    RPiece = !piece.IsLight ? piece : opponent;
    battleMat.SetFloat("_Alpha", 0);
    Battle.gameObject.SetActive(true);
    RPower.SetValueWithoutNotify(0);
    LPower.SetValueWithoutNotify(0);
    float time = 0;
    camStart = cam.transform.position;
    camRStart = cam.transform.rotation;
    fovStart = cam.fieldOfView;
    Transform l = LPiece.transform;
    Transform r = RPiece.transform;
    Vector3 rPosS = r.position;
    Vector3 rPosE = new(5, 0, 0);
    Vector3 lPosS = l.position;
    Vector3 lPosE = new(-5, 0, 0);
    Quaternion rRotS = r.rotation;
    Quaternion rRotE = Quaternion.Euler(0, -90, 0);
    Quaternion lRotS = l.rotation;
    Quaternion lRotE = Quaternion.Euler(0, 90, 0);
    opponent.transform.SetParent(Battle.transform);
    piece.transform.SetParent(Battle.transform);

    while (time < 1) {
      cam.transform.SetPositionAndRotation(Vector3.Lerp(camStart, camEnd, time), Quaternion.Slerp(camRStart, camREnd, time));
      cam.fieldOfView = Mathf.Lerp(fovStart, fovEnd, time);
      Board.localScale = Vector3.one * (1 - time);
      r.SetPositionAndRotation(Vector3.Lerp(rPosS, rPosE, time), Quaternion.Slerp(rRotS, rRotE, time));
      l.SetPositionAndRotation(Vector3.Lerp(lPosS, lPosE, time), Quaternion.Slerp(lRotS, lRotE, time));
      battleMat.SetFloat("_Alpha", time);
      RPower.SetValueWithoutNotify(time);
      LPower.SetValueWithoutNotify(time);
      time += Time.deltaTime * 1.25f;
      yield return null;
    }
    cam.transform.SetPositionAndRotation(camEnd, camREnd);
    cam.fieldOfView = fovEnd;
    Board.localScale = Vector3.zero;
    Board.gameObject.SetActive(false);
    r.SetPositionAndRotation(rPosE, rRotE);
    l.SetPositionAndRotation(lPosE, lRotE);
    battleMat.SetFloat("_Alpha", 1);
    battleCanvas.enabled = true;
    RPower.SetValueWithoutNotify(1);
    LPower.SetValueWithoutNotify(1);
    RPiece.SetFight(LPiece);
    LPiece.SetFight(RPiece);
  }

  Piece RPiece, LPiece;
  public Slider RPower, LPower;
  Vector3 dstFightWalk;
  public LayerMask FightGroundMask;
  public Toggle PlayAsLight;
  void Fight() {
    if (Input.GetMouseButtonDown(0) && !LPiece.IsSlashing()) { // Move
      if (!Physics.Raycast(cam.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, 50, FightGroundMask)) return;
      dstFightWalk = hit.point;
      dstFightWalk.y = 0;
      if (PlayAsLight.isOn) LPiece.SetDestination(dstFightWalk);
      else RPiece.SetDestination(dstFightWalk);
    }
    if (Input.GetMouseButtonDown(1)) { // Hit
      if (PlayAsLight.isOn && LPiece.type == PieceType.Banshee) LPiece.SlashArea(true);
      if (!PlayAsLight.isOn && RPiece.type == PieceType.Banshee) RPiece.SlashArea(true);
      else if (PlayAsLight.isOn) LPiece.SlashSingle();
      else RPiece.SlashSingle();
    }
    else if (Input.GetMouseButtonUp(1)) { // Stop hit
      if (PlayAsLight.isOn && LPiece.type == PieceType.Banshee) LPiece.SlashArea(false);
      if (!PlayAsLight.isOn && RPiece.type == PieceType.Banshee) RPiece.SlashArea(false);
    }
  }

  internal void Hit(Piece enemy, float power) {
    enemy.Health -= power;
    if (enemy.Health <= 0) {
      enemy.Health = 0;
      enemy.Die();
      // End the fight when the anim is completed
    }
    if (enemy == RPiece) StartCoroutine(AlterHealthR(enemy));
    else StartCoroutine(AlterHealthL(enemy));
  }

  IEnumerator AlterHealthR(Piece p) {
    float time = 0;
    float start = RPower.value;
    float end = p.Health / p.MaxHealth;
    while (time < 1) {
      RPower.SetValueWithoutNotify(Mathf.Lerp(start, end, time));
      time += Time.deltaTime * 3;
      yield return null;
    }
  }
  IEnumerator AlterHealthL(Piece p) {
    float time = 0;
    float start = LPower.value;
    float end = p.Health / p.MaxHealth;
    while (time < 1) {
      LPower.SetValueWithoutNotify(Mathf.Lerp(start, end, time));
      time += Time.deltaTime * 3;
      yield return null;
    }
  }

  internal void EndBattle(Piece pieceDead) {
    StartCoroutine(EndBattleCoroutine(pieceDead));
  }
  IEnumerator EndBattleCoroutine(Piece pieceDead) {
    yield return null;
    Vector3 deadPs = pieceDead.transform.position;
    Vector3 deadPe = deadPs - Vector3.up * .25f;
    Vector3 deadHs = pieceDead.transform.localScale;
    Vector3 deadHe = deadHs;
    deadHe.y = 0;
    Piece winner = RPiece == pieceDead ? LPiece : RPiece;
    Vector3 winnerPs = winner.transform.position;
    Vector3 winnerPe = winner.GetWorldPosition();
    Quaternion winnerRs = winner.transform.rotation;
    Quaternion winnerRe = winner.GetWorldRotation();
    float time = 0;
    Board.gameObject.SetActive(true);
    Board.localScale = Vector3.one * .01f;
    foreach (Transform t in Pieces) if (t.TryGetComponent(out Piece p)) p.transform.position = p.GetWorldPosition();
    while (time < 1) {
      if (!pieceDead.dead) {
        pieceDead.transform.localScale = Vector3.Lerp(deadHs, deadHe, time * 1.5f);
        pieceDead.transform.position = Vector3.Lerp(deadPs, deadPe, time * 1.5f);
      }
      winner.transform.SetPositionAndRotation(Vector3.Lerp(winnerPs, winnerPe, time), Quaternion.Slerp(winnerRs, winnerRe, time));
      battleMat.SetFloat("_Alpha", 1 - time);

      cam.transform.SetPositionAndRotation(Vector3.Lerp(camEnd, camStart, time), Quaternion.Slerp(camREnd, camRStart, time));
      cam.fieldOfView = Mathf.Lerp(fovEnd, fovStart, time);
      Board.localScale = Vector3.one * time;

      time += Time.deltaTime;
      yield return null;
    }
    tiles[pieceDead.x + 9 * pieceDead.y].piece = null;
    battleTile.piece = winner;
    if (!pieceDead.dead) {
      pieceDead.dead = true;
      Destroy(pieceDead.gameObject);
    }
    winner.transform.SetParent(Pieces);
    Battle.gameObject.SetActive(false);
    Board.localScale = Vector3.one;
    foreach(Transform t in Pieces) if (t.TryGetComponent(out Piece p)) p.transform.position = p.GetWorldPosition();

    // FIXME Check if we won
    winner.EndFight();
    status = GameStatus.PlayerPickPiece; // FIXME Should be the player? Can be the opponent
  }
}

public enum GameStatus { Intro, PlayerPickPiece, PlayerSelectDestination, WalkToDestination, Fight
}