using System.ComponentModel;
using System.Configuration;

namespace Dicom_Print_SCU.Configuration
{
    public class AeTitleConfigSection : ConfigurationSection, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [ConfigurationProperty("", IsRequired = true, IsDefaultCollection = true)]
        public AeTitleConfigCollection AeTitleCollection
        {
            get { return (AeTitleConfigCollection)this[""]; }
            set
            {
                this[""] = value;
                PropertyChanged(this, new PropertyChangedEventArgs(null));
            }
        }
    }
}
