using UnityEngine;

public class LocalPlayerWormSteering : MonoBehaviour
{
  public void Update () {
    gameObject.GetComponent<Worm>().steer(Input.GetAxis("Horizontal"), Input.GetButton("Jump"));
  }
}
