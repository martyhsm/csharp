namespace Marty
{
    /// <summary>
    /// Defines Block Transition Handler
    /// </summary>
    public delegate void BlockTransitionHandler();

    /// <summary>
    /// Defines Processing Handler
    /// </summary>
    /// <param name="isProcessing">Specifies whether the state host is processing an instruction</param>
    public delegate void ProcessingHandler(bool isProcessing);

    /// <summary>
    /// Defines Event Handler
    /// </summary>
    /// <param name="evt">Specifies event</param>
    /// <param name="payload">Specifies payload</param>
    /// <returns>Returns whether the event was handled</returns>
    public delegate bool EventHandler(int evt, object payload);

    /// <summary>
    /// Looks up a state based on state's ID
    /// </summary>
    /// <param name="id">Specifies ID</param>
    /// <returns></returns>
    public delegate MartyState StateLookupHandler(long? id);
}
