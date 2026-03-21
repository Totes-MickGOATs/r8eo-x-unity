using NUnit.Framework;
using R8EOX.Debug;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for ContractViolationCounters — the per-session violation counter struct
    /// extracted from ContractDebugger.
    /// </summary>
    [TestFixture]
    public class ContractViolationCountersTests
    {
        // ---- Default State ----

        [Test]
        public void DefaultConstruction_AllCountersZero()
        {
            var counters = new ContractViolationCounters();

            Assert.AreEqual(0, counters.Input);
            Assert.AreEqual(0, counters.Vehicle);
            Assert.AreEqual(0, counters.Wheel);
            Assert.AreEqual(0, counters.Observable);
        }

        // ---- AddInput ----

        [Test]
        public void AddInput_IncreasesInputCount()
        {
            var counters = new ContractViolationCounters();
            counters.AddInput(3);

            Assert.AreEqual(3, counters.Input);
        }

        [Test]
        public void AddInput_DoesNotAffectOtherCounters()
        {
            var counters = new ContractViolationCounters();
            counters.AddInput(5);

            Assert.AreEqual(0, counters.Vehicle);
            Assert.AreEqual(0, counters.Wheel);
            Assert.AreEqual(0, counters.Observable);
        }

        // ---- AddVehicle ----

        [Test]
        public void AddVehicle_IncreasesVehicleCount()
        {
            var counters = new ContractViolationCounters();
            counters.AddVehicle(2);

            Assert.AreEqual(2, counters.Vehicle);
        }

        // ---- AddWheel ----

        [Test]
        public void AddWheel_IncreasesWheelCount()
        {
            var counters = new ContractViolationCounters();
            counters.AddWheel(4);

            Assert.AreEqual(4, counters.Wheel);
        }

        // ---- AddObservable ----

        [Test]
        public void AddObservable_IncreasesObservableCount()
        {
            var counters = new ContractViolationCounters();
            counters.AddObservable(1);

            Assert.AreEqual(1, counters.Observable);
        }

        // ---- Reset ----

        [Test]
        public void Reset_AfterAdds_AllCountersZero()
        {
            var counters = new ContractViolationCounters();
            counters.AddInput(1);
            counters.AddVehicle(2);
            counters.AddWheel(3);
            counters.AddObservable(4);

            counters.Reset();

            Assert.AreEqual(0, counters.Input);
            Assert.AreEqual(0, counters.Vehicle);
            Assert.AreEqual(0, counters.Wheel);
            Assert.AreEqual(0, counters.Observable);
        }

        // ---- Accumulation ----

        [Test]
        public void AddInput_MultipleAdds_Accumulates()
        {
            var counters = new ContractViolationCounters();
            counters.AddInput(1);
            counters.AddInput(2);
            counters.AddInput(3);

            Assert.AreEqual(6, counters.Input);
        }
    }
}
