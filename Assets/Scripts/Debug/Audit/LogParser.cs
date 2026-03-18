#if UNITY_EDITOR || DEBUG
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace R8EOX.Debug.Audit
{
    /// <summary>
    /// Stateless helpers for parsing Unity log messages and computing correlation hashes.
    /// Extracted from <see cref="DebugLogSink"/> so the MonoBehaviour is limited to
    /// lifecycle and flush concerns.
    /// </summary>
    internal static class LogParser
    {
        // ---- Regex Patterns ----

        /// <summary>Matches a system tag at the start of a log message: [tagname]</summary>
        internal static readonly Regex k_TagPattern = new Regex(
            @"^\[(\w+)\]\s*(.*)$", RegexOptions.Singleline);

        /// <summary>Matches trailing JSON context: { ... }</summary>
        internal static readonly Regex k_JsonContextPattern = new Regex(
            @"\{[^{}]+\}\s*$", RegexOptions.Singleline);


        // ---- Parsing ----

        /// <summary>
        /// Attempts to extract a recognised system tag and message body from a raw log string.
        /// Returns false if the message has no recognised tag prefix.
        /// </summary>
        internal static bool TryParseTag(
            string logMessage,
            out string tag,
            out string messageBody)
        {
            tag = null;
            messageBody = null;

            var match = k_TagPattern.Match(logMessage);
            if (!match.Success)
                return false;

            string candidate = match.Groups[1].Value.ToLowerInvariant();
            if (!AuditConstants.k_RecognizedTags.Contains(candidate))
                return false;

            tag = candidate;
            messageBody = match.Groups[2].Value;
            return true;
        }

        /// <summary>
        /// Extracts a trailing JSON context object from the end of a message body, or null if absent.
        /// </summary>
        internal static string ExtractJsonContext(string messageBody)
        {
            var match = k_JsonContextPattern.Match(messageBody);
            return match.Success ? match.Value.Trim() : null;
        }


        // ---- Hashing ----

        /// <summary>
        /// Computes a short SHA-256 correlation hash from level + system + message.
        /// Uses the caller-owned <paramref name="hasher"/> to avoid per-call allocation.
        /// </summary>
        internal static string ComputeHash(SHA256 hasher, string level, string system, string message)
        {
            string input = level + system + message;
            byte[] hashBytes = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
            var sb = new StringBuilder(AuditConstants.k_HashLength);
            for (int i = 0; i < AuditConstants.k_HashLength / 2; i++)
                sb.Append(hashBytes[i].ToString("x2"));
            return sb.ToString();
        }


        // ---- Log Level ----

        /// <summary>Maps a Unity <see cref="LogType"/> to a lowercase level string.</summary>
        internal static string LogTypeToLevel(LogType logType)
        {
            switch (logType)
            {
                case LogType.Error:
                case LogType.Exception:
                    return "error";
                case LogType.Assert:
                    return "assert";
                case LogType.Warning:
                    return "warning";
                default:
                    return "info";
            }
        }
    }
}
#endif
