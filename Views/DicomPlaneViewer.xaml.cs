using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Models;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Wpf;
using ScottPlot.Statistics;
using ViewModels;
using Views.Graphics;

namespace Views
{
    /// <summary>
    /// Interaction logic for DicomPlaneViewer.xaml
    /// </summary>
    public partial class DicomPlaneViewer : UserControl
    {
        private DicomScene? glState;
        private bool shouldAskForPixels = false;
        private DicomViewModel? viewModel;

        public DicomPlaneViewer()
        {
            InitializeComponent();
        }

        public DicomViewModel? ViewModel
        {
            get => viewModel;

            set
            {
                if (viewModel is not null)
                    viewModel.PropertyChanged -= ViewModel_PropertyChanged;

                if (value is not null)
                    value.PropertyChanged += ViewModel_PropertyChanged;

                DataContext = value;
                viewModel = value;

                glState?.LoadDicomSeries(viewModel!.Series);
            }
        }

        public IGraphicsContext InitOpenGL(GLWpfControlSettings settings)
        {
            openTkControl.Start(settings);

            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);

            GL.DebugMessageCallback((source, type, id, severity, length, message, userParam) =>
            {
                Debug.WriteLine($"OpenGL Debug: {Marshal.PtrToStringAnsi(message)}");
            }, IntPtr.Zero);

            return openTkControl.Context!;
        }

        public void LoadScene(DicomScene? dicomScene = null)
        {
            glState = dicomScene is null ? new() : dicomScene;
        }

        private void DrawHistogram()
        {
            //var histogram = Histogram.WithBinCount(ViewModel!.IntervalsNumber, ViewModel.PixelsForAnalysis.Select())
            //histogramPlot.Plot.Add.Histogram()
        }

        private void OpenTkControl_Render(TimeSpan obj)
        {
            openTkControl.Context?.MakeCurrent();
            GL.ClearColor(0.3f, 0.6f, 0.2f, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            glState!.UseLUT = viewModel!.UseLUT;

            glState?.DrawVertices(viewModel!.DisplayedPlane, viewModel.CurrentSpaceSlice);
            if (shouldAskForPixels && ViewModel is not null)
            {
                var initialUseLut = glState!.UseLUT;
                glState.UseLUT = false;

                var rgb = glState!.GetColorPixels(viewModel.DisplayedPlane, viewModel.CurrentSpaceSlice).Select(rgb=>rgb.R);
                var monochrome = glState!.GetMonochromePixels(viewModel.DisplayedPlane, viewModel.CurrentSpaceSlice);

                //glState.UseLUT = true;


                glState.UseLUT = initialUseLut;
                //ViewModel.CurrentSliceRGB = glState!.GetColorPixels(viewModel.DisplayedPlane, viewModel.CurrentSpaceSlice).ToImmutableArray();
                shouldAskForPixels = false;
            }
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            var vm = (DicomViewModel) sender!;
            if (e.PropertyName == nameof(vm.Series))
            {
                glState?.LoadDicomSeries(vm.Series);
            }
            if (e.PropertyName == nameof(viewModel.Lut) && vm.Lut is not null)
            {
                glState?.UploadLUT(vm.Lut.Values);
            }
            if (vm.IsHistogramShown)
                shouldAskForPixels = true;

            if (e.PropertyName == nameof(vm.CurrentSliceRGB))
            {
                DrawHistogram();
            }
        }
    }
}