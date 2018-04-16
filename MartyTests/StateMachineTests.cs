using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Marty;

namespace MartyTests
{
    [TestClass]
    public class StateMachineTests
    {
        [TestMethod]
        public void Constructor_VerifyCreation_Successful()
        {
            TestStateMachine testStateMachine = new TestStateMachine();

            Assert.IsNotNull(testStateMachine);
            Assert.IsTrue(testStateMachine.GetType().IsSubclassOf(typeof(MartyBase)));
        }
    }
}
