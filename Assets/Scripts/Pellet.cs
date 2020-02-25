using System.Collections.Generic;
using UnityEngine;

public class Pellet : MonoBehaviour
{
  public void OnTriggerEnter (Collider collider) {
    if ("Head".Equals(collider.tag)) {
      var worm = collider.gameObject.GetComponent<Worm>();
      Destroy(this.gameObject);
      worm.PelletWasEaten();
    }
  }
}
