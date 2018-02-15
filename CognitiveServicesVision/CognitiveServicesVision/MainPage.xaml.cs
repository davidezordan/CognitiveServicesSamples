using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace CognitiveServicesVision
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        readonly string _subscriptionKey;
        readonly string _apiRoot;

        public MainPage()
        {
            this.InitializeComponent();
            //set your key here
            _subscriptionKey = "YourKey";
            _apiRoot = "ApiRoot";
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            var openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".gif");
            openPicker.FileTypeFilter.Add(".bmp");
            var file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                await ShowPreviewAndAnalyzeImage(file);
            }
        }

        private async Task<ImageSource> LoadImage(StorageFile file)
        {
            var bitmapImage = new BitmapImage();
            FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);
            bitmapImage.SetSource(stream);
            return bitmapImage;
        }

        private async Task ShowPreviewAndAnalyzeImage(StorageFile file)
        {
            var bitmap = await LoadImage(file);
            ImageToAnalyze.Source = bitmap;

            var results = await AnalyzeImage(file);
            var ocrResults = await AnalyzeImageForText(file, "en");

            var output = JsonConvert.SerializeObject(results);
            var ocrOutput = JsonConvert.SerializeObject(ocrResults);

            ResultsTextBlock.Text = output + "\n\n" + ocrOutput;
        }

        private async Task<AnalysisResult> AnalyzeImage(StorageFile file)
        {

            var VisionServiceClient = GetVisionServiceClient();

            using (Stream imageFileStream = await file.OpenStreamForReadAsync())
            {
                // Analyze the image for all visual features
                VisualFeature[] visualFeatures = new VisualFeature[] { VisualFeature.Adult, VisualFeature.Categories
            , VisualFeature.Color, VisualFeature.Description, VisualFeature.Faces, VisualFeature.ImageType
            , VisualFeature.Tags };
                AnalysisResult analysisResult = await VisionServiceClient.AnalyzeImageAsync(imageFileStream, visualFeatures);
                return analysisResult;
            }
        }

        private async Task<OcrResults> AnalyzeImageForText(StorageFile file, string language)
        {
            var VisionServiceClient = GetVisionServiceClient();
            using (Stream imageFileStream = await file.OpenStreamForReadAsync())
            {
                OcrResults ocrResult = await VisionServiceClient.RecognizeTextAsync(imageFileStream, language);
                return ocrResult;
            }
        }

        private VisionServiceClient GetVisionServiceClient()
        {
            return new VisionServiceClient(_subscriptionKey, _apiRoot);
        }
    }
}
