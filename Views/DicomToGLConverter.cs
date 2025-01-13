using FellowOakDicom.Imaging;
using Models;
using OpenTK.Graphics.OpenGL;

namespace Views;

public class DicomToGLConverter(DicomSeries dicom)
{
    private readonly DicomSeries dicomManager = dicom;

    public int Depth => (int) dicomManager.NumberOfSpacePositions;

    public PixelFormat Format => InternalFormat switch
    {
        PixelInternalFormat.R8i or
        PixelInternalFormat.R8ui or
        PixelInternalFormat.R16i or
        PixelInternalFormat.R16ui or
        PixelInternalFormat.R32i or
        PixelInternalFormat.R32ui => PixelFormat.RedInteger,
        _ => throw new NotImplementedException("Float texture are not implemented yet.")
    };

    public int Height => (int) dicomManager.Height;

    public PixelInternalFormat InternalFormat => dicomManager.PhotometricInterpretation switch
    {
        var i when i == PhotometricInterpretation.Monochrome2 => Type switch
        {
            PixelType.Byte => PixelInternalFormat.R8i,
            PixelType.UnsignedByte => PixelInternalFormat.R8ui,
            PixelType.Short => PixelInternalFormat.R16i,
            PixelType.UnsignedShort => PixelInternalFormat.R16ui,
            PixelType.Int => PixelInternalFormat.R32i,
            PixelType.UnsignedInt => PixelInternalFormat.R32ui,
            _ => throw new NotImplementedException("Other photometric interpretation is not implemented yet.")
        },
        _ => throw new NotImplementedException("Other photometric interpretation is not implemented yet.")
    };

    public PixelType Type => dicomManager.BitDepth switch
    {
        { BitsStored: (> 0) and (<= 8), IsSigned: true } => PixelType.Byte,
        { BitsStored: (> 0) and (<= 8), IsSigned: false } => PixelType.UnsignedByte,
        { BitsStored: (<= 16), IsSigned: true } => PixelType.Short,
        { BitsStored: (<= 16), IsSigned: false } => PixelType.UnsignedShort,
        { BitsStored: (<= 32), IsSigned: true } => PixelType.Int,
        { BitsStored: (<= 32), IsSigned: false } => PixelType.UnsignedInt,
        // 12 bit => Розширити до GL_SHORT (16 bit)
        _ => throw new NotImplementedException("Other bit depth is not implemented")
    };

    public int Width => (int) dicomManager.Width;
}