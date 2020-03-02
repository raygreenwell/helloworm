using UnityEngine;
using System;
using System.Collections.Generic;

public class AiWormSteering : MonoBehaviour
{
//  private enum Mode { None, Seek, Target, Course };

  // just testing. This would actually be on a component of the pickup.
//  public event EventHandler<object> onDestroy;

  public void Update () {
//    if ((_think -= Time.deltaTime) <= 0) {
//      List<Collider> targets = new List<Collider>();
//      bool glow = false;
//      foreach (var collider in Physics.OverlapSphere(transform.position, 20f)) {
//        if (collider.gameObject.tag == "Glow") {
//          if (!glow) {
//            glow = true;
//            targets.Clear();
//          }
//          targets.Add(collider);
//
//        } else if (!glow && collider.gameObject.tag == "Pellet") {
//          targets.Add(collider);
//        }
//      }
//      if (targets.Count == 0) {
//        // pick a target and aim towards it
//        // TODO
//        // How can we tell if our target has died?
//      }
//    }

    if (_turn == 0 && UnityEngine.Random.Range(0, 50) == 0) {
      _turn = UnityEngine.Random.Range(-1f, 1f);
      Invoke("resetTurn", UnityEngine.Random.Range(.25f, 1.5f));
    }
    gameObject.GetComponent<Worm>().steer(_turn, _boost);
  }

//  public void Awake () {
//    // testing
//    this.onDestroy += (object src, object arg) => {
//      Debug.Log("an AI worm was disabled");
//    };
//  }

//  public void OnDisable () {
//    onDestroy?.Invoke(this, null);
//  }

  protected void resetTurn () {
    _turn = 0;
  }

//  // we think we when _think gets to 0.
//  protected float _think;

//  protected Mode _mode = Mode.None;

  protected float _turn;
  protected bool _boost;

//  protected static readonly System.Random _rand = new System.Random();
}
