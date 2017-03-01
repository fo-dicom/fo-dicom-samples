// Copyright (c) 2012-2017 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.IO;

using Dicom;
using Dicom.Imaging;
using Dicom.Log;

using UIKit;

namespace SimpleViewer.iOS
{
    public partial class ViewController : UIViewController
    {
		private readonly string[] _fileNames = { "Assets/CT-MONO2-8-abdo", "Assets/jpeg-baseline.dcm", "Assets/US1_J2KI" };

		private int _counter = 0;

        public ViewController(IntPtr handle) : base(handle)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
			Display(_fileNames[_counter]);
        }

        public override void DidReceiveMemoryWarning()
        {
            base.DidReceiveMemoryWarning();
        }

		partial void NextImageButtonTouchUpInside(UIButton sender)
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
				UIImage image;
				string dump;

				using (var stream = File.OpenRead(fileName))
				{
					var dicomFile = DicomFile.Open(stream);
					var dicomImage = new DicomImage(dicomFile.Dataset);
					image = dicomImage.RenderImage().AsUIImage();
					dump = dicomFile.WriteToString();
				}

				// Draw rendered image in image view
				_imageView.Image = image;

				// Display dump
				_textView.Text = dump;
			}
			catch (Exception e)
			{
				var alert = new UIAlertView()
				{
					Title = "DICOM display failed",
					Message = e.Message
				};
				alert.AddButton("OK");
				alert.Show();
			}
		}
	}
}