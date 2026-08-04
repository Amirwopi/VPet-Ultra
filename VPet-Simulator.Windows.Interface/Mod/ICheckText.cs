using LinePutScript.Converter;
using System;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// </summary>
    public abstract class ICheckText : IText
    {
        [Line(ignoreCase: true)]
        public int mode { get; set; } = 7;
        /// <summary>
        /// </summary>      
        public ModeType Mode
        {
            get => (ModeType)mode;
            set => mode = (int)value;
        }
        /// <summary>
        /// </summary>
        [Flags]
        public enum ModeType
        {
            /// <summary>
            /// </summary>
            Happy = 1,
            /// <summary>
            /// </summary>
            Nomal = 2,
            /// <summary>
            /// </summary>
            PoorCondition = 4,
            /// <summary>
            /// </summary>
            Ill = 8
        }

        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)]
        public double LikeMin { get; set; } = 0;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)]
        public double LikeMax { get; set; } = int.MaxValue;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)]
        public double HealthMin { get; set; } = 0;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)]
        public double HealthMax { get; set; } = int.MaxValue;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double LevelMin { get; set; } = 0;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double LevelMax { get; set; } = int.MaxValue;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double MoneyMin { get; set; } = int.MinValue;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double MoneyMax { get; set; } = int.MaxValue;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double FoodMin { get; set; } = 0;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double FoodMax { get; set; } = int.MaxValue;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double DrinkMin { get; set; } = 0;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double DrinkMax { get; set; } = int.MaxValue;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double FeelMin { get; set; } = 0;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double FeelMax { get; set; } = int.MaxValue;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double StrengthMin { get; set; } = 0;
        /// <summary>
        /// </summary>
        [Line(IgnoreCase = true)] public double StrengthMax { get; set; } = int.MaxValue;

        /// <summary>
        public virtual bool CheckState(IGameSave save)
        {
            if (save.Likability < LikeMin || save.Likability > LikeMax)
                return false;
            if (save.Health < HealthMin || save.Health > HealthMax)
                return false;
            if (save.Level < LevelMin || save.Level > LevelMax)
                return false;
            if (save.Money < MoneyMin || save.Money > MoneyMax)
                return false;
            if (save.StrengthFood < FoodMin || save.StrengthFood > FoodMax)
                return false;
            if (save.StrengthDrink < DrinkMin || save.StrengthDrink > DrinkMax)
                return false;
            if (save.Feeling < FeelMin || save.Feeling > FeelMax)
                return false;
            if (save.Strength < StrengthMin || save.Strength > StrengthMax)
                return false;
            return true;
        }
        /// <summary>
        public virtual bool CheckState(Main m) => CheckState(m.Core.Save);
    }
}
