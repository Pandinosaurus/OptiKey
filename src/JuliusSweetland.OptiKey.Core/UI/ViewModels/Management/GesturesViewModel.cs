using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using JuliusSweetland.OptiKey.Extensions;
using JuliusSweetland.OptiKey.Models;
using JuliusSweetland.OptiKey.Properties;
using log4net;
using Prism.Commands;

namespace JuliusSweetland.OptiKey.UI.ViewModels.Management
{
    public class GesturesViewModel : INotifyPropertyChanged
    {
        #region Private Member Vars

        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion

        #region Ctor

        public GesturesViewModel()
        {
            OpenFileCommand = new DelegateCommand(OpenFile);
            SaveFileCommand = new DelegateCommand(SaveFile);
            ResetChangesCommand = new DelegateCommand(ResetChanges);
            StartCommand = new DelegateCommand(StartAll);
            StopCommand = new DelegateCommand(StopAll);
            AddGestureCommand = new DelegateCommand(AddGesture);
            DeleteGestureCommand = new DelegateCommand(DeleteGesture);
            Load();
        }

        #endregion

        #region Properties

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            if (gestureList != null)
                gestureList = new ObservableCollection<EyeGesture>(gestureList.OrderBy(e => e.Name));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public DelegateCommand OpenFileCommand { get; private set; }
        public DelegateCommand SaveFileCommand { get; private set; }
        public DelegateCommand ResetChangesCommand { get; private set; }
        public DelegateCommand StartCommand { get; private set; }
        public DelegateCommand StopCommand { get; private set; }
        public DelegateCommand AddGestureCommand { get; private set; }
        public DelegateCommand DeleteGestureCommand { get; private set; }

        public bool HideEditor { get { return !eyeGesturesEnabled; } }
        public bool AllStart { get; private set; } = false;
        public bool AllStop { get; private set; } = true;
        private bool eyeGesturesEnabled = false;
        public bool EyeGesturesEnabled
        {
            get { return eyeGesturesEnabled; }
            set {
                eyeGesturesEnabled = value;
                OnPropertyChanged("HideEditor");
            }
        }
        public bool EyeGestureUpdated { get; set; }
        public string EyeGestureFile { get; set; }
        public string EyeGestureString;

        private XmlEyeGestures xmlEyeGestures;
        public XmlEyeGestures XmlEyeGestures
        {
            get { return xmlEyeGestures; }
            set { xmlEyeGestures = value; }
        }

        private ObservableCollection<EyeGesture> gestureList;
        public ObservableCollection<EyeGesture> GestureList
        {
            get { return gestureList; }
            set { gestureList = value; }
        }

        private EyeGesture eyeGesture;
        public EyeGesture EyeGesture
        {
            get { return eyeGesture; }
            set
            {
                eyeGesture = value;
                OnPropertyChanged();
                Preview = null;
            }
        }

        private EyeGesture testGesture;
        public EyeGesture TestGesture
        {
            get { return testGesture; }
            set { testGesture = value; }
        }

        private Canvas preview;
        public Canvas Preview
        {
            get { return preview; }
            set
            {
                RenderPreview();
                OnPropertyChanged();
            }
        }

        #endregion

        #region Methods

        public void ApplyChanges()
        {
            var newString = xmlEyeGestures.WriteToString();
            if (newString != EyeGestureString)
            {
                Settings.Default.EyeGestureString = newString;
                Settings.Default.EyeGestureUpdated = true;
            }
            Settings.Default.EyeGesturesEnabled = EyeGesturesEnabled;
            Settings.Default.EyeGestureFile = EyeGestureFile;
        }

        private void Load()
        {
            EyeGesturesEnabled = Settings.Default.EyeGesturesEnabled;
            EyeGestureFile = Settings.Default.EyeGestureFile;
            EyeGestureString = Settings.Default.EyeGestureString;
            xmlEyeGestures = XmlEyeGestures.ReadFromString(EyeGestureString);
            LoadGestureList();
        }

        private void LoadGestureList()
        {
            if (xmlEyeGestures.GestureList != null && xmlEyeGestures.GestureList.Any())
            {
                gestureList = xmlEyeGestures.GestureList;
                EyeGesture = gestureList[0];
            }
            OnPropertyChanged(null);
        }

        private void OpenFile()
        {
            var fileDialog = new System.Windows.Forms.OpenFileDialog();
            var result = fileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    EyeGestureFile = fileDialog.FileName;
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    break;
            }
            xmlEyeGestures = XmlEyeGestures.ReadFromFile(EyeGestureFile);
            LoadGestureList();
        }

