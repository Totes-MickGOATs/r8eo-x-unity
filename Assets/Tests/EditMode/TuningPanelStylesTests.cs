using NUnit.Framework;
using UnityEngine;
using R8EOX.Debug;

namespace R8EOX.Tests.EditMode
{
    /// <summary>
    /// Unit tests for TuningPanelStyles — GUIStyle initialisation extracted from TuningPanel.
    /// </summary>
    [TestFixture]
    public class TuningPanelStylesTests
    {
        // ---- Build ----

        [Test]
        public void Build_ReturnsNonNullStyles()
        {
            var styles = TuningPanelStyles.Build();

            Assert.IsNotNull(styles.Label,   "Label style should not be null");
            Assert.IsNotNull(styles.Header,  "Header style should not be null");
            Assert.IsNotNull(styles.Value,   "Value style should not be null");
        }

        [Test]
        public void Build_LabelStyle_CorrectFontSize()
        {
            var styles = TuningPanelStyles.Build();

            Assert.AreEqual(TuningPanelStyles.k_FontSize, styles.Label.fontSize);
        }

        [Test]
        public void Build_HeaderStyle_CorrectFontSize()
        {
            var styles = TuningPanelStyles.Build();

            Assert.AreEqual(TuningPanelStyles.k_HeaderFontSize, styles.Header.fontSize);
        }

        [Test]
        public void Build_HeaderStyle_IsBold()
        {
            var styles = TuningPanelStyles.Build();

            Assert.AreEqual(FontStyle.Bold, styles.Header.fontStyle);
        }

        [Test]
        public void Build_LabelStyle_WhiteTextColor()
        {
            var styles = TuningPanelStyles.Build();

            Assert.AreEqual(Color.white, styles.Label.normal.textColor);
        }

        [Test]
        public void Build_HeaderStyle_YellowTextColor()
        {
            var styles = TuningPanelStyles.Build();

            Assert.AreEqual(Color.yellow, styles.Header.normal.textColor);
        }

        [Test]
        public void Build_ValueStyle_RightAligned()
        {
            var styles = TuningPanelStyles.Build();

            Assert.AreEqual(TextAnchor.MiddleRight, styles.Value.alignment);
        }
    }
}
