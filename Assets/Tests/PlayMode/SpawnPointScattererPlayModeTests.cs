using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Mirror;
using System.Collections;
using kcp2k;

public class SpawnPointScattererPlayModeTests
{
    [UnitySetUp]
    public IEnumerator SetUp()
    {
        LogAssert.Expect(LogType.Error, "No Transport on Network Manager...add a transport and assign it.");

        var nmGO = new GameObject("NetworkManager");
        nmGO.AddComponent<NetworkManager>();
        nmGO.AddComponent<KcpTransport>();

        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go == null) continue;
            if (go.name.Contains("NetworkManager") || go.GetComponent<NetworkStartPosition>() != null)
                Object.DestroyImmediate(go);
        }
        NetworkManager.startPositions.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator SamplePosition_AlwaysReturnsPointInsideRadius()
    {
        for (int i = 0; i < 100; i++)
        {
            float radius = Random.Range(10f, 200f);
            float innerRadius = Random.Range(0f, radius * 0.7f);

            Vector3 point = SpawnPointScatterer.Test_SamplePosition(radius, innerRadius, Random.value, true);

            Assert.LessOrEqual(point.magnitude, radius + 0.001f, "Точка вышла за внешний радиус");
            Assert.GreaterOrEqual(point.magnitude, innerRadius - 0.001f, "Точка ближе внутреннего радиуса");
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator SamplePosition_FullSphereHasYVariation()
    {
        Vector3 point = SpawnPointScatterer.Test_SamplePosition(50f, 0f, 0f, true);
        Assert.AreNotEqual(0f, point.y, "В полной сфере должна быть вариация по Y");

        yield return null;
    }

    [UnityTest]
    public IEnumerator SamplePosition_XZPlaneHasZeroY()
    {
        Vector3 point = SpawnPointScatterer.Test_SamplePosition(50f, 0f, 0f, false);
        Assert.AreEqual(0f, point.y, 0.0001f, "В плоском режиме Y должен быть равен 0");

        yield return null;
    }
}