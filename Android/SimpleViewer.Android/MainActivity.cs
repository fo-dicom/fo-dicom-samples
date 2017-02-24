using System;
using System.IO;

using Android.App;
using Android.Graphics;
using Android.Widget;
using Android.OS;
using Dicom;
using Dicom.Imaging;

namespace SimpleViewer.Android
{
    [Activity(Label = "SimpleViewer.Android", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            try
            {
                // Read and render DICOM image
                Bitmap bitmap;
                using (var stream = Assets.Open("jpeg-baseline.dcm"))
                using (var inner = new MemoryStream())
                {
                    stream.CopyTo(inner);
                    inner.Seek(0, SeekOrigin.Begin);

                    var dicomFile = DicomFile.Open(inner);
                    var dicomImage = new DicomImage(dicomFile.Dataset);
                    bitmap = dicomImage.RenderImage().AsBitmap();
                }

                // Draw rendered image in image view
                var imageView = FindViewById<ImageView>(Resource.Id.MyImageView);
                imageView.SetImageBitmap(bitmap);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}

