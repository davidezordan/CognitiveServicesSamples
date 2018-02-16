using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace CognitiveServicesVisionLibrary
{
    public class CognitiveVisionHelper
    {
        string _subscriptionKey;
        string _apiRoot;

        public CognitiveVisionHelper()
        {
            _subscriptionKey = "SubscriptionKey";
            _apiRoot = "ApiKey";
        }

        public async Task<ImageSource> ConvertImage(StorageFile file)
        {
            var bitmapImage = new BitmapImage();
            FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);
            bitmapImage.SetSource(stream);
            return bitmapImage;
        }

        public async Task<AnalysisResult> AnalyzeImage(StorageFile file)
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

        public async Task<OcrResults> AnalyzeImageForText(StorageFile file, string language)
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

        public string ExtractOutput(AnalysisResult analysisResult)
        {
            return analysisResult.Description.Captions[0].Text;
        }
    }
}
