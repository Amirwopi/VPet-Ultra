using LinePutScript.Converter;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// </summary>
    public class LowText : IText
    {
        /// <summary>
        /// </summary>
        public enum ModeType
        {
            /// <summary>
            /// </summary>
            H,
            /// <summary>
            /// </summary>
            L,
        }
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public ModeType Mode { get; set; } = ModeType.L;
        /// <summary>
        /// Stamina
        /// </summary>
        public enum StrengthType
        {
            /// <summary>
            /// </summary>
            L,
            /// <summary>
            /// </summary>
            M,
            /// <summary>
            /// </summary>
            S,
        }
        /// <summary>
        /// Stamina
        /// </summary>
        [Line(IgnoreCase = true)] public StrengthType Strength { get; set; } = StrengthType.S;
        /// <summary>
        /// </summary>
        public enum LikeType
        {
            /// <summary>
            /// </summary>
            N,
            /// <summary>
            /// </summary>
            S,
            /// <summary>
            /// </summary>
            M,
            /// <summary>
            /// </summary>
            L,
        }
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public LikeType Like { get; set; } = LikeType.N;
    }
}
