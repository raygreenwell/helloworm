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
      _segments.Add(Instantiate(segment, transform.position + offset, Quaternion.identity));
    }
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

    var tx = Vector3.forward * Time.deltaTime * speed;
    transform.Translate(tx);
    foreach (GameObject segment in _segments) {
      segment.transform.Translate(tx);
    }
  }

  protected float _rotation = 0;

  protected IList<GameObject> _segments = new List<GameObject>();
}
