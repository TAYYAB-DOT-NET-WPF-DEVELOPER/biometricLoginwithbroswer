using Back_Office.ViewModels;
using DPUruNet;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Imaging;
using System.Drawing;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using Back_Office.Styles;
using System.IO;
using System.Windows.Input;
using static DPUruNet.Constants;
using System.Net;
using Newtonsoft.Json;

namespace biometric_Login.ViewModels
{
    public class LoginVM : ViewModelBase
    {
        int USERID = 0000;
        private bool getuserid = false;
        public ICommand ClearPinCommand { get; }
        public String connectionString = "Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = 182.180.159.89)(PORT = 1521)))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = SCAR))); User Id = KRC; Password=KRC;";
        private Window browserWindow;  // Field to hold the browser window instance
        private HttpListener listener;
        private Visibility _keypadVisibility = Visibility.Collapsed;
        public Visibility KeypadVisibility
        {
            get { return _keypadVisibility; }
            set
            {
                _keypadVisibility = value;
                OnPropertyChanged(nameof(KeypadVisibility));
            }
        }
        private string _pin;
        public string Pin
        {
            get { return _pin; }
            set
            {
                _pin = value;
                OnPropertyChanged(nameof(Pin));
                OnPropertyChanged(nameof(PinDisplay));
            }
        }
        public string PinDisplay => new string('*', Pin?.Length ?? 0);

        public ICommand ToggleKeypadCommand { get; }
        public ICommand AppendPinCommand { get; }
        private HttpListener listener2;
        private ObservableCollection<string> _devices;
        private readonly UserRegistrationVM _mainwindowVM;
        private readonly MainWindowVM _broserwindow;
        public ICommand LoginCommand { get; }
        public ICommand SignUpCommand { get; }
        // Public property with ObservableCollection<string>
        public ObservableCollection<string> Devices
        {
            get { return _devices; }
            set
            {
                _devices = value;
                OnPropertyChanged(nameof(Devices));
            }
        }
        private string _selecteddevices;

