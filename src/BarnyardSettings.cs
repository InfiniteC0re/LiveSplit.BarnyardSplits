using LiveSplit.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.BarnyardSplits
{
    public partial class BarnyardSettings : UserControl
    {
        public BarnyardSettings()
        {
            InitializeComponent();
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            var parent = document.CreateElement("Settings");
            CreateSettingsNode(document, parent);
            return parent;
        }

        public int GetSettingsHashCode()
        {
            return CreateSettingsNode(null, null);
        }

        private int CreateSettingsNode(XmlDocument document, XmlElement parent)
        {
            return SettingsHelper.CreateSetting(document, parent, "Version", "1.0");
            //return SettingsHelper.CreateSetting(document, parent, "Version", "1.0") ^
            //    SettingsHelper.CreateSetting(document, parent, "Accuracy", Accuracy) ^
            //    SettingsHelper.CreateSetting(document, parent, "Display2Rows", Display2Rows);
        }

        public void SetSettings(XmlNode node)
        {
            //var element = (XmlElement)node;
            //Accuracy = SettingsHelper.ParseEnum<ResetChanceAccuracy>(element["Accuracy"]);
            //Display2Rows = SettingsHelper.ParseBool(element["Display2Rows"], false);
        }
    }
}
