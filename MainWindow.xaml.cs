using Microsoft.Win32;
using ObjectStorageExplorer.Model;
using ObjectStorageExplorer.Service;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace ObjectStorageExplorer
{
    public partial class MainWindow : Window
    {
        private SettingsModel currentSettings;
        private ObjectStorageService objectStorageService;
        private ObservableCollection<StorageItem> folders = new ObservableCollection<StorageItem>();
        private ObservableCollection<StorageItem> files = new ObservableCollection<StorageItem>();
        private string selectedFolderPath = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
            tabItemExplorer.IsEnabled = true;
            LoadSettingsAsync().ConfigureAwait(false);
        }

        private async Task LoadSettingsAsync()
        {
            try
            {
                currentSettings = await DataService.LoadSettingsAsync();

                if (currentSettings != null)
                {
                    txtTenantId.Text = currentSettings.TenantId;
                    txtUserId.Text = currentSettings.UserId;
                    txtFingerprint.Text = currentSettings.Fingerprint;
                    txtPrivateKey.Text = currentSettings.PrivateKey;
                    txtRegion.Text = currentSettings.Region;
                    txtBucketName.Text = currentSettings.BucketName;
                    txtNamespaceName.Text = currentSettings.NamespaceName;
                    txtGatewayUrl.Text = currentSettings.GatewayUrl;
                    objectStorageService = new ObjectStorageService(new OracleStorageConfig
                    {
                        BucketName = currentSettings.BucketName,
                        Fingerprint = currentSettings.Fingerprint,
                        NamespaceName = currentSettings.NamespaceName,
                        PrivateKey = currentSettings.PrivateKey,
                        Region = currentSettings.Region,
                        TenantId = currentSettings.TenantId,
                        UserId = currentSettings.UserId
                    });

                    await objectStorageService.LoadFolderItemsAsync("", folders, files);
                    treeViewStorage.ItemsSource = folders;
                    dataGridFiles.ItemsSource = files;
                    tabItemExplorer.IsEnabled = true;

                }
                else
                {
                    // Initialize a new SettingsModel if no file exists
                    currentSettings = new SettingsModel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }
        private async void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.DataContext is StorageItem folder && !folder.IsLoaded)
            {
                folder.IsLoaded = true; // Prevents reloading the same items
                folder.Children.Clear(); // Remove placeholder
                await objectStorageService.LoadFolderItemsAsync(folder.Path, folder.Children, files);
            }
        }

        private async void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = e.NewValue as StorageItem;
            if (selectedItem is not null)
            {
                selectedFolderPath = selectedItem.Path;
                await objectStorageService.LoadFolderItemsAsync(selectedItem.Path, selectedItem.Children, files, false);
                e.Handled = true;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            currentSettings.OptionType = "ODT";
            currentSettings.TenantId = txtTenantId.Text;
            currentSettings.UserId = txtUserId.Text;
            currentSettings.Fingerprint = txtFingerprint.Text;
            currentSettings.PrivateKey = txtPrivateKey.Text;
            currentSettings.Region = txtRegion.Text;
            currentSettings.BucketName = txtBucketName.Text;
            currentSettings.NamespaceName = txtNamespaceName.Text;
            currentSettings.GatewayUrl = txtGatewayUrl.Text;
            objectStorageService = new ObjectStorageService(new OracleStorageConfig
            {
                BucketName = currentSettings.BucketName,
                Fingerprint = currentSettings.Fingerprint,
                NamespaceName = currentSettings.NamespaceName,
                PrivateKey = currentSettings.PrivateKey,
                Region = currentSettings.Region,
                TenantId = currentSettings.TenantId,
                UserId = currentSettings.UserId
            });
            await objectStorageService.LoadFolderItemsAsync("", folders, files);
            treeViewStorage.ItemsSource = folders;
            dataGridFiles.ItemsSource = files;
            tabItemExplorer.IsEnabled = true;

            // Save the settings to JSON
            bool isSaved = await DataService.SaveSettingsAsync(currentSettings);
            if (isSaved)
            {
                MessageBox.Show("Settings saved successfully!");
            }
            else
            {
                MessageBox.Show("Failed to save settings.");
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void Upload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedFolderPath))
                    throw new ArgumentException("Oracle Object Storage Path is missing. Please select the folder.");
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.DefaultExt = ".*"; // Required file extension 
                fileDialog.Filter = "All Files (.*)|*.*"; // Optional file extensions

                if (fileDialog.ShowDialog() == true)
                {

                    System.IO.StreamReader sr = new StreamReader(fileDialog.FileName);
                    await objectStorageService.UploadFileAsync($"{selectedFolderPath}{Path.GetFileName(fileDialog.FileName)}", sr.BaseStream);
                    sr.Close();
                    await objectStorageService.LoadFolderItemsAsync(selectedFolderPath, folders, files, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void GatewayUrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedFolderPath))
                    throw new ArgumentException("Oracle Object Storage Path is missing. Please select the folder.");
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"{txtGatewayUrl.Text}{selectedFolderPath}",
                    UseShellExecute = true // Opens the URL in the default browser
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true // Opens the URL in the default browser
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void Hyperlink_Delete(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to delete this file?",
                    "Delete file",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    await objectStorageService.DeleteObjectAsync(e.Uri.ToString());
                    await objectStorageService.LoadFolderItemsAsync(selectedFolderPath, folders, files, false);
                    e.Handled = true;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


    }
}