using GameOverlay.Drawing;
using GameOverlay.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sb1
{
    class Overlay<T>
    {
        GraphicsWindow window;
        Graphics gfx;
        IList<IEnumerable<T>> items = new List<IEnumerable<T>>();
        Dictionary<string, Font> fonts = new Dictionary<string, Font>();
        Dictionary<string, IBrush> brushes = new Dictionary<string, IBrush>();
        int fontSize = 14;

        int keypadsize = 1000;

        public bool IsVisible
        {
            get => window.IsVisible;
            set => window.IsVisible = value;
        }

        public Overlay()
        {
            gfx = new Graphics()
            {
                MeasureFPS = true,
                PerPrimitiveAntiAliasing = true,
                TextAntiAliasing = true
            };

            window = new GraphicsWindow(0, 0, keypadsize * 5 / 4, keypadsize, gfx)
            {
                FPS = 30,
                IsTopmost = true,
                IsVisible = true,
                Title = "Overlay",
            };

            Geometry grid = null;

            window.SetupGraphics += (sender, e) =>
            {
                fonts["consolas"] = gfx.CreateFont("Consolas", 14, true);
                brushes["black"] = gfx.CreateSolidBrush(0, 0, 0);
                brushes["blackTransparent"] = gfx.CreateSolidBrush(0, 0, 0, 0.5f);
                brushes["green"] = gfx.CreateSolidBrush(0, 255, 0);
                brushes["background"] = gfx.CreateSolidBrush(255, 255, 255, 0.35f);
                brushes["helperText"] = gfx.CreateSolidBrush(0, 0, 0, 0.25f);

                var gridBounds = new Rectangle(0, 0, keypadsize, keypadsize);
                grid = gfx.CreateGeometry();

                for (int x = (int)gridBounds.Left; x <= gridBounds.Right; x += (int)gridBounds.Width / 3)
                {
                    var line = new Line(x, gridBounds.Top, x, gridBounds.Bottom);
                    grid.BeginFigure(line);
                    grid.EndFigure(false);
                }

                for (int y = (int)gridBounds.Top; y <= gridBounds.Bottom; y += (int)gridBounds.Height / 3)
                {
                    var line = new Line(gridBounds.Left, y, gridBounds.Right, y);
                    grid.BeginFigure(line);
                    grid.EndFigure(false);
                }

                grid.Close();
            };

            window.DrawGraphics += (sender, e) =>
            {
                gfx.ClearScene();

                gfx.DrawGeometry(grid, brushes["black"], 1.0f);
                gfx.DrawBox2D(brushes["black"], brushes["background"], new Rectangle(0, 0, e.Graphics.Width, e.Graphics.Height), 1.0f);

                var helpSize = gfx.Height / 3;
                for (int i = 0; i < 9; i++)
                {
                    // draw transparent keypad number using index magic
                    DrawTextCentered(
                        fonts["consolas"],
                        helpSize,
                        brushes["helperText"],
                        (2 - (i % 3)) * keypadsize / 3 + keypadsize / 6,  // i_h * cell_w + cell_w/2
                        (i / 3) * keypadsize / 3 + keypadsize / 6,        // i_v * cell_h + cell_h/2
                        int.MinValue,
                        int.MinValue,
                        (9 - i).ToString()
                        );


                    if (items.Count() <= i || items[i].Count() == 0)
                    {
                        DrawTextCentered(
                            fonts["consolas"],
                            14,
                            brushes["blackTransparent"],
                            i % 3 * keypadsize / 3 + keypadsize / 6,
                            i / 3 * keypadsize / 3 + keypadsize / 6,
                            i % 3 * keypadsize / 3,
                            i / 3 * keypadsize / 3,
                            "(none)"
                            );
                        continue;
                    }

                    // then draw the text itself
                    StringBuilder sb = new StringBuilder();
                    foreach (var item in items[i])
                    {
                        sb.AppendLine(item.ToString());
                    }

                    DrawLongText(
                        fonts["consolas"],
                        fontSize,
                        brushes["black"],
                        (i % 3 * keypadsize / 3) + 10,
                        i / 3 * keypadsize / 3,
                        keypadsize / 3 - 10,
                        keypadsize / 3,
                        sb.ToString()
                        );
                }

                DrawTextCentered(
                    fonts["consolas"],
                    14,
                    brushes["black"],
                    keypadsize,
                    0,
                    keypadsize + 5,
                    0,
                    @"Hotkeys:
     * - │ *: switch font size
 7 8 9 + │ -: stop current sound
 4 5 6   │ +: toggle overlay
 1 2 3   │ Number: go into group

If the group has only one sound,
it will be played and you will
return to the top level.
                    "
                    );
            };
        }

        private void DrawLongText(Font f, int size, IBrush b, int x, int y, int maxWidth, int maxHeight, string text)
        {
            var l = new SharpDX.DirectWrite.TextLayout(gfx.GetFontFactory(), text, f.TextFormat, gfx.Width, gfx.Height);
            l.MaxWidth = maxWidth;
            l.MaxHeight = maxHeight;
            l.SetFontSize(size, new SharpDX.DirectWrite.TextRange(0, text.Length));
            var target = new SharpDX.Mathematics.Interop.RawVector2(x, y);
            gfx.GetRenderTarget().DrawTextLayout(target, l, b.Brush, SharpDX.Direct2D1.DrawTextOptions.Clip);
        }

        private void DrawTextCentered(Font f, int size, IBrush b, int x, int y, int minx, int miny, string text)
        {
            var l = new SharpDX.DirectWrite.TextLayout(gfx.GetFontFactory(), text, f.TextFormat, gfx.Width, gfx.Height);
            l.SetFontSize(size, new SharpDX.DirectWrite.TextRange(0, text.Length));
            var target = new SharpDX.Mathematics.Interop.RawVector2(Math.Max(x - l.Metrics.Width / 2, minx), Math.Max(y - l.Metrics.Height / 2, miny));
            gfx.GetRenderTarget().DrawTextLayout(target, l, b.Brush, SharpDX.Direct2D1.DrawTextOptions.Clip);
        }

        public void Run()
        {
            window.Create();
            //WindowHelper.EnableBlurBehind(window.Handle);
            window.Join();
        }

        public void SetItems(IEnumerable<IEnumerable<T>> items)
        {
            this.items = items?.ToList();
        }

        public void SwitchFontSize()
        {
            switch (fontSize)
            {
                case 14:
                    fontSize = 20;
                    break;
                case 20:
                    fontSize = 32;
                    break;
                case 32:
                    fontSize = 48;
                    break;
                case 48:
                    fontSize = 9;
                    break;
                case 9:
                    fontSize = 14;
                    break;
            }
        }
    }
}
