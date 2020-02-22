using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Worm : MonoBehaviour
{
  public GameObject segment;
  public int length = 10;
  public float speed = 2.5f;
  public float angularSpeed = 5f;

  void Start () {
    // Skip index 0 because we are inside the "head", just set up the body segments
    for (int ii = 1; ii < length; ii++) {
      Vector3 offset = new Vector3(0, 0, -ii);
      var seggy = Instantiate(segment, transform.position + offset, Quaternion.identity);
      _segments.Add(new SegmentRecord(seggy));
    }

    snapshotTarget();
  }

  void Update () {
    var turn = Input.GetAxis("Horizontal");
    if (turn != 0) {
      if (false) {
        // I don't know why this one sucks
        transform.RotateAround(Vector3.zero, Vector3.up, turn * angularSpeed * Time.deltaTime);
      } else {
        // This one's better but why?
        _rotation += (turn * Time.deltaTime * angularSpeed);
        transform.rotation = Quaternion.Euler(0, _rotation, 0);
      }
    }

    var moveDistance = Time.deltaTime * speed;

    // now, move the head forward
    var tx = Vector3.forward * moveDistance;
    transform.Translate(tx);

    // if rotation changed, make a new Target.
    if (turn != 0) snapshotTarget();

    // now visit each segment and interpolate it towards its next target
    var lowestTarget = _targets.Count;
    foreach (var segment in _segments) {
      var segMove = moveDistance;
      var target = _targets[segment.targetIndex];
      var pos = segment.segment.transform.localPosition;
      do {
        var distance = Vector3.Distance(target.position, pos);
        segment.segment.transform.localPosition =
            Vector3.MoveTowards(pos, target.position, segMove);
        segMove -= distance;
        if (segMove > 0) {
          segment.targetIndex += 1;
          // snapshot a new target if we haven't turned in a while...
          if (segment.targetIndex == _targets.Count) snapshotTarget();
          target = _targets[segment.targetIndex];
          segment.segment.transform.rotation = Quaternion.Euler(0, target.rotation, 0);
        }
      } while (segMove > 0);
      lowestTarget = Math.Min(lowestTarget, segment.targetIndex);
    }

    // let's prune things sometimes
    var toPrune = lowestTarget; //_segments[_segments.Count - 1].targetIndex;
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

  private IList<SegmentRecord> _segments = new List<SegmentRecord>();
  private IList<Target> _targets = new List<Target>();
}

class Target {
  public Vector3 position;
  public float rotation;

  public Target (Vector3 pos, float rot) {
    this.position = pos;
    this.rotation = rot;
  }
}

class SegmentRecord {
  public GameObject segment;
  public int targetIndex;

  public SegmentRecord (GameObject segment) {
    this.segment = segment;
    this.targetIndex = 0;
  }
}
