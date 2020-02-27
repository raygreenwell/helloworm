using UnityEngine;

public class PickupAttrs : MonoBehaviour, IPickupAttrs
{
  /** The default power when there are no PickupAttrs attached to a pickup GameObject. */
  public const float DEFAULT_POWER = 1f;

  /** The length "power" of this pickup. */
  public float power;

  // from IPickupAttrs
  public float getPower () {
    return power;
  }
}
