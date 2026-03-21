using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Tests for TumbleMath.Smoothstep and TumbleMath.ComputeTiltAngle.
    /// ComputeTumbleFactor tests live in TumbleFactorTests.cs.
    /// </summary>
    public class TumbleSmoothstepTests
    {
        [Test]
        public void Smoothstep_AtZero_ReturnsZero()
        {
            Assert.AreEqual(0f, TumbleMath.Smoothstep(0f), 0.0001f);
        }

        [Test]
        public void Smoothstep_AtOne_ReturnsOne()
        {
            Assert.AreEqual(1f, TumbleMath.Smoothstep(1f), 0.0001f);
        }

        [Test]
        public void Smoothstep_AtHalf_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, TumbleMath.Smoothstep(0.5f), 0.0001f);
        }

        [Test]
        public void Smoothstep_ClampsBelow_ReturnsZero()
        {
            Assert.AreEqual(0f, TumbleMath.Smoothstep(-0.5f), 0.0001f);
        }

        [Test]
        public void Smoothstep_ClampsAbove_ReturnsOne()
        {
            Assert.AreEqual(1f, TumbleMath.Smoothstep(1.5f), 0.0001f);
        }

        [Test]
        public void ComputeTiltAngle_Upright_ReturnsZero()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.up);
            Assert.AreEqual(0f, angle, 0.01f);
        }

        [Test]
        public void ComputeTiltAngle_Inverted_Returns180()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.down);
            Assert.AreEqual(180f, angle, 0.01f);
        }

        [Test]
        public void ComputeTiltAngle_OnSide_Returns90()
        {
            float angle = TumbleMath.ComputeTiltAngle(Vector3.right);
            Assert.AreEqual(90f, angle, 0.01f);
        }

        [Test]
        public void ComputeTiltAngle_At45Degrees_Returns45()
        {
            Vector3 tilted = new Vector3(0f, 1f, 1f).normalized;
            float angle = TumbleMath.ComputeTiltAngle(tilted);
            Assert.AreEqual(45f, angle, 0.1f);
        }
    }
}
