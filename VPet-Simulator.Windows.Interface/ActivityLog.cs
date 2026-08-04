using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// </summary>
public class ActivityLog
{
    /// <summary>
    /// </summary>
    public ActivityLog()
    {
    }
    /// <summary>
    /// </summary>
    public ActivityLog(string type, bool isDebug = false, params string[] description)
    {
        Time = DateTime.Now;
        Type = type;
        Description = string.Join('|', description);
        IsDebug = isDebug;
    }
    /// <summary>
    /// </summary>
    public ActivityLog(string type, params string[] description)
    {
        Time = DateTime.Now;
        Type = type;
        Description = string.Join('|', description);
        IsDebug = false;
    }
    /// <summary>
    /// </summary>
    [Line]
    public DateTime Time { get; set; }
    /// <summary>
    /// </summary>
    [Line]
    public string Type { get; set; }
    /// <summary>
    /// </summary>
    [Line]
    public string Description { get; set; }

    /// <summary>
    /// </summary>
    [Line] public bool IsDebug { get; set; }

    /// <summary>
    /// </summary>
    public string ToString(Main m)
    {
        return $"[{Time.ToShortTimeString()}] {string.Format(IText.ConverText(("al_" + Type).Translate(), m), Description.Split('|'))}";
    }
}
