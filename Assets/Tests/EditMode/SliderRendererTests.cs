using NUnit.Framework;
using R8EOX.Debug.Tuning;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for SliderDefinition struct and SliderRenderer logic.
    /// OnGUI calls cannot be executed outside Unity's rendering loop, so these tests
    /// focus on the data layer: struct construction, delegate binding, and clamping.
    /// </summary>
    public class SliderRendererTests
    {
        // ---- SliderDefinition construction ----

        [Test]
        public void SliderDefinition_DefaultFormat_IsF2()
        {
            var def = new SliderDefinition("Test", 0f, 1f, () => 0.5f, _ => { });
            Assert.AreEqual("F2", def.Format);
        }

        [Test]
        public void SliderDefinition_AllFields_SetCorrectly()
        {
            float backing = 3f;
            var def = new SliderDefinition(
                "Spring (N/m)", 10f, 2000f,
                () => backing,
                v => backing = v,
                "F1");

            Assert.AreEqual("Spring (N/m)", def.Label);
            Assert.AreEqual(10f,   def.Min);
            Assert.AreEqual(2000f, def.Max);
            Assert.AreEqual("F1",  def.Format);
            Assert.AreEqual(3f,    def.Getter());

            def.Setter(42f);
            Assert.AreEqual(42f, backing);
        }

        [Test]
        public void SliderDefinition_Getter_ReturnsLiveValue()
        {
            float liveValue = 7f;
            var def = new SliderDefinition("Live", 0f, 100f, () => liveValue, _ => { });

            liveValue = 99f;
            Assert.AreEqual(99f, def.Getter());
        }

        [Test]
        public void SliderDefinition_Setter_MutatesExternalState()
        {
            float result = 0f;
            var def = new SliderDefinition("Mut", 0f, 50f, () => result, v => result = v * 2f);

            def.Setter(5f);
            Assert.AreEqual(10f, result);
        }

        // ---- SliderDefinition array helpers ----

        [Test]
        public void SliderDefinition_ArrayOfFive_CanBeConstructed()
        {
            var sliders = new SliderDefinition[]
            {
                new SliderDefinition("A", 0f, 10f, () => 1f, _ => { }),
                new SliderDefinition("B", 0f, 10f, () => 2f, _ => { }),
                new SliderDefinition("C", 0f, 10f, () => 3f, _ => { }),
                new SliderDefinition("D", 0f, 10f, () => 4f, _ => { }),
                new SliderDefinition("E", 0f, 10f, () => 5f, _ => { }),
            };

            Assert.AreEqual(5, sliders.Length);
            for (int i = 0; i < sliders.Length; i++)
                Assert.AreEqual((float)(i + 1), sliders[i].Getter());
        }
    }
}
