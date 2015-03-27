using System.Configuration;
using Dicom_Print_SCU.Configuration;

namespace Dicom_Print_SCU
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        internal AeTitleConfigSection AETitleConfigSection;

        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            LoadConfigSettings();
        }

        private void LoadConfigSettings()
        {
            AETitleConfigSection = (AeTitleConfigSection)ConfigurationManager.GetSection("endpointDetails");
        }
    }
}
