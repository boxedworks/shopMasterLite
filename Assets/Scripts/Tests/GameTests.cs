
using System.Collections;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class GameTests
{


  // Tests that no error logs are thrown during scene load
  [UnityTest]
  public IEnumerator SceneLoad_ErrorCheckTest()
  {

    SceneManager.LoadScene("Scene");

    LogAssert.Expect(LogType.Log, "Game loaded");

    yield return null;
  }

  [UnityTest]
  public IEnumerator SceneLoad_ErrorCheckTest_AfterTick()
  {

    SceneManager.LoadScene("Scene");
    yield return null;

    LogAssert.Expect(LogType.Log, "Game loaded");

    yield return null;
  }

}