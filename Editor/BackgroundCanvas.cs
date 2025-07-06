using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Editor
{
    public class BackgroundCanvas : Canvas
    {
        public static readonly DependencyProperty BackgroundSpriteProperty = DependencyProperty.Register(
            nameof(BackgroundSprite), typeof(string), typeof(BackgroundCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TiledProperty = DependencyProperty.Register(
            nameof(Tiled), typeof(bool), typeof(BackgroundCanvas),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public string BackgroundSprite
        {
            get => (string)GetValue(BackgroundSpriteProperty);
            set => SetValue(BackgroundSpriteProperty, value);
        }

        public bool Tiled
        {
            get => (bool)GetValue(TiledProperty);
            set => SetValue(TiledProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            if (string.IsNullOrEmpty(BackgroundSprite)) return;
            var assetsRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "Assets");
            var spritePath = Path.Combine(assetsRoot, "Sprites", BackgroundSprite);
            if (!File.Exists(spritePath)) return;
            var bmp = new BitmapImage(new Uri(spritePath));
            if (Tiled)
            {
                for (double x = 0; x < ActualWidth; x += bmp.PixelWidth)
                {
                    for (double y = 0; y < ActualHeight; y += bmp.PixelHeight)
                    {
                        dc.DrawImage(bmp, new Rect(x, y, bmp.PixelWidth, bmp.PixelHeight));
                    }
                }
            }
            else
            {
                double x = (ActualWidth - bmp.PixelWidth) / 2;
                double y = (ActualHeight - bmp.PixelHeight) / 2;
                dc.DrawImage(bmp, new Rect(x, y, bmp.PixelWidth, bmp.PixelHeight));
            }
        }
    }
} 