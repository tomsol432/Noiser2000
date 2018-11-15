using Microsoft.Win32;
using SharpNoise;
using SharpNoise.Builders;
using SharpNoise.Modules;
using SharpNoise.Utilities.Imaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Noiser2000
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Bitmap NoiseBitmapTerrain;
        Bitmap NoiseBitmapHeight;
        public MainWindow()
        {

            InitializeComponent();
            
        }
        
        private void buttonGenerate_Click(object sender, RoutedEventArgs e)
        {
            GenerateNoiseMaps();

        }

        private void GenerateNoiseMaps()
        {
            tbLogView.Text = "Started\r\n";
            int SizeX;
            int SizeY;
            try
            {
                SizeX = Int32.Parse(tbWidth.Text);
                SizeY = Int32.Parse(tbHeight.Text);

                NoiseBitmapTerrain = new Bitmap(SizeX, SizeY);
                NoiseBitmapHeight = new Bitmap(SizeX, SizeY);
                tbLogView.AppendText("Size OK \r\n");
            }
            catch (Exception)
            {
                SizeX = 250;
                SizeY = 250;
                MessageBox.Show("Nonparsable size values! Running at 250x250\r\n");
                NoiseBitmapTerrain = new Bitmap(250, 250);
                NoiseBitmapTerrain = new Bitmap(250, 250);
                tbLogView.AppendText("Size was incorrect, going default\r\n");
            }
            tbLogView.AppendText("Loading Perlin from Noiser(legacy)\r\n");
            SharpNoise.Modules.Perlin perlin = new SharpNoise.Modules.Perlin();
            tbLogView.AppendText("Loading OK\r\n");
            try
            {
                perlin.OctaveCount = Int32.Parse(tbOctaves.Text);
                perlin.Seed = Int32.Parse(tbSeed.Text);
                perlin.Frequency = Double.Parse(tbFreq.Text);
                tbLogView.AppendText("Settings OK\r\n");
            }
            catch (Exception)
            {
                MessageBox.Show("Wrong octaves count or seed! Running at 1 octave and seed 000000 @ Frequency = 10.");
                perlin.OctaveCount = 1;
                perlin.Seed = 000000;
                perlin.Frequency = 10.0;
                tbLogView.AppendText("Setting incorrect, going default\r\n");
            }
            double down, up, right, left;
            try
            {
                down = Double.Parse(tbTileDown.Text);
                up = Double.Parse(tbTileUp.Text);
                left = Double.Parse(tbTileLeft.Text);
                right = Double.Parse(tbTileRight.Text);
                tbLogView.AppendText("Tiles OK\r\n");
            }
            catch (Exception)
            {
                up = 3;
                left = -3;
                right = 3;
                down = -3;
                tbLogView.AppendText("Tiles incorrect, going default\r\n");

            }
            var NoiseMap = new NoiseMap(SizeX, SizeY);
            if (comboModuleSelector.SelectedIndex == 0)
            {
                var NoiseMapBuilder = new PlaneNoiseMapBuilder() { DestNoiseMap = NoiseMap, SourceModule = perlin };
                tbLogView.AppendText("Module OK, Destination OK\r\n");
                NoiseMapBuilder.SetDestSize(SizeX, SizeY);
               
                NoiseMapBuilder.SetBounds(left, right, down, up);
                tbLogView.AppendText("Building maps.....\r\n");
                NoiseMapBuilder.Build();
            }
            if (comboModuleSelector.SelectedIndex == 1)
            {
                var GlobeMapBuilder = new SphereNoiseMapBuilder() { DestNoiseMap = NoiseMap, SourceModule = perlin };
                tbLogView.AppendText("Module OK, Destination OK\r\n");
                GlobeMapBuilder.SetDestSize(SizeX, SizeY);
                GlobeMapBuilder.SetBounds(down, up, left, right);
                GlobeMapBuilder.Build();
                tbLogView.AppendText("Building maps.....\r\n");


            }
            tbLogView.AppendText("Building OK\r\n");

            var ImageTerrain = new SharpNoise.Utilities.Imaging.Image();
            var RendererTerrain = new ImageRenderer()
            {
                SourceNoiseMap = NoiseMap,
                DestinationImage = ImageTerrain

            };
            
            tbLogView.AppendText("Renderer starting\r\n");
            
            if (chboxLightMap.IsChecked == true)
            {
                RendererTerrain.EnableLight = true;
                RendererTerrain.LightAzimuth = Double.Parse(tbLightAzimuth.Text);
                RendererTerrain.LightBrightness = Double.Parse(tbLightBrightness.Text);
                RendererTerrain.LightContrast = Double.Parse(tbLightContrast.Text);
                RendererTerrain.LightElevation = Double.Parse(tbLightElevation.Text);
                RendererTerrain.LightIntensity = Double.Parse(tbLightIntensity.Text);
                
  
            }
            Thread ColorBuilder = new Thread(() =>
            {
                RendererTerrain.BuildTerrainGradient();
                RendererTerrain.Render();
                
                NoiseBitmapTerrain = ImageTerrain.ToGdiBitmap();
                ImageNoiseHolder.Dispatcher.Invoke(new Action(() =>
                {
                    ImageNoiseHolder.Source = BitmapToImageSource(NoiseBitmapTerrain);
                    tbLogView.AppendText("Done! Noise map OK, renderer OK\r\n");
                }));
            });
            ColorBuilder.Start();

            var ImageTerrainHeight = new SharpNoise.Utilities.Imaging.Image();
            var RendererTerrainHeight = new ImageRenderer()
            {
                SourceNoiseMap = NoiseMap,
                DestinationImage = ImageTerrainHeight

            };


            Thread heightBuilder = new Thread(() =>
            {
                RendererTerrainHeight.BuildGrayscaleGradient();
                RendererTerrainHeight.Render();
                NoiseBitmapHeight = ImageTerrainHeight.ToGdiBitmap();

                ImageNoiseHeightHolder.Dispatcher.Invoke(new Action(() =>
                {
                    ImageNoiseHeightHolder.Source = BitmapToImageSource(NoiseBitmapHeight);
                    tbLogView.AppendText("Done! Noise map OK, renderer OK\r\n");
                }));
            });
            heightBuilder.Start();

            tbLogView.AppendText("Process status: OK\r\n");


        }
        BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }

        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.ShowDialog();

            NoiseBitmapTerrain.Save(saveFileDialog.FileName + ".jpeg", ImageFormat.Jpeg);

            saveFileDialog = new SaveFileDialog();
            saveFileDialog.ShowDialog();
            NoiseBitmapHeight.Save(saveFileDialog.FileName + ".jpeg", ImageFormat.Jpeg);

        }

        private void tbLogView_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            tbLogView.ScrollToEnd();
        }
    }

}
