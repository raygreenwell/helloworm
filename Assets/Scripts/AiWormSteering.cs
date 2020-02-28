using UnityEngine;

public class AiWormSteering : MonoBehaviour
{
  public void Update () {
    if (_turn == 0 && Random.Range(0, 50) == 0) {
      _turn = Random.Range(-1f, 1f);
      Invoke("resetTurning", Random.Range(.25f, 1.5f));
    }
    gameObject.GetComponent<Worm>().steer(_turn, _boost);
  }

  protected void resetTurning () {
    _turn = 0;
  }

  protected float _turn;
  protected bool _boost;
}
