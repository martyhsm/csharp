using System;
using System.Collections.Generic;
using System.Linq;

namespace Marty
{
    /// <summary>
    /// Defines MartyBase class
    /// </summary>
    public abstract class MartyBase
    {
        /// <summary>
        /// Stores value of start event
        /// </summary>
        public const int Start = -3;

        /// <summary>
        /// Stores value of entry event
        /// </summary>
        public const int Entry = -2;

        /// <summary>
        /// Stores value of exit event
        /// </summary>
        public const int Exit = -1;

        /// <summary>
        /// Stores states
        /// </summary>
        private IDictionary<long, MartyState> states = new Dictionary<long, MartyState>();

        /// <summary>
        /// Stores events
        /// </summary>
        private IList<int> events = new List<int>();

        /// <summary>
        /// Stores queued instructions
        /// </summary>
        private Queue<MartyInstruction> instructions = new Queue<MartyInstruction>();

        /// <summary>
        /// Gets or sets a value indicating whether or not to allow transitions
        /// </summary>
        private bool AllowTransitions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether state host has been initialized
        /// </summary>
        private bool IsInitialized { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether state host transition is transitioning
        /// </summary>
        private bool IsTransitioning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether state host is currently processing an event
        /// </summary>
        private bool IsProcessing { get; set; }

        /// <summary>
        /// Gets a value indicating whether state host busy
        /// </summary>
        private bool IsBusy
        {
            get
            {
                return this.IsTransitioning || this.IsProcessing;
            }
        }

        /// <summary>
        /// Gets or sets current state
        /// </summary>
        private MartyState CurrentState { get; set; }

        /// <summary>
        /// Gets Top state
        /// </summary>
        protected MartyState Top { get; private set; }

        /// <summary>
        /// Constructs an MartyBase object
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected MartyBase()
        {
            // Start state host.
            this.Initialize();
        }

        /// <summary>
        /// Gets state
        /// </summary>
        /// <param name="id">Specifies ID</param>
        /// <returns></returns>
        internal MartyState GetState(long? id)
        {
            return id == null ? null : this.states[(long)id];
        }

        /// <summary>
        /// Transitions to a registered state
        /// </summary>
        /// <param name="destination">Specifies destination state</param>
        protected void TransitionTo(MartyState destination)
        {
            // Check if destination state is null.
            if (destination == null)
            {
                // Throw an exception if destination state is null.
                throw new InvalidOperationException(string.Format("Cannot transition to a null state."));
            }

            // Throw an exception if a designation state is not registered.
            if (!this.states.Keys.Any(key => key == destination.Id))
            {
                throw new InvalidOperationException(string.Format("The state {0} hasn't been registered.", destination.Name));
            }

            // Throw an exception if transitioning is blocked in current state.
            if (!this.AllowTransitions)
            {
                throw new InvalidOperationException(string.Format("Transitions cannot be made during Start or Exit events.  See state {0}.", this.CurrentState.Name));
            }

            // Check if current state equals destination.
            if (this.CurrentState == destination)
            {
                // Leave.
                return;
            }

            // Check if state is busy.
            if (this.IsBusy)
            {
                // Queue instruction.
                this.instructions.Enqueue(new MartyInstruction(MartyInstructionTypes.Transition, destination.Id, null));

                // Leave.
                return;
            }

            // Change state.
            this.ChangeState(destination);
        }

        /// <summary>
        /// Generates and event for the state host to process
        /// </summary>
        /// <param name="evt">Specifies event</param>
        /// <param name="payload">Specifies payload</param>
        public void RaiseEvent(int evt, object payload = null)
        {
            // Check if event is registered.
            if (!events.Contains(evt))
            {
                // Throw an exception if event is not registered.
                throw new InvalidOperationException(string.Format("The event {0} hasn't been registered.", evt));
            }

            // Check if this state is busy.
            if (this.IsBusy)
            {
                // Queue instruction.
                this.instructions.Enqueue(new MartyInstruction(MartyInstructionTypes.Event, evt, payload));

                // Leave.
                return;
            }
            
            // Start event processing.
            this.SetProcessing(true);

            // Process event.
            this.CurrentState.ProcessEvent(evt, payload);


            // Start event processing.
            this.SetProcessing(false);
        }

        /// <summary>
        /// Initializes state host
        /// </summary>
        private void Initialize()
        {
            // Check if state host has already been initialized.
            if (this.IsInitialized)
            {
                // Leave.
                return;
            }

            // Initialize properties.
            this.AllowTransitions = true;
            this.IsTransitioning = false;
            this.IsProcessing = false;
            this.IsInitialized = false;

            // Prepare top state.
            this.PrepareTopState();

            // Register states.
            this.RegisterStates();

            // Prepare states.
            this.PrepareStates();

            // Register infrastructure events.
            this.RegisterEvent(Start);
            this.RegisterEvent(Entry);
            this.RegisterEvent(Exit);

            // Register events.
            this.RegisterEvents();

            // Checks event numbering.
            this.CheckEventNumbering();

            // Transition to starting state.
            this.TransitionTo(this.GetState(this.Top.StartingState));

            // Store that state host has been initialized.
            this.IsInitialized = true;
        }

        /// <summary>
        /// Prepares Top state for use
        /// </summary>
        private void PrepareTopState()
        {
            // Initailize top state.
            this.Top = new MartyState(MartyConstants.TopStateName);

            // Register Top state.
            this.RegisterState(this.Top, this.TopHandler, null);

            // Throw an exception if starting state is null.
            if (this.TopStartingState == null)
            {
                throw new NullReferenceException(string.Format("The starting state for the {0} state cannot be null.", MartyConstants.TopStateName));
            }

            // Set starting state for Top state.
            this.Top.StartingState = this.TopStartingState.Id;
        }

        /// <summary>
        /// Prepares states to be used by state host
        /// </summary>
        private void PrepareStates()
        {
            // Iterate through each state.
            foreach (MartyState state in this.states.Values)
            {
                // Check composition of starting state.
                this.CheckStartingState(state);
            }

            // Start state host at the Top state.
            this.CurrentState = this.Top;
        }

        /// <summary>
        /// Registers event(s)
        /// </summary>
        /// <param name="events">Specifies events</param>
        protected void RegisterEvent(params int[] events)
        {
            // Iterate through events.
            foreach (int evt in events)
            {
                // Check for invalid events.
                if (evt < Start)
                //if (evt < Start)
                {
                    // Throw an exception if negative events are registered.
                    throw new ArgumentOutOfRangeException("Only infrastructure events can be negative.");
                }

                // Add event.
                if (this.events.Contains(evt))
                {
                    throw new InvalidOperationException(string.Format("Cannot register the event {0} more than once.", evt));
                }

                this.events.Add(evt);
            }
        }

        /// <summary>
        /// Registers a state
        /// </summary>
        /// <param name="state">Specifies state</param>
        /// <param name="eventHandler">Specifies event handler</param>
        /// <param name="parentState">Specifies parent</param>
        /// <param name="startingState">Specifies starting state</param>
        protected void RegisterState(MartyState state, EventHandler eventHandler, MartyState parentState = null, MartyState startingState = null)
        {
            // Check if state is null.
            if (state == null)
            {
                throw new ArgumentNullException("Cannot register a null state.");
            }

            // Check if state is being set as its own parent.
            if (state == parentState)
            {
                throw new ArgumentException(string.Format("State {0} cannot be its own parent.", state.Name));
            }

            // Check if state is being set as its own starting state.
            if (state == startingState)
            {
                throw new ArgumentException(string.Format("State {0} cannot be its own starting state.", state.Name));
            }

            // Try adding state to dictionary.
            try
            {
                // Add state to dictionary.
                this.states.Add(state.Id, state);
            }
            // Catch argument exceptions.
            catch (Exception exception)
            {
                // Throw exception with more detailed error message.
                throw new InvalidOperationException(string.Format("An exception occurred during state registration: {0}; Exception: {1}", state.Name, exception.Message));
            }

            // Add state to parent state's children.
            if (parentState != null)
            {
                if (state.IsTopState)
                {
                    throw new InvalidOperationException(string.Format("Cannot set '{0}' state as parent state. Use NULL instead.", MartyConstants.TopStateName));
                }

                parentState.AddChild(state.Id);
            }
            else
            {
                if (!state.IsTopState)
                {
                    parentState = this.Top;

                    parentState.AddChild(state.Id);
                }
            }

            // Set state's properties.
            state.StartingState = startingState == null ? default(long?) : startingState.Id;
            state.ParentState = parentState == null ? default(long?) : parentState.Id;
            state.EventHandler = eventHandler;
            state.StateLookupHandler = this.GetState;
            state.ProcessingHandler = this.SetProcessing;
            state.BlockTransitionHandler = this.BlockTransitioning;
        }

        /// <summary>
        /// Registers states
        /// </summary>
        protected abstract void RegisterStates();

        /// <summary>
        /// Registers events
        /// </summary>
        protected abstract void RegisterEvents();

        /// <summary>
        /// Gets starting state for Top state
        /// </summary>
        protected abstract MartyState TopStartingState { get; }

        /// <summary>
        /// Handles events for the Top state
        /// </summary>
        /// <param name="evt">Specifies event</param>
        /// <param name="payload">Specifies payload (if any)</param>
        /// <returns>Returns whether the handler was able to handle event</returns>
        protected virtual bool TopHandler(int evt, object payload)
        {
            // By default, the top state doesn't handle anything unless overwritten.
            //      Note: Start, entry, and exit events are NEVER executed for 'Top' state.
            return false;
        }

        /// <summary>
        /// Changes states during state transition
        /// </summary>
        /// <param name="destinationState">Specifies destination state</param>
        private void ChangeState(MartyState destinationState)
        {
            // Mark that state transition is in progress.
            this.IsTransitioning = true;

            // Initialize old state as current state.
            MartyState oldState = this.CurrentState;

            // Check if current state is null.
            if (this.CurrentState != null)
            {
                this.CurrentState.Exit(destinationState);
            }

            // Set current state to be destination state.
            this.CurrentState = destinationState;

            // Check if starting state of current state is null.
            if (this.CurrentState.StartingState != null)
            {
                // Process start events until the actual starting state has been reached.
                do
                {
                    // Process start event.
                    this.CurrentState.Start();

                    // Set current state's starting state as current state.
                    this.CurrentState = this.GetState(this.CurrentState.StartingState);

                } while (this.CurrentState.StartingState != null);
            }

            // Check if old state is null.
            if (oldState != null)
            {
                // Process entry event if it isn't null.
                this.CurrentState.Enter(oldState);
            }

            // Store that transitioning has ended.
            this.IsTransitioning = false;

            // Run next instruction.
            this.RunNextInstruction();
        }

        /// <summary>
        /// Runs next instruction
        /// </summary>
        private void RunNextInstruction()
        {
            // Check if there are instructions in the queue.
            if (this.instructions == null || this.instructions.Count == 0)
            {
                return;
            }

            // Get the next instruction.
            MartyInstruction instruction = this.instructions.Dequeue();

            // Process instruction.
            switch (instruction.Type)
            {
                // Process event.
                case MartyInstructionTypes.Event:
                    this.CurrentState.ProcessEvent((int)instruction.Value, instruction.Payload);
                    break;

                // Process transition.
                case MartyInstructionTypes.Transition:
                    this.ChangeState(this.GetState((long)instruction.Value));
                    break;

                // Throw an exception if an unknown instruction is specified.
                default:
                    // Throw an exception if the ID-HSM doesn't know how to process an instruction.
                    throw new NotSupportedException(string.Format("The instruction {0} is not supported by the ID-HSM.", Enum.GetName(typeof(MartyInstructionTypes), instruction.Type)));
            }
        }

        /// <summary>
        /// Sets whether the state host is processing a state
        /// </summary>
        /// <param name="isProcessing">Specifies whether an instruction is being processed</param>
        internal void SetProcessing(bool isProcessing)
        {
            // Check if state host is already processing an instruction.
            if (isProcessing && this.IsProcessing)
            {
                // Throw an exception if state host is instructed to process multiple instructions.
                throw new InvalidOperationException("The state host cannot process more than one instruction at a time.");
            }

            // Store whether or not the state host is processing an instruction.
            this.IsProcessing = isProcessing;

            // Check if state host is processing.
            if (!this.IsProcessing)
            {
                // Allow transitions.
                this.AllowTransitions = true;

                // Run next instruction.
                this.RunNextInstruction();
            }
        }

        /// <summary>
        /// Checks a state's starting state
        /// </summary>
        /// <param name="state">Specifies state</param>
        private void CheckStartingState(MartyState state)
        {
            // Check if state is null.
            if (state == null)
            {
                throw new ArgumentNullException("Cannot check the starting state of a null state.");
            }

            // Check if state's starting state is null.
            if (state.StartingState == null)
            {
                // Leave.
                return;
            }

            // Check if state has children.
            if (state.Children != null && state.Children.Count == 0)
            {
                throw new FormatException(string.Format("A state cannot have a starting state if it has no children; State = {0}, Invalid Starting State = {1}.",
                    state.Name,
                    this.GetState(state.StartingState).Name));
            }

            // Check if starting state is one of the states children.
            if (!state.IsChild(state.StartingState))
            {
                throw new FormatException(string.Format("A state cannot have a starting state that isn't one of its children; State = {0}, Invalid Starting State = {1}.",
                    state.Name,
                    this.GetState(state.StartingState).Name));
            }
        }

        /// <summary>
        /// Checks event numbering
        /// </summary>
        private void CheckEventNumbering()
        {
            // Iterate through events.
            for (int i = 0, j = Start; i < this.events.Count; i++, j++)
            {
                // Check current event's numbering.
                if (j != this.events.ElementAt(i))
                {
                    // Throw an exception if current events numbering is incorrect.
                    throw new FormatException(string.Format("Event numbering is incorrect for state host.  Expected: {0}; Found: {1}.", j, i));
                }
            }
        }

        /// <summary>
        /// Blocks transitioning
        /// </summary>
        internal void BlockTransitioning()
        {
            // Disallow trnsitions.
            this.AllowTransitions = false;
        }
    }
}
