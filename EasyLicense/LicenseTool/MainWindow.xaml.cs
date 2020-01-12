using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows;
using EasyLicense.Lib;
using EasyLicense.Lib.License;
using EasyLicense.Lib.License.Validator;

namespace EasyLicense.LicenseTool
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            txtComputerKey.Text = new HardwareInfo().GetHardwareString();
            expireDatePicker.DisplayDateStart = DateTime.Now.AddMonths(-10);
            expireDatePicker.DisplayDateEnd = DateTime.Now.AddYears(10);
            expireDatePicker.SelectedDate = DateTime.Now.AddDays(5);
        }

        private void btnGenerateLicense_Click(object sender, RoutedEventArgs e)
        {
            GenerateLicense();

            ValidateLicense();
        }

        private void GenerateLicense()
        {

            var privateKeyPath = GetWorkDirFile("privateKey.xml");
            var publicKeyPath = GetWorkDirFile("publicKey.xml");

            if (!File.Exists(privateKeyPath))
            {
                MessageBox.Show("Please create a license key first");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtComputerKey.Text))
            {
                MessageBox.Show("Some field is missing");
                return;
            }

            var privateKey = File.ReadAllText(privateKeyPath);
            var generator = new LicenseGenerator(privateKey);

            var dict = new Dictionary<string, string>();

            dict["name"] = txtName.Text;
            dict["key"] = txtComputerKey.Text;

            LicenseType licenseType = LicenseType.None;
            var selectedType = (type.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Tag;
            Enum.TryParse<LicenseType>(selectedType.ToString(), out licenseType);

            var expireDate = DateTime.Now.AddYears(10);

            if (licenseType == LicenseType.None)
            {
                MessageBox.Show("授权类型不能为空");
                return;
            }

            switch (licenseType)
            {
                case LicenseType.None:
                    break;
                case LicenseType.Trial:
                    expireDate = expireDatePicker.SelectedDate.Value;
                    break;
                case LicenseType.Standard:
                    break;
                case LicenseType.Personal:
                    break;
                case LicenseType.Floating:
                    break;
                case LicenseType.Subscription:
                    break;
                default:
                    break;
            }


            // generate the license
            var license = generator.Generate("W7.License", Guid.NewGuid(), expireDate, dict,
                licenseType);

            txtLicense.Text = license;
            var dir = $"{GetWorkDirFile("")}\\{dict["name"]}-{licenseType.ToString()}-{txtComputerKey.Text}-{expireDate.ToString("yyyyMMdd")}\\";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(dir + "license.lic", license);

            File.Copy(publicKeyPath, dir + "publicKey.xml",true);

            System.Diagnostics.Process.Start(dir);

            File.AppendAllText("license.log", $"License to {dict["name"]}, key is {dict["key"]}, Date is {DateTime.Now}");
        }

        private void ValidateLicense()
        {

            var privateKeyPath = GetWorkDirFile("privateKey.xml");
            var publicKeyPath = GetWorkDirFile("publicKey.xml");

            if (!File.Exists(publicKeyPath))
            {
                MessageBox.Show("Please create a license key first");
                return;
            }

            var publicKey = File.ReadAllText(publicKeyPath);

            var validator = new LicenseValidator(publicKey, @"license.lic");

            try
            {
                validator.AssertValidLicense();

                var dict = validator.LicenseAttributes;
                MessageBox.Show($"License to {dict["name"]}, key is {dict["key"]}");

                if (dict["key"] != txtComputerKey.Text)
                {
                    MessageBox.Show("invalid!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void btnGenerateLicenseKey_Click(object sender, RoutedEventArgs e)
        {
            // var assembly = AppDomain.CurrentDomain.BaseDirectory;
            var privateKeyPath = GetWorkDirFile("privateKey.xml");
            var publicKeyPath = GetWorkDirFile("publicKey.xml");

            if (File.Exists(privateKeyPath) || File.Exists(publicKeyPath))
            {
                var result = MessageBox.Show("The key is existed, override it?", "Warning", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            var privateKey = "";
            var publicKey = "";
            LicenseGenerator.GenerateLicenseKey(out privateKey, out publicKey);

            File.WriteAllText(privateKeyPath, privateKey);
            File.WriteAllText(publicKeyPath, publicKey);

            MessageBox.Show("The Key is created, please backup it.");
        }


        private void btnChangeWorkDir_Click(object sender, RoutedEventArgs e)
        {
            var d = new System.Windows.Forms.FolderBrowserDialog();
            var result = d.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //if (File.Exists(Path.Combine(d.SelectedPath, "publicKey.xml"))
                //     && File.Exists(Path.Combine(d.SelectedPath, "privateKey.xml")))
                //{
                txtWorkDir.Text = d.SelectedPath;
                //}
                //else
                //{
                //    MessageBox.Show("The public/private Key is not found.");
                //}
            }

        }

        string GetWorkDirFile(string name)
        {
            return Path.Combine(txtWorkDir.Text, name);
        }
    }
}