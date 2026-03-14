#if UNITY_EDITOR || DEBUG
using System.Collections.Generic;

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Named constants for the physics audit database layer.
    /// All thresholds, paths, and configuration values live here — no bare literals elsewhere.
    /// </summary>
    public static class AuditConstants
    {
        // ---- Database ----

        /// <summary>Relative path from project root to the SQLite database file.</summary>
        public const string k_DbPath = "Logs/physics_audit.db";

        /// <summary>Hours of debug log retention before automatic purge.</summary>
        public const int k_PurgeHours = 48;


        // ---- Ring Buffer ----

        /// <summary>Seconds between automatic flushes of the in-memory log buffer to SQLite.</summary>
        public const float k_FlushIntervalSeconds = 5.0f;

        /// <summary>Maximum entries held in the in-memory ring buffer before forcing a flush.</summary>
        public const int k_RingBufferCapacity = 100;


        // ---- Conformance Tier Thresholds ----

        /// <summary>Tolerance below this is "Excellent".</summary>
        public const double k_TierExcellent = 0.01;

        /// <summary>Tolerance below this is "Good".</summary>
        public const double k_TierGood = 0.05;

        /// <summary>Tolerance below this is "Noticeable".</summary>
        public const double k_TierNoticeable = 0.15;

        /// <summary>Tolerance below this is "Poor".</summary>
        public const double k_TierPoor = 0.50;


        // ---- Conformance Tier Names ----

        public const string k_Excellent = "Excellent";
        public const string k_Good = "Good";
        public const string k_Noticeable = "Noticeable";
        public const string k_Poor = "Poor";
        public const string k_Broken = "Broken";


        // ---- Log Hash ----

        /// <summary>Number of hex characters from SHA-256 used as the correlation hash.</summary>
        public const int k_HashLength = 8;


        // ---- Recognized System Tags ----

        /// <summary>
        /// System tags that DebugLogSink recognises. Only messages prefixed with one of these
        /// (e.g. "[physics] wheel slip exceeded threshold") are persisted to the database.
        /// </summary>
        public static readonly HashSet<string> k_RecognizedTags = new HashSet<string>
        {
            "physics",
            "grip",
            "suspension",
            "drivetrain",
            "air",
            "esc",
            "input",
            "surface",
            "conformance"
        };


        // ---- Helpers ----

        /// <summary>
        /// Compute the conformance tier name for a given tolerance value.
        /// </summary>
        public static string ComputeTier(double tolerance)
        {
            if (tolerance < k_TierExcellent) return k_Excellent;
            if (tolerance < k_TierGood) return k_Good;
            if (tolerance < k_TierNoticeable) return k_Noticeable;
            if (tolerance < k_TierPoor) return k_Poor;
            return k_Broken;
        }
    }
}
#endif
