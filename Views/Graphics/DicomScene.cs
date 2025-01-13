using System.Collections.Immutable;
using System.IO;
using System.Numerics;
using Models;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using static Views.Graphics.OpenGLHelpers;

namespace Views.Graphics;

public class DicomScene : IDisposable
{
    private static readonly float [] coords = {
        -1f, -1f,     0f, 0f,
        -1f, 1f,      0f, 1f,
        1f,  -1f,     1f, 0f,
        1f,  1f,      1f, 1f,
    };

    private readonly TextureWrapMode wrapMode = TextureWrapMode.MirroredRepeat;
    private DicomToGLConverter? dicomGLData;
    private bool disposed;
    private DicomSeries? lastLoadedSeries;
    private ImmutableArray<RGB<byte>>? loadedLUT;
    private uint lutTexture;
    private uint monochromeTexture;
    private uint outputFramebuffer;
    private uint rgbTargetTexture;
    private uint texture3D;
    private bool useLUT = false;
    private uint vao;
    private uint vbo;
    private int vertShader, fragShader, program;

    public DicomScene()
    {
        while (GL.GetError() is not ErrorCode.NoError) ;

        CreateVertices();
        CreateProgram();
        SetupRGBTarget();
        SetupMonochromeTarget();
        UnbindAll();
    }

    public static string FragShaderLoc { get; } = "Shaders/shader.frag";

    public static string VertShaderLoc { get; } = "Shaders/shader.vert";

    public bool IsTextureLoaded { get; private set; } = false;

    public bool LUTLoaded { get; private set; }

    public bool UseLUT
    {
        get => useLUT; set
        {
            var loc = GL.GetUniformLocation(program, "useLUT");
            GL.UseProgram(program);
            GL.Uniform1(loc, Convert.ToInt32(value));
            useLUT = value;
        }
    }

    public static Matrix4 ToOpenTKMatrix(Matrix4x4 matrix) => new(
            matrix.M11, matrix.M12, matrix.M13, matrix.M14,
        matrix.M21, matrix.M22, matrix.M23, matrix.M24,
        matrix.M31, matrix.M32, matrix.M33, matrix.M34,
        matrix.M41, matrix.M42, matrix.M43, matrix.M44);

    public void ApplyWindowLevel(float windowWidth, float windowLevel)
    {
        // Upload window-level uniforms
        int widthLoc = GL.GetUniformLocation(program, "windowWidth");
        int levelLoc = GL.GetUniformLocation(program, "windowLevel");

        GL.Uniform1(widthLoc, windowWidth);
        GL.Uniform1(levelLoc, windowLevel);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            UnbindAll();
            GL.DeleteTextures(1, [texture3D]);
            GL.DeleteVertexArrays(1, [vao]);
            GL.DeleteBuffers(1, [vbo]);
            GL.DeleteProgram(program);
            GL.DeleteShader(fragShader);
            GL.DeleteShader(vertShader);
            disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    public void DrawVertices(AnatomicPlane targetPlane, uint spaceLocation)
    {
        CoordsPixelLength coordsPixelLength = new(lastLoadedSeries!, targetPlane);
        if (IsTextureLoaded)
        {
            //GL.BindFramebuffer(FramebufferTarget.Framebuffer, outputFramebuffer);

            float relativeDepth = (float) spaceLocation / coordsPixelLength.ZPixels;

            // Bind All
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture3D, texture3D);
            GL.UseProgram(program);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture1D, lutTexture);



            // Upload a transformation matrix
            var sourcePlane = lastLoadedSeries!.Plane;
            Matrix4x4 changePlanes = AnatomicPlaneRelations.PlaneTransform(sourcePlane, targetPlane);
            var transformMatrix = Matrix4x4.CreateTranslation(0, 0, relativeDepth) * changePlanes;
            var opentkmatrix = ToOpenTKMatrix(transformMatrix);
            var transMatLoc = GL.GetUniformLocation(program, "u_transform_matrix");
            GL.UniformMatrix4(transMatLoc, false, ref opentkmatrix);

            //ApplyNormalization(0f, lastLoadedSeries.Slices [0].);


    
            int numberOfVertices = 4;
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, numberOfVertices);



