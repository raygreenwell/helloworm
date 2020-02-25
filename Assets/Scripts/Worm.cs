using System;
using System.Collections.Generic;
using UnityEngine;

public class Worm : MonoBehaviour
{
  public const bool BOUNCE_OFF_PLANE = false;

  /** The object to use for segments. */
  public GameObject segment;

  /** How many segments should we add behind the head? */
  [Range(0, 1000)]
  public int segments = 10;

  /** Our speed. */
  public float speed = 10f;

  /** Turn speed. */
  public float turnSpeed = 60f;

  public void OnTriggerEnter (Collider collider) {
    if (collider.tag == null) return;

    switch (collider.tag) {
    case "Pellet":
      pelletWasEaten();
      Destroy(collider.gameObject);
      Debug.Log("pellet!");
      break;

    case "Body":
      if (_segments.Count > 0 && collider.gameObject == _segments[0].gameObject) {
        // Do not collide with our own first segment.
        // TODO: maybe there's a better way to filter this out while still having the first segment
        // collide with *other* worms.
        return;
      }
      Debug.Log("KABOOM!");
      break;
    }
  }

  public void Start () {
    for (int ii = 0; ii < segments; ii++) {
      var offset = new Vector3(0, 0, -(ii + 1));
      var newGameObject = Instantiate(segment, transform.position + offset, Quaternion.identity);
      var newSeg = new SegmentRecord(newGameObject);
      newSeg.distance = 0;
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
    #pragma warning disable CS0162 // unreachable code
    if (BOUNCE_OFF_PLANE) {
      var wloc = transform.position;
      if (wloc.y < .5) {
        wloc.y = 5f;
        transform.position = wloc;
        var body = GetComponent<Rigidbody>();
        var vel = body.velocity;
        if (vel.y < 0) {
          vel.y *= -1;
          body.velocity = vel;
          Debug.Log("BOinG!");
        }
      }
    }
    #pragma warning restore CS0162 // unreachable code

    // if rotation changed, make a new Target.
    if (turn != 0) snapshotTarget();

    // now visit each segment and interpolate it towards its next target
    foreach (var segment in _segments) {
      var segMove = moveDistance;
      if (segment.distance > 0) {
        var didMove = Math.Min(segment.distance, segMove);
        segment.distance -= didMove;
        if (segment.distance > 0) continue;
        segMove -= didMove;
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
    if (_segments.Count == 0) {
      snapshotTarget();
      lastSegment = gameObject;
      index = _targets.Count - 1;
    } else {
      var seggy = _segments[_segments.Count - 1];
      lastSegment = seggy.gameObject;
      index = seggy.targetIndex;
    }
    var newGameObject = Instantiate(segment, lastSegment.transform.position,
        lastSegment.transform.rotation);
    var newSeg = new SegmentRecord(newGameObject);
    newSeg.targetIndex = index;
    _segments.Add(newSeg);
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
  public float distance = 1f;

  public SegmentRecord (GameObject gameObject) {
    this.gameObject = gameObject;
  }
}
