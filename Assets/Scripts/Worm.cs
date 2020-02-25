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

  /**
   * Called from a pellet when we've eaten it.
   */
  public void PelletWasEaten () {
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
    var offset = lastSegment.transform.forward * -1;
    var newGameObject = Instantiate(segment, lastSegment.transform.position + offset,
        Quaternion.identity);
    var newSeg = new SegmentRecord(newGameObject);
    newSeg.targetIndex = index;
    _segments.Add(newSeg);
  }

  public void Start () {
    for (int ii = 0; ii < segments; ii++) {
      var offset = new Vector3(0, 0, -(ii + 1));
      var newGameObject = Instantiate(segment, transform.position + offset, Quaternion.identity);
      _segments.Add(new SegmentRecord(newGameObject));
    }

    snapshotTarget();
  }

  public void Update () {
    var turn = Input.GetAxis("Horizontal");
    if (turn != 0) {
      #pragma warning disable CS0162 // unreachable code
      if (false) {
        // I don't know why this one sucks
        transform.RotateAround(Vector3.zero, Vector3.up, turn * turnSpeed * Time.deltaTime);
      } else {
        // This one's better but why?
        _rotation += (turn * Time.deltaTime * turnSpeed);
        transform.rotation = Quaternion.Euler(0, _rotation, 0);
      }
      #pragma warning restore CS0162 // unreachable code
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
    _targets.Add(new Target(this.transform.localPosition, this._rotation));
  }

  protected float _rotation = 0;

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
  public int targetIndex;

  public SegmentRecord (GameObject gameObject) {
    this.gameObject = gameObject;
    this.targetIndex = 0;
  }
}
