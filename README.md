# [Analysing visual content using HoloLens, Computer Vision APIs, Unity and the Mixed Reality Toolkit](https://davide.dev/analysing-visual-content-using-hololens-computer-vision-apis-unity-and-the-windows-mixed-reality-toolkit/)

Reuse some code from:<br />
https://github.com/JannikLassahn/hololens-photocapture<br />
https://blogs.windows.com/buildingapps/2017/02/13/cognitive-services-apis-vision/#kGJJ5dKM9yWTOhjD.97<br />

(This article was originally published @ https://davide.dev)

In these days, I’m exploring the combination of HoloLens/Windows Mixed Reality and the capabilities offered by Cognitive Services to analyse and extract information from images captured via the device camera and processed using the Computer Vision APIs and the intelligent cloud.
In this article, we’ll explore the steps I followed for creating a Unity application running on HoloLens and communicating with the Microsoft AI platform.
<h1>Registering for Computer Vision APIs</h1>
The first step was to navigate to the Azure portal <a href="https://portal.azure.com/" target="_blank" rel="noopener">https://portal.azure.com</a> and create a new <strong>Computer Vision API</strong> resource:

<img class="aligncenter size-large wp-image-8236" src="../wp-content/uploads/2018/02/01-Azure-Portal-1024x717.png" alt="" width="660" height="462" />

I noted down the <strong>Keys</strong> and <strong>Endpoint</strong> and started investigating how to approach the code for capturing images on HoloLens and sending them to the intelligent cloud for processing.

Before creating the Unity experience, I decided to start with a simple UWP app for analysing images.
<h1>Writing the UWP test app and the shared library</h1>
There are already some samples available for Cognitive Services APIs, so I decided to reuse some code available and described in this article <a href="https://blogs.windows.com/buildingapps/2017/02/13/cognitive-services-apis-vision/#kGJJ5dKM9yWTOhjD.97" target="_blank" rel="noopener">here</a> supplemented by some camera capture UI in UWP.

I created a new Universal Windows app and library (<strong>CognitiveServicesVisionLibrary</strong>) to provide, respectively, a test UI and some reusable code that could be referenced later by the HoloLens experience.

<img class="aligncenter size-full wp-image-8237" src="https://davide.dev/wp-content/uploads/2018/02/02-Visual-Studio-Solution.png" alt="" width="985" height="780" />

The Computer Vision APIs can be accessed via the package <strong>Microsoft.ProjectOxford.Vision </strong>available on NuGet so I added a reference to both projects:

<img class="aligncenter size-large wp-image-8238" src="https://davide.dev/wp-content/uploads/2018/02/03-NuGet-Microsoft.ProjectOxford.Vision-1024x673.png" alt="" width="660" height="434" />

The test UI contains an image and two buttons: one for selecting a file using a <strong>FileOpenPicker</strong> and another for capturing a new image using the <strong>CameraCaptureUI</strong>. I decided to wrap these two actions in an <strong>InteractionsHelper</strong> class:
<pre title="LUIS classes" class="lang:default decode:true ">
public class InteractionsHelper
{
    CognitiveVisionHelper _visionHelper;

    public InteractionsHelper()
    {
        _visionHelper = new CognitiveVisionHelper();
    }

    public async Task&lt;InteractionsResult&gt; RecognizeUsingCamera()
    {
        CameraCaptureUI captureUI = new CameraCaptureUI();
        captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;

        StorageFile file = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

        return await BuildResult(file);
    }

    public async Task&lt;InteractionsResult&gt; RecognizeFromFile()
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

    private async Task&lt;InteractionsResult&gt; BuildResult(StorageFile file)
    {
        InteractionsResult result = null;

        if (file != null)
        {
            var bitmap = await _visionHelper.ConvertImage(file);
            var results = await _visionHelper.AnalyzeImage(file);
            var output = _visionHelper.ExtractOutput(results);
            result = new InteractionsResult {
                Description = output,
                Image = bitmap,
                AnalysisResult = results
            };
        }

        return result;
    }
}
</pre>

I then worked on the shared library creating a helper class for processing the image using the Vision APIs available in <strong>Microsoft.ProjectOxford.Vision</strong> and parsing the result.

<em>Tip: after creating the VisionServiceClient, I received an unauthorised error when specifying only the key: the error disappeared by also specifying the endpoint URL available in the Azure portal.</em>

{% highlight csharp %}
using Microsoft.ProjectOxford.Vision;
using Microsoft.ProjectOxford.Vision.Contract;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

public class CognitiveVisionHelper
{
    string _subscriptionKey;
    string _apiRoot;

    public CognitiveVisionHelper()
    {
        _subscriptionKey = "SubKey";
        _apiRoot = "ApiRoot";
    }

    private VisionServiceClient GetVisionServiceClient()
    {
        return new VisionServiceClient(_subscriptionKey, _apiRoot);
    }

    public async Task&lt;ImageSource&gt; ConvertImage(StorageFile file)
    {
        var bitmapImage = new BitmapImage();
        FileRandomAccessStream stream = (FileRandomAccessStream)await file.OpenAsync(FileAccessMode.Read);
        bitmapImage.SetSource(stream);
        return bitmapImage;
    }

    public async Task&lt;AnalysisResult&gt; AnalyzeImage(StorageFile file)
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

    public async Task&lt;OcrResults&gt; AnalyzeImageForText(StorageFile file, string language)
    {
        var VisionServiceClient = GetVisionServiceClient();
        using (Stream imageFileStream = await file.OpenStreamForReadAsync())
        {
            OcrResults ocrResult = await VisionServiceClient.RecognizeTextAsync(imageFileStream, language);
            return ocrResult;
        }
    }

    public string ExtractOutput(AnalysisResult analysisResult)
    {
        return analysisResult.Description.Captions[0].Text;
    }
}
{% endhighlight %}

I then launched the test UI, and the image was successfully analysed, and the results returned from the Computer Vision APIs, in this case identifying a building and several other tags like outdoor, city, park: great!

<img class="aligncenter size-large wp-image-8239" src="https://davide.dev/wp-content/uploads/2018/02/04-Running-the-UWP-test-app-1024x800.png" alt="" width="660" height="516" />

I also added a Speech Synthesizer playing the general description returned by the Cognitive Services call:

{% highlight csharp %}
private async void Speak(string Text)
{
    MediaElement mediaElement = this.media;
    var synth = new SpeechSynthesizer();
    SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(Text);
    mediaElement.SetSource(stream, stream.ContentType);

    mediaElement.Play();
}
{% endhighlight %}

I then moved to HoloLens and started creating the interface using Unity, the Mixed Reality Toolkit and UWP.
<h1>Creating the Unity HoloLens experience</h1>
First of all, I created a new Unity project using Unity 2017.2.1p4 and then added a new folder named <strong>Scenes</strong> and saved the active scene as <strong>CognitiveServicesVision Scene</strong>.

I downloaded the corresponding version of the Mixed Reality Toolkit from the releases section of the GitHub project <a href="https://github.com/Microsoft/MixedRealityToolkit-Unity/releases" target="_blank" rel="noopener">https://github.com/Microsoft/MixedRealityToolkit-Unity/releases</a> and imported the toolkit package HoloToolkit-Unity-2017.2.1.1.unitypackage using the menu <em>Assets-&gt;Import Package-&gt;Custom package.</em>

Then, I applied the <strong>Mixed Reality Project settings</strong> using the corresponding item in the toolkit menu:

<img class="aligncenter size-full wp-image-8241" src="https://davide.dev/wp-content/uploads/2018/02/05-MRTK-Project-Settings.png" alt="" width="871" height="941" />

And selected the Scene Settings adding the <strong>Camera</strong>, <strong>Input Manager </strong>and <strong>Default Cursor</strong> prefabs:

<img class="aligncenter size-full wp-image-8242" src="https://davide.dev/wp-content/uploads/2018/02/06-MRTK-Scene-Settings.png" alt="" width="867" height="692" />

And finally set the UWP capabilities as I needed access to the camera for retrieving the image, the microphone for speech recognition and internet client for communicating with Cognitive Services:

<img class="aligncenter size-full wp-image-8243" src="https://davide.dev/wp-content/uploads/2018/02/07-MRTK-UWP-Capabilities.png" alt="" width="870" height="943" />

I was then ready to add the logic to retrieve the image from the camera, save it to the HoloLens device and then call the Computer Vision APIs.
<h1>Creating the Unity Script</h1>
The <strong>CameraCaptureUI</strong> UWP API is not available in HoloLens, so I had to research a way to capture an image from Unity, save it to the device and then convert it to a <strong>StorageFile</strong> ready to be used by the <strong>CognitiveServicesVisionLibrary </strong>implemented as part of the previous project.

First of all, I enabled the <strong>Experimental (.NET 4.6 Equivalent)</strong> Scripting Runtime version in the Unity player for using features like <strong>async/await. </strong>Then, I enabled the <strong>PicturesLibrary</strong> capability in the Publishing Settings to save the captured image to the device.

<img class="aligncenter size-large wp-image-8244" src="https://davide.dev/wp-content/uploads/2018/02/08-Player-settings-1024x549.png" alt="" width="660" height="354" />

Then, I created a <em>Scripts</em> folder and added a new <strong>PhotoManager.cs</strong> script taking as a starting point the implementation available in <a href="https://github.com/JannikLassahn/hololens-photocapture" target="_blank" rel="noopener">this</a> GitHub project.

The script can be attached to a <strong>TextMesh</strong> component visualising the status:

{% highlight csharp %}
using HoloToolkit.Unity;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.WSA.WebCam;

#if UNITY_UWP
using Windows.Storage;
#endif

public class PhotoManager : MonoBehaviour
{
    private TextMesh _statusText;
    private PhotoCapture _capture;
    private TextToSpeech _textToSpeechComponent;
    private bool _isCameraReady = false;
    private string _currentImagePath;
    private string _pictureFolderPath;
#if UNITY_UWP
    private CognitiveServicesVisionLibrary.CognitiveVisionHelper _cognitiveHelper;
#endif
    private void Start()
    {
        _statusText = GetComponent&lt;TextMesh&gt;();
        _textToSpeechComponent = GetComponent&lt;TextToSpeech&gt;();

#if UNITY_UWP
        _cognitiveHelper = new CognitiveServicesVisionLibrary.CognitiveVisionHelper();
#endif
        StartCamera();
    }
#if UNITY_UWP
    private async void getPicturesFolderAsync() {
        StorageLibrary picturesStorage = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
        _pictureFolderPath = picturesStorage.SaveFolder.Path;
    }
#endif
{% endhighlight %}

Initialising the <strong>PhotoCapture</strong> API available in Unity <a href="https://docs.unity3d.com/Manual/windowsholographic-photocapture.html" target="_blank" rel="noopener">https://docs.unity3d.com/Manual/windowsholographic-photocapture.html</a>

{% highlight csharp %}
public void StartCamera()
{
    PhotoCapture.CreateAsync(true, OnPhotoCaptureCreated);

#if UNITY_UWP
    getPicturesFolderAsync();
#endif
}
private void OnPhotoCaptureCreated(PhotoCapture captureObject)
{
    _capture = captureObject;

    Resolution resolution = PhotoCapture.SupportedResolutions.OrderByDescending(res =&gt; res.width * res.height).First();

    var camera = new CameraParameters(WebCamMode.PhotoMode)
    {
        hologramOpacity = 1.0f,
        cameraResolutionWidth = resolution.width,
        cameraResolutionHeight = resolution.height,
        pixelFormat = CapturePixelFormat.BGRA32
    };

    _capture.StartPhotoModeAsync(camera, OnPhotoModeStarted);
}
private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
{
    _isCameraReady = result.success;
    SetStatus("Camera ready. Say 'Describe' to start");
}
And then capturing an image using the TakePhoto() public method:
public void TakePhoto()
{
    if (_isCameraReady)
    {
        var fileName = string.Format(@"Image_{0:yyyy-MM-dd_hh-mm-ss-tt}.jpg", DateTime.Now);
        _currentImagePath = Application.persistentDataPath + "/" + fileName;

        _capture.TakePhotoAsync(_currentImagePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
    }
    else
    {
        SetStatus("The camera is not yet ready.");
    }
}
{% endhighlight %}

Saving the photo to the pictures library folder and then passing it to the library created in the previous section:

{% highlight csharp %}
private async void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
{
    if (result.success)
    {

#if UNITY_UWP
        try 
        {
            if(_pictureFolderPath != null)
            {
                var newFile = System.IO.Path.Combine(_pictureFolderPath, "Camera Roll", 
                    System.IO.Path.GetFileName(_currentImagePath));
                if (System.IO.File.Exists(newFile))
                {
                    System.IO.File.Delete(newFile);
                }
                System.IO.File.Move(_currentImagePath, newFile);
                var storageFile = await StorageFile.GetFileFromPathAsync(newFile);

                SetStatus("Analysing picture...");

                var visionResult = await _cognitiveHelper.AnalyzeImage(storageFile);
                var description = _cognitiveHelper.ExtractOutput(visionResult);

                SetStatus(description);
            }
        } 
        catch(Exception e) 
        {
            SetStatus("Error processing image");
        }
#endif
    }
    else
    {
        SetStatus("Failed to save photo");
    }
}
{% endhighlight %}

The code references the <strong>CognitiveServicesVisionLibrary</strong> UWP library created previously: to use it from Unity, I created a new <em>Plugins </em>folder in my project and ensured that the Build output of the Visual Studio library project was copied to this folder:

<img class="aligncenter size-large wp-image-8245" src="https://davide.dev/wp-content/uploads/2018/02/09-Visual-Studio-Project-Settings-1024x292.png" alt="" width="660" height="188" />And then set the import settings in Unity for the custom library:

<img class="aligncenter size-large wp-image-8246" src="https://davide.dev/wp-content/uploads/2018/02/10-Unity-import-settings-684x1024.png" alt="" width="660" height="988" />

And for the NuGet library too:

<img class="aligncenter size-large wp-image-8247" src="https://davide.dev/wp-content/uploads/2018/02/11-Unity-import-settings-CognitiveVision-855x1024.png" alt="" width="660" height="790" />

Nearly there! Let’s see now how I enabled Speech recognition and Tagalong/Billboard using the Mixed Reality Toolkit.
<h1>Enabling Speech</h1>
I decided to implement a very minimal UI for this project, using the speech capabilities available in HoloLens for all the interactions.

In this way, a user can just simply say the work <strong>Describe</strong> to trigger the image acquisition and the processing using the Computer Vision API, and then naturally listening to the results.

In the Unity project, I selected the <strong>InputManager</strong> object:

<img class="aligncenter size-large wp-image-8249" src="https://davide.dev/wp-content/uploads/2018/02/12-InputManager-672x1024.png" alt="" width="660" height="1006" />

And added a new <strong>Speech Input Handler Component</strong> to it:

<img class="aligncenter size-large wp-image-8250" src="https://davide.dev/wp-content/uploads/2018/02/13-Speech-Input-Handler-824x1024.png" alt="" width="660" height="820" />

Then, I mapped the keyword <strong>Describe</strong> with the <strong>TakePhoto() </strong>method available in the <strong>PhotoManager.cs</strong> script already attached to the TextMesh that I previously named as <strong>Status Text Object</strong>.

The last step required to enable <strong>Text to Speech</strong> for receiving the output: I simply added a <strong>Text to Speech</strong> component to my TextMesh:

<img class="aligncenter size-large wp-image-8251" src="https://davide.dev/wp-content/uploads/2018/02/14-Text-to-Speech-1024x386.png" alt="" width="660" height="249" />

And enabled the speech in the script using <strong>StartSpeaking()</strong>:

{% highlight csharp %}
…
  _textToSpeechComponent = GetComponent&lt;TextToSpeech&gt;();
…

private void SetStatus(string statusText)
{
    _statusText.text = statusText;
    Speak(statusText);
}

private void Speak(string description)
{
    _textToSpeechComponent.StartSpeaking(description);
}
{% endhighlight %}

I also added other two components available in the Mixed Reality Toolkit: <strong>Tagalong</strong> and <strong>Billboard</strong> to have the status text following me and not anchored to a specific location:

<img class="aligncenter size-large wp-image-8252" src="https://davide.dev/wp-content/uploads/2018/02/15-TagAlong-and-Billboard-1024x769.png" alt="" width="660" height="496" />

I was then able to generate the final package using Unity specifying the starting scene:

<img class="aligncenter size-large wp-image-8253" src="https://davide.dev/wp-content/uploads/2018/02/16-Generating-the-package-1024x991.png" alt="" width="660" height="639" />And then I deployed the solution to the HoloLens device and started extracting and categorising visual data using HoloLens, Camera, Speech and the Cognitive Services Computer Vision APIs.
<h1>Conclusions</h1>
The combination of Mixed Reality and Cognitive Services opens a new world of experiences combining the capabilities of HoloLens and all the power of the intelligent cloud. In this article, I’ve analysed the Computer Vision APIs, but a similar approach could be applied to augment Windows Mixed Reality apps and enrich them with the AI platform <a href="https://www.microsoft.com/en-gb/ai" target="_blank" rel="noopener">https://www.microsoft.com/en-gb/ai</a>.
