using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace timednotes {

    public class NotifyingDateTime : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;
        private string _now;

        public NotifyingDateTime() {

            _now = GetDateTime();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        public string Now {
            get { return _now; }
            private set {
                _now = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Now"));
            }
        }

        void timer_Tick(object sender, EventArgs e) {
            Now = GetDateTime();
        }

        string GetDateTime() {
            return DateTime.Now.ToString("F", Notebook.defaultCulture);
        }
    }

    public class ObservableFileList : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<FileItem> _files = new ObservableCollection<FileItem>();
        public ObservableCollection<FileItem> Files {
            get { return _files; }
            set {
                _files = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Files"));
            }
        }

        public ObservableFileList() {
            //Create the save directory if it doesn't already exist
            (new FileInfo(MainWindow.saveDirectory)).Directory.Create();
            LoadNotebooks();

            MainWindow.directoryWatcher.Changed += LoadNotebooks;
            MainWindow.directoryWatcher.Created += LoadNotebooks;
            MainWindow.directoryWatcher.Deleted += LoadNotebooks;
            MainWindow.directoryWatcher.EnableRaisingEvents = true;
        }

        void LoadNotebooks(object s, EventArgs e) {
            LoadNotebooks();
        }

        void LoadNotebooks() {
            List<FileItem> notebookFiles = new List<FileItem>();
            string[] files = new string[] { };

            //We can try to load some notebooks
            if (Directory.Exists(MainWindow.saveDirectory)) {
                files = Directory.GetFiles(MainWindow.saveDirectory, MainWindow.filePrefix + "*.txt");               

                for (int i = 0; i < files.Length; i++) {
                    notebookFiles.Add(new FileItem(files[i]));
                }

                Files = new ObservableCollection<FileItem>(notebookFiles);
                Console.WriteLine("Files length = " + files.Length + "...");
            } else {
                //We need to create the directory
                Directory.CreateDirectory(MainWindow.saveDirectory);
            }
        }
    }
}
