﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Geotagger_V2
{
    public class GeotagManager
    {
        private string progressMessage = "";
        private double progressValue = 0;
        private int photoCount = 0;
        private int geotagCount;
        private int sizeRecordQueue;
        private int sizeBitmapQueue;
        private Boolean mZip;
        private Stopwatch stopwatch;
        private ConcurrentDictionary<string, Record> recordDict;
        private BlockingCollection<Record> recordQueue;
        private BlockingCollection<ThreadInfo> geotagQueue;
        private ConcurrentDictionary<string, string> photoDict;
        private ConcurrentDictionary<string, Record> noPhotoDict;
        private BlockingCollection<object[]> bitmapQueue;
        private ConcurrentDictionary<string, Exception> errorDict;
        private string connectionString;
        private ProgressObject progress;

        public GeotagManager()
        {
            intialise();
        }
        public GeotagManager(int sizeRecordQueue, int sizeBitmapQueue)
        {
            intialise(sizeRecordQueue, sizeBitmapQueue);
        }

        private void intialise(int sizeRecordQueue = 50000, int sizeBitmapQueue = 50)
        {
            geotagCount = 0;
            this.sizeRecordQueue = sizeRecordQueue;
            this.sizeBitmapQueue = sizeBitmapQueue;
            recordDict = new ConcurrentDictionary<string, Record>();
            recordQueue = new BlockingCollection<Record>(sizeRecordQueue);
            geotagQueue = new BlockingCollection<ThreadInfo>();
            noPhotoDict = new ConcurrentDictionary<string, Record>();
            bitmapQueue = new BlockingCollection<object[]>(sizeBitmapQueue);
            errorDict = new ConcurrentDictionary<string, Exception>();
            stopwatch = Stopwatch.StartNew();
            progress = new ProgressObject();
        }

        public BlockingCollection<string> buildQueue(string path)
        {
            
            BlockingCollection<string> fileQueue = new BlockingCollection<string>();
            string[] files = Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories);
            Task producer = Task.Factory.StartNew(() =>
            {
                foreach (string file in files)
                {
                    fileQueue.Add(file);
                }
                fileQueue.CompleteAdding();
            });
            Task.WaitAll(producer);
            return fileQueue;
        }

        /// <summary>
        /// Adds all image files(.jpg) found in directory to a concurrent dictionary- key: filename, value: filepath
        /// </summary>
        /// <param name="path">parent folder path</param>
        /// <param name="zip">searches and reads zip directory for .jpg files</param>
        public void photoReader(string path, Boolean zip)
        {
            string initialMessage = "Searching directories...";
            Interlocked.Exchange(ref progressMessage, initialMessage);
            mZip = zip;
            Task search = Task.Factory.StartNew(() =>
            {
                if (zip)
                {
                    photoDict = new ConcurrentDictionary<string, string>(); //
                    string[] files = Directory.GetFiles(path, "*.zip", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        string f = file;
                        using (FileStream zipToOpen = new FileStream(file, FileMode.Open))
                        {
                            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                            {
                                foreach (ZipArchiveEntry entry in archive.Entries)
                                {
                                    string s = entry.FullName;
                                    string[] tokens = s.Split('/');
                                    s = tokens[tokens.Length - 1];
                                    if (s.Substring(s.Length - 3) == "jpg")

                                    {
                                        string key = s.Substring(0, s.Length - 4);
                                        bool added = photoDict.TryAdd(key, file);
                                        if (!added)
                                        {
                                            string photo = file;
                                            Console.WriteLine(photo);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                }
                else
                {
                    photoDict = new ConcurrentDictionary<string, string>();
                    string[] files = Directory.GetFiles(path, "*.jpg", SearchOption.AllDirectories);
                    int fileCount = files.Length;
                    int i = 0;
                    string message = "Building dictionary...";
                    Interlocked.Exchange(ref progressMessage, message);
                    foreach (var file in files)
                    {
                        string key = Path.GetFileNameWithoutExtension(file);
                        
                        bool added = photoDict.TryAdd(key, file);
                        i++;

                        double newvalue = ((double)i / (double)fileCount) * 100;
                        Interlocked.Exchange(ref photoCount, i);
                        Interlocked.Exchange(ref progressValue, newvalue);
                        if (!added)
                        {
                            string photo = file;
                        }
                    }
                }
            });
            Task.WaitAll(search);
            string finalMessage = "Finished";
            Interlocked.Exchange(ref progressMessage, finalMessage);
        }


        public ProgressObject updateProgress
        {
            get
            {
                return progress;
                //return Interlocked.CompareExchange(ref progress, new ProgressObject(), new ProgressObject());

            }
        }

        public string updateProgessMessage
        {
            get
            {
                return Interlocked.CompareExchange(ref progressMessage, "", "");

            }
        }

        public int updatePhotoCount
        {
            get
            {
                return Interlocked.CompareExchange(ref photoCount, 0, 0);

            }
        }

        public double updateProgessValue
        {
            get
            {
                return Interlocked.CompareExchange(ref progressValue, 0, 0);

            }
        }
    }
}
