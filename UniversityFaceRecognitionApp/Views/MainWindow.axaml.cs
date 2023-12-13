using System;
using System.Collections.ObjectModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using UniversityFaceRecognitionApp.Services;
using VisioForge.Core.Types;
using VisioForge.Core.Types.Events;
using VisioForge.Core.Types.VideoCapture;
using VisioForge.Core.UI.Avalonia;
using VisioForge.Core.VideoCapture;


namespace UniversityFaceRecognitionApp.Views;

public partial class MainWindow : Window
{
    private readonly PersonService _personService = new();

    private bool _initialized;
    private VideoCaptureCore _videoCapture1;
    public ObservableCollection<string> VideoInputDevices { get; set; } = new ObservableCollection<string>();
    public ObservableCollection<string> VideoInputFormats { get; set; } = new ObservableCollection<string>();
    public ObservableCollection<string> VideoInputFrameRates { get; set; } = new ObservableCollection<string>();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        InitControls();
        Activated += MainWindow_Activated;
    }

    private void MainWindow_Activated(object sender, EventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        Closing += Window_Closing;
        _initialized = true;
        CreateEngine();
        Title += $" (SDK v{_videoCapture1.SDK_Version()})";
        _videoCapture1.Debug_Dir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge");

        foreach (var device in _videoCapture1.Video_CaptureDevices())
        {
            VideoInputDevices.Add(device.Name);
        }

        if (VideoInputDevices.Count > 0)
        {
            cbVideoInputDevice.SelectedIndex = 0;
        }

        cbVideoInputDevice_SelectionChanged(null, null);
    }

    private void CreateEngine()
    {
        _videoCapture1 = new VideoCaptureCore(VideoView1);

        _videoCapture1.OnError += VideoCapture1_OnError;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        DestroyEngine();
    }

    private void DestroyEngine()
    {
        if (_videoCapture1 == null)
        {
            return;
        }

        _videoCapture1.OnError -= VideoCapture1_OnError;
        _videoCapture1.Dispose();
        _videoCapture1 = null;
    }

    private void VideoCapture1_OnError(object sender, ErrorsEventArgs e)
    {
        Console.WriteLine(e.Message);
    }


    private void InitControls()
    {
        VideoView1 = this.FindControl<VideoView>("VideoView1");

        cbVideoInputDevice = this.FindControl<ComboBox>("cbVideoInputDevice");
        cbVideoInputDevice.SelectionChanged += cbVideoInputDevice_SelectionChanged;
        
        cbVideoInputFormat = this.FindControl<ComboBox>("cbVideoInputFormat");
        cbVideoInputFormat.SelectionChanged += cbVideoInputFormat_SelectionChanged;
        
        cbUseBestVideoInputFormat = this.FindControl<CheckBox>("cbUseBestVideoInputFormat");
        cbUseBestVideoInputFormat.Checked += cbUseBestVideoInputFormat_Checked;
        cbUseBestVideoInputFormat.Unchecked += cbUseBestVideoInputFormat_Checked;
        
        cbVideoInputFrameRate = this.FindControl<ComboBox>("cbVideoInputFrameRate");

        rbPreview = this.FindControl<RadioButton>("rbPreview");

        lbTimestamp = this.FindControl<TextBlock>("lbTimestamp");

        btSaveSnapshot = this.FindControl<Button>("btSaveSnapshot");
        btSaveSnapshot.Click += btSaveSnapshot_Click;

        btStart = this.FindControl<Button>("btStart");
        btStart.Click += btStart_Click;

        btStop = this.FindControl<Button>("btStop");
        btStop.Click += btStop_Click;

        btSaveSnapshot = this.FindControl<Button>("btSaveSnapshot");

        tcMain = this.FindControl<TabControl>("tcMain");
    }
    
    private void cbUseBestVideoInputFormat_Checked(object sender, RoutedEventArgs e)
    {
        cbVideoInputFormat.IsEnabled = cbUseBestVideoInputFormat.IsChecked == false;
    }

    private void cbVideoInputDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cbVideoInputDevice.SelectedIndex != -1 && e is { AddedItems.Count: > 0 })
        {
            var deviceItem = _videoCapture1.Video_CaptureDevices()
                .FirstOrDefault(device => device.Name == e.AddedItems[0].ToString());
            if (deviceItem == null)
            {
                return;
            }
            
            foreach (var format in deviceItem.VideoFormats)
            {
                VideoInputFormats.Add(format.ToString());
            }

            if (VideoInputFormats.Count > 0)
            {
                cbVideoInputFormat.SelectedIndex = 0;
                cbVideoInputFormat_SelectionChanged(null, null);
            }
        }
    }
    
    private async void btSaveSnapshot_Click(object sender, RoutedEventArgs e)
    {
        var sfd = new SaveFileDialog
        {
            InitialFileName = "image.jpg",
            DefaultExtension = ".jpg",
            Directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge")
        };

        var exts = new[] { "jpg", "bmp", "png", "gif", "tiff" };
        foreach (var extension in exts)
        {
            var filter = new FileDialogFilter
            {
                Name = extension.ToUpperInvariant()
            };
            filter.Extensions.Add(extension);
            sfd.Filters.Add(filter);
        }

        var filename = await sfd.ShowAsync(this);

        if (string.IsNullOrEmpty(filename))
        {
            return;
        }
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        switch (ext)
        {
            case ".bmp":
                await _videoCapture1.Frame_SaveAsync(filename, ImageFormat.Bmp, 0);
                break;
            case ".jpg":
                await _videoCapture1.Frame_SaveAsync(filename, ImageFormat.Jpeg, 85);
                break;
            case ".gif":
                await _videoCapture1.Frame_SaveAsync(filename, ImageFormat.Gif, 0);
                break;
            case ".png":
                await _videoCapture1.Frame_SaveAsync(filename, ImageFormat.Png, 0);
                break;
            case ".tiff":
                await _videoCapture1.Frame_SaveAsync(filename, ImageFormat.Tiff, 0);
                break;
        }
    }
    
    private void cbVideoInputFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(cbVideoInputFormat.SelectedItem!.ToString()) || string.IsNullOrEmpty(cbVideoInputDevice.SelectedItem.ToString()))
        {
            return;
        }

        if (cbVideoInputDevice.SelectedIndex != -1)
        {
            var deviceItem = _videoCapture1.Video_CaptureDevices().FirstOrDefault(device => device.Name == cbVideoInputDevice.SelectedItem.ToString());
            if (deviceItem == null)
            {
                return;
            }

            var videoFormat = deviceItem.VideoFormats.Find(format => format.Name == cbVideoInputFormat.SelectedItem.ToString());
            if (videoFormat == null)
            {
                return;
            }

            VideoInputFrameRates.Clear();
            foreach (var frameRate in videoFormat.FrameRates)
            {
                VideoInputFrameRates.Add(frameRate.ToString(CultureInfo.CurrentCulture));
            }

            if (VideoInputFrameRates.Count > 0)
            {
                cbVideoInputFrameRate.SelectedIndex = 0;
            }
        }
    }

    private async void btStart_Click(object sender, RoutedEventArgs e)
    {
        _videoCapture1.Video_Sample_Grabber_Enabled = true;

        _videoCapture1.Debug_Dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VisioForge");

        _videoCapture1.Audio_RecordAudio = false;
        _videoCapture1.Audio_PlayAudio = false;

        _videoCapture1.Video_Renderer_SetAuto();
        
        // apply capture params
        _videoCapture1.Video_CaptureDevice = new VideoCaptureSource(cbVideoInputDevice.SelectedItem.ToString())
            {
                Format = cbVideoInputFormat.SelectedItem.ToString(),
                Format_UseBest = cbUseBestVideoInputFormat.IsChecked == true
            };
        
        if (cbVideoInputFrameRate.SelectedIndex != -1)
        {
            _videoCapture1.Video_CaptureDevice.FrameRate = new VideoFrameRate(Convert.ToDouble(cbVideoInputFrameRate.SelectedItem.ToString()));
        }

        _videoCapture1.Mode = rbPreview.IsChecked == true ? VideoCaptureMode.VideoPreview :
            VideoCaptureMode.VideoCapture;

        await _videoCapture1.StartAsync();

        tcMain.SelectedIndex = 3;
    }
    
    private async void btStop_Click(object sender, RoutedEventArgs e)
    {
        await _videoCapture1.StopAsync();
    }
}