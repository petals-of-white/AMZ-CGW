using System.Windows;
using Models;
using OpenTK.Windowing.Common;
using OpenTK.Wpf;
using Views.Graphics;

namespace Views
{
    /// <summary>
    /// Interaction logic for MainWindows.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var settings = new GLWpfControlSettings()
            {
                MajorVersion = 4,
                MinorVersion = 6,
                ContextFlags = ContextFlags.Debug,
                Profile = ContextProfile.Compatability
            };

            IGraphicsContext glContext = axialViewer.InitOpenGL(settings);

            settings.ContextToUse = glContext;

            sagittalViewer.InitOpenGL(settings);

            coronalViewer.InitOpenGL(settings);

            glContext.MakeCurrent();
            DicomScene dicomScene = new();

            axialViewer.LoadScene(dicomScene);
            sagittalViewer.LoadScene(dicomScene);
            coronalViewer.LoadScene(dicomScene);


        }

        private void openDICOMbtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog() { Multiselect = true };
            if (dialog.ShowDialog() == true)
            {
                string [] files = dialog.FileNames;
                var dicomData = DicomSeries.FromFiles(files);
                axialViewer.ViewModel = new ViewModels.DicomViewModel(dicomData) { DisplayedPlane = AnatomicPlane.Axial };
                sagittalViewer.ViewModel = new ViewModels.DicomViewModel(dicomData) { DisplayedPlane = AnatomicPlane.Sagittal };
                coronalViewer.ViewModel = new ViewModels.DicomViewModel(dicomData) { DisplayedPlane = AnatomicPlane.Coronal };

                var lutList = LookupTable.LoadLUTsFromCSV("LUT.csv");
                var goodLut = lutList.Find(lut => lut.Name == "GE-Rainbow");
                axialViewer.ViewModel!.Lut = goodLut;
                sagittalViewer.ViewModel!.Lut = goodLut;
                coronalViewer.ViewModel!.Lut = goodLut;
            }
        }
    }
}