using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;

namespace Editor
{
    public partial class AnimatedSpriteDisplay : UserControl
    {
        private BitmapImage originalImage;
        private MainWindow.SpriteData spriteData;
        private int currentFrame = 0;
        private DispatcherTimer animationTimer;

        public AnimatedSpriteDisplay()
        {
            InitializeComponent();
            animationTimer = new DispatcherTimer();
            animationTimer.Tick += AnimationTimer_Tick;
        }

        public void LoadSprite(string spritePath, MainWindow.SpriteData data = null)
        {
            try
            {
                if (!File.Exists(spritePath))
                {
                    SpriteImage.Source = null;
                    return;
                }

                // Load the original image
                byte[] imageBytes = File.ReadAllBytes(spritePath);
                originalImage = new BitmapImage();
                using (var ms = new MemoryStream(imageBytes))
                {
                    originalImage.BeginInit();
                    originalImage.CacheOption = BitmapCacheOption.OnLoad;
                    originalImage.StreamSource = ms;
                    originalImage.EndInit();
                    originalImage.Freeze();
                }

                spriteData = data ?? new MainWindow.SpriteData 
                { 
                    frameWidth = originalImage.PixelWidth,
                    frameHeight = originalImage.PixelHeight,
                    frameCount = 1,
                    frameSpeed = 1.0,
                    animated = false
                };

                UpdateDisplay();
                StartAnimation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading sprite: {ex.Message}");
                SpriteImage.Source = null;
            }
        }

        private void UpdateDisplay()
        {
            if (originalImage == null || spriteData == null) return;

            try
            {
                int frameWidth = spriteData.frameWidth > 0 ? spriteData.frameWidth : originalImage.PixelWidth;
                int frameHeight = spriteData.frameHeight > 0 ? spriteData.frameHeight : originalImage.PixelHeight;
                int frameCount = spriteData.frameCount > 0 ? spriteData.frameCount : 1;
                double frameSpeed = spriteData.frameSpeed > 0 ? spriteData.frameSpeed : 1.0;

                // Clamp frameWidth/frameHeight to image size
                frameWidth = Math.Min(frameWidth, originalImage.PixelWidth);
                frameHeight = Math.Min(frameHeight, originalImage.PixelHeight);

                if (spriteData.animated && frameCount > 1)
                {
                    // Clamp currentFrame
                    if (currentFrame >= frameCount) currentFrame = 0;

                    // Ensure cropping rectangle is valid
                    int x = currentFrame * frameWidth;
                    if (x + frameWidth > originalImage.PixelWidth)
                        x = 0; // fallback to first frame

                    var croppedBitmap = new CroppedBitmap(originalImage,
                        new Int32Rect(x, 0, frameWidth, frameHeight));

                    SpriteImage.Source = croppedBitmap;
                    SpriteImage.Width = frameWidth;
                    SpriteImage.Height = frameHeight;
                    this.Width = frameWidth;
                    this.Height = frameHeight;
                }
                else
                {
                    SpriteImage.Source = originalImage;
                    SpriteImage.Width = originalImage.PixelWidth;
                    SpriteImage.Height = originalImage.PixelHeight;
                    this.Width = originalImage.PixelWidth;
                    this.Height = originalImage.PixelHeight;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating sprite display: {ex.Message}");
                SpriteImage.Source = originalImage;
            }
        }

        private void StartAnimation()
        {
            if (spriteData?.animated == true && spriteData.frameCount > 1 && spriteData.frameSpeed > 0)
            {
                animationTimer.Interval = TimeSpan.FromSeconds(1.0 / spriteData.frameSpeed);
                animationTimer.Start();
            }
            else
            {
                StopAnimation();
            }
        }

        private void StopAnimation()
        {
            animationTimer.Stop();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (spriteData?.animated == true && spriteData.frameCount > 1)
            {
                currentFrame = (currentFrame + 1) % spriteData.frameCount;
                UpdateDisplay();
            }
        }

        public void SetAnimationProperties(MainWindow.SpriteData data)
        {
            spriteData = data;
            currentFrame = 0;
            UpdateDisplay();
            StartAnimation();
        }
    }
} 