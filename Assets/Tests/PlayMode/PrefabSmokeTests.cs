using System.Collections;
using NUnit.Framework;
using Presentation.Battle;
using Presentation.Battle.Result;
using Presentation.Title;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.TestTools;

public sealed class PrefabSmokeTests
{
    [UnityTest]
    public IEnumerator TitleScreenAddressLoadsExpectedPrefab()
    {
        yield return AssertAddressableView<TitleScreenView>("UI/Title/TitleScreen");
    }

    [UnityTest]
    public IEnumerator BattleHudAddressLoadsExpectedPrefab()
    {
        yield return AssertAddressableView<BattleHudView>("UI/Battle/BattleHud");
    }

    [UnityTest]
    public IEnumerator ResultPopupAddressLoadsExpectedPrefab()
    {
        yield return AssertAddressableView<ResultPopupView>("UI/Battle/ResultPopup");
    }

    private static IEnumerator AssertAddressableView<T>(string address) where T : Component
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(address);
        yield return handle;

        try
        {
            Assert.That(handle.Status, Is.EqualTo(AsyncOperationStatus.Succeeded),
                $"Addressables failed to load '{address}'.");
            Assert.That(handle.Result, Is.Not.Null, $"Address '{address}' returned no prefab.");
            Assert.That(handle.Result.GetComponent<T>(), Is.Not.Null,
                $"Prefab '{address}' does not contain {typeof(T).Name} on its root.");
        }
        finally
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
    }
}
