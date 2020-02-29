using UnityEngine;

public class LocalPlayerWormSteering : MonoBehaviour
{
  public Camera mainCamera;

  public Camera overheadCamera;

  public void Start () {
    gameObject.GetComponent<Worm>().setPlayer(true);
  }

  public void Update () {
    gameObject.GetComponent<Worm>().steer(Input.GetAxis("Horizontal"), Input.GetButton("Jump"));

    var fire = Input.GetButton("Fire1");
    if (fire != _cameraChanged) {
      if (fire) {
        mainCamera.enabled = !mainCamera.enabled;
        overheadCamera.enabled = !overheadCamera.enabled;
      }
      _cameraChanged = fire;
    }
  }

  protected bool _cameraChanged;
}
