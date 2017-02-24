using System;
using System.IO;

using Android.App;
using Android.Graphics;
using Android.Widget;
using Android.OS;
using Android.Views;

using Dicom;
using Dicom.Imaging;
using Dicom.Log;

using Java.Interop;

namespace SimpleViewer.Android
{
    [Activity(Label = "SimpleViewer.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        #region Fields

        private readonly string[] _fileNames = { "jpeg-baseline.dcm", "CT-MONO2-8-abdo", "US1_J2KI" };

        private int _counter;

        private ImageView _imageView;

        private TextView _textView;

        #endregion

        #region Methods

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _imageView = FindViewById<ImageView>(Resource.Id.MyImageView);
            _textView = FindViewById<TextView>(Resource.Id.MyTextView);

            Display(_fileNames[_counter]);
        }

        [Export("OnMyButtonClick")]
        public void OnMyButtonClick(View v)
        {
            ++_counter;
            if (_counter >= _fileNames.Length) _counter = 0;
            Display(_fileNames[_counter]);
        }

        private void Display(string fileName)
        {
            try
            {
                // Read and render DICOM image
                Bitmap bitmap;
                string dump;
                using (var stream = Assets.Open(fileName))
                using (var inner = new MemoryStream())
                {
                    stream.CopyTo(inner);
                    inner.Seek(0, SeekOrigin.Begin);

                    var dicomFile = DicomFile.Open(inner);
                    var dicomImage = new DicomImage(dicomFile.Dataset);
                    bitmap = dicomImage.RenderImage().AsBitmap();
                    dump = dicomFile.WriteToString();
                }

                // Draw rendered image in image view
                _imageView.SetImageBitmap(bitmap);

                // Display dump
                _textView.Text = dump;
            }
            catch (Exception e)
            {
                Toast.MakeText(ApplicationContext, e.Message, ToastLength.Long).Show();
            }
        }

        #endregion
    }
}

