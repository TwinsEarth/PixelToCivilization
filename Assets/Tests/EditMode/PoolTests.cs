using NUnit.Framework;
using PxC.Infra.Pool;
using UnityEngine;

namespace PxC.Tests.EditMode
{
    public sealed class PoolTests
    {
        public sealed class TestData
        {
            public int Value;
        }

        [Test]
        public void DataPool_ReusesReturnedInstance()
        {
            var root = new GameObject();
            var pool = new ObjectPool(root.transform);
            var d = pool.RentData<TestData>();
            pool.ReturnData(d);
            var d2 = pool.RentData<TestData>();
            Assert.AreSame(d, d2);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void GoPool_ReturnThenRent_ReusesSameInstance()
        {
            var root = new GameObject();
            var pool = new ObjectPool(root.transform);
            var prefab = new GameObject("p");

            var inst = pool.Rent(prefab);
            pool.Return(inst);
            Assert.AreEqual(1, pool.InactiveCount(prefab));

            var inst2 = pool.Rent(prefab);
            Assert.AreSame(inst, inst2);
            Assert.AreEqual(1, pool.ActiveCount(prefab));

            Object.DestroyImmediate(inst2);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Prewarm_CreatesInactiveInstances()
        {
            var root = new GameObject();
            var pool = new ObjectPool(root.transform);
            var prefab = new GameObject("p");
            pool.Prewarm(prefab, 3);
            Assert.AreEqual(3, pool.InactiveCount(prefab));

            pool.Clear(prefab);
            Object.DestroyImmediate(prefab);
            Object.DestroyImmediate(root);
        }
    }
}
