using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using R8EOX.Tests.PlayMode.Helpers;

namespace R8EOX.Tests.PlayMode
{
    /// <summary>
    /// Black-box PlayMode tests that verify tagged debug log messages appear in the Unity
    /// console when an RC car is active or experiences physics events.
    ///
    /// These tests observe ONLY external behavior: car is active/driving → tagged console
    /// output appears. No debug implementation types are referenced (no DebugBootstrap,
    /// no ContractDebugger, no WheelTerrainDiagnostics, no DebugLogSink).
    ///
    /// Tagged log format: [tag] message — e.g. [physics] ..., [suspension] ...
    /// </summary>
    [TestFixture]
    [Category("Debug")]
    public class DebugLoggingTests
    {
        // ---- Timing Constants ----

        /// <summary>Physics frames to wait after spawn for settle (2s at 50Hz).</summary>
        const int k_SettleFrames = 120;
        /// <summary>Physics frames to apply motor force during drive scenario (1s at 50Hz).</summary>
        const int k_DriveFrames = 60;
        /// <summary>Physics frames to wait for landing and settle (3s at 50Hz).</summary>
        const int k_LandingFrames = 180;

        // ---- Spawn Positions ----

        static readonly Vector3 k_GroundSpawn = new Vector3(0f, 0.5f, 0f);
        static readonly Vector3 k_ElevatedSpawn = new Vector3(0f, 1.5f, 0f);

        // ---- Motor Force Constants ----

        /// <summary>Motor force per rear wheel for simulated throttle (N). Half of 26N engine force.</summary>
        const float k_MotorForcePerWheel = 13f;

        // ---- Log Pattern Constants ----

        /// <summary>Regex pattern matching any tagged log: [word] at start of message.</summary>
        const string k_AnyTagPattern = @"^\[(\w+)\]";
        /// <summary>Regex pattern matching physics or suspension tagged logs.</summary>
        const string k_PhysSuspTagPattern = @"^\[(physics|suspension)\]";


        // ---- Fixtures ----

        private GameObject _ground;
        private GameObject _car;
        private R8EOX.Vehicle.RaycastWheel[] _wheels;


        // ---- Setup / Teardown ----

        [TearDown]
        public void TearDown()
        {
            if (_car != null) Object.DestroyImmediate(_car);
            if (_ground != null) Object.DestroyImmediate(_ground);
            _car = null;
            _ground = null;
            _wheels = null;
        }


        // ---- Helpers ----

        private void SpawnTestVehicle(Vector3 spawnPosition)
        {
            _ground = ConformanceSceneSetup.CreateGround();
            _car = ConformanceSceneSetup.CreateTestVehicle(spawnPosition);
            _wheels = _car.GetComponentsInChildren<R8EOX.Vehicle.RaycastWheel>();
        }

        private void ApplyMotorForce()
        {
            foreach (var wheel in _wheels)
                if (wheel.IsMotor) wheel.MotorForceShare = k_MotorForcePerWheel;
        }

        private void ClearMotorForce()
        {
            foreach (var wheel in _wheels)
                wheel.MotorForceShare = 0f;
        }


        // ================================================================
        // Test 1: CarActive_ProducesTaggedLogsInConsole
        // ================================================================

        /// <summary>
        /// When a car is spawned and driven, at least one tagged log message
        /// matching [word] at the start of the line must appear in the console.
        /// Black-box: does not assert which system produced the log.
        /// </summary>
        [UnityTest]
        [Timeout(15000)]
        public IEnumerator CarActive_ProducesTaggedLogsInConsole()
        {
            SpawnTestVehicle(k_GroundSpawn);

            var logs = new List<string>();
            Application.LogCallback capture = (msg, _, type) => logs.Add(msg);
            Application.logMessageReceived += capture;
            try
            {
                // Settle on ground
                yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_SettleFrames);

                // Apply motor force to drive the car
                ApplyMotorForce();
                yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_DriveFrames);

                ClearMotorForce();

                bool hasTaggedLog = logs.Any(
                    m => System.Text.RegularExpressions.Regex.IsMatch(m, k_AnyTagPattern));

                Assert.IsTrue(hasTaggedLog,
                    $"Expected at least one tagged debug log (matching {k_AnyTagPattern}) " +
                    $"while car is active and driving. Total logs captured: {logs.Count}. " +
                    (logs.Count > 0
                        ? $"Sample: \"{logs[0]}\""
                        : "No logs captured at all."));
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
        }


        // ================================================================
        // Test 2: CarLanding_ProducesSuspensionLogs
        // ================================================================

        /// <summary>
        /// When a car is dropped from height and lands, at least one log tagged
        /// [physics] or [suspension] must appear in the console.
        /// Black-box: does not assert which component emitted the log.
        /// </summary>
        [UnityTest]
        [Timeout(15000)]
        public IEnumerator CarLanding_ProducesSuspensionLogs()
        {
            SpawnTestVehicle(k_ElevatedSpawn);

            var logs = new List<string>();
            Application.LogCallback capture = (msg, _, type) => logs.Add(msg);
            Application.logMessageReceived += capture;
            try
            {
                // Wait for landing and settle
                yield return VehicleIntegrationHelper.WaitPhysicsFrames(k_LandingFrames);

                bool hasSuspensionLog = logs.Any(
                    m => System.Text.RegularExpressions.Regex.IsMatch(m, k_PhysSuspTagPattern));

                Assert.IsTrue(hasSuspensionLog,
                    $"Expected at least one [physics] or [suspension] tagged log after car " +
                    $"drops from height {k_ElevatedSpawn.y}m and lands. " +
                    $"Total logs captured: {logs.Count}. " +
                    (logs.Count > 0
                        ? $"Sample logs: {string.Join(", ", logs.Take(5).Select(l => $"\"{l}\""))}"
                        : "No logs captured at all."));
            }
            finally
            {
                Application.logMessageReceived -= capture;
            }
        }
    }
}
