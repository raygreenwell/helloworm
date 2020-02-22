using UnityEngine;
using System.Collections;

public class TransformFollower : MonoBehaviour {
  public Transform target;

  public Vector3 offsetPosition;

  private void Update () {
    Refresh();
  }

  public void Refresh () {
    if (target == null) {
      Debug.LogWarning("Missing target ref !", this);
      return;
    }

    // compute position
    transform.position = target.TransformPoint(offsetPosition);
    // compute rotation
    transform.LookAt(target);
  }
}
