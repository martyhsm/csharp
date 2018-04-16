namespace Marty
{
    internal sealed class MartyStateIdGenerator
    {
        private static int currentId = 0;

        private static MartyStateIdGenerator instance = new MartyStateIdGenerator();

        internal static MartyStateIdGenerator Instance
        {
            get
            {
                return instance;
            }
        }

        private MartyStateIdGenerator()
        {
        }

        internal int GetId()
        {
            lock (this)
            {
                return currentId++;
            }
        }
    }
}
