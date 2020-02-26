using System;
using System.Collections.Generic;
using UnityEngine;

public class Worm : MonoBehaviour
{
  /** The minimum number of segments allowable. */
  public const int MIN_SEGMENTS = 5;

  /** The object to use for segments. */
  public GameObject segment;

  /** The object to use for glow, when we die. */
  public GameObject glow;

  /** How many segments should we add behind the head? */
  [Range(MIN_SEGMENTS, int.MaxValue)]
  public int segments = 10;

  /** Our speed. */
  public float speed = 10f;

  /** Turn speed. */
  public float turnSpeed = 60f;

  public float boostSpeedFactor = 2.5f;

  public float boostLengthLoss = 2f;

  public void OnTriggerEnter (Collider collider) {
    switch (collider.tag) {
    case "Pickup":
      pickupConsumed(collider.gameObject.GetComponent<PickupAttrs>());
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
    _length = segments;

    snapshotTarget();
  }

  public void Update () {
    var turn = Input.GetAxis("Horizontal");
    if (turn != 0) {
      transform.Rotate(new Vector3(0, turn * turnSpeed * Time.deltaTime, 0));
    }

    var moveDistance = Time.deltaTime * speed;
    var lengthAdjusted = false;
    if (Input.GetButton("Jump") && _length >= (MIN_SEGMENTS + 1)) {
      _length = Math.Max(MIN_SEGMENTS, _length - (boostLengthLoss * Time.deltaTime));
      moveDistance *= boostSpeedFactor;
      lengthAdjusted = true;
    }

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

    if (lengthAdjusted) {
      var targetSegments = Math.Floor(_length);
      while (targetSegments < _segments.Count) {
        dropSegment();
      }
    }
  }

  /** Make a target with the head's current position and rotation. */
  protected void snapshotTarget () {
    _targets.Add(new Target(this.transform.localPosition, this.transform.eulerAngles.y));
  }

  protected void pickupConsumed (PickupAttrs attrs) {
    var power = (attrs == null) ? PickupAttrs.DEFAULT_POWER : attrs.power;
    _length += power;
    var targetSegments = Math.Floor(_length);
    while (targetSegments > _segments.Count) {
      addSegment();
    }
  }

  /**
   * Called from a pellet when we've eaten it.
   */
  protected void addSegment () {
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

  protected void dropSegment () {
    var lastIndex = _segments.Count - 1;
    var seg = _segments[lastIndex];
    spawnGlowAt(seg.gameObject.transform.position + (seg.gameObject.transform.forward * -1));
    _segments.RemoveAt(lastIndex);
    Destroy(seg.gameObject);
  }

  protected void die () {
    // drop a "glow" near each body segment
    foreach (var seg in _segments) {
      spawnGlowNear(seg.gameObject, UnityEngine.Random.Range(.75f, 1.25f));
      Destroy(seg.gameObject);
    }
    _segments.Clear();
    _targets.Clear();

    // and the head
    spawnGlowNear(gameObject, UnityEngine.Random.Range(.75f, 1.25f));

    // reset the head location and rotation
    transform.SetPositionAndRotation(new Vector3(0, .5f, 0), Quaternion.identity);
    _length = 0;
    snapshotTarget();
  }

  protected void spawnGlowNear (GameObject gobj, float power = PickupAttrs.DEFAULT_POWER) {
    var offset = new Vector3(
        UnityEngine.Random.Range(-.5f, .5f), 0, UnityEngine.Random.Range(-.5f, .5f));
    spawnGlowAt(gobj.transform.position + offset, power);
  }

  protected void spawnGlowAt (Vector3 world, float power = PickupAttrs.DEFAULT_POWER) {
    var newGlow = Instantiate(glow, world, Quaternion.identity);
    if (power != PickupAttrs.DEFAULT_POWER) {
      newGlow.transform.localScale *= power;
      var attrs = newGlow.GetComponent<PickupAttrs>();
      if (attrs == null) {
        attrs = newGlow.AddComponent<PickupAttrs>();
      }
      attrs.power = power;
    }
  }

  /** Our length, excluding the head. */
  protected float _length = 0;

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
