using System.ComponentModel;
using System.Configuration;

namespace Dicom_Print_SCU.Configuration
{
    public class AeTitleConfigElement : ConfigurationElement, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [ConfigurationProperty("displayName", IsRequired = true, IsKey = true)]
        public string DisplayName
        {
            get
            {
                return this["displayName"] as string;
            }
            set
            {
                this["displayName"] = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("displayName"));
            }
        }

        [ConfigurationProperty("localAeTitle", IsRequired = false, IsKey = true)]
        public string LocalAeTitle
        {
            get
            {
                return this["localAeTitle"] as string;
            }
            set
            {
                this["localAeTitle"] = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("localAeTitle"));
            }
        }

        [ConfigurationProperty("remoteAeTitle", IsRequired = true, IsKey = true)]
        public string RemoteAeTitle
        {
            get
            {
                return this["remoteAeTitle"] as string;
            }
            set
            {
                this["remoteAeTitle"] = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("remoteAeTitle"));
            }
        }

        [ConfigurationProperty("remoteIpAddress", IsRequired = true, IsKey = true)]
        public string RemoteIpAddress
        {
            get
            {
                return this["remoteIpAddress"] as string;
            }
            set
            {
                this["remoteIpAddress"] = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("remoteIpAddress"));
            }
        }

        [ConfigurationProperty("remotePort", IsRequired = true, IsKey = true)]
        public string RemotePort
        {
            get
            {
                return this["remotePort"] as string;
            }
            set
            {
                this["remotePort"] = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("remotePort"));
            }
        }

        [ConfigurationProperty("staticReference", IsRequired = false, IsKey = true)]
        public string StaticReference
        {
            get
            {
                return this["staticReference"] as string;
            }
            set
            {
                this["staticReference"] = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("staticReference"));
            }
        }
    }
}
