using System;
using UnityEngine;

public class Spawner : MonoBehaviour
{
  /** The object to spawn. */
  public GameObject toSpawn;

  public Vector3 minPosition = new Vector3(0, 0, 0);

  public Vector3 maxPosition = new Vector3(0, 0, 0);

  public Vector3 minRotation = new Vector3(0, 0, 0);

  public Vector3 maxRotation = new Vector3(0, 0, 0);

  public string avoidTag;

  public float avoidRadius;

  public int avoidTries = 10;

  [Range(0, Single.MaxValue)]
  public float frequency = 1;

  public void Update () {
    _accum += Time.deltaTime;
    if (_accum < frequency) return;

    _accum -= frequency;
    var loc = findGoodLocation();
    if (loc == null) return;
    Instantiate(toSpawn,
        (Vector3)loc,
        Quaternion.Euler(rand(minRotation, maxRotation)));
  }

  protected Vector3? findGoodLocation () {
    for (int tries = 0; tries < avoidTries; tries++) {
      var loc = rand(minPosition, maxPosition);
      var isGood = true;
      if (avoidRadius > 0) {
        var colliders = Physics.OverlapSphere(loc, avoidRadius);
        foreach (var collider in colliders) {
          if (collider.gameObject.tag == avoidTag) {
            isGood = false;
            break;
          }
        }
      }
      if (isGood) return loc;
    }
    return null;
  }

  protected Vector3 rand (Vector3 min, Vector3 max) {
    return new Vector3(
        UnityEngine.Random.Range(min.x, max.x),
        UnityEngine.Random.Range(min.y, max.y),
        UnityEngine.Random.Range(min.z, max.z));
  }

  protected float _accum = 0;
}
