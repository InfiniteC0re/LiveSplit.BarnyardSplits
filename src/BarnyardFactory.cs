using LiveSplit.Model;
using System;

namespace LiveSplit.UI.Components
{

    public class BarnyardFactory : IComponentFactory
    {
        // The displayed name of the component in the Layout Editor.
        public string ComponentName => "Barnyard Autosplitter";

        public string Description => "Enables auto splits in Barnyard the video game. Requires BYSpeedrunHelper mod.";

        // The sub-menu this component will appear under when adding the component to the layout.
        public ComponentCategory Category => ComponentCategory.Control;

        public IComponent Create(LiveSplitState state) => new BarnyardComponent(state);

        public string UpdateName => ComponentName;

        // Fill in this empty string with the URL of the repository where your component is hosted.
        // This should be the raw content version of the repository. If you're not uploading this
        // to GitHub or somewhere, you can ignore this.
        public string UpdateURL => "https://storage.opentoshi.net/BarnyardSplits/";

        // Fill in this empty string with the path of the XML file containing update information.
        // Check other LiveSplit components for examples of this. If you're not uploading this to
        // GitHub or somewhere, you can ignore this.
        public string XMLURL => UpdateURL + "updates.xml";

        public Version Version => Version.Parse("1.0.0");
    }
}