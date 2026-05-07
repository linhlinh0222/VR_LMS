namespace MaritimeLMS.Bridge
{
    /// <summary>
    /// Source of compass-style heading in degrees (0-360, clockwise from North).
    /// </summary>
    /// <remarks>
    /// Port abstraction so navigation instruments (compass, AIS, ECDIS) decouple from
    /// the concrete heading source. Swap implementations without touching equipment code:
    /// camera yaw for desktop sim, integrated helm input for realistic steering, or a
    /// gyro feed in production. Follows DNV-ST-0033 simulator interoperability guidance.
    /// </remarks>
    public interface IHeadingProvider
    {
        /// <summary>Current heading in degrees [0, 360).</summary>
        float HeadingDegrees { get; }
    }
}
