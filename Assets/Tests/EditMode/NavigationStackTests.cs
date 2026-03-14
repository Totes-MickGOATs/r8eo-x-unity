using System;
using NUnit.Framework;
using R8EOX.GameFlow;

namespace R8EOX.Tests.EditMode
{
    [TestFixture]
    public sealed class NavigationStackTests
    {
        private NavigationStack _stack;

        [SetUp]
        public void SetUp()
        {
            _stack = new NavigationStack();
        }

        [Test]
        public void Push_AddsScreen_CurrentReturnsIt()
        {
            _stack.Push("MainMenu");

            Assert.AreEqual("MainMenu", _stack.Current);
        }

        [Test]
        public void Push_NullOrEmpty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _stack.Push(null));
            Assert.Throws<ArgumentException>(() => _stack.Push(""));
            Assert.Throws<ArgumentException>(() => _stack.Push("   "));
        }

        [Test]
        public void Pop_RemovesTop_ReturnsIt()
        {
            _stack.Push("MainMenu");
            _stack.Push("ModeSelect");

            string popped = _stack.Pop();

            Assert.AreEqual("ModeSelect", popped);
            Assert.AreEqual("MainMenu", _stack.Current);
        }

        [Test]
        public void Pop_Empty_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _stack.Pop());
        }

        [Test]
        public void CanGoBack_OneItem_ReturnsFalse()
        {
            _stack.Push("MainMenu");

            Assert.IsFalse(_stack.CanGoBack);
        }

        [Test]
        public void CanGoBack_TwoItems_ReturnsTrue()
        {
            _stack.Push("MainMenu");
            _stack.Push("ModeSelect");

            Assert.IsTrue(_stack.CanGoBack);
        }

        [Test]
        public void GetBreadcrumbs_ReturnsBottomFirst()
        {
            _stack.Push("MainMenu");
            _stack.Push("ModeSelect");
            _stack.Push("CarSelect");

            string[] breadcrumbs = _stack.GetBreadcrumbs();

            Assert.AreEqual(3, breadcrumbs.Length);
            Assert.AreEqual("MainMenu", breadcrumbs[0]);
            Assert.AreEqual("ModeSelect", breadcrumbs[1]);
            Assert.AreEqual("CarSelect", breadcrumbs[2]);
        }

        [Test]
        public void Clear_EmptiesStack()
        {
            _stack.Push("MainMenu");
            _stack.Push("ModeSelect");

            _stack.Clear();

            Assert.AreEqual(0, _stack.Count);
            Assert.IsNull(_stack.Current);
        }

        [Test]
        public void Push_FiresEvent()
        {
            string pushedScreen = null;
            _stack.OnScreenPushed += screen => pushedScreen = screen;

            _stack.Push("MainMenu");

            Assert.AreEqual("MainMenu", pushedScreen);
        }

        [Test]
        public void Pop_FiresEvent()
        {
            _stack.Push("MainMenu");
            _stack.Push("ModeSelect");

            string poppedScreen = null;
            _stack.OnScreenPopped += screen => poppedScreen = screen;

            _stack.Pop();

            Assert.AreEqual("ModeSelect", poppedScreen);
        }

        [Test]
        public void Count_ReflectsStackSize()
        {
            Assert.AreEqual(0, _stack.Count);

            _stack.Push("MainMenu");
            Assert.AreEqual(1, _stack.Count);

            _stack.Push("ModeSelect");
            Assert.AreEqual(2, _stack.Count);

            _stack.Pop();
            Assert.AreEqual(1, _stack.Count);
        }

        [Test]
        public void Current_EmptyStack_ReturnsNull()
        {
            Assert.IsNull(_stack.Current);
        }
    }
}
