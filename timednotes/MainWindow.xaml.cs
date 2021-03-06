﻿using System;
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

                try {
                    SelectedNotebook.Save();
                } catch (NullReferenceException) {
                    //the notebook was deleted and not just updated
                }
            }
        }

        TimedNote _selectedNote = null;
        public TimedNote SelectedNote {
            get { return _selectedNote; }
            private set {

                Console.WriteLine("Shit's goin down");

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

        /// <summary>
        /// Creates a new empty notebook in MyDocuments
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void createNotebookBtn_Click(object sender, RoutedEventArgs e) {
            Notebook.CreateNewNotebook(notebookName.Text);
        }

        /// <summary>
        /// Deletes the currently selected notebook, there is a popup first though
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Deletes the note from the selected notebook, automatically saves, fuck u if u fuck up boi haha
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void deleteNoteButton_Click(object sender, RoutedEventArgs e) {
            DeleteSelectedNoteFromSelectedNotebook();
        }

        void DeleteSelectedNoteFromSelectedNotebook() {
            if (SelectedNote == null || SelectedNotebook == null)
                return;

            Console.WriteLine("delete button pressed");
            SelectedNotebook.RemoveNoteByTime(SelectedNote.Time);
        }

        /// <summary>
        /// Saves changes to the currently loaded notebook and loads the new notebook
        /// </summary>
        private void notebookListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            selectedNotebookFile = notebookListBox.SelectedItem as FileItem;

            if (notebookListBox.SelectedIndex < 0) {
                Console.WriteLine("no item selected.");
                return;
            }

            SelectedNotebook = LoadNotebookFromFile(selectedNotebookFile.Path);
        }

        /// <summary>
        /// Loads a notebook from a file given a path, parse and validate the path before you call this.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Submit a new note, update the file directly and add it to the current SelectedNote so the view updates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void submitNoteButton_Click(object sender, RoutedEventArgs e) {
            string text = textBox.Text;

            if (selectedNotebookFile == null)
                return;

            Notebook.AddNote(selectedNotebookFile.Path, text);
            SelectedNotebook.AddNote(new TimedNote(text));
            ReplaceTextbox(defaultTextBox);
        }

        /// <summary>
        /// Opens the selected FileItem with the machine's default process (notepad since we use .txt)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openNotebookFileButton_Click(object sender, RoutedEventArgs e) {
            if (notebookListBox.SelectedIndex < 0)
                return;

            string path = (notebookListBox.SelectedItem as FileItem).Path;
            Process.Start(path);
        }

        /// <summary>
        /// If the textbox has default text in it delete it, dont delete any text if the user has changed it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (textBox.Text == defaultTextBox)
                ReplaceTextbox(string.Empty);
        }

        void ReplaceTextbox(string newText) {
            textBox.Text = newText;
        }

        /// <summary>
        /// The user didn't put anything in the textbox, so reset it to default.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_LostFocus(object sender, RoutedEventArgs e) {
            if (textBox.Text == string.Empty)
                textBox.Text = defaultTextBox;
        }

        /// <summary>
        /// Updates the SelectedNote property to match the currently selected note
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void noteListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (noteListBox.SelectedIndex < 0) {
                SelectedNote = null;
                return;
            }

            SelectedNote = noteListBox.SelectedItem as TimedNote;
            Console.WriteLine("Selection changed to " + noteListBox.SelectedIndex);
        }

        /// <summary>
        /// Called when the user clicks the button to edit the currenlty selected note
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void editNoteButton_Click(object sender, RoutedEventArgs e) {
            if (SelectedNote == null)
                return;

            PopupTextInput popup = new PopupTextInput(this, SelectedNote);
            popup.ShowDialog();
        }

        /// <summary>
        /// Called by a PopupTextInput instance to edit the currently selected note.
        /// </summary>
        /// <param name="editedNote"></param>
        /// <param name="windowToClose"></param>
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

        private void openFolderButton_Click(object sender, RoutedEventArgs e) {
            Process.Start(saveDirectory);
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
