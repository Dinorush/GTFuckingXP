using System.Collections.Generic;
using System.Text.Json;

namespace GTFuckingXP.Information.ClassSelector
{
    /// <summary>
    /// An information class containing group name and an id for putting different classes into different headers of the class selector.
    /// </summary>
    public class Group
    {

        /// <summary>
        /// Gets or sets the single existing key for this <see cref="Group"/>.
        /// </summary>
        public int PersistentId { get; set; }

        /// <summary>
        /// Gets or sets all playercounts that this header is visible.
        /// </summary>
        public List<int> VisibleForPlayerCount { get; set; }

        /// <summary>
        /// Allows the class to be used for player counts > 4 if 4 is allowed.
        /// </summary>
        /// 
        public bool ExpandAboveFourCount { get; set; } = true;

        /// <summary>
        /// Gets or sets the name this group should run under.
        /// </summary>
        public string Name { get; set; }
    }
}
