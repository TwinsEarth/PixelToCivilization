using NUnit.Framework;
using PxC.Core.Capabilities;
using PxC.Host;

namespace PxC.Tests.EditMode
{
    public sealed class CapabilityTests
    {
        [Test]
        public void Grant_AllowsCapability()
        {
            var cap = new CapabilitySystem();
            cap.Grant("p", new System.Collections.Generic.List<CapabilityClaim>
            {
                new CapabilityClaim { Capability = Capability.Save }
            });
            Assert.IsTrue(cap.HasCapability("p", Capability.Save));
        }

        [Test]
        public void CheckCapability_NotGranted_RecordsViolation()
        {
            var cap = new CapabilitySystem();
            cap.CheckCapability("p", Capability.Network);
            Assert.AreEqual(1, cap.ViolationCount("p"));
        }

        [Test]
        public void Revoke_RemovesCapability()
        {
            var cap = new CapabilitySystem();
            cap.Grant("p", Capability.Save, null);
            cap.Revoke("p", Capability.Save, null);
            Assert.IsFalse(cap.HasCapability("p", Capability.Save));
        }

        [Test]
        public void CheckCapability_Granted_NoViolation()
        {
            var cap = new CapabilitySystem();
            cap.Grant("p", Capability.Network, null);
            cap.CheckCapability("p", Capability.Network);
            Assert.AreEqual(0, cap.ViolationCount("p"));
        }

        [Test]
        public void Grant_DoesNotGrantToOtherPlugins()
        {
            var cap = new CapabilitySystem();
            cap.Grant("p1", Capability.Save, null);
            Assert.IsFalse(cap.HasCapability("p2", Capability.Save));
        }
    }
}
