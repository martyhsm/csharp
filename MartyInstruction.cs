using System;

namespace Marty
{
    /// <summary>
    /// Defines MartyInstruction class
    /// </summary>
    internal class MartyInstruction
    {
        /// <summary>
        /// Gets or sets type
        /// </summary>
        internal MartyInstructionTypes Type { get; set; }

        /// <summary>
        /// Gets or sets value
        /// </summary>
        internal object Value { get; set; }

        /// <summary>
        /// Gets or sets payload
        /// </summary>
        internal object Payload { get; set; }

        /// <summary>
        /// Constructs an MartyInstruction object
        /// </summary>
        /// <param name="instructionTypes">Specifies type of instruction</param>
        /// <param name="value">Specifies value</param>
        /// <param name="payload">Specifies paylaod (if any)</param>
        internal MartyInstruction(MartyInstructionTypes instructionTypes, object value, object payload)
        {
            // Initialize properties.
            this.Type = instructionTypes;
            this.Value = value;
            this.Payload = payload;

            // Check if value is null.
            if (value == null)
            {
                // Throw an exception if value is null.
                throw new ArgumentNullException("An instruction's value cannot be NULL.");
            }
        }
    }
}
