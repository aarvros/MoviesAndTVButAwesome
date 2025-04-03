using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using LibVLCSharp.Shared;
using LibVLCSharp.WinForms;

namespace MTVBAForm{
public class MTVBAView : Form
{
    private LibVLC _libVLC;
    private MediaPlayer _mediaPlayer;
    private VideoView videoView;
    private Button pauseButton;
    private TextBox outputFileName;
    private TrackBar volumeTrackBar;
    private TrackBar trackBar;
    private System.Windows.Forms.Timer trackBarTimer;
    private CustomScrubBar scrubBar;
    private Label currTime;
    private Label mediaLength;
    private Label startTrim;
    private Label endTrim;
    private string[] videoFiles;
    private int currentVideoIndex;
    private readonly int trackBarFidelity = 100; // each value is 10ms, 1000 = each step is 1000ms

    public MTVBAView(){
        Text = "MTVBA";
        ClientSize = new Size(1000, 755);

        string videoDirectory = @"C:\Users\awclo\Desktop\GitHub\MoviesAndTVButAwesome\TestVideos";  // Set to your video folder path
        videoFiles = Directory.GetFiles(videoDirectory, "*.mp4").OrderBy(file => file).ToArray();
        currentVideoIndex = 0;

        TableLayoutPanel panel = new TableLayoutPanel{Dock = DockStyle.Fill,ColumnCount = 3,RowCount = 2,AutoSize = true};
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8f));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 84f));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8f));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 75f));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 25f));

        Button lp = new Button{Dock=DockStyle.Fill, Name="LoadPrevButton", Text="Prev"};
        Button ln = new Button{Dock=DockStyle.Fill, Name="LoadNextButton", Text="Next"};
        lp.Click += LoadPrevVideo;
        ln.Click += LoadNextVideo;

        panel.Controls.Add(lp, 0, 1);
        panel.Controls.Add(ln, 2, 1);

        Core.Initialize();
        _libVLC = new LibVLC("--input-repeat=1000");
        videoView = new VideoView{Dock = DockStyle.Fill, MediaPlayer = new MediaPlayer(_libVLC)};
        _mediaPlayer = videoView.MediaPlayer;
        panel.Controls.Add(videoView, 0, 0);
        panel.SetColumnSpan(videoView, 3);

        TableLayoutPanel controlPanel = new TableLayoutPanel{Dock = DockStyle.Fill,ColumnCount = 1,RowCount = 5,AutoSize = true};
        controlPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
        controlPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
        controlPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
        controlPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
        controlPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));
        panel.Controls.Add(controlPanel, 1, 1);

        TableLayoutPanel topControlPanel = new TableLayoutPanel{Dock = DockStyle.Fill,ColumnCount = 4,RowCount = 1,AutoSize = true};
        topControlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33f));
        topControlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
        topControlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        topControlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13f));
        controlPanel.Controls.Add(topControlPanel, 0, 0);

        volumeTrackBar = new TrackBar{Dock = DockStyle.Fill ,Minimum = 0,Maximum = 100,TickFrequency = 1,SmallChange = 1,LargeChange = 10, Value = 100, TickStyle = TickStyle.None};
        volumeTrackBar.Scroll += AdjVolume;
        topControlPanel.Controls.Add(volumeTrackBar, 0, 0);

        pauseButton = new Button{Text = "Pause",Dock = DockStyle.Top};
        pauseButton.Click += TogglePlayback;
        topControlPanel.Controls.Add(pauseButton, 1, 0);

        outputFileName = new TextBox{Dock = DockStyle.Top};
        topControlPanel.Controls.Add(outputFileName, 2, 0);

        Button trimButton = new Button{Text = "Trim",Dock = DockStyle.Top};
        trimButton.Click += TrimMedia;
        topControlPanel.Controls.Add(trimButton, 3, 0);

        trackBar = new TrackBar{Dock = DockStyle.Fill,Minimum = 0,Maximum = 1,TickFrequency = 1,SmallChange = 1,LargeChange = 1, TickStyle = TickStyle.None};
        trackBar.Scroll += TrackBar_Scroll;
        trackBar.Scroll += UpdateTimeLabel;
        controlPanel.Controls.Add(trackBar, 0, 1);

        TableLayoutPanel mediaTimePanel = new TableLayoutPanel{Dock = DockStyle.Fill,ColumnCount = 2,RowCount = 1,AutoSize = true};
        controlPanel.Controls.Add(mediaTimePanel, 0, 2);

        currTime = new Label{Dock = DockStyle.Left, Text = "00:00:00:00"};
        mediaLength = new Label{Dock = DockStyle.Right, Text = "00:00:00:00", TextAlign = System.Drawing.ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right};
        mediaTimePanel.Controls.Add(currTime, 0, 0);
        mediaTimePanel.Controls.Add(mediaLength, 1, 0);

        scrubBar = new CustomScrubBar{Dock = DockStyle.Fill};
        controlPanel.Controls.Add(scrubBar, 0, 3);

        TableLayoutPanel trimTimePanel = new TableLayoutPanel{Dock = DockStyle.Fill,ColumnCount = 2,RowCount = 1,AutoSize = true};
        controlPanel.Controls.Add(trimTimePanel, 0, 4);

        startTrim = new Label{Dock = DockStyle.Left, Text = "00:00:00"};
        endTrim = new Label{Dock = DockStyle.Right, Text = "00:00:00", TextAlign = System.Drawing.ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right};
        trimTimePanel.Controls.Add(startTrim, 0, 0);
        trimTimePanel.Controls.Add(endTrim, 1, 0);

        scrubBar.OnStartTrimChanged += (timestamp) => startTrim.Text = $"{timestamp}";
        scrubBar.OnEndTrimChanged += (timestamp) => endTrim.Text = $"{timestamp}";

        trackBarTimer = new System.Windows.Forms.Timer { Interval = 100 };
        trackBarTimer.Tick += UpdateTrackBar;
        trackBarTimer.Tick += UpdateTimeLabel;

        Controls.Add(panel);

        LoadCurrentVideo();
    }

    private void AdjVolume(object? sender, EventArgs e){
        if (_mediaPlayer != null){
            _mediaPlayer.Volume = volumeTrackBar.Value;
        }
    }

    private void TrackBar_Scroll(object? sender, EventArgs e){
        if (_mediaPlayer != null && _mediaPlayer.Length > 0){
            //Console.WriteLine($"Setting time to {trackBar.Value * trackBarFidelity} ms with max {trackBar.Maximum}");
            _mediaPlayer.Time = trackBar.Value * trackBarFidelity;
        }
    }

    private void UpdateTrackBar(object? sender, EventArgs e){
        if (_mediaPlayer.IsPlaying){
            if (_mediaPlayer.Length > 0){
                long videoLength = _mediaPlayer.Length;
                if(trackBar.Maximum == 1){
                    //Console.WriteLine($"Setting trackbar max to {(int)(videoLength / trackBarFidelity)} s");
                    trackBar.Maximum = (int)(videoLength / trackBarFidelity);   // max is video length in seconds
                    scrubBar.TotalDurationMs = videoLength;
                }
                try{
                    trackBar.Value = (int)(_mediaPlayer.Time / trackBarFidelity);   // set the value to the current time in seconds
                }catch(Exception){
                    
                }
            }
        }
    }

    private void UpdateTimeLabel(object? sender, EventArgs e){
        currTime.Text = FormatTime(_mediaPlayer.Time);
        if (_mediaPlayer.Length > 0){
            mediaLength.Text = FormatTime(_mediaPlayer.Length);
        }
    }

    private void LoadCurrentVideo(){
        if (videoFiles.Length == 0){
            MessageBox.Show("No videos in video files", "warn", MessageBoxButtons.OK);
            return;
        }
        string currentVideoPath = videoFiles[currentVideoIndex];
        Text = $"MTVBA - {currentVideoPath.Split('\\').Last()}";

        using var media = new Media(_libVLC, new Uri(currentVideoPath));
        _mediaPlayer.Media = media;
        
        trackBar.Maximum = 1;
        scrubBar.ResetTrim();
        TogglePlayback();
    }

    private void TogglePlayback(object? sender, EventArgs e){
        TogglePlayback();
    }

    private void TogglePlayback(){
        if(trackBar.Value >= trackBar.Maximum){
            trackBar.Value = trackBar.Maximum - 1;
        }
        if (_mediaPlayer.IsPlaying){
            _mediaPlayer.Pause();
            trackBarTimer.Stop();
            pauseButton.Text = "Play";
        }else{
            _mediaPlayer.Play();
            trackBarTimer.Start();
            pauseButton.Text = "Pause";
        }
    }

    private void TrimMedia(object? sender, EventArgs e){
        if(_mediaPlayer.IsPlaying){
            TogglePlayback();
        }

        string inputPath = videoFiles[currentVideoIndex];
        string inputVideoDir = Path.GetDirectoryName(inputPath)!;
        string outputPath;

        if(outputFileName.Text.Trim() == ""){
            outputPath = inputPath[..^4] + "_Trim.mp4";
        }else{
            outputPath = $"{inputVideoDir}\\{outputFileName.Text.Trim()}.mp4";
        }

        if(File.Exists(outputPath)){
            MessageBox.Show($"File {outputPath} already exists!", "File Already Exists", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        string startTime = startTrim.Text;
        string endTime = endTrim.Text;

        TimeSpan start = TimeSpan.Parse(startTime);
        TimeSpan end = TimeSpan.Parse(endTime);
        TimeSpan duration = end - start;

        string ffmpegPath = @"ffmpeg\ffmpeg.exe";
        string arguments = $"-i \"{inputPath}\" -ss {startTime} -t {duration} -c:v copy -c:a copy \"{outputPath}\"";

        // Start the FFmpeg process
        ProcessStartInfo processStartInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try{
        using (Process process = Process.Start(processStartInfo)!){}
        MessageBox.Show($"Successfully saved trim to {outputPath}", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }catch (Exception ex){
            MessageBox.Show($"Error running FFmpeg: {ex.Message}", "FFmpeg Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Console.WriteLine("Error running FFmpeg: " + ex.Message);
        }
    }

    private void LoadPrevVideo(object? sender, EventArgs e){
        if(currentVideoIndex <= 0){
            MessageBox.Show("This button should be disabed", "How did this happen?", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        currentVideoIndex--;
        Controls.Find("LoadNextButton", true)[0].Enabled = true;
        if(currentVideoIndex <= 0){
            Controls.Find("LoadPrevButton", true)[0].Enabled = false;
        }

        LoadCurrentVideo();
    }

    private void LoadNextVideo(object? sender, EventArgs e){
        if(currentVideoIndex >= videoFiles.Length){
            MessageBox.Show("This button should be disabed", "How did this happen?", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return;
        }

        currentVideoIndex++;
        Controls.Find("LoadPrevButton", true)[0].Enabled = true;
        if(currentVideoIndex >= videoFiles.Length-1){
            Controls.Find("LoadNextButton", true)[0].Enabled = false;
        }
        
        LoadCurrentVideo();
    }

    private string FormatTime(long milliseconds){
        TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);
        return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
    }
}
}