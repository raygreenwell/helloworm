using UnityEngine;

public class BarePickupAttrs : IPickupAttrs
{
  public BarePickupAttrs (float power) {
    _power = power;
  }

  /**
   * Get the power of this pickup.
   */
  public float getPower () {
    return _power;
  }

  /** The power. */
  protected float _power;
}
