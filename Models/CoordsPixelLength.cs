namespace Models;

public class CoordsPixelLength
{
    public CoordsPixelLength(DicomSeries sourceSeries, AnatomicPlane targetPlane)
    {
        switch (sourceSeries.Plane, targetPlane)
        {
            case (var source, var target) when source == target:
                XPixels = sourceSeries.Width;
                YPixels = sourceSeries.Height;
                ZPixels = (uint) sourceSeries.NumberOfSpacePositions;
                break;

            case (AnatomicPlane.Sagittal, AnatomicPlane.Coronal):
                XPixels = sourceSeries.NumberOfSpacePositions;
                YPixels = sourceSeries.Height;
                ZPixels = sourceSeries.Width;
                break;

            case (AnatomicPlane.Sagittal, AnatomicPlane.Axial):
                XPixels = sourceSeries.NumberOfSpacePositions;
                YPixels = sourceSeries.Width;
                ZPixels = sourceSeries.Height;
                break;
        }
    }

    public uint XPixels { get; private init; }
    public uint YPixels { get; private init; }
    public uint ZPixels { get; private init; }
}