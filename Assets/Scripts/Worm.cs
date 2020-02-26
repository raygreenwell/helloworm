using System;
using System.Collections.Generic;
using UnityEngine;

public class Worm : MonoBehaviour
{
  /** The object to use for segments. */
  public GameObject segment;

  /** The object to use for glow, when we die. */
  public GameObject glow;

  /** How many segments should we add behind the head? */
  [Range(0, 1000)]
  public int segments = 10;

  /** Our speed. */
  public float speed = 10f;

  /** Turn speed. */
  public float turnSpeed = 60f;

  public void OnTriggerEnter (Collider collider) {
    switch (collider.tag) {
    case "Pellet":
      pelletWasEaten();
      Destroy(collider.gameObject);
      break;

    case "Body":
      if (_segments.Count > 0) {
        // Do not collide with our own first segment.
        // TODO: maybe there's a better way to filter this out while still having the first segment
        // collide with *other* worms.
        if (collider.gameObject == _segments[0].gameObject) return;

        // if the segment we've collided with is currently ticking down "waitDistance" then that
        // means it was just added and it's close to the head and we should definitely ignore it.
        for (var ii = _segments.Count - 1; ii >= 0; ii--) {
          var seg = _segments[ii];
          if (seg.waitDistance == 0) break;
          if (seg.gameObject == collider.gameObject) return;
        }
      }
      //Debug.Log("Got smacked by obj at " + collider.gameObject.transform.position);
      die();
      break;
    }
  }

  public void Start () {
    for (int ii = 0; ii < segments; ii++) {
      var offset = new Vector3(0, 0, -(ii + 1));
      var newGameObject = Instantiate(segment, transform.position + offset, Quaternion.identity);
      var newSeg = new SegmentRecord(newGameObject);
      newSeg.waitDistance = 0;
      _segments.Add(newSeg);
    }

    snapshotTarget();
  }

  public void Update () {
    var turn = Input.GetAxis("Horizontal");
    if (turn != 0) {
      transform.Rotate(new Vector3(0, turn * turnSpeed * Time.deltaTime, 0));
    }

    var moveDistance = Time.deltaTime * speed;

    // now, move the head forward
    transform.Translate(Vector3.forward * moveDistance);

    // if rotation changed, make a new Target.
    if (turn != 0) snapshotTarget();

    // now visit each segment and interpolate it towards its next target
    foreach (var segment in _segments) {
      var segMove = moveDistance;
      if (segment.waitDistance > 0) {
        var waitMoveAmount = Math.Min(segment.waitDistance, segMove);
        segment.waitDistance -= waitMoveAmount;
        if (segment.waitDistance > 0) continue;
        segMove -= waitMoveAmount;
      }
      var target = _targets[segment.targetIndex];
      var pos = segment.gameObject.transform.localPosition;
      do {
        var distance = Vector3.Distance(target.position, pos);
        segment.gameObject.transform.localPosition =
            Vector3.MoveTowards(pos, target.position, segMove);
        segMove -= distance;
        if (segMove > 0) {
          pos = target.position;
          segment.targetIndex += 1;
          // snapshot a new target if we haven't turned in a while...
          if (segment.targetIndex == _targets.Count) snapshotTarget();
          target = _targets[segment.targetIndex];
          segment.gameObject.transform.rotation = Quaternion.Euler(0, target.rotation, 0);
        }
      } while (segMove > 0);
    }

    // let's prune things sometimes
    var toPrune = (_segments.Count == 0)
        ? _targets.Count - 1
        : _segments[_segments.Count - 1].targetIndex;
    if (toPrune > 0) {
      foreach (var segment in _segments) segment.targetIndex -= toPrune;
      for (; toPrune > 0; --toPrune) _targets.RemoveAt(0);
    }
  }

  /** Make a target with the head's current position and rotation. */
  protected void snapshotTarget () {
    _targets.Add(new Target(this.transform.localPosition, this.transform.eulerAngles.y));
  }

  /**
   * Called from a pellet when we've eaten it.
   */
  protected void pelletWasEaten () {
    // duplicate the last segment but 1 unit behind it
    GameObject lastSegment;
    int index;
    float addDistance = 0;
    if (_segments.Count == 0) {
      snapshotTarget();
      lastSegment = gameObject;
      index = _targets.Count - 1;
    } else {
      var seggy = _segments[_segments.Count - 1];
      lastSegment = seggy.gameObject;
      index = seggy.targetIndex;
      addDistance = seggy.waitDistance;
    }
    var newGameObject = Instantiate(segment, lastSegment.transform.position,
        lastSegment.transform.rotation);
    var newSeg = new SegmentRecord(newGameObject);
    newSeg.waitDistance += addDistance;
    newSeg.targetIndex = index;
    _segments.Add(newSeg);
  }

  protected void die () {
    // drop a "glow" near each body segment
    foreach (var seg in _segments) {
      spawnGlowNear(seg.gameObject);
      Destroy(seg.gameObject);
    }
    _segments.Clear();
    _targets.Clear();

    // and the head
    spawnGlowNear(gameObject);

    // reset the head location and rotation
    transform.SetPositionAndRotation(new Vector3(0, .5f, 0), Quaternion.identity);
    snapshotTarget();
  }

  protected void spawnGlowNear (GameObject gobj) {
    var offset = new Vector3(
        UnityEngine.Random.Range(-.5f, .5f), 0, UnityEngine.Random.Range(-.5f, .5f));
    Instantiate(glow, gobj.transform.position + offset, Quaternion.identity);
  }

  private readonly IList<SegmentRecord> _segments = new List<SegmentRecord>();
  private readonly IList<Target> _targets = new List<Target>();
}

class Target {
  public readonly Vector3 position;
  public readonly float rotation;

  public Target (Vector3 pos, float rot) {
    this.position = pos;
    this.rotation = rot;
  }
}

class SegmentRecord {
  public readonly GameObject gameObject;
  public int targetIndex = 0;
  public float waitDistance = 1f;

  public SegmentRecord (GameObject gameObject) {
    this.gameObject = gameObject;
  }
}
