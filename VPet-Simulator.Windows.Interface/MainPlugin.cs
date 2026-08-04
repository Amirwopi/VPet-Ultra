namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// </summary>
    public abstract class MainPlugin
    {
        /// <summary>
        /// </summary>
        public abstract string PluginName { get; }
        /// <summary>
        /// </summary>
        public IMainWindow MW;
        /// <summary>
        /// </summary>
        public MainPlugin(IMainWindow mainwin)
        {
            MW = mainwin;
        }
        ///// <summary>//TODO
        ///// </summary>
        /// <param name="Theme">Theme</param>
        //public virtual void LoadTheme(Theme theme) { }
        /// <summary>
        /// </summary>
        public virtual void LoadPlugin() { }
        /// <summary>
        /// </summary>
        public virtual void GameLoaded() { }

        /// <summary>
        /// </summary>
        public virtual void EndGame() { }

        /// <summary>
        /// </summary>
        public virtual void Save() { }

        /// <summary>
        /// </summary>
        public virtual void Setting() { }
        /// <summary>
        /// </summary>
        public virtual void LoadDIY() { }
    }
}
