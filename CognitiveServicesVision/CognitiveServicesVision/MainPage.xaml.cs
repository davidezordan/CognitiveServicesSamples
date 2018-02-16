using CognitiveServicesVisionLibrary;
using System;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace CognitiveServicesVision
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        CognitiveVisionHelper _visionHelper;
        InteractionsHelper _interactionsHelper;

        public MainPage()
        {
            this.InitializeComponent();

            _visionHelper = new CognitiveVisionHelper();
            _interactionsHelper = new InteractionsHelper();
        }

        private async void CameraButton_Click(object sender, RoutedEventArgs e)
        {
            var output = await _interactionsHelper.RecognizeUsingCamera();
            ImageToAnalyze.Source = output.Image;
            Speak(output.Description);
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            var output = await _interactionsHelper.RecognizeFromFile();
            ImageToAnalyze.Source = output.Image;
            Speak(output.Description);
        }

        private async void Speak(string Text)
        {
            MediaElement mediaElement = this.media;
            var synth = new SpeechSynthesizer();
            SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(Text);
            mediaElement.SetSource(stream, stream.ContentType);

            mediaElement.Play();
        }
    }
}
