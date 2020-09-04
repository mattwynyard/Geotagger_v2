﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Geotagger_V2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string mDBPath;
        private string mInputPath;
        private string mOutputPath;
        private DispatcherTimer dispatcherTimer;
        private GeotagManager manager;
        private Stopwatch stopwatch;
        private Boolean timer = false;
        public MainWindow()
        {
            InitializeComponent();
            ProgessBar.Visibility = Visibility.Hidden;

        }

        private void BrowseDB_Button_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button b = sender as System.Windows.Controls.Button;
            if (b.Name == "BrowseDB")
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Import Access Database";
                openFileDialog.Filter = "MS Access (*.mdb *.accdb)|*.mdb;*.accdb";
                openFileDialog.RestoreDirectory = true;
                openFileDialog.ShowDialog();
                
                if (mDBPath != "")
                {
                    txtBoxDB.Text = mDBPath = openFileDialog.FileName;
                }
                else
                {
                    Console.WriteLine("cancel");
                }
            } else 
            {
                FolderBrowserDialog browseFolderDialog = new FolderBrowserDialog();
                browseFolderDialog.ShowDialog();
                if (b.Name == "BrowseInput")
                {
                    if (browseFolderDialog.SelectedPath != "")
                    {
                        txtBoxInput.Text = mInputPath = browseFolderDialog.SelectedPath;
                    }
                } else
                {
                    if (browseFolderDialog.SelectedPath != "")
                    {
                        txtBoxOutput.Text = mOutputPath = browseFolderDialog.SelectedPath;
                    }

                }
                   
            }
        }

        /// <summary>
        /// Fired when user clicks geotag button. Starts geotagger
        /// </summary>
        /// <param name="sender">object - the geotag button</param>
        /// <param name="e">click event</param>
        private async void Geotag_Click(object sender, RoutedEventArgs e)
        {
            
            manager = new GeotagManager();
            dispatcherTimer = new DispatcherTimer();
            stopwatch = new Stopwatch();
            stopwatch.Start();

            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
            Timer();
            Task worker = Task.Factory.StartNew(() =>
            {
                showProgressBar();
                manager.photoReader(mInputPath, false);
                manager.readDatabase(mDBPath, "");
                manager.writeGeotag(mOutputPath);
                //
            });
            await Task.WhenAll(worker);
            //dispatcherTimer.Stop();
            //hideProgressBar();
        }

        public void DispatcherTimer_Tick(object sender, EventArgs args)
        {
            Console.WriteLine("tick");
            ProgessLabel.Content = manager.updateProgessMessage;
            ProgessBar.Value = manager.updateProgessValue;
            PhotoCountLabel.Content = manager.updatePhotoCount;
            RecordsLabel.Content = manager.updateRecordCount;
            string value = "Geotag Count: " + manager.updateGeoTagCount;
            RecordQueueLabel.Content = "Record Queue: " + manager.updateRecordQueueCount;
            GeotagLabel.Content = value;
        }

        private void Timer()
        {
            timer = true;
            stopwatch = new Stopwatch();
            stopwatch.Start();
            Task sw = Task.Factory.StartNew(() =>
            {
                while (timer)
                {
                    TimeSpan ts = stopwatch.Elapsed;
                    //TimeSpan time = new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds);
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
                    Dispatcher.Invoke((Action)(() =>
                    {
                        TimeLabel.Content = elapsedTime;
                    }));
                    Thread.Sleep(10);
                }
            });
        }

        private void hideProgressBar()
        {
            Dispatcher.Invoke((Action)(() => {
                ProgessBar.Visibility = Visibility.Hidden;
            }));
        }
        private void showProgressBar()
        {
            Dispatcher.Invoke((Action)(() => {
                ProgessBar.Visibility = Visibility.Visible;
            }));
        }

        private void updateProgressLabel(string message)
        {
            ProgessLabel.Content= message;
        }
    }
}
