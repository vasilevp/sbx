﻿using GameOverlay.Drawing;
using GameOverlay.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static sbx.Translator;

namespace sbx
{
    class Overlay<T> : IDisposable
    {
        GraphicsWindow window;
        Graphics gfx;
        Dictionary<string, Font> fonts = new();
        Dictionary<string, IBrush> brushes = new();
        int fontSize = 14;
        Geometry grid = null;

        int keypadsize = 1000;

        ReaderWriterLock rwl = new();
        IList<IEnumerable<T>> items = new List<IEnumerable<T>>();

        /// <summary>
        /// Gets or sets a Boolean indicating whether this window is visible.
        /// </summary>
        public bool IsVisible
        {
            get => window.IsVisible;
            set => window.IsVisible = value;
        }

        /// <summary>
        /// Initializes a new Overlay window.
        /// </summary>
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
                IsVisible = false,
                Title = Translate("Overlay"),
            };



            window.SetupGraphics += SetupGraphics;
            window.DrawGraphics += DrawGraphics;


            window.Create();
        }

        private void DrawLongText(Font f, int size, IBrush b, int x, int y, int maxWidth, int maxHeight, string text)
        {
            var l = new SharpDX.DirectWrite.TextLayout(gfx.GetFontFactory(), text, f.TextFormat, gfx.Width, gfx.Height)
            {
                MaxWidth = maxWidth,
                MaxHeight = maxHeight
            };
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

        /// <summary>
        /// Set items to display in the Overlay.
        /// </summary>
        public void SetItems(IEnumerable<IEnumerable<T>> items)
        {
            rwl.AcquireWriterLock(1000);
            this.items = items?.ToList() ?? new List<IEnumerable<T>>();
            rwl.ReleaseWriterLock();
        }

        /// <summary>
        /// Switch between supported font sizes.
        /// </summary>
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

        /// <summary>
        /// Releases all resources used by the Overlay.
        /// </summary>
        public void Dispose()
        {
            window.Pause();
            window.Dispose();
            gfx.Dispose();
        }

        void SetupGraphics(object sender, SetupGraphicsEventArgs _)
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
        }

        void DrawGraphics(object sender, DrawGraphicsEventArgs e)
        {
            rwl.AcquireReaderLock(1000);
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

                if (items.Count <= i || !items[i].Any())
                {
                    DrawTextCentered(
                        fonts["consolas"],
                        14,
                        brushes["blackTransparent"],
                        i % 3 * keypadsize / 3 + keypadsize / 6,
                        i / 3 * keypadsize / 3 + keypadsize / 6,
                        i % 3 * keypadsize / 3,
                        i / 3 * keypadsize / 3,
                        Translate("(none)")
                        );
                    continue;
                }

                // then draw the text itself
                StringBuilder sb = new();
                foreach (var item in items[i])
                {
                    sb.AppendLine(item?.ToString() ?? "NULL");
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
                Translate("overlayHelp")
                );

            rwl.ReleaseReaderLock();
        }
    }
}
