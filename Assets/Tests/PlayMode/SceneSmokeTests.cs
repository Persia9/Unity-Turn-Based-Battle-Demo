using System.Collections;
using NUnit.Framework;
using Presentation.Title;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class SceneSmokeTests
{
    private const float TimeoutSeconds = 10f;

    [UnityTest]
    public IEnumerator TitleSceneOpensTitleScreen()
    {
        yield return SceneManager.LoadSceneAsync("Title", LoadSceneMode.Single);

        var scene = SceneManager.GetActiveScene();
        Assert.That(scene.name, Is.EqualTo("Title"));
        Assert.That(scene.isLoaded, Is.True);

        var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
        TitleScreenView titleScreen = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            titleScreen = Object.FindFirstObjectByType<TitleScreenView>();
            if (titleScreen != null) break;
            yield return null;
        }

        Assert.That(titleScreen, Is.Not.Null,
            "TitleScreenView was not opened. Check ProjectContext, Title SceneContext, master data, and Addressables.");
        Assert.That(titleScreen.gameObject.activeInHierarchy, Is.True);
        Assert.That(titleScreen.gameObject.scene, Is.EqualTo(scene));
        Assert.That(titleScreen.transform.Find("Safe Area Content"), Is.Not.Null,
            "TitleScreenView did not create its mobile safe-area layout.");
    }
}
