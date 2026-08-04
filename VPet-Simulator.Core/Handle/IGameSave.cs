using LinePutScript;

namespace VPet_Simulator.Core
{
    /// <summary>
    /// </summary>
    public interface IGameSave
    {
        /// <summary>
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// </summary>
        string HostName { get; set; }
        /// <summary>
        /// Money
        /// </summary>
        double Money { get; set; }
        /// <summary>
        /// EXP
        /// </summary>
        double Exp { get; set; }
        /// <summary>
        /// </summary>
        double ExpBonus { get; }
        /// <summary>
        /// Grade
        /// </summary>
        int Level { get; }
        /// <summary>
        /// </summary>
        /// <returns></returns>
        int LevelUpNeed();
        /// <summary>
        /// Stamina 0-100
        /// </summary>
        double Strength { get; set; }
        /// <summary>
        /// </summary>
        double StrengthMax { get; }
        /// <summary>
        double StoreStrength { get; set; }
        /// <summary>
        /// </summary>
        double ChangeStrength { get; set; }
        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        void StrengthChange(double value);
        /// <summary>
        /// Hunger
        /// </summary>
        double StrengthFood { get; set; }
        /// <summary>
        double StoreStrengthFood { get; set; }
        void StrengthChangeFood(double value);
        /// <summary>
        /// </summary>
        double ChangeStrengthFood { get; set; }
        /// <summary>
        /// Thirst
        /// </summary>
        double StrengthDrink { get; set; }

        /// <summary>
        double StoreStrengthDrink { get; set; }
        /// <summary>
        /// </summary>
        double ChangeStrengthDrink { get; set; }
        /// <summary>
        /// </summary>
        void StrengthChangeDrink(double value);
        /// <summary>
        /// </summary>
        void FeelingChange(double value);
        /// <summary>
        /// </summary>
        double ChangeFeeling { get; set; }
        /// <summary>
        /// Mood
        /// </summary>
        double Feeling { get; set; }
        /// <summary>
        /// </summary>
        double FeelingMax { get; }

        /// <summary>
        /// </summary>
        double Health { get; set; }

        /// <summary>
        /// </summary>
        double Likability { get; set; }
        /// <summary>
        /// </summary>
        double LikabilityMax { get; }

        /// <summary>
        /// </summary>
        void CleanChange();
        /// <summary>
        /// </summary>
        void StoreTake();
        /// <summary>
        /// </summary>
        void EatFood(IFood food);
        /// <summary>
        /// </summary>
        ModeType Mode { get; set; }
        /// <summary>
        /// </summary>
        public enum ModeType
        {
            /// <summary>
            /// </summary>
            Happy,
            /// <summary>
            /// </summary>
            Nomal,
            /// <summary>
            /// </summary>
            PoorCondition,
            /// <summary>
            /// </summary>
            Ill
        }

        /// <summary>
        /// </summary>
        ModeType CalMode();

        /// <summary>
        /// </summary>
        Line ToLine();
    }
}
