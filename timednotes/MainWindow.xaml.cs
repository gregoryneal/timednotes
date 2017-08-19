using System;
using System.Collections.Generic;
using System.Globalization;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using System.ComponentModel;

namespace timednotes {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {

        //DO NOT CHANGE UNLESS YOU DELETE THESE FOLDERS FIRST
        public static string filePrefix = "timednotebook_";
        public static string dateStringFormat = "O";
        public static string saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "timednotebooks", "notebooks");
        //-------------

        public static FileSystemWatcher directoryWatcher;
        public static MainWindow mainWindow;

        public ObservableCollection<FileItem> NotebookFiles = new ObservableCollection<FileItem>();
        public string defaultTextBox = "write note here";

        FileItem selectedNotebookFile = null;
        Notebook _selectedNotebook = null;
        //Only used as a reference to the file
        public Notebook SelectedNotebook {
            get { return _selectedNotebook; }
            private set {
                _selectedNotebook = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedNotebook"));
            }
        }

        TimedNote _selectedNote = null;
        public TimedNote SelectedNote {
            get { return _selectedNote; }
            private set {
                _selectedNote = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedNote"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow() {
            //Create the save directory if it doesn't already exist
            directoryWatcher = new FileSystemWatcher(saveDirectory, filePrefix + "*.txt");
            (new FileInfo(saveDirectory)).Directory.Create();

            InitializeComponent();

            mainWindow = this;
        }

        private void createNotebookBtn_Click(object sender, RoutedEventArgs e) {
            Notebook.CreateNewNotebook(notebookName.Text);
        }

        private void deleteNotebookButton_Click(object sender, RoutedEventArgs e) {
            if (selectedNotebookFile == null)
                return;

            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure?", "Delete " + selectedNotebookFile.Name, MessageBoxButton.YesNo);

            if (result == MessageBoxResult.No)
                return;

            //clear the old notebook from memory
            SelectedNotebook = null;

            //delete the file from the directory
            File.Delete(selectedNotebookFile.Path);
            selectedNotebookFile = null;
        }

        //Saves changes to the currently loaded notebook and loads the new notebook
        private void notebookListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            selectedNotebookFile = notebookListBox.SelectedItem as FileItem;

            if (notebookListBox.SelectedIndex < 0) {
                Console.WriteLine("no item selected.");
                return;
            }

            SelectedNotebook = LoadNotebookFromFile(selectedNotebookFile.Path);
        }

        private Notebook LoadNotebookFromFile(string path) {
            //Turn off the directoryWatcher so that no file changed/added/removed events are raised.
            directoryWatcher.EnableRaisingEvents = false;

            string name = (new FileInfo(path)).Name.Replace(filePrefix, "").Replace(".txt", "");
            Notebook notebook = new Notebook(name);
            TimedNote note = new TimedNote();
            int j = 0;
            //even lines (and 0) contain string dates, odd lines contain the note for the previous lines date
            foreach (string s in File.ReadLines(path)) {
                //Console.WriteLine("index: " + j + "\nstring:" + s);

                if (j == 0 || j % 2 == 0) {
                    note.Time = DateTime.ParseExact(s, dateStringFormat, Notebook.defaultCulture);
                } else {
                    note.Note = s;
                    notebook.AddNote(note);
                    note = new TimedNote();
                }

                j++;
            }

            directoryWatcher.EnableRaisingEvents = true;
            return notebook;
        }

        private void submitNoteButton_Click(object sender, RoutedEventArgs e) {
            string text = textBox.Text;

            if (selectedNotebookFile == null)
                return;

            Notebook.AddNote(selectedNotebookFile.Path, text);
            SelectedNotebook.AddNote(new TimedNote(text));
            ReplaceTextbox(defaultTextBox);
        }

        private void openNotebookFileButton_Click(object sender, RoutedEventArgs e) {
            if (notebookListBox.SelectedIndex < 0)
                return;

            string path = (notebookListBox.SelectedItem as FileItem).Path;
            Process.Start(path);
        }

        private void textBox_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            Console.WriteLine("text box left mouse btn down");

            if (textBox.Text == defaultTextBox)
                ReplaceTextbox(string.Empty);
        }

        void ReplaceTextbox(string newText) {
            textBox.Text = newText;
        }

        private void textBox_LostFocus(object sender, RoutedEventArgs e) {
            if (textBox.Text == string.Empty)
                textBox.Text = defaultTextBox;
        }

        private void noteListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (noteListBox.SelectedIndex < 0) {
                SelectedNote = null;
                return;
            }

            SelectedNote = noteListBox.SelectedItem as TimedNote;
            Console.WriteLine("Selection changed to " + noteListBox.SelectedIndex);

            noteListBox.ColumnWidth = 0;
            noteListBox.UpdateLayout();
            noteListBox.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
        }

        private void noteListBox_AddingNewItem(object sender, AddingNewItemEventArgs e) {
            TimedNote item = e.NewItem as TimedNote;
            item.Note = string.Empty;
            item.Time = DateTime.Now;
            Console.WriteLine("new item: " + item.Note);
        }

        private void editNoteButton_Click(object sender, RoutedEventArgs e) {
            if (SelectedNote == null)
                return;

            PopupTextInput popup = new PopupTextInput(this, SelectedNote);
            popup.ShowDialog();
        }

        public void popupTextInput_Edit(string editedNote, PopupTextInput windowToClose) {
            if (SelectedNotebook == null || SelectedNote == null)
                return;

            windowToClose.Close();

            SelectedNote.Note = editedNote;
            Notebook.SaveNotebook(SelectedNotebook);
        }

        /// <summary>
        /// Prevents certain characters from registering, this prevents errors in windows file creation
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void notebookName_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            List<string> illegal = new List<string>() { "<", ">", ":", "\"", "/", "\\", "|", "?", "*", };

            Console.WriteLine(e.Text);

            if (illegal.Contains(e.Text.ToLower())) {
                e.Handled = true;
            }
        }
    }
}
/* Parsing a notebook from a file code
Notebook notebook = new Notebook(name);
TimedNote note = new TimedNote();
int j = 0;
//even lines (and 0) contain string dates, odd lines contain the note for the previous lines date
foreach (string s in File.ReadLines(files[i])) {
    if (j == 0 || j % 2 == 0) {
        note.time = DateTime.ParseExact(s, dateStringFormat, Notebook.defaultCulture);
    } else {
        note.note = s;
        notebook.AddNote(note);
        note = new TimedNote();
    }
}
*/
