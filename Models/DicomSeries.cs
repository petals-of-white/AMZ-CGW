using System.Collections.Immutable;
using System.Data;
using FellowOakDicom;
using FellowOakDicom.Imaging;

namespace Models;

public class DicomSeries
{
    public DicomSeries(IReadOnlyList<DicomSlice> slices)
    {
        switch (slices)
        {
            case ([var first, ..]):

                Plane = first.DefaultPlane;
                Slices = SortSlices(slices);
                Width = first.Width;
                Height = first.Height;
                NumberOfSpacePositions = (uint) slices.Count;
                PixelRepresentation = first.PixelRepresentation;
                PhotometricInterpretation = first.PhotometricInterpretation;
                BitDepth = first.BitDepth;

                NumberOfSpacePositions = (uint) slices.Count;
                break;

            default:
                throw new ArgumentException("Empty collection of slices", nameof(slices));
        }
    }

    public BitDepth BitDepth { get; }
    public uint Height { get; }
    public uint NumberOfSpacePositions { get; }
    public PhotometricInterpretation PhotometricInterpretation { get; }
    public PixelRepresentation PixelRepresentation { get; }

    public AnatomicPlane Plane { get; }

    public IReadOnlyList<DicomSlice> Slices { get; }
    public uint Width { get; }

    public static DicomSeries FromFiles(string [] filepaths)
    {
        var slices = filepaths.Select(fp => DicomSlice.FromDicomFile(DicomFile.Open(fp))).ToImmutableArray();
        return new DicomSeries(slices);
    }

    public static IReadOnlyList<DicomSlice> SortSlices(IReadOnlyList<DicomSlice> slices) => slices;

    public byte [] ConcatPixelData()
    {
        return Slices.SelectMany(slice => slice.PixelData).ToArray();
    }
}