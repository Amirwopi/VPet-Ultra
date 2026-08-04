using LinePutScript;
using LinePutScript.Converter;
using LinePutScript.Localization.WPF;
using Panuon.WPF;
using Panuon.WPF.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// </summary>
public class Item : NotifyPropertyChangedBase
{
    /// <summary>
    /// </summary>
    public static Item CreateItem(IMainWindow imw, ILine data)
    {
        if (Creators.ContainsKey(data[(gstr)"itemtype"]))
        {
            return Creators[data[(gstr)"itemtype"]](imw, data);
        }
        else
        {
            return LPSConvert.DeserializeObject<Item>(data);
        }
    }
    /// <summary>
    /// </summary>
    public static Dictionary<string, Func<IMainWindow, ILine, Item>> Creators = new()
    {
        { "Food", (_,line) => { return LPSConvert.DeserializeObject<Food>(line); } },
    };
    /// <summary>
    /// </summary>
    public static Dictionary<string, List<Func<IMainWindow, Item, bool>>> UseAction = new();
    /// <summary>
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual string Image { get; set; } = null;
    /// <summary>
    /// </summary>
    public virtual void Use(IMainWindow imw)
    {
        if (UseAction.ContainsKey(ItemType))
        {
            foreach (var action in UseAction[ItemType])
            {
                if (action(imw, this))
                    return;
            }
            return;
        }
        MessageBoxX.Show("Item {0} failed to use".Translate(TranslateName), "This item cannot be used".Translate());
    }
    /// <summary>
    /// </summary>
    public virtual void Consume(IMainWindow imw, int count = 1)
    {
        Count -= count;
        if (Count <= 0)
        {
            imw.Items.Remove(this);
        }
    }

    /// <summary>
    /// </summary>
    [Line(name: "name")]
    public string Name { get; set; }
    private string transname = null;
    private string transdesc = null;
    /// <summary>
    /// </summary>
    public string TranslateName
    {
        get
        {
            if (transname == null)
            {
                transname = LocalizeCore.Translate(Name);
            }
            return transname;
        }
    }

    /// <summary>
    /// </summary>
    [Line(name: "itemtype")]
    public virtual string ItemType { get; set; } = "Item";
    /// <summary>
    /// </summary>

    public virtual string Description
    {
        get
        {
            if (transdesc == null)
            {
                transdesc = LocalizeCore.Translate(Desc);
            }
            return transdesc;
        }
    }

    /// <summary>
    /// </summary>

    public static List<string> ItemTypes = new List<string>()
    {
        "Item",
        "Food",
        "Tool",
        "Toy",
        "Mail",
    };

    /// <summary>
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual double Price { get; set; }
    /// <summary>
    /// descriptions
    /// </summary>
    [Line(ignoreCase: true)]
    public string Desc { get; set; }

    /// <summary>
    /// </summary>
    public virtual BitmapImage ImageSource { get; set; }

    /// <summary>
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual int Count { get; set; } = 1;
    /// <summary>
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual string Data { get; set; } = "";
    /// <summary>
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual bool CanUse { get; set; } = true;
    /// <summary>
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual bool Star { get; set; } = false;
    /// <summary>
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual bool IsSingle { get; set; } = false;

    /// <summary>
    /// </summary>
    [Line(ignoreCase: true)]
    public virtual bool Visibility { get; set; } = true;
    /// <summary>
    /// </summary>
    public virtual void LoadSource(IMainWindow imw)
    {
        ImageSource = imw.ImageSources.FindImage(ItemType + "_" + (Image ?? Name), "food");
    }
}
