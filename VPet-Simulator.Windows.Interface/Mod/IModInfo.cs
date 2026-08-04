using LinePutScript.Converter;
using LinePutScript.Dictionary;
using LinePutScript.Localization.WPF;
using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using VPet_Simulator.Core;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// </summary>
public interface IModInfo
{
    /// <summary>
    /// </summary>
    public string Name { get; }
    /// <summary>
    /// </summary>
    public string Author { get; }
    /// <summary>
    /// </summary>
    public long AuthorID { get; }
    /// <summary>
    /// </summary>
    public ulong ItemID { get; }
    /// <summary>
    /// </summary>
    public string Intro { get; }
    /// <summary>
    /// </summary>
    public DirectoryInfo Path { get; }
    /// <summary>
    /// Game version 
    /// </summary>
    public int GameVer { get; }
    /// <summary>
    /// </summary>
    public int Ver { get; }
    /// <summary>
    /// </summary>
    public HashSet<string> Tag { get; }
}
