namespace MaritimeLMS.Telegraph
{
    /// <summary>
    /// Standard engine order telegraph positions ordered astern → ahead.
    /// Underlying integer values are intentionally sequential so the enum doubles
    /// as an index into <see cref="EngineOrderTelegraph.PositionAngles"/>.
    /// </summary>
    /// <remarks>
    /// Layout follows the IMO/SOLAS V/19 EOT convention used in cadet training
    /// and STCW 2010 Bridge Resource Management courses.
    /// </remarks>
    public enum TelegraphPosition
    {
        FullAstern = 0,
        HalfAstern = 1,
        SlowAstern = 2,
        Stop = 3,
        SlowAhead = 4,
        HalfAhead = 5,
        FullAhead = 6,
    }
}
