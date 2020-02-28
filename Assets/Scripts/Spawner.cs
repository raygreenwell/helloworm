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

  [Range(0, Single.MaxValue)]
  public float frequency = 1;

  public void Update () {
    _accum += Time.deltaTime;
    if (_accum < frequency) return;

    _accum -= frequency;
    Instantiate(toSpawn,
        rand(minPosition, maxPosition),
        Quaternion.Euler(rand(minRotation, maxRotation)));
  }

  protected Vector3 rand (Vector3 min, Vector3 max) {
    return new Vector3(
        UnityEngine.Random.Range(min.x, max.x),
        UnityEngine.Random.Range(min.y, max.y),
        UnityEngine.Random.Range(min.z, max.z));
  }

  protected float _accum = 0;
}
