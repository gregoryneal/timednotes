using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace timednotes {
    public class Notebook : INotifyPropertyChanged {

        //Define the localization for the time display
        public static CultureInfo defaultCulture = CultureInfo.InvariantCulture;

        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<TimedNote> _notes = new ObservableCollection<TimedNote>();

        public ObservableCollection<TimedNote> Notes {
            get { return _notes; }
            set {
                _notes = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Notes"));
            }
        }

        public string Name { get; private set; }

        public Notebook(string name) {

            this.Name = name;
            PropertyChanged += new PropertyChangedEventHandler(Save);
        }


        /// <summary>
        /// Writes a note directly to a file, only use if you are certain you're path is a notebook
        /// </summary>
        /// <param name="notebookPath"></param>
        /// <param name="note"></param>
        public static void AddNote(string notebookPath, string note) {
            MainWindow.directoryWatcher.EnableRaisingEvents = false;
            TimedNote n = new TimedNote(note);
            string dateLine = n.Time.ToString(MainWindow.dateStringFormat);
            File.AppendAllLines(notebookPath, new string[] { dateLine, note });
            MainWindow.directoryWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Writes a note directly to this notebooks file.
        /// </summary>
        /// <param name="note"></param>
        public void AddNote(string note) {
            MainWindow.directoryWatcher.EnableRaisingEvents = false;
            TimedNote n = new TimedNote(note);
            Notes.Add(n);
            string dateLine = n.Time.ToString(MainWindow.dateStringFormat);
            File.AppendAllLines(Path.Combine(MainWindow.saveDirectory, MainWindow.filePrefix + this.Name + ".txt"), new string[] { dateLine, note });
            MainWindow.directoryWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Adds a note to the Notes property, does not save.
        /// </summary>
        /// <param name="note"></param>
        public void AddNote(TimedNote note) {
            Notes.Add(note);
        }

        /// <summary>
        /// Removes the first note recorded at the specified time from the Notes list.
        /// </summary>
        /// <param name="time"></param>
        public void RemoveNoteByTime(DateTime time) {
            for (int i = 0; i < Notes.Count; i++) {
                if (Notes[i].Time.CompareTo(time) == 0) {
                    Notes.RemoveAt(i);
                    Save();
                    return;
                }
            }
            Save();
        }

        //Deletes the file if it exists and resaves a new one like it
        public void Save(object s, PropertyChangedEventArgs args) {
            Save();
        }

        public void Save() {
            SaveNotebook(this);
        }

        //Saves a notebook to a file, replaces the existing one if replaceExisting is true, otherwise if the file exists it will terminate execution
        public static void SaveNotebook(Notebook notebook) {
            Console.WriteLine("Saving");

            MainWindow.directoryWatcher.EnableRaisingEvents = false;

            string filePath = Path.Combine(MainWindow.saveDirectory, MainWindow.filePrefix + notebook.Name + ".txt");

            //Clear the file, creating an empty one if it doesn't already exist
            File.WriteAllText(filePath, string.Empty);

            //Open a filestream to write to the file
            using (StreamWriter fs = new StreamWriter(filePath)) {

                //save the timednotes as strings in the file
                for (int i = 0; i < notebook.Notes.Count; i++) {
                    fs.WriteLine(notebook.Notes[i].Time.ToString(MainWindow.dateStringFormat));
                    fs.WriteLine(notebook.Notes[i].Note);
                }
            }

            MainWindow.directoryWatcher.EnableRaisingEvents = true;
        }

        public static void CreateNewNotebook(string name) {
            string filePath = Path.Combine(MainWindow.saveDirectory, MainWindow.filePrefix + name + ".txt");
            File.WriteAllText(filePath, string.Empty);
        }
    }

    public class TimedNote : INotifyPropertyChanged {

        string _note = string.Empty;
        public string Note {
            get { return _note; }
            set {
                _note = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Note"));
            }
        }
        public DateTime Time { get; set; }

        public TimedNote() { }

        public TimedNote(string note) {
            this.Note = note;
            this.Time = DateTime.Now;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class FileItem {
        public string Name { get; set; }
        public string Path { get; set; }

        public FileItem(string path) {
            this.Path = path;

            FileInfo f = new FileInfo(path);
            this.Name = f.Name.Replace(MainWindow.filePrefix, "").Replace(".txt", "");
        }

        public FileItem() { }
    }
}
