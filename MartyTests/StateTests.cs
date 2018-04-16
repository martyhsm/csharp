using Marty;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MartyTests
{
    [TestClass]
    public class StateTests
    {
        [TestMethod]
        public void Constructor_VerifyCreation_Successful()
        {
            MartyState testState = new MartyState("Test");

            Assert.IsNotNull(testState);
            Assert.IsInstanceOfType(testState, typeof(MartyState));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException), AllowDerivedTypes = true)]
        public void Constructor_VerifyCreation_Unsuccessful()
        {
            MartyState testState = new MartyState(string.Empty);
        }
    }
}
