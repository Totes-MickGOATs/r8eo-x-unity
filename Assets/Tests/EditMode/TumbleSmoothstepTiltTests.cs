using NUnit.Framework;
using R8EOX.Vehicle.Physics;
using UnityEngine;

namespace R8EOX.Tests.EditMode
{
    /// <summary>Unit tests for TumbleMath.Smoothstep and ComputeTiltAngle.</summary>
    public class TumbleSmoothstepTiltTests
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
            Assert.AreEqual(0f, TumbleMath.ComputeTiltAngle(Vector3.up), 0.01f);
        }

        [Test]
        public void ComputeTiltAngle_Inverted_Returns180()
        {
            Assert.AreEqual(180f, TumbleMath.ComputeTiltAngle(Vector3.down), 0.01f);
        }

        [Test]
        public void ComputeTiltAngle_OnSide_Returns90()
        {
            Assert.AreEqual(90f, TumbleMath.ComputeTiltAngle(Vector3.right), 0.01f);
        }

        [Test]
        public void ComputeTiltAngle_At45Degrees_Returns45()
        {
            Vector3 tilted = new Vector3(0f, 1f, 1f).normalized;
            Assert.AreEqual(45f, TumbleMath.ComputeTiltAngle(tilted), 0.1f);
        }
    }
}