        // Public string property with explicit get and set methods
        public string SelectedDevice
        {
            get { return _selecteddevices; }
            set
            {
                _selecteddevices = value;
                OnPropertyChanged(nameof(SelectedDevice));
            }
        }
        private byte[] _selectedImage;
        public byte[] SelectedImage
        {
            get { return _selectedImage; }
            set
            {
                _selectedImage = value;
                OnPropertyChanged(nameof(SelectedImage));
            }
        }
        public LoginVM(UserRegistrationVM mainwindowVM, MainWindowVM browswerwindow)
        {
            //LoginCommand = new RelayCommand(param => Load());
            LoginCommand = new RelayCommand(param => GotoMainWindow());
            ToggleKeypadCommand = new RelayCommand(p => ToggleKeypad());
            ClearPinCommand = new RelayCommand(p => Pin = string.Empty);
            AppendPinCommand = new RelayCommand(AppendPin);
            SignUpCommand = new RelayCommand(UserRegis);
            _mainwindowVM = mainwindowVM;
            _broserwindow = browswerwindow;
            InitializeHttpListener();
                  Load();
        }
        private void ToggleKeypad()
        {
            KeypadVisibility = KeypadVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void AppendPin(object param)
        {
            if (param is string number)
            {
                Pin += number;
            }
        }
        private void InitializeHttpListener()
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:5001/");
            listener.Start();
            listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
            listener2 = new HttpListener();
            listener2.Prefixes.Add("http://localhost:5002/");
            listener2.Start();
            listener2.BeginGetContext(new AsyncCallback(ListenerCallback2), listener2);

        }
        private void ListenerCallback2(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            try
            {
                getuserid = true;
                Load();
                response.ContentType = "application/json";
                var jsonResponse = new { USERID = USERID };
                string jsonText = JsonConvert.SerializeObject(jsonResponse);
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    writer.Write(jsonText);
                }
                response.StatusCode = 200;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error handling JSON request: " + ex.Message);
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }
            listener.BeginGetContext(new AsyncCallback(ListenerCallback2), listener);
        }
        private void ListenerCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);
            listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            try
            {
                CloseBrowserWindow();
                using (StreamWriter writer = new StreamWriter(response.OutputStream))
                {
                    writer.WriteLine("Handled Request: Browser window should be closed.");
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine("Error handling request: " + ex.Message);
            }
            finally
            {
                // Always close the response to avoid resource leaks
                response.Close();
            }
        }
        public void Dispose()
        {
            listener?.Stop();
            listener?.Close();
        }
        private void UserRegis(object obj)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            var window = new SaveFinger()
            {
                DataContext = _mainwindowVM
            };
            window.Show();
            Application.Current.MainWindow.Close();
            Application.Current.MainWindow = window;
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
        }
        private void GotoMainWindow()
        {
            OracleConnection connection = new OracleConnection(connectionString);
            connection.Open();
            OracleCommand cmd = new OracleCommand("Select * from  SMUSER", connection);
            OracleDataAdapter adapter = new OracleDataAdapter(cmd);
            DataTable dt = new DataTable();
            adapter.Fill(dt);
            connection.Close();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    string password = row["PWD"].ToString();
                    if (password == Pin)
                    {
                        string url = "https://alburoojmgmtconsultants.com/";
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            OpenFullScreenBrowser(url);
                        });
                    }

                }
                ShowErrorDialog("Wrong Password Please Try Again.");

            }
            //string url = "http://krc:7999/apex/f?p=105:11:::NO:111:P11_USERNAME,P11_PASSWORD:Controller,77779999";

        }
        private void OpenFullScreenBrowser(string url)
        {
            if (browserWindow != null)
            {
                browserWindow.Close();  // Close the existing window if it's already open
            }

            browserWindow = new Window
            {
                Topmost = true
            };
            var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
            webView.Source = new Uri(url);
            browserWindow.Content = webView;
            browserWindow.Show();
        }
        public void CloseBrowserWindow()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (browserWindow != null)
                {
                    browserWindow.Close();
                    browserWindow = null;  // Set to null after closing
                }
            });
        }
        private void Load()
        {
            frmDBVerify_Load(null, EventArgs.Empty);
        }
        private Reader currentReader;
        public Reader CurrentReader
        {
            get { return currentReader; }
            set
            {
                currentReader = value;
                SendMessage(Action.UpdateReaderState, value);
            }
        }
        private ReaderCollection _readers;
        private void LoadScanners()
        {
            Devices = new ObservableCollection<string>();
            try
            {
                _readers = ReaderCollection.GetReaders();
                foreach (Reader Reader in _readers)
                {
                    Devices.Add(Reader.Description.Name);
                }
                if (Devices.Count > 0)
                {
                    ShowSuccessDialog("Finger Print Device Connected Successfully");
                    SelectedDevice = Devices[0];
                }
                else
                {
                    ShowErrorDialog("Unable to connect device.");
                
                }
            }
            catch (Exception ex)
            {
                //message box:
                String text = ex.Message;
                text += "\r\n\r\nPlease check if DigitalPersona service has been started";
                String caption = "Cannot access readers";
                MessageBox.Show(text, caption);
            }
        }
        private const int PROBABILITY_ONE = 0x7fffffff;
        private Fmd firstFinger;
        int count = 0;
        DataResult<Fmd> resultEnrollment;
        List<Fmd> preenrollmentFmds;
        /// <summary>
        /// Open a device and check result for errors.
        /// </summary>
        /// <returns>Returns true if successful; false if unsuccessful</returns>
        public bool OpenReader()
        {
            using (Tracer tracer = new Tracer("Form_Main::OpenReader"))
            {
                reset = false;
                Constants.ResultCode result = Constants.ResultCode.DP_DEVICE_FAILURE;

                // Open reader
                result = currentReader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);

                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    MessageBox.Show("Error:  " + result);
                    reset = true;
                    return false;
                }

                return true;
            }
        }
        /// <summary>
        /// Check quality of the resulting capture.
        /// </summary>
        public bool CheckCaptureResult(CaptureResult captureResult)
        {
            using (Tracer tracer = new Tracer("Form_Main::CheckCaptureResult"))
            {
                if (captureResult.Data == null || captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    if (captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                    {
                        reset = true;
                        throw new Exception(captureResult.ResultCode.ToString());
                    }

                    // Send message if quality shows fake finger
                    if ((captureResult.Quality != Constants.CaptureQuality.DP_QUALITY_CANCELED))
                    {
                        throw new Exception("Quality - " + captureResult.Quality);
                    }
                    return false;
                }

                return true;
            }
        }
        //   private ReaderSelection _readerSelection;
        /// <summary>
        /// Hookup capture handler and start capture.
        /// </summary>
        /// <param name="OnCaptured">Delegate to hookup as handler of the On_Captured event</param>
        /// <returns>Returns true if successful; false if unsuccessful</returns>
        public bool StartCaptureAsync(Reader.CaptureCallback OnCaptured)
        {
            using (Tracer tracer = new Tracer("Form_Main::StartCaptureAsync"))
            {
                // Activate capture handler
                currentReader.On_Captured += new Reader.CaptureCallback(OnCaptured);

                // Call capture
                if (!CaptureFingerAsync())
                {
                    return false;
                }

                return true;
            }
        }
        /// <summary>
        /// Check the device status before starting capture.
        /// </summary>
        /// <returns></returns>
        public void GetStatus()
        {
            using (Tracer tracer = new Tracer("Form_Main::GetStatus"))
            {
                Constants.ResultCode result = currentReader.GetStatus();

                if ((result != Constants.ResultCode.DP_SUCCESS))
                {
                    reset = true;
                    throw new Exception("" + result);
                }

                if ((currentReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_BUSY))
                {
                    Thread.Sleep(50);
                }
                else if ((currentReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_NEED_CALIBRATION))
                {
                    currentReader.Calibrate();
                }
                else if ((currentReader.Status.Status != Constants.ReaderStatuses.DP_STATUS_READY))
                {
                    throw new Exception("Reader Status - " + currentReader.Status.Status);
                }
            }
        }
        /// <summary>
        /// Function to capture a finger. Always get status first and calibrate or wait if necessary.  Always check status and capture errors.
        /// </summary>
        /// <param name="fid"></param>
        /// <returns></returns>
        public bool CaptureFingerAsync()
        {
            using (Tracer tracer = new Tracer("Form_Main::CaptureFingerAsync"))
            {
                try
                {
                    GetStatus();

                    Constants.ResultCode captureResult = currentReader.CaptureAsync(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, currentReader.Capabilities.Resolutions[0]);
                    if (captureResult != Constants.ResultCode.DP_SUCCESS)
                    {
                        reset = true;
                        throw new Exception("" + captureResult);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:  " + ex.Message);
                    return false;
                }
            }
        }
        /// <summary>
        /// Create a bitmap from raw data in row/column format.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Bitmap CreateBitmap(byte[] bytes, int width, int height)
        {
            byte[] rgbBytes = new byte[bytes.Length * 3];

            for (int i = 0; i <= bytes.Length - 1; i++)
            {
                rgbBytes[(i * 3)] = bytes[i];
                rgbBytes[(i * 3) + 1] = bytes[i];
                rgbBytes[(i * 3) + 2] = bytes[i];
            }
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            for (int i = 0; i <= bmp.Height - 1; i++)
            {
                IntPtr p = new IntPtr(data.Scan0.ToInt64() + data.Stride * i);
                System.Runtime.InteropServices.Marshal.Copy(rgbBytes, i * bmp.Width * 3, p, bmp.Width * 3);
            }

            bmp.UnlockBits(data);

            return bmp;
        }
        /// <summary>
        /// Handler for when a fingerprint is captured.
        /// </summary>
        /// <param name="captureResult">contains info and data on the fingerprint capture</param>
        public void OnCaptured(CaptureResult captureResult)
        {
            try
            {
                if (!CheckCaptureResult(captureResult)) return;
                foreach (Fid.Fiv fiv in captureResult.Data.Views)
                {
                    SendMessage(Action.SendBitmap, CreateBitmap(fiv.RawImage, fiv.Width, fiv.Height));
                }
                try
                {
                    if (!CheckCaptureResult(captureResult)) return;
                    DataResult<Fmd> resultConversion = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);
                    if (resultConversion.ResultCode != Constants.ResultCode.DP_SUCCESS)
                    {
                        if (resultConversion.ResultCode != Constants.ResultCode.DP_TOO_SMALL_AREA)
                        {
                            Reset = true;
                        }
                        throw new Exception(resultConversion.ResultCode.ToString());
                    }
                    firstFinger = resultConversion.Data;
                    OracleConnection connection = new OracleConnection(connectionString);
                    connection.Open();
                    OracleCommand cmd = new OracleCommand("Select * from  userfinger", connection);
                    OracleDataAdapter adapter = new OracleDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    connection.Close();
                    List<string> lstledgerIds = new List<string>();
                    count = 0;
                    if (dt.Rows.Count > 0)
                    {
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            var userid = dt.Rows[i]["UserID"].ToString();
                            lstledgerIds.Add(dt.Rows[i]["UserID"].ToString());
                            byte[] fingerDataBytes = (byte[])dt.Rows[i]["fingerdata"];
                            string fingerDataXml = System.Text.Encoding.UTF8.GetString(fingerDataBytes);
                            Fmd val = Fmd.DeserializeXml(fingerDataXml);
                            CompareResult compare = Comparison.Compare(firstFinger, 0, val, 0);
                            if (compare.ResultCode != Constants.ResultCode.DP_SUCCESS)
                            {
                                Reset = true;
                                throw new Exception(compare.ResultCode.ToString());
                            }
                            if (Convert.ToDouble(compare.Score.ToString()) == 0)
                            {
                                count++;
                                if (getuserid)
                                {
                                    USERID = Convert.ToInt32(userid);
                                    
                                    getuserid = false;
                                }
                                else
                                {
                                    GotoMainWindow();
                                }
                            }

                        }
                        if (count == 0)
                        {
                            SendMessage(Action.SendMessage, "Fingerprint not registered.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Send error message, then close form
                    SendMessage(Action.SendMessage, "Error:  " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                // Send error message, then close form
                SendMessage(Action.SendMessage, "Error:  " + ex.Message);
            }
        }
        /// <summary>
        /// Holds fmds enrolled by the enrollment GUI.
        /// </summary>
        public Dictionary<int, Fmd> Fmds
        {
            get { return fmds; }
            set { fmds = value; }
        }
        private Dictionary<int, Fmd> fmds = new Dictionary<int, Fmd>();

        /// <summary>
        /// Reset the UI causing the user to reselect a reader.
        /// </summary>
        public bool Reset
        {
            get { return reset; }
            set { reset = value; }
        }
        private bool reset;


        private enum Action
        {
            UpdateReaderState,
            SendBitmap,
            SendMessage
        }
        private delegate void SendMessageCallback(Action state, object payload);
        private void SendMessage(Action action, object payload)
        {
            try
            {
                switch (action)
                {
                    case Action.SendMessage:
                        MessageBox.Show((string)payload);
                        break;
                    case Action.SendBitmap:
                        // Convert Bitmap to byte array
                        byte[] imageBytes;
                        using (MemoryStream stream = new MemoryStream())
                        {
                            ((System.Drawing.Bitmap)payload).Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            imageBytes = stream.ToArray();
                        }

                        // Set SelectedImage property
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            SelectedImage = imageBytes;
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
        private void frmDBVerify_Load(object sender, EventArgs e)
        {
            LoadScanners();
            if (Devices.Count == 0) // Check if any devices are connected
            {
                firstFinger = null;
                resultEnrollment = null;
                preenrollmentFmds = new List<Fmd>();
                return; // Exit the method if no devices are connected
            }
            firstFinger = null;
            resultEnrollment = null;
            preenrollmentFmds = new List<Fmd>();
            if (CurrentReader != null)
            {
                CurrentReader.Dispose();
                CurrentReader = null;
            }
            CurrentReader = _readers[0];
            if (!OpenReader())
            {
                //this.Close();
            }

            if (!StartCaptureAsync(this.OnCaptured))
            {
                //this.Close();
            }

        }
        public void ShowErrorDialog(string errorMessage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Create and show error dialog on the UI thread
                ErrorDialogue errorDialogue = new ErrorDialogue();
                errorDialogue.ErrorMessageText.Text = errorMessage;
                errorDialogue.ShowDialog();
            });
        }
        public void ShowSuccessDialog(string errorMessage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Create and show error dialog on the UI thread
                SuccessDialogue errorDialogue = new SuccessDialogue();
                errorDialogue.SuccessMessageText.Text = errorMessage;
                errorDialogue.ShowDialog();
            });
        }

    }
}