        private void SaveFile()
        {
            var fp = Path.GetDirectoryName(EyeGestureFile);
            var fn = Path.GetFileName(EyeGestureFile);
            var saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.InitialDirectory = string.IsNullOrEmpty(fp) ? @"C:\" : fp;
            saveFileDialog.FileName = string.IsNullOrEmpty(fn) ? "EyeGestures.xml" : fn;
            saveFileDialog.Title = "Save File";
            saveFileDialog.CheckFileExists = true;
            saveFileDialog.CheckPathExists = true;
            saveFileDialog.DefaultExt = "xml";
            saveFileDialog.Filter = "Xml files (*.xml)|*.xml|All files (*.*)|*.*";
            saveFileDialog.FilterIndex = 2;
            saveFileDialog.RestoreDirectory = true;
            var result = saveFileDialog.ShowDialog();
            switch (result)
            {
                case System.Windows.Forms.DialogResult.OK:
                    EyeGestureFile = saveFileDialog.FileName;
                    XmlEyeGestures.WriteToFile(EyeGestureFile);
                    break;
                case System.Windows.Forms.DialogResult.Cancel:
                default:
                    break;
            }
        }

        private void ResetChanges()
        {
            xmlEyeGestures = XmlEyeGestures.ReadFromString(EyeGestureString);
            LoadGestureList();
            Settings.Default.EyeGestureUpdated = true;
        }

        private void StartAll()
        {
            foreach (var e in gestureList)
                e.enabled = true;
            Settings.Default.EyeGestureString = xmlEyeGestures.WriteToString();
            Settings.Default.EyeGestureUpdated = true;
            OnPropertyChanged(null);
        }

        private void StopAll()
        {
            foreach (var e in gestureList)
                e.enabled = false;
            Settings.Default.EyeGestureString = xmlEyeGestures.WriteToString();
            Settings.Default.EyeGestureUpdated = true;
            OnPropertyChanged(null);
        }

        private void AddGesture()
        {
            EyeGesture = new EyeGesture() { Steps = new ObservableCollection<EyeGestureStep>() { new EyeGestureStep() } };

            if (gestureList != null)
                GestureList.Add(EyeGesture);
            else
                gestureList = new ObservableCollection<EyeGesture>() { EyeGesture };
            OnPropertyChanged(null);
        }

        private void DeleteGesture()
        {
            if (gestureList != null && gestureList.Contains(EyeGesture))
                GestureList.Remove(EyeGesture);
            if (gestureList.Any())
                EyeGesture = GestureList[0];
            else
                EyeGesture = null;
            OnPropertyChanged(null);
        }

