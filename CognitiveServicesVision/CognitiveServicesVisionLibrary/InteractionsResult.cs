using Microsoft.ProjectOxford.Vision.Contract;
using Windows.UI.Xaml.Media;

namespace CognitiveServicesVisionLibrary
{
    public class InteractionsResult
    {
        public string Description { get; set; }
        public ImageSource Image { get; set; }
        public AnalysisResult AnalysisResult { get; set; }
        public OcrResults OcrResults { get; set; }
    }
}
