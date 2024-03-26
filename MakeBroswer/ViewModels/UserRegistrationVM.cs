using Back_Office.ViewModels;
using DPUruNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Oracle.ManagedDataAccess.Client;
using System.IO;
using System.Collections.ObjectModel;
using Back_Office.Styles;
using System.Windows.Input;

namespace biometric_Login.ViewModels
{
    public class UserRegistrationVM:ViewModelBase
    {
        public UserRegistrationVM()
        {
            SaveCommand = new RelayCommand(parameter => button1_Click(parameter));

            CloseWindowCommand = new RelayCommand(closeWindow);

            //InitializeComponent();
        }
        public ICommand CloseWindowCommand { get; }
        private static DataResult<Fmd> _resultEnrollment;

        public static DataResult<Fmd> ResultEnrollment
        {
            get { return _resultEnrollment; }
            set { _resultEnrollment = value; }
        }

        public ICommand SaveCommand { get; }
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
        private string _descriptionText;
        public string DescriptionText
        {
            get { return _descriptionText; }
            set
            {
                if (_descriptionText != value)
                {
                    _descriptionText = value;
                    OnPropertyChanged(nameof(DescriptionText));
                   // OnPropertyChanged(nameof(RedAsteriskVisibility));
                }
            }
        }
        private ObservableCollection<string> _devices;

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
            set { _selecteddevices = value;
                OnPropertyChanged(nameof(SelectedDevice));
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
        private void closeWindow(object obj)
        {
            frmDBEnrollment_FormClosing(obj);
            if (obj is Window window)
            {
                window.Close();
            }
        }

        private Reader _reader;

      //  private ReaderSelection _readerSelection;
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
        /// Cancel the capture and then close the reader.
        /// </summary>
        /// <param name="OnCaptured">Delegate to unhook as handler of the On_Captured event </param>
        public void CancelCaptureAndCloseReader(Reader.CaptureCallback OnCaptured)
        {
            using (Tracer tracer = new Tracer("Form_Main::CancelCaptureAndCloseReader"))
            {
                if (currentReader != null)
                {
                    currentReader.CancelCapture();

                    // Dispose of reader handle and unhook reader events.
                    currentReader.Dispose();

                    if (reset)
                    {
                        CurrentReader = null;
                    }
                }
            }
        }
        // When set by child forms, shows s/n and enables buttons.
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
           // Devices.Clear();
          //  cboReaders.SelectedIndex = -1;

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
                    //btnSelect.Enabled = false;
                    //btnCaps.Enabled = false;
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
        public void frmDBEnrollment_Load(object sender, EventArgs e)
        {
            // Reset variables
            LoadScanners();
            firstFinger = null;
            _resultEnrollment = null;
            preenrollmentFmds = new List<Fmd>();
          //  pbFingerprint.Image = null;
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
        private const int PROBABILITY_ONE = 0x7fffffff;
        private Fmd firstFinger;
        int count = 0;
        DataResult<Fmd> resultEnrollment;
        List<Fmd> preenrollmentFmds;
        /// <summary>
        /// Handler for when a fingerprint is captured.
        /// </summary>
        /// <param name="captureResult">contains info and data on the fingerprint capture</param>
        
        private readonly object _lock = new object(); // Define a lock object for thread safety

        private void OnCaptured(CaptureResult captureResult)
        {
            try
            {
                // Check capture quality and throw an error if bad.
                if (!CheckCaptureResult(captureResult)) return;

                // Create bitmap
                foreach (Fid.Fiv fiv in captureResult.Data.Views)
                {
                    SendMessage(Action.SendBitmap, CreateBitmap(fiv.RawImage, fiv.Width, fiv.Height));
                }

                // Enrollment Code:
                try
                {
                    count++;
                    // Check capture quality and throw an error if bad.
                    DataResult<Fmd> resultConversion = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);

                    ShowSuccessDialog("A finger was captured: " + (count));

                    if (resultConversion.ResultCode != Constants.ResultCode.DP_SUCCESS)
                    {
                        Reset = true;
                        throw new Exception(resultConversion.ResultCode.ToString());
                    }

                    lock (_lock) // Lock access to ensure thread safety
                    {
                        preenrollmentFmds.Add(resultConversion.Data);

                        if (count >= 4)
                        {
                            _resultEnrollment = DPUruNet.Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ANSI, preenrollmentFmds);

                            if (_resultEnrollment.ResultCode == Constants.ResultCode.DP_SUCCESS)
                            {
                                preenrollmentFmds.Clear();
                                count = 0;
                                return;
                            }
                            else if (_resultEnrollment.ResultCode == Constants.ResultCode.DP_ENROLLMENT_INVALID_SET)
                            {
                                SendMessage(Action.SendMessage, "Enrollment was unsuccessful.  Please try again.");
                                preenrollmentFmds.Clear();
                                count = 0;
                                return;
                            }
                        }
                    }
                    ShowSuccessDialog("Now place the same finger on the reader.");
                }
                catch (Exception ex)
                {
                    SendMessage(Action.SendMessage, "Error:  " + ex.Message);
                }
            }
            catch (Exception ex)
            {
                SendMessage(Action.SendMessage, "Error:  " + ex.Message);
            }
        }
        private void button1_Click(object parameter)
        {
            lock (_lock) // Lock access to ensure thread safety
            {
                if (_resultEnrollment != null)
                {
                    try
                    {
                        OracleConnection connection = new OracleConnection(connectionString);
                        connection.Open();
                        OracleCommand cmd = new OracleCommand("INSERT INTO userfinger (UserID, fingerdata) VALUES (:UserID, :FingerData)", connection);
                        cmd.Parameters.Add("UserID", OracleDbType.Varchar2).Value = DescriptionText.ToString();

                        // Serialize fingerprint data to byte array
                        string xmlString = Fmd.SerializeXml(_resultEnrollment.Data);

                        // Convert XML string to byte array
                        byte[] fingerprintBytes = Encoding.UTF8.GetBytes(xmlString);

                        // Bind byte array to Oracle parameter
                        cmd.Parameters.Add("FingerData", OracleDbType.Blob).Value = fingerprintBytes;

                        cmd.ExecuteNonQuery();
                        connection.Close();
                        ShowSuccessDialog("Customer Finger Print was successfully enrolled.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
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
            Bitmap bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            for (int i = 0; i <= bmp.Height - 1; i++)
            {
                IntPtr p = new IntPtr(data.Scan0.ToInt64() + data.Stride * i);
                System.Runtime.InteropServices.Marshal.Copy(rgbBytes, i * bmp.Width * 3, p, bmp.Width * 3);
            }

            bmp.UnlockBits(data);

            return bmp;
        }

        public String connectionString = "Data Source = (DESCRIPTION = (ADDRESS_LIST = (ADDRESS = (PROTOCOL = TCP)(HOST = 182.180.159.89)(PORT = 1521)))(CONNECT_DATA = (SERVER = DEDICATED)(SERVICE_NAME = SCAR))); User Id = KRC; Password=KRC;";

        private void frmDBEnrollment_FormClosing(object para)
        {
            CancelCaptureAndCloseReader(this.OnCaptured);
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
