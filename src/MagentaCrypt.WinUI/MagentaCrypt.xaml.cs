using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WinUI.Models;
using System.Windows.Controls;
using System.Windows.Media;
using MegentaCrypt.Core.CryptParams;
using MagentaCrypt.Providers.FileProviders;
using MagentaCrypt.Providers.Events;

namespace WinUI
{
    /// <summary>
    /// Main Window for MagentaCrypt Application.
    /// </summary>
    public partial class MainWindow : Window
    {
        List<FileItem> Files = new List<FileItem>();
        string Destiantion;
        long AllBytes = 0;
        public MainWindow()
        {
            Destiantion = string.Empty;
            InitializeComponent();
        }
        private void lst_items_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lst_items.SelectedItems.Count == 1)
            {
                RemoveItem(((FileItem)lst_items.SelectedItem).FullName);
            }
            if (lst_items.Items.Count == 0)
                SwitchGrids();
            CheckUI();
        }

        private void slider_blockSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            => updateBlockLabel();

        private void updateBlockLabel()
        {
            try
            {
                double value = slider_blockSize.Value;
                lbl_blockSize_value.Content = $"{(int)value}MB";
            }
            catch { }
        }

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            lst_items.DragEnter += new DragEventHandler(lst_newItem_drag);
            lst_items.Drop += new DragEventHandler(lst_newItem_drop);
            lst_items.DragLeave += new DragEventHandler(lst_newItem_dragLeave);
        }

        private void lst_newItem_dragLeave(object sender, DragEventArgs e)
        {
            if (lst_items.Items.Count == 0)
                SwitchGrids();
            CheckUI();
        }

        private void lst_newItem_drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (var fn in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    string filename = fn;
                    FileInfo file = new FileInfo(filename);
                    AddItem(file.Name, file.Extension, file.Length, file.FullName);
                }
            }
        }

        private void AddItem(string filename, string extension, long length, string fullname)
        {
            var checkList = Files.Where(x => x.FullName == fullname);
            if (checkList.Count() == 0)
            {
                int id = 0;
                if (lst_items.Items.Count == 0)
                    id = 0;
                else if (lst_items.Items.Count > 0)
                    id = ((FileItem)lst_items.Items[lst_items.Items.Count - 1]).ID + 1;
                Files.Add(new FileItem()
                {
                    ID = id,
                    Filename = filename,
                    Extension = extension,
                    Length = ConvertToKB(length),
                    FullName = fullname
                });
                AllBytes += length;
            }
            else
            {
                txt_status.Text = $"{filename} already exist.";
            }
            updateList();
        }

        private string ConvertToKB(long num)
        {
            if (num > 1024)
                return $"{Convert.ToDouble(Math.Ceiling((double)(num / 1024)))}KB";
            else
                return $"{num} bytes";
        }

        private void RemoveItem(string fullname)
        {
            var checkList = Files.Where(x => x.FullName == fullname);
            if (checkList.Count() == 1)
            {
                Files.Remove(checkList.Single());
            }
            else
                txt_status.Text = $"{fullname} doesn't exist.";
            updateList();
        }

        private void updateList()
        {
            lst_items.ItemsSource = Files;
            lst_items.Items.Refresh();
            CheckUI();
        }

        private void lst_newItem_drag(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
        }

        private void Crypt()
        {
            KeyCrypt key = new KeyCrypt(txt_key.Password);
            IVCrypt iv = new IVCrypt(txt_iv.Password);
            Dictionary<string, string> listOfFiles = new Dictionary<string, string>(Files.Count);
            if (string.IsNullOrEmpty(Destiantion))
                foreach (var file in Files)
                    listOfFiles.Add(file.FullName, file.FullName);
            else
                foreach (var file in Files)
                    listOfFiles.Add(file.FullName, $"{Path.Combine($"{Destiantion}\\", file.Filename)}");
            MultiFileCryptor cryptor = new MultiFileCryptor(listOfFiles,
                key, iv, blocksize: ConvertMBToBytes(slider_blockSize.Value));
            CryptMode mode = CryptMode.Encrypt;
            if (rdb_Decrypt.IsChecked == true)
                mode = CryptMode.Decrypt;
            CryptAlgorithm algorithm;
            algorithm = (CryptAlgorithm)Enum.Parse(typeof(CryptAlgorithm), cmb_Algorithms.Text);
            cryptor.OneFileCryptCompleted += new EventHandler<FileCryptCompletedEventArgs>(OneFileCompleted);
            cryptor.CryptographyCompleted += new EventHandler<EventArgs>(OperationCompleted);

            //preparing crypt operation
            progress_bar.Maximum = Files.Count;
            grid_listitems.IsEnabled = false;
            grid_controls.IsEnabled = false;

            //start the cryptography
            cryptor.StartCrypt(mode, algorithm);
        }

        private void OperationCompleted(object sender, EventArgs e)
        {
            //using Dispatcher because this event will be triggered by another thread.
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                grid_listitems.IsEnabled = true;
                grid_controls.IsEnabled = true;
                updateList();
                txt_key.Clear();
                txt_iv.Clear();
                MessageBox.Show($"Cryptography on {AllBytes}bytes has been successfully completed.", "Completed", MessageBoxButton.OK, MessageBoxImage.Information);
                txt_status.Text = "Idle";
                CheckUI();
            }));
        }

        private void OneFileCompleted(object sender, FileCryptCompletedEventArgs e)
        {
            //using Dispatcher because this event will be triggered by another thread.
            progress_bar.Dispatcher.BeginInvoke(new Action(() =>
            {
                progress_bar.Value = progress_bar.Value + 1;
            }));
            txt_status.Dispatcher.BeginInvoke(new Action(() =>
           {
               txt_status.Text = $"{e.Filename} has been successfully {e.Cryptmode}ed.";
           }));
            lst_items.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (Files.Count != 0)
                    Files.Remove(Files.Where(x => x.FullName == e.Filename).Single());
                updateList();
            }));
        }

        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(Destiantion))
                {
                    var result = MessageBox.Show("You Did not set a destination folder to save file(s).\nIf you want to set a destination folder press Yes.\nIf you want to use the current paths press NO.", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        DestinationFolderDialog();
                        btn_start_Click(null, null);
                    }
                }
                Crypt();
            }
            catch (Exception ex)
            { txt_status.Text = ex.Message; }
        }

        private int ConvertMBToBytes(double num)
        {
            int n = Convert.ToInt32(num);
            return ((n * 1024) * 1024);
        }

        private void grid_vacant_DragEnter(object sender, DragEventArgs e)
        {
            SwitchGrids();
        }

        private void SwitchGrids()
        {
            if ((int)grid_vacant.Visibility % 2 == 0)
                grid_vacant.Visibility++;
            else
                grid_vacant.Visibility--;
            if ((int)grid_listitems.Visibility % 2 == 0)
                grid_listitems.Visibility++;
            else
                grid_listitems.Visibility--;
        }

        private void txt_header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                new AboutAuthor().ShowDialog();
        }

        private void txt_passwordsChanged(object sender, RoutedEventArgs e)
        {
            CheckUI();
        }
        private void CheckUI()
        {
            bool b1 = CheckPasswordBox(ref txt_key, 6, 16);
            bool b2 = CheckPasswordBox(ref txt_iv, 6, 16);
            bool b3 = CheckListViewItems(ref lst_items);
            btn_start.IsEnabled = (b1 && b2 & b3);
        }
        private bool CheckListViewItems(ref ListView list)
        {
            if (list.Items.Count <= 0)
                return false;
            else
                return true;
        }
        private bool CheckPasswordBox(ref PasswordBox passwordbox, int LowBound, int UpBound)
        {
            if (!(passwordbox.Password.Count() < UpBound && passwordbox.Password.Count() > LowBound))
            {
                passwordbox.Background = Brushes.MediumVioletRed;
                passwordbox.Foreground = Brushes.White;
                return false;
            }
            else
            {
                passwordbox.Background = Brushes.White;
                passwordbox.Foreground = Brushes.Black;
                return true;
            }
        }

        private void img_destinationFolder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DestinationFolderDialog();
            }
        }

        private void DestinationFolderDialog()
        {
            using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Destiantion = dlg.SelectedPath;
                }
            }
        }
    }
    public static class WpfWin32Window
    {
        public static System.Windows.Forms.IWin32Window GetIWin32Window(this System.Windows.Media.Visual visual)
        {
            var source = System.Windows.PresentationSource.FromVisual(visual) as System.Windows.Interop.HwndSource;
            System.Windows.Forms.IWin32Window win = new OldWindow(source.Handle);
            return win;
        }

        private class OldWindow : System.Windows.Forms.IWin32Window
        {
            private readonly System.IntPtr _handle;
            public OldWindow(System.IntPtr handle)
            {
                _handle = handle;
            }

            #region IWin32Window Members
            System.IntPtr System.Windows.Forms.IWin32Window.Handle
            {
                get { return _handle; }
            }
            #endregion
        }
    }
}
