using System;
using UnityEngine;

public class PelletSpawner : MonoBehaviour
{
  /** The pellet to spawn. */
  public GameObject pellet;

  public Vector3 minExtent = new Vector3(0, 0, 0);

  public Vector3 maxExtent = new Vector3(0, 0, 0);

  [Range(0, Single.MaxValue)]
  public float frequency = 1;

  public void Update () {
    _accum += Time.deltaTime;
    if (_accum < frequency) return;

    _accum -= frequency;
    var location = new Vector3(
        UnityEngine.Random.Range(minExtent.x, maxExtent.x),
        UnityEngine.Random.Range(minExtent.y, maxExtent.y),
        UnityEngine.Random.Range(minExtent.z, maxExtent.z));
    Instantiate(pellet, location, Quaternion.identity);
  }

  protected float _accum = 0;
}
