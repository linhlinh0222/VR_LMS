namespace MaritimeLMS.Bridge
{
    /// <summary>
    /// Source of rudder steering demand normalized to [-1, +1].
    /// </summary>
    /// <remarks>
    /// Port abstraction so heading providers and ship controllers can read
    /// rudder demand without depending on a specific helm input device (mouse-
    /// driven helm wheel, keyboard fallback, autopilot, recorded session).
    /// Convention: -1 = hard a-port, 0 = midships, +1 = hard a-starboard.
    /// Matches IMO/COLREG steering vocabulary.
    /// </remarks>
    public interface IRudderInput
    {
        /// <summary>Rudder demand in [-1, +1]. -1 = hard a-port, +1 = hard a-starboard.</summary>
        float RudderNormalized { get; }
    }
}
