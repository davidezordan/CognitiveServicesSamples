using System;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace CognitiveServicesVisionLibrary
{
    public class InteractionsHelper
    {
        CognitiveVisionHelper _visionHelper;

        public InteractionsHelper()
        {
            _visionHelper = new CognitiveVisionHelper();
        }

        public async Task<InteractionsResult> RecognizeUsingCamera()
        {
            CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;

            StorageFile file = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

            return await BuildResult(file);
        }

        public async Task<InteractionsResult> RecognizeFromFile()
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

            return await BuildResult(file);
        }

        private async Task<InteractionsResult> BuildResult(StorageFile file)
        {
            InteractionsResult result = null;

            if (file != null)
            {
                var bitmap = await _visionHelper.ConvertImage(file);
                var results = await _visionHelper.AnalyzeImage(file);
                //var ocrResults = await _visionHelper.AnalyzeImageForText(file, "en");
                var output = _visionHelper.ExtractOutput(results);
                result = new InteractionsResult {
                    Description = output,
                    Image = bitmap,
                    AnalysisResult = results,
                    //OcrResults = ocrResults
                };
            }

            return result;
        }
    }
}
