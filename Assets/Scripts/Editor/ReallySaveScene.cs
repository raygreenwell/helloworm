using UnityEngine;
using UnityEditor;
using System;

public class ReallySaveScene : MonoBehaviour
{
  [MenuItem("File/ReallySaveScene")]
  public static void saveScene () {
    UnityEngine.SceneManagement.Scene currentScene =
        UnityEngine.SceneManagement.SceneManager.GetActiveScene();
    if (!currentScene.isDirty) print("Scene was NOT marked dirty");
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(currentScene);
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(currentScene)) {
      print("WARNING: Scene Not Saved!!!");
    }
  }
}
