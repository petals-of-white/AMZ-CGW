using FellowOakDicom;
using FellowOakDicom.Imaging;

namespace Models;

public record class DicomSlice
{
    public required BitDepth BitDepth { get; init; }
    public required AnatomicPlane DefaultPlane { get; init; }
    public required ushort Height { get; init; }
    public required ushort WindowWidth { get; init; }
    public required ushort WindowLevel { get; init; }
    public required PhotometricInterpretation PhotometricInterpretation { get; init; }
    public required IReadOnlyList<byte> PixelData { get; init; }
    public required PixelRepresentation PixelRepresentation { get; init; }
    public required (double VerticalSpacing, double HorizontalSpacing) PixelSpacing { get; init; }
    public required ushort SmallestPixelValue { get; init; }
    public required ushort LargestPixelValue { get; init; }
    public required ushort Width { get; init; }
    public static DicomSlice FromDicomFile(DicomFile dicom)
    {
        var pixData = DicomPixelData.Create(dicom.Dataset);
        double [] spacing = dicom.Dataset.GetValues<double>(DicomTag.PixelSpacing);
        (double vertspacing, double horzspacing) = (spacing [0], spacing [1]);
        ushort wl = dicom.Dataset.GetSingleValue<ushort>(DicomTag.WindowCenter);
        ushort ww = dicom.Dataset.GetSingleValue<ushort>(DicomTag.WindowWidth);
        ushort maxPix = dicom.Dataset.GetSingleValue<ushort>(DicomTag.LargestImagePixelValue);
        ushort minPix = dicom.Dataset.GetSingleValue<ushort>(DicomTag.SmallestImagePixelValue);

        return new DicomSlice
        {
            BitDepth = pixData.BitDepth,
            PhotometricInterpretation = pixData.PhotometricInterpretation,
            PixelRepresentation = pixData.PixelRepresentation,
            DefaultPlane = AnatomicPlane.Sagittal,
            Width = pixData.Width,
            Height = pixData.Height,
            PixelData = pixData.GetFrame(0).Data,
            WindowLevel = wl,
            WindowWidth = ww,
            PixelSpacing = (vertspacing, horzspacing),
            LargestPixelValue = maxPix,
            SmallestPixelValue = minPix
        };
    }
}