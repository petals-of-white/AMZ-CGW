using System.Collections.Immutable;
using System.Drawing;
using System.Runtime.ConstrainedExecution;
using CommunityToolkit.Mvvm.ComponentModel;
using FellowOakDicom.Imaging;
using Models;

namespace ViewModels;

public partial class DicomViewModel : ObservableObject
{
    [ObservableProperty]
    private ImmutableArray<RGB<byte>> currentSliceRGB;

    [ObservableProperty]
    private ImmutableArray<ushort> currentSliceMonochrome;

    //[ObservableProperty]
    private uint currentSpaceSlice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplaySize))]
    private AnatomicPlane displayedPlane;

    [ObservableProperty]
    private bool isHistogramShown;

    [ObservableProperty]
    private LookupTable? lut;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplaySize))]
    private DicomSeries series;

    [ObservableProperty]
    private bool useLUT;

    public DicomViewModel(DicomSeries dicomSlices)
    {
        series = dicomSlices;
    }

    public uint CurrentSpaceSlice
    {
        get => currentSpaceSlice; set
        {
            if (value < NumberOfSpacePositions)
            {
                SetProperty(ref currentSpaceSlice, value);
            }
        }
    }

    public CoordsPixelLength DisplaySize => new(Series, DisplayedPlane);

    public int IntervalsNumber => (int) Math.Ceiling(1 + Math.Log2(PixelsForAnalysis.Count()));
    public int NumberOfSpacePositions => (int) DisplaySize.ZPixels;
    public IEnumerable<RGB<byte>> PixelsForAnalysis => CurrentSliceRGB.Where(rgb => rgb.R < rgb.G && rgb.R < rgb.B);
}