        private void RenderPreview()
        {
            if (preview != null && preview.Children != null && preview.Children.Count > 0)
                preview.Children.Clear();

            if (eyeGesture != null && eyeGesture.Steps != null && eyeGesture.Steps.Any())
            {
                var scrWidth = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width / 6;
                var scrHeight = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height / 6;
                var gestureShapes = new List<EyeGestureShape>();

                double xPos = 0.5 * scrWidth;
                double yPos = 0.5 * scrHeight;
                double screenLeft = 0.2 * scrWidth;
                double screenTop = 0.2 * scrHeight;
                double canvasWidth = 1.4 * scrWidth;
                double canvasHeight = 1.4 * scrHeight;
                double labelRadius = 18;

                foreach (var step in eyeGesture.Steps)
                {
                    var shape = new EyeGestureShape();

                    if (step.type == Enums.EyeGestureStepTypes.Fixation)
                    {
                        eyeGesture.FixationPoint = new Point(xPos, yPos);
                        shape.Left = xPos - step.Radius * scrHeight / 100;
                        shape.Top = yPos - step.Radius * scrHeight / 100;
                        shape.Width = Math.Max(step.Radius * scrHeight / 50, 5);
                        shape.Height = Math.Max(step.Radius * scrHeight / 50, 5);
                        shape.Radius = shape.Width;
                        shape.Background = step.DwellTime > 100 ? Brushes.LightCoral : Brushes.ForestGreen;
                        shape.X = shape.Width / 2 > labelRadius ? xPos : xPos - labelRadius;
                        shape.Y = shape.Width / 2 > labelRadius ? yPos : yPos - labelRadius;
                    }
                    else if (step.type == Enums.EyeGestureStepTypes.LookInDirection)
                    {
                        xPos += step.X * scrWidth / 100;
                        yPos += step.Y * scrHeight / 100;
                        shape.Left = step.X > 0 ? xPos : xPos - 2 * scrWidth;
                        shape.Top = step.Y > 0 ? yPos : yPos - 2 * scrHeight;
                        shape.Width = step.X != 0 ? 2 * scrWidth : 4 * scrWidth;
                        shape.Height = step.Y != 0 ? 2 * scrHeight : 4 * scrHeight;
                        shape.Radius = 0;
                        shape.Background = Brushes.ForestGreen;
                        shape.X = step.X != 0 ? xPos + Math.Sign(step.X) * labelRadius : xPos;
                        shape.Y = step.Y != 0 ? yPos + Math.Sign(step.Y) * labelRadius : yPos;
                    }
                    else if (step.type == Enums.EyeGestureStepTypes.LookAtArea)
                    {
                        xPos = xPos.Clamp(step.Left * scrWidth / 100 + labelRadius, step.Left * scrWidth / 100 + step.Width * scrWidth / 100 - labelRadius);
                        yPos = yPos.Clamp(step.Top * scrHeight / 100 + labelRadius, step.Top * scrHeight / 100 + step.Height * scrHeight / 100 - labelRadius);
                        shape.Left = step.Left * scrWidth / 100;
                        shape.Top = step.Top * scrHeight / 100;
                        shape.Width = step.Width * scrWidth / 100;
                        shape.Height = step.Height * scrHeight / 100;
                        shape.Radius = step.Round ? shape.Width + shape.Height : 0;
                        shape.Background = step.DwellTime > 100 ? Brushes.LightCoral : Brushes.ForestGreen;
                        shape.X = xPos;
                        shape.Y = yPos;
                    }
                    else if (step.type == Enums.EyeGestureStepTypes.ReturnToFixation)
                    {
                        shape.Width = step.Radius * scrHeight / 50;
                        shape.Height = step.Radius * scrHeight / 50;
                        shape.Radius = shape.Width;
                        shape.Background = step.DwellTime > 100 ? Brushes.LightCoral : Brushes.ForestGreen;
                        shape.X = xPos < eyeGesture.FixationPoint.X
                            ? eyeGesture.FixationPoint.X - labelRadius : eyeGesture.FixationPoint.X + labelRadius;
                        shape.Y = eyeGesture.FixationPoint.Y + labelRadius;
                        xPos = eyeGesture.FixationPoint.X;
                        yPos = eyeGesture.FixationPoint.Y;
                        shape.Left = xPos - step.Radius * scrHeight / 100;
                        shape.Top = yPos - step.Radius * scrHeight / 100;
                    }
                    gestureShapes.Add(shape);
                    screenLeft = Math.Max(screenLeft, labelRadius - shape.X);
                    screenTop = Math.Max(screenTop, labelRadius - shape.Y);
                    canvasWidth = Math.Max(canvasWidth, labelRadius + shape.X);
                    canvasHeight = Math.Max(canvasHeight, labelRadius + shape.Y);
                }

                var canvas = new Canvas()
                {
                    Background = Brushes.Black,
                    Width = canvasWidth,
                    Height = canvasHeight
                };
                canvas.Children.Add(new Border()
                {
                    Background = Brushes.LightBlue,
                    Width = scrWidth,
                    Height = scrHeight,
                    Margin = new Thickness(screenLeft, screenTop, 0, 0),
                });

                foreach(var shape in gestureShapes)
                {
                    canvas.Children.Add(new Border()
                    {
                        Background = shape.Background,
                        Opacity = 0.5,
                        CornerRadius = new CornerRadius(shape.Radius),
                        Margin = new Thickness(screenLeft + shape.Left, screenTop + shape.Top, 0, 0),
                        Width = shape.Width,
                        Height = shape.Height
                    });

                    if (eyeGesture.Steps.Count > 1)
                    {
                        for(int i = 1; i < eyeGesture.Steps.Count; i++)
                        {
                            canvas.Children.Add(new System.Windows.Shapes.Line()
                            {
                                X1 = gestureShapes[i - 1].X + screenLeft,
                                Y1 = gestureShapes[i - 1].Y + screenTop,
                                X2 = gestureShapes[i].X + screenLeft,
                                Y2 = gestureShapes[i].Y + screenTop,
                                SnapsToDevicePixels = true,
                                Stroke = Brushes.White,
                                StrokeThickness = 2
                            });
                        }
                    }
                }

                int b = 1;
                foreach (var shape in gestureShapes)
                {
                    var labelBorder = new Border()
                    {
                        Margin = new Thickness(screenLeft + shape.X - labelRadius, screenTop + shape.Y - labelRadius, 0, 0),
                        Width = 2 * labelRadius,
                        Height = 2 * labelRadius,
                        Background = Brushes.LightSlateGray,
                        BorderBrush = Brushes.White,
                        BorderThickness = new Thickness(2),
                        CornerRadius = new CornerRadius(9999)
                    };

                    labelBorder.Child = new Label()
                    {
                        Background = Brushes.Transparent,
                        Content = b.ToString(),
                        FontSize = labelRadius,
                        Foreground = Brushes.White,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    canvas.Children.Add(labelBorder);
                    b++;
                }
                canvas.ClipToBounds = true;
                preview = canvas;
            }
        }

        #endregion

    }
}