            var displaySize = new CoordsPixelLength(lastLoadedSeries!, targetPlane);

            int width = (int) displaySize.XPixels, height = (int) displaySize.YPixels;

            // again
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, outputFramebuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            // rgb setup
            //GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, rgbTargetTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, nint.Zero);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, rgbTargetTexture, 0);
            GL.Viewport(new System.Drawing.Size(width, height));
            // monochrome setup
            GL.BindTexture(TextureTarget.Texture2D, monochromeTexture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16ui, width, height, 0, PixelFormat.RedInteger, PixelType.UnsignedShort, nint.Zero);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, monochromeTexture, 0);

            GL.DrawBuffers(2, [DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2]);
            
            var framebuffererror = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (framebuffererror != FramebufferErrorCode.FramebufferComplete)
            {
                throw new InvalidOperationException($"Error framebuffer: {framebuffererror}");
            }
            //GL.DrawBuffers(1, [DrawBuffersEnum.ColorAttachment0]);
            //Draw vertices

            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, numberOfVertices);
            //GL.Clear(ClearBufferMask.ColorBufferBit);









            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // unbind
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindTexture(TextureTarget.Texture3D, 0);
            GL.UseProgram(0);
        }
    }

    public RGB<byte> [] GetColorPixels(AnatomicPlane targetPlane, uint spaceLocation)
    {
        var displaySize = new CoordsPixelLength(lastLoadedSeries!, targetPlane);

        //int width = (int) displaySize.XPixels, height= (int) displaySize.YPixels;

        //// rgb setup
        //GL.BindTexture(TextureTarget.Texture2D, rgbTargetTexture);
        //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb8ui, width, height, 0, PixelFormat.RgbInteger, PixelType.UnsignedByte, nint.Zero);
        //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, rgbTargetTexture, 0);

        //// monochrome setup
        //GL.BindTexture(TextureTarget.Texture2D, monochromeTexture);
        //GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16ui, width, height, 0, PixelFormat.RedInteger, PixelType.UnsignedShort, nint.Zero);
        //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, monochromeTexture, 0);

        //GL.DrawBuffers(1, [DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2]);

        //var framebuffererror = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        //if (framebuffererror != FramebufferErrorCode.FramebufferComplete)
        //{
        //    throw new InvalidOperationException($"Error framebuffer: {framebuffererror}");
        //}

        //DrawVertices(targetPlane, spaceLocation);

        //GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.BindTexture(TextureTarget.Texture2D, rgbTargetTexture);
        var pixels = new RGB<byte> [displaySize.XPixels * displaySize.YPixels];
        GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);

        return pixels;
    }

    public ushort [] GetMonochromePixels(AnatomicPlane targetPlane, uint spaceLocation)
    {
        var displaySize = new CoordsPixelLength(lastLoadedSeries!, targetPlane);



        //GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.BindTexture(TextureTarget.Texture2D, monochromeTexture);
        var pixels = new ushort [displaySize.XPixels * displaySize.YPixels];
        GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.RedInteger, PixelType.UnsignedShort, pixels);

        return pixels;
    }

    public void LoadDicomSeries(DicomSeries dicomSeries)
    {
        if (dicomSeries != lastLoadedSeries)
        {
            var converter = new DicomToGLConverter(dicomSeries);
            dicomGLData = converter;

            GL.GenTextures(1, out texture3D);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture3D, texture3D);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int) wrapMode);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int) wrapMode);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int) wrapMode);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Nearest);

            var combinedData = dicomSeries.ConcatPixelData();

            GL.TexImage3D(TextureTarget.Texture3D, 0, converter.InternalFormat,
                    converter.Width, converter.Height, (int) dicomSeries.NumberOfSpacePositions,
                    0, converter.Format, converter.Type, combinedData);

            GL.BindTexture(TextureTarget.Texture3D, 0);

            lastLoadedSeries = dicomSeries;
            IsTextureLoaded = true;

            var maxValue = dicomSeries.Slices.Max(sl => sl.LargestPixelValue);
            var minValue = dicomSeries.Slices.Min(sl => sl.SmallestPixelValue);
            ApplyNormalization(minValue, maxValue);
        }
    }

    public void UnbindAll()
    {
        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.UseProgram(0);
        GL.BindTexture(TextureTarget.Texture3D, 0);
    }

    public void UploadLUT(ImmutableArray<RGB<byte>> lut)
    {
        if (lut != loadedLUT)
        {
            GL.GenTextures(1, out lutTexture);

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture1D, lutTexture);
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Nearest);

            var pixData = lut.SelectMany(col => new byte [] { col.R, col.G, col.B }).ToArray();

            GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgb8ui,
                    lut.Length, 0, PixelFormat.RgbInteger, PixelType.UnsignedByte, pixData);

            var lutSamplerLoc = GL.GetUniformLocation(program, "lutSampler");
            GL.UseProgram(program);
            GL.Uniform1(lutSamplerLoc, 1);

            LUTLoaded = true;
        }
    }

    private void ApplyNormalization(float minPeak, float maxPeak)
    {
        // upload normalization uniforms
        GL.UseProgram(program);
        int minPeakLoc = GL.GetUniformLocation(program, "minPeak");
        int maxPeakLoc = GL.GetUniformLocation(program, "maxPeak");

        GL.Uniform1(minPeakLoc, minPeak);
        GL.Uniform1(maxPeakLoc, maxPeak);
    }

    private void ApplyNormalization(byte [] byteArray)
    {
        // upload normalization uniforms
        int minPeakLoc = GL.GetUniformLocation(program, "minPeak");
        int maxPeakLoc = GL.GetUniformLocation(program, "maxPeak");
        short [] texArray = new short [byteArray.Length / 2];
        System.Buffer.BlockCopy(byteArray, 0, texArray, 0, byteArray.Length);
        var maxVal = texArray.Max();
        var minVal = texArray.Min();
        GL.Uniform1(minPeakLoc, (float) minVal);
        GL.Uniform1(maxPeakLoc, (float) maxVal);
    }

    private void BindAll()
    {
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

        GL.UseProgram(program);

        GL.BindTexture(TextureTarget.Texture3D, texture3D);
    }

    private void CreateProgram()
    {
        vertShader = MakeShader(ShaderType.VertexShader, File.ReadAllText(VertShaderLoc));
        fragShader = MakeShader(ShaderType.FragmentShader, File.ReadAllText(FragShaderLoc));

        program = GL.CreateProgram();
        GL.AttachShader(program, vertShader);
        GL.AttachShader(program, fragShader);
        GL.LinkProgram(program);
        GL.ValidateProgram(program);

        // TODO: validate progam
    }

    private void CreateVertices()
    {
        uint [] vaos = new uint [1];
        uint [] vbos = new uint [1];
        GL.GenVertexArrays(1, vaos);
        GL.BindVertexArray(vaos [0]);

        GL.GenBuffers(1, vbos);

        GL.BindBuffer(BufferTarget.ArrayBuffer, vbos [0]);
        GL.BufferData(BufferTarget.ArrayBuffer, coords.Length * sizeof(float), coords, BufferUsageHint.StaticDraw);

        ThrowIfGLError();

        // buffer data...
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // unbindg vao
        GL.BindVertexArray(0);

        // unbind vbo

        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        vbo = vbos [0];
        vao = vaos [0];

        ThrowIfGLError();
    }

    private void SetupMonochromeTarget()
    {
        GL.GenTextures(1, out monochromeTexture);

        GL.BindTexture(TextureTarget.Texture2D, rgbTargetTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    private void SetupRGBTarget()
    {
        GL.GenFramebuffers(1, out outputFramebuffer);
        GL.GenTextures(1, out rgbTargetTexture);

        GL.BindTexture(TextureTarget.Texture2D, rgbTargetTexture);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Nearest);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }
}