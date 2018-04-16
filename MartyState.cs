using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Marty
{
    /// <summary>
    /// Defines MartyState class
    /// </summary>
    public class MartyState
    {
        /// <summary>
        /// Gets or sets name
        /// </summary>
        internal string Name { get; set; }

        /// <summary>
        /// Gets ID
        /// </summary>
        internal long Id { get; private set; }

        /// <summary>
        /// Gets or sets parent
        /// </summary>
        internal long? ParentState { get; set; }

        /// <summary>
        /// Stores children
        /// </summary>
        private IList<long> children = new List<long>();

        /// <summary>
        /// Gets children
        /// </summary>
        internal IList<long> Children
        {
            get
            {
                return this.children;
            }
        }

        /// <summary>
        /// Gets or sets starting state
        /// </summary>
        internal long? StartingState { get; set; }

        /// <summary>
        /// Gets or sets event handler
        /// </summary>
        internal EventHandler EventHandler { get; set; }

        /// <summary>
        /// Gets or sets processing handler
        /// </summary>
        internal ProcessingHandler ProcessingHandler { get; set; }

        /// <summary>
        /// Gets or sets block transition handler
        /// </summary>
        internal BlockTransitionHandler BlockTransitionHandler { get; set; }

        /// <summary>
        /// Gets or sets state lookup handler
        /// </summary>
        internal StateLookupHandler StateLookupHandler { get; set; }

        /// <summary>
        /// Gets a value indicating whether state is the top state
        /// </summary>
        internal bool IsTopState
        {
            get
            {
                return this.Name.Equals(MartyConstants.TopStateName, StringComparison.CurrentCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        private MartyState()
        {
            throw new InvalidOperationException(string.Format("Cannot instantiate {0} class using default constructor.", "State"));
        }

        /// <summary>
        /// Instantiates a state
        /// </summary>
        /// <param name="name">Specifies name</param>
        public MartyState(string name)
        {
            // Check state's name.
            if (!Regex.IsMatch(name, MartyConstants.StateNameRegex, RegexOptions.IgnoreCase))
            {
                throw new InvalidOperationException(string.Format("A valid state's name must match the following regex: {0}", MartyConstants.StateNameRegex));
            }

            // Initialize properties.
            this.Name = name;
            this.Id = MartyStateIdGenerator.Instance.GetId();
            this.children = new List<long>();
            this.EventHandler = null;
            this.ProcessingHandler = null;
            this.BlockTransitionHandler = null;
            this.StateLookupHandler = null;
            this.ParentState = null;
            this.StartingState = null;
        }

        internal void AddChild(long state)
        {
            if (children.Any(child => child == state))
            {
                throw new InvalidOperationException(string.Format("Cannot add duplicate child", this.StateLookupHandler(state).Name));
            }

            this.children.Add(state);
        }

        internal bool IsSibling(MartyState state)
        {
            if (this.Id == state.Id)
            {
                return false;
            }

            return this.ParentState == state.ParentState;
        }

        internal bool IsAncestor(MartyState state)
        {
            return !state.IsTopState && state.IsDescendant(this);
        }

        internal bool IsDescendant(MartyState state)
        {
            if (this.IsChild(state))
            {
                return true;
            }

            return this.children.Any(child => this.StateLookupHandler(child).IsDescendant(state));
        }

        internal bool IsChild(MartyState state)
        {
            return this.children.Any(child => child == state.Id);
        }

        internal bool IsChild(long? stateId)
        {
            return this.IsChild(this.StateLookupHandler(stateId));
        }
        
        /// <summary>
        /// Processes start event
        /// </summary>
        internal void Start()
        {
            // Start event processing.
            this.ProcessingHandler(true);

            // Disable transitioning.
            this.BlockTransitionHandler();

            // Process start event.
            this.EventHandler(MartyBase.Start, null);

            // Stop event processing.
            this.ProcessingHandler(false);
        }

        /// <summary>
        /// Performs entry action
        /// </summary>
        /// <param name="source">Specifies source state</param>
        internal void Enter(MartyState source)
        {
            // Mark that the event is being processed.
            this.ProcessingHandler(true);

            MartyState parentState = this.StateLookupHandler(this.ParentState);
                
            // Check if parent's entry event needs processing.
            if (!(null == parentState || parentState.IsTopState || source.IsAncestor(parentState) || this.IsDescendant(source)))
            {
                // Process parent's entry event.
                parentState.EventHandler(MartyBase.Entry, null);
            }

            // Check if source is NOT a descendant.
            if (!this.IsDescendant(source))
            {
                // Process
                this.EventHandler(MartyBase.Entry, null);
            }

            // Release processing.
            this.ProcessingHandler(false);
        }

        /// <summary>
        /// Processes an event
        /// </summary>
        /// <param name="evt">Specifies event</param>
        /// <param name="payload">Specifies payload</param>
        internal void ProcessEvent(int evt, object payload)
        {
            // Check if this state handled the event.
            if (!this.EventHandler(evt, payload))
            {
                // Check if this state has a parent.
                if (null != this.ParentState)
                {
                    this.StateLookupHandler(this.ParentState).EventHandler(evt, payload);
                }
            }
        }

        /// <summary>
        /// Process exit event
        /// </summary>
        /// <param name="destinationState">Specifies destination state</param>
        internal void Exit(MartyState destinationState)
        {
            // Start event processing.
            this.ProcessingHandler(true);

            // Block transitioning.
            this.BlockTransitionHandler();

            MartyState parentState = this.StateLookupHandler(this.ParentState);

            // Check if destination state is a descendant of this state.
            if (!this.IsDescendant(destinationState))
            {
                // Process this state's exit event.
                this.EventHandler(MartyBase.Exit, null);

                // Check if this state's parent needs to process its exit event.
                if (!(this.ParentState == null || parentState.IsTopState || this.IsSibling(destinationState)))
                {
                    // Process parent's exit event.
                    parentState.EventHandler(MartyBase.Exit, null);
                }
            }

            // Stop event processing.
            this.ProcessingHandler(false);
        }
    }
}
