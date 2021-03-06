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
  [Range(MIN_SEGMENTS, 1000)]
  public int segments = MIN_SEGMENTS;

  /** Our speed. */
  public float speed = 10f;

  /** Turn speed. */
  public float turnSpeed = 500f;

  public float boostSpeedFactor = 2.5f;

  public float boostLengthLoss = 2f;

  public void steer (float turn, bool boost) {
    _turn = turn;
    _boost = boost;
  }

  public void setPlayer (bool isPlayer) {
    _isPlayer = isPlayer;
  }

  public void OnTriggerEnter (Collider collider) {
    switch (collider.tag) {
    case "Untagged":
      break;

    case "Pickup":
      pickupConsumed(collider.gameObject.GetComponent<PickupAttrs>());
      Destroy(collider.gameObject);
      break;

    case "Worm":
      if (collider.gameObject.GetComponent<WormIdentity>().id == _id) return;
      //Debug.Log("Got smacked by obj (" + collider.tag + ") at " +
      //    collider.gameObject.transform.position);
      die();
      break;
    }
  }

  public void Awake () {
    _id = _nextWormId++; // assign ourselves an ID
    // proceed to tag all our fucking game objects with this id
    gameObject.AddComponent<WormIdentity>().id = _id;

    _initialLocation = transform.localPosition;
    _boostParticles = GetComponentInChildren<ParticleSystem>();
    _boostParticles.Stop();
  }

  public void Start () {
    _scale.Set(1, 1, 1);
    transform.localScale = _scale;
    snapshotTarget();
    // fake a pickup so that we grow when we start
    pickupConsumed(this.segments);
  }

  public void Update () {
    if (_turn != 0) {
      transform.Rotate(new Vector3(0, _turn * turnSpeed * Time.deltaTime, 0));
    }

    var moveDistance = Time.deltaTime * speed;
    var lengthAdjusted = false;
    if (_boost && _length >= (MIN_SEGMENTS + 1)) {
      _length = Math.Max(MIN_SEGMENTS, _length - (boostLengthLoss * Time.deltaTime));
      moveDistance *= boostSpeedFactor;
      lengthAdjusted = true;
    }
    if (_isBoosting != lengthAdjusted) {
      _isBoosting = lengthAdjusted;
      if (_isBoosting) {
        _boostParticles.Play();
      } else {
        _boostParticles.Stop();
      }
    }

    // now, move the head forward
    transform.Translate(Vector3.forward * moveDistance);

    // if rotation changed, make a new Target.
    if (_turn != 0) snapshotTarget();

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
      updateScale();
    }
  }

  protected void updateScale () {
    // at the minimum length we have scale = 1, go up from there.
    var scale = (float)Math.Log10(10 + (_length - MIN_SEGMENTS));
    _scale.Set(scale, scale, scale);

    transform.localScale = _scale;
    foreach (var segment in _segments) {
      segment.gameObject.transform.localScale = _scale;
    }
  }

  /** Make a target with the head's current position and rotation. */
  protected void snapshotTarget () {
    _targets.Add(new Target(this.transform.localPosition, this.transform.eulerAngles.y));
  }

  protected void pickupConsumed (PickupAttrs attrs) {
    pickupConsumed(attrs?.power ?? PickupAttrs.DEFAULT_POWER);
  }

  protected void pickupConsumed (float power) {
    _length += power;
    updateScale();
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
    newGameObject.transform.localScale = _scale;
    // copy our ID to the segment
    newGameObject.AddComponent<WormIdentity>().id = _id;
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
    Debug.Log("DIE!");
    // drop a "glow" near each body segment
    foreach (var seg in _segments) {
      spawnGlowNear(seg.gameObject, UnityEngine.Random.Range(.75f, 1.25f));
      Destroy(seg.gameObject);
    }
    _segments.Clear();
    _targets.Clear();

    // and the head
    spawnGlowNear(gameObject, UnityEngine.Random.Range(.75f, 1.25f));

    if (_isPlayer) {
      // reset the head location and rotation
      transform.SetPositionAndRotation(_initialLocation,
          Quaternion.Euler(0, UnityEngine.Random.Range(-180, 180), 0));
      _length = 0;

      // call Start again to reset some other stuff
      Start();
    } else {
      // destroy our gameObject
      Destroy(gameObject);
    }
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
      var attrs = newGlow.GetComponent<PickupAttrs>() ?? newGlow.AddComponent<PickupAttrs>();
      attrs.power = power;
    }
  }

  /** Our id. */
  protected int _id;

  protected bool _isPlayer;

  /** Our length, excluding the head. */
  protected float _length = 0;
  /** Our scale. */
  protected Vector3 _scale = new Vector3(1, 1, 1);

  protected Vector3 _initialLocation;

  /** Our current controls, provided by the "steering" component. */
  // TODO: probably change
  protected float _turn;
  protected bool _boost;

  protected ParticleSystem _boostParticles;
  protected bool _isBoosting;

  private readonly IList<SegmentRecord> _segments = new List<SegmentRecord>();
  private readonly IList<Target> _targets = new List<Target>();

  private static int _nextWormId = 0;
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

class WormIdentity : MonoBehaviour {
  /** This worm's id. */
  public int id;
}
