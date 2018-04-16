using Marty;

namespace MartyTests
{
    public class TestStateMachine: MartyBase
    { 
        public TestStateMachine() { }

        protected override MartyState TopStartingState => new MartyState("Top");

        protected override void RegisterEvents()
        {
        }

        protected override void RegisterStates()
        {

        }
    }
}
