using System.Drawing;
using Cave;
using Cave.Media;
using Cave.Media.OpenGL;
using Cave.Media.Video;
using SkiaSharp;

namespace openglfw
{
    class Program
    {
        const string dllbasepath = @"c:\dll\glfw\";
        const string assetbasepath = @"..\..\..\..\assets\bitmap";

        IBitmap32Loader bmLoader;
        Glfw3Renderer renderer;
        bool closing;

        ARGBImageData imageData;

        List<IRenderSprite> sprites = new List<IRenderSprite>();
        List<IRenderSprite> tiles = new List<IRenderSprite>();

        float zoom;
        Vector2 translation = Vector2.Empty;
        bool dragging;
        Vector2 translationStart = Vector2.Empty;
        Vector2 translationTemp = Vector2.Empty;

        Vector2 cursorPos = Vector2.Empty;

        IStopWatch watch;

        static void Main(string[] args)
        {
            var dllpath = dllbasepath + ((IntPtr.Size == 4) ? "32" : "64");
            glfw3.ConfigureNativesDirectory(dllpath);
            var p = new Program();
            p.Run(args);
        }

        public Program()
        {
            bmLoader = new SkiaBitmap32Loader();
            renderer = new Glfw3Renderer();
            watch = StopWatch.StartNew();
        }

        private void CheckTimer()
        {
            if (watch != null)
            {
                if (watch.ElapsedMilliSeconds > 100)
                {
                    renderer.SetWindowTitle($"Zoom: {zoom} / Cursor: {cursorPos:d}");
                    watch.Reset();
                }
            }
            else
            {
                watch = StopWatch.StartNew();
            }
        }

        private void Run(string[] args)
        {
            if (args.Length < 1) { throw new Exception("No Image file given"); }
            if (!LoadImage(args[0])) { throw new Exception("Could not Load Image!"); }
            InitRenderer();
            LoadSprites();
            while (!closing)
            {
                Render();
                CheckTimer();
                if (Console.KeyAvailable) closing = true;
            }
        }

        private bool LoadImage(string imageFileName)
        {
            using (var planImage = Bitmap32.FromFile(imageFileName))
            {
                imageData = planImage.GetImageData();
                return true;
            }
        }

        private void Render()
        {
            if (renderer == null) throw new Exception("could not render");

            renderer.Clear(Color.DarkBlue);
            renderer.Render(sprites);
            renderer.Present();
        }

        private void InitRenderer()
        {
            if (!renderer.IsAvailable) throw new Exception("could not init");
            var rdevs = renderer.GetDevices();
            renderer.AspectCorrection = ResizeMode.TouchFromInside;
            renderer.Closed += Renderer_Closed;
            renderer.ScrollEvent += Renderer_ScrollEvent;
            renderer.MouseButtonChanged += Renderer_MouseButtonChanged;
            renderer.CursorPosChanged += Renderer_CursorPosChanged;

            renderer.Initialize(rdevs[0], RendererMode.Window, RendererFlags.WaitRetrace, 1024, 768, "OpenGL");
            Console.WriteLine("Max Texture size: " + renderer.MaxTextureSize);
        }

        private void Renderer_CursorPosChanged(object? sender, glfw3.CursorPosEventArgs e)
        {

            if (dragging)
            {
                translationTemp = CalcProjectionOffset(e.Position) - translationStart;
                CalcProjection();
            }

            cursorPos = renderer.CalculateProjectionCoordinates(e.Position);

        }

        private void Renderer_MouseButtonChanged(object? sender, glfw3.MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case glfw3.MouseButton.Button1:
                    switch (e.State)
                    {
                        case glfw3.InputState.Press:
                            translationStart = CalcProjectionOffset(e.Position);
                            dragging = true;
                            break;
                        case glfw3.InputState.Release:
                            dragging = false;
                            translation += translationTemp;
                            translationTemp = Vector2.Empty;                            
                            break;
                    }
                    break;
            }
        }

        private Vector2 CalcProjectionOffset(Vector2 coords)
        {
            return renderer.CalculateProjectionCoordinates(coords) + renderer.WorldTranslation;
        }

        private void CalcProjection()
        {
            var scale = (float)Math.Pow(2d, zoom / 5);
            renderer.WorldScale = Vector2.Create(scale, scale);
            renderer.WorldTranslation = translation + translationTemp;
        }

        private void Renderer_ScrollEvent(object? sender, glfw3.ScrollEventArgs e)
        {
            zoom += e.Offset.Y;
            CalcProjection();
        }

        private ARGBImageData getImageDataBlock(ARGBImageData from, int xPos, int yPos, int width, int height)
        {
            var source = from.GetBytes();
            var data = new int[width * height];
            for (int y = 0; y < height; y++)
            {
                Buffer.BlockCopy(source, (y + yPos) * from.Stride + xPos, data, y * width * 4, width * 4);
            }
            return new ARGBImageData(data, width, height, width * 4);
        }

        private void LoadSprites()
        {
            IRenderSprite sprite;
            int w = 1024;
            int h = 1024;

            ARGBImageData d = getImageDataBlock(imageData, 800, 800, w, h);

            sprite = renderer.CreateSprite("1");
            sprite.LoadTexture(d);
            sprite.Position = Vector3.Create(0, 0, 0);
            sprites.Add(sprite);
        }

        private void Renderer_Closed(object? sender, EventArgs e)
        {
            closing = true;
        }
    }
}
