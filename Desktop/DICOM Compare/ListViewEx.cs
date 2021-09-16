// Copyright (c) 2012-2021 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System.Windows.Forms;

namespace FellowOakDicom.Samples.Compare
{
    internal class ListViewEx : ListView
    {
        private const int WM_VSCROLL = 0x0115;

        private const int WM_MOUSEWHEEL = 0x020A;

        public event ScrollEventHandler Scroll;

        protected virtual void OnScroll(ScrollEventArgs e)
        {
            Scroll?.Invoke(this, e);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_VSCROLL)
            {
                OnScroll(new ScrollEventArgs((ScrollEventType)(m.WParam.ToInt32() & 0xffff), 0));
            }
            else if (m.Msg == WM_MOUSEWHEEL)
            {
                OnScroll(new ScrollEventArgs(ScrollEventType.EndScroll, 0));
            }
        }
    }
}
