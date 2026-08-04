using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Panuon.WPF.UI;
using System;
using System.Windows.Threading;
using VPet_Simulator.Core;
using static VPet_Simulator.Core.IGameSave;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// </summary>
public class GameSave_VPet : IGameSave
{
    /// <summary>
    /// </summary>
    [Line(name: "name")]
    public string Name { get; set; }
    /// <summary>
    /// </summary>
    [Line(name: "hostname")]
    public string HostName { get; set; } = "";
    /// <summary>
    /// Money
    /// </summary>
    [Line(Type = LPSConvert.ConvertType.ToFloat, Name = "money")]
    public double Money { get; set; }


    double exp { get; set; }
    /// <summary>
    /// Grade
    /// </summary>
    [Line]
    public int Level { get; set; } = 1;
    /// <summary>
    /// </summary>
    [Line]
    public int LevelMax { get; set; } = 0;
    /// <summary>
    /// EXP
    /// </summary>
    [Line(type: LPSConvert.ConvertType.ToFloat, name: "exp")]
    public double Exp
    {
        get => exp;
        set
        {
            int lun = LevelUpNeed();
            bool islevelup = false;
            bool islevelmaxup = false;
            int BeforeLevel = Level;
            int BeforeLevelMax = LevelMax;
            while (value >= lun)
            {
                islevelup = true;
                value -= lun;
                LikabilityMax += 10;
                if (++Level > 1000 + LevelMax * 100)
                {
                    LevelMax++;
                    islevelmaxup = true;
                    Level = 100 * LevelMax;
                }
                lun = LevelUpNeed();
            }
            exp = value;
            if (islevelup)
            {
                Event_LevelUp?.Invoke(new LevelUpEventArgs()
                {
                    IsLevelMaxUp = islevelmaxup,
                    BeforeLevel = BeforeLevel,
                    BeforeLevelMax = BeforeLevelMax
                });
            }
        }
    }
    public class LevelUpEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        public bool IsLevelUp => true;
        /// <summary>
        /// </summary>
        public bool IsLevelMaxUp { get; set; }
        /// <summary>
        /// </summary>
        public int BeforeLevel { get; set; }
        /// <summary>
        /// </summary>
        public int BeforeLevelMax { get; set; }
    }
    public event Action<LevelUpEventArgs> Event_LevelUp;
    /// <summary>
    /// </summary>
    public double TotalExpGained()
    {
        double totalExp = 0;
        for (int i = 1; i <= LevelMax; i++)
        {
            for (int j = 100 * i + 1; j <= 1000 + 100 * i; j++)
                totalExp += 200 * j - 100;
        }
        totalExp += (Level - 100 * LevelMax) * (200 * (Level - 1) - 100);
        totalExp += Exp;
        return totalExp;
    }
    /// <summary>
    /// </summary>
    public int LevelUpNeed() => 200 * Level - 100;
    /// <summary>
    /// Stamina 0-100
    /// </summary>
    public double Strength { get => strength; set => strength = Math.Min(StrengthMax, Math.Max(0, value)); }

    public double StrengthMax => 100 + (int)(Math.Pow(Level * (1 + LevelMax), 0.75) * 4);

    [Line(Type = LPSConvert.ConvertType.ToFloat, IgnoreCase = true)]
    protected double strength { get; set; }
    /// <summary>
    [Line(Type = LPSConvert.ConvertType.ToFloat, IgnoreCase = true)]
    public double StoreStrength { get; set; }
    /// <summary>
    /// </summary>
    public double ChangeStrength { get; set; } = 0;
    public void StrengthChange(double value)
    {
        ChangeStrength += value;
        Strength += value;
    }
    /// <summary>
    /// Hunger
    /// </summary>
    public double StrengthFood
    {
        get => strengthFood; set
        {
            value = Math.Min(StrengthMax, value);
            if (value <= 0)
            {
                Health += value;
                strengthFood = 0;
            }
            else
                strengthFood = value;
        }
    }
    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    protected double strengthFood { get; set; }
    /// <summary>
    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    public double StoreStrengthFood { get; set; }
    public void StrengthChangeFood(double value)
    {
        ChangeStrengthFood += value;
        StrengthFood += value;
    }
    /// <summary>
    /// </summary>
    public double ChangeStrengthFood { get; set; } = 0;
    /// <summary>
    /// Thirst
    /// </summary>
    public double StrengthDrink
    {
        get => strengthDrink; set
        {
            value = Math.Min(StrengthMax, value);
            if (value <= 0)
            {
                Health += value;
                strengthDrink = 0;
            }
            else
                strengthDrink = value;
        }
    }

    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    protected double strengthDrink { get; set; }
    /// <summary>
    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    public double StoreStrengthDrink { get; set; }
    /// <summary>
    /// </summary>
    public double ChangeStrengthDrink { get; set; } = 0;
    public void StrengthChangeDrink(double value)
    {
        ChangeStrengthDrink += value;
        StrengthDrink += value;
    }
    /// <summary>
    /// Mood
    /// </summary>
    public double Feeling
    {
        get => feeling; set
        {

            value = Math.Min(FeelingMax, value);
            if (value <= 0)
            {
                Health += value / 2;
                Likability += value / 2;
                feeling = 0;
            }
            else
                feeling = value;
        }
    }

    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    protected double feeling { get; set; }
    /// <summary>
    /// </summary>
    public double ChangeFeeling { get; set; } = 0;
    public void FeelingChange(double value)
    {
        ChangeFeeling += value;
        Feeling += value;
    }
    /// <summary>
    /// </summary>
    public double Health { get => health; set => health = Math.Min(100, Math.Max(0, value)); }

    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    protected double health { get; set; }
    /// <summary>
    /// </summary>
    public double Likability
    {
        get => likability; set
        {
            var max = LikabilityMax;
            value = Math.Max(0, value);
            if (value > max)
            {
                likability = max;
                Health += value - max;
            }
            else
                likability = value;
        }
    }

    [Line(Type = LPSConvert.ConvertType.ToFloat)]
    protected double likability { get; set; }

    /// <summary>
    /// </summary>
    public void CleanChange()
    {
        ChangeStrength /= 2;
        ChangeFeeling /= 2;
        ChangeStrengthDrink /= 2;
        ChangeStrengthFood /= 2;
    }
    /// <summary>
    /// </summary>
    public void StoreTake()
    {
        const int t = 10;

        var s = StoreStrength / t;
        StoreStrength -= s;
        if (Math.Abs(StoreStrength) < 1)
            StoreStrength = 0;
        else
            StrengthChange(s);

        s = StoreStrengthDrink / t;
        StoreStrengthDrink -= s;
        if (Math.Abs(StoreStrengthDrink) < 1)
            StoreStrengthDrink = 0;
        else
            StrengthChangeDrink(s);

        s = StoreStrengthFood / t;
        StoreStrengthFood -= s;
        if (Math.Abs(StoreStrengthFood) < 1)
            StoreStrengthFood = 0;
        else
            StrengthChangeFood(s);
    }
    /// <summary>
    /// </summary>
    public void EatFood(IFood food)
    {
        Exp += food.Exp;
        var tmp = food.Strength / 2;
        StrengthChange(tmp);
        StoreStrength += tmp;
        tmp = food.StrengthFood / 2;
        StrengthChangeFood(tmp);
        StoreStrengthFood += tmp;
        tmp = food.StrengthDrink / 2;
        StrengthChangeDrink(tmp);
        StoreStrengthDrink += tmp;
        FeelingChange(food.Feeling);
        Health += food.Health;
        Likability += food.Likability;
    }
    /// <summary>
    /// </summary>
    [Line(name: "mode")]
    public ModeType Mode { get; set; } = ModeType.Nomal;
    [Line]
    public double LikabilityMax { get; set; } = 100;

    public double FeelingMax => 100 + (int)(Math.Pow(Level * (1 + LevelMax), 0.75) * 2);
    /// <summary>
    /// </summary>
    public double ExpBonus { get; set; } = 1;

    /// <summary>
    /// </summary>
    public ModeType CalMode()
    {
        int realhel = 60 - (Feeling / FeelingMax >= 80 ? 12 : 0) - (Likability >= 80 ? 12 : (Likability >= 40 ? 6 : 0));
        if (Health <= realhel)
        {
            if (Health <= realhel / 2)
            {
                return ModeType.Ill;
            }
            else
            {
                return ModeType.PoorCondition;
            }
        }
        double realfel = .90 - (Likability >= 80 ? .20 : (Likability >= 40 ? .10 : 0));
        double felps = Feeling / FeelingMax;
        if (felps >= realfel)
        {
            return ModeType.Happy;
        }
        else if (felps <= realfel / 2)
        {
            return ModeType.PoorCondition;
        }
        return ModeType.Nomal;
    }
    /// <summary>
    /// </summary>
    public GameSave_VPet(string name)
    {
        Name = name;
        Money = 100;
        Exp = 0;
        Strength = 100;
        StrengthFood = 100;
        StrengthDrink = 100;
        Feeling = 60;
        Health = 100;
        Likability = 0;
        Mode = CalMode();
    }
    /// <summary>
    /// </summary>
    public GameSave_VPet()
    {
    }
    /// <summary>
    /// </summary>
    public static GameSave_VPet Load(ILine data) => LPSConvert.DeserializeObject<GameSave_VPet>(data);
    /// <summary>
    /// </summary>
    public Line ToLine()
    {
        //Line save = new Line("vpet", Name);
        //save.SetFloat("money", Money);
        //save.SetInt("exp", Exp);
        //save.SetFloat("strength", Strength);
        //save.SetFloat("strengthdrink", StrengthDrink);
        //save.SetFloat("strengthfood", StrengthFood);
        //save.SetFloat("feeling", Feeling);
        //save.SetFloat("health", Health);
        //save.SetFloat("Likability", Likability);
        return LPSConvert.SerializeObject(this, "vpet");
    }

}
