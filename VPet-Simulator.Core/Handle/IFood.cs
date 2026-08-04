namespace VPet_Simulator.Core
{
    /// <summary>
    /// </summary>
    public interface IFood
    {
        /// <summary>
        /// EXP
        /// </summary>
        int Exp { get; }

        /// <summary>
        /// Stamina 0-100
        /// </summary>
        double Strength { get; }
        /// <summary>
        /// Hunger
        /// </summary>
        double StrengthFood { get; }
        /// <summary>
        /// Thirst
        /// </summary>
        double StrengthDrink { get; }

        /// <summary>
        /// Mood
        /// </summary>
        double Feeling { get; }

        /// <summary>
        /// Health
        /// </summary>
        double Health { get; }

        /// <summary>
        /// Likeablilty
        /// </summary>
        double Likability { get; }
    }
}
