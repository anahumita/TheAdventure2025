using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.Fonts;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;

namespace TheAdventure;

public unsafe class GameRenderer
{
    private Sdl _sdl;
    private Renderer* _renderer;
    private GameWindow _window;
    private Camera _camera;

    private Dictionary<int, IntPtr> _texturePointers = new();
    private Dictionary<int, TextureData> _textureData = new();
    private int _textureId;

    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;
        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

        _window = window;
        var windowSize = window.Size;
        _camera = new Camera(windowSize.Width, windowSize.Height);
    }

    public void SetWorldBounds(Rectangle<int> bounds)
    {
        _camera.SetWorldBounds(bounds);
    }

    public void CameraLookAt(int x, int y)
    {
        _camera.LookAt(x, y);
    }

    public int LoadTexture(string fileName, out TextureData textureInfo)
    {
        using (var fStream = new FileStream(fileName, FileMode.Open))
        {
            var image = Image.Load<Rgba32>(fStream);
            textureInfo = new TextureData()
            {
                Width = image.Width,
                Height = image.Height
            };

            var imageRAWData = new byte[textureInfo.Width * textureInfo.Height * 4];
            image.CopyPixelDataTo(imageRAWData.AsSpan());

            fixed (byte* data = imageRAWData)
            {
                var surface = _sdl.CreateRGBSurfaceWithFormatFrom(
                    data,
                    textureInfo.Width,
                    textureInfo.Height,
                    32,
                    textureInfo.Width * 4,
                    (uint)PixelFormatEnum.Rgba32
                );

                if (surface == null)
                    throw new Exception("Failed to create surface from image data.");

                var texture = _sdl.CreateTextureFromSurface(_renderer, surface);
                if (texture == null)
                {
                    _sdl.FreeSurface(surface);
                    throw new Exception("Failed to create texture from surface.");
                }

                _sdl.FreeSurface(surface);
                _texturePointers[_textureId] = (IntPtr)texture;
                _textureData[_textureId] = textureInfo;
            }
        }

        return _textureId++;
    }

    public void RenderTexture(int textureId, Rectangle<int> src, Rectangle<int> dst,
        RendererFlip flip = RendererFlip.None, double angle = 0.0, Point center = default)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            var translatedDst = _camera.ToScreenCoordinates(dst);
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in src, in translatedDst, angle, in center, flip);
        }
    }

    public void RenderTextureScreenSpace(int textureId, Rectangle<int> src, Rectangle<int> dst)
    {
        if (_texturePointers.TryGetValue(textureId, out var imageTexture))
        {
            _sdl.RenderCopyEx(_renderer, (Texture*)imageTexture, in src, in dst, 0.0, null, RendererFlip.None);
        }
    }

    public Vector2D<int> ToWorldCoordinates(int x, int y)
    {
        return _camera.ToWorldCoordinates(new Vector2D<int>(x, y));
    }

    public void SetDrawColor(byte r, byte g, byte b, byte a)
    {
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
    }

    public void ClearScreen()
    {
        _sdl.RenderClear(_renderer);
    }

    public void PresentFrame()
    {
        _sdl.RenderPresent(_renderer);
    }

    public unsafe void RenderTextCrossPlatform(string text, int rightPadding, int topPadding)
    {
        var collection = new FontCollection();
        var family = collection.Add("Assets/DejaVuSans.ttf");
        var font = family.CreateFont(24);
        var textColor = SixLabors.ImageSharp.Color.White;

        using var img = new Image<Rgba32>(300, 50);
        img.Mutate(ctx =>
        {
            ctx.Fill(SixLabors.ImageSharp.Color.Transparent);
            ctx.DrawText(text, font, textColor, new SixLabors.ImageSharp.PointF(0, 0));
        });

        var rawData = new byte[img.Width * img.Height * 4];
        img.CopyPixelDataTo(rawData);

        fixed (byte* dataPtr = rawData)
        {
            var surface = _sdl.CreateRGBSurfaceWithFormatFrom(
                dataPtr,
                img.Width,
                img.Height,
                32,
                img.Width * 4,
                (uint)PixelFormatEnum.Abgr8888
            );

            if (surface == null)
                throw new Exception("Failed to create text surface.");

            var texture = _sdl.CreateTextureFromSurface(_renderer, surface);
            _sdl.FreeSurface(surface);

            var screenWidth = _window.Size.Width;
            var dstRect = new Rectangle<int>(
                screenWidth - img.Width - rightPadding, // poziție dreapta
                topPadding,
                img.Width,
                img.Height
            );

            var srcRect = new Rectangle<int>(0, 0, img.Width, img.Height);
            _sdl.RenderCopyEx(_renderer, (Texture*)texture, in srcRect, in dstRect, 0, null, RendererFlip.None);
            _sdl.DestroyTexture((Texture*)texture);
        }
    }
    public (int Width, int Height) WindowSize => _window.Size;

}
