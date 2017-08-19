using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace timednotes {
    /// <summary>
    /// Interaction logic for PopupTextInput.xaml
    /// </summary>
    public partial class PopupTextInput : Window {

        MainWindow window;

        public PopupTextInput(MainWindow window, TimedNote editingNote) {
            InitializeComponent();

            this.window = window;
            SetText(editingNote.Note);
            label.Content = editingNote.Time.ToShortDateString();
        }

        public void SetText(string text) {
            editNoteTextBox.Text = text;
        }

        string _editingNote = string.Empty;

        private void submitEditedNoteButton_Click(object sender, RoutedEventArgs e) {
            if (this.window == null)
                return;

            window.popupTextInput_Edit(editNoteTextBox.Text, this);
        }
    }
}
