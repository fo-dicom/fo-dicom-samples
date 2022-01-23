// Copyright (c) 2012-2022 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FellowOakDicom.IO.Buffer;

namespace FellowOakDicom.Samples.Compare
{
    public partial class MainForm : Form
    {
        private static readonly Color _none = Color.Transparent;

        private static readonly Color _green = Color.FromArgb(190, 240, 190);

        private static readonly Color _yellow = Color.FromArgb(255, 255, 217);

        private static readonly Color _red = Color.FromArgb(255, 200, 200);

        private static readonly Color _gray = Color.FromArgb(200, 200, 200);

        private DicomFile _file1;

        private DicomFile _file2;

        private int _level = 0;

        private string _indent = string.Empty;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
        }

        public int Level
        {
            get => _level;
            set
            {
                _level = value;
                _indent = "".PadRight(_level * 4);
            }
        }

        private void OnClickSelect(object sender, EventArgs e)
        {
            DicomFile file1;
            while (true)
            {
                var ofd = new OpenFileDialog
                {
                    Title = "Choose first DICOM file",
                    Filter = "DICOM Files (*.dcm;*.dic)|*.dcm;*.dic|All Files (*.*)|*.*"
                };

                if (ofd.ShowDialog(this) == DialogResult.Cancel)
                {
                    return;
                }

                try
                {
                    file1 = DicomFile.Open(ofd.FileName);
                    break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this,
                        ex.Message,
                        "Error opening DICOM file",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            DicomFile file2;
            while (true)
            {
                var ofd = new OpenFileDialog
                {
                    Title = "Choose second DICOM file",
                    Filter = "DICOM Files (*.dcm;*.dic)|*.dcm;*.dic|All Files (*.*)|*.*"
                };

                if (ofd.ShowDialog(this) == DialogResult.Cancel)
                {
                    return;
                }

                try
                {
                    file2 = DicomFile.Open(ofd.FileName);
                    break;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this,
                        ex.Message,
                        "Error opening DICOM file",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }

            _file1 = file1;
            _file2 = file2;

            lblFile1.Text = _file1.File.Name;
            lblFile2.Text = _file2.File.Name;

            CompareFiles();
        }

        private void CompareFiles()
        {
            Level = 0;

            try
            {
                lvFile1.BeginUpdate();
                lvFile2.BeginUpdate();

                lvFile1.Items.Clear();
                lvFile2.Items.Clear();

                CompareDatasets(_file1.FileMetaInfo, _file2.FileMetaInfo);
                CompareDatasets(_file1.Dataset, _file2.Dataset);

                OnSizeChanged(lvFile1, EventArgs.Empty);
                OnSizeChanged(lvFile2, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    this,
                    ex.Message,
                    "Error comparing DICOM files",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                lvFile1.EndUpdate();
                lvFile2.EndUpdate();
            }
        }

        private void CompareDatasets(DicomDataset d1, DicomDataset d2)
        {
            var e1 = new Queue<DicomItem>(d1 ?? new DicomDataset());
            var e2 = new Queue<DicomItem>(d2 ?? new DicomDataset());

            while (e1.Count > 0 || e2.Count > 0)
            {
                DicomItem i1 = null;
                if (e1.Count > 0)
                {
                    i1 = e1.Peek();
                }

                DicomItem i2 = null;
                if (e2.Count > 0)
                {
                    i2 = e2.Peek();
                }

                if (i1 != null && i2 != null)
                {
                    if (i1.Tag.Group < i2.Tag.Group)
                    {
                        AddItem(i1, null);
                        e1.Dequeue();
                        continue;
                    }

                    if (i1.Tag.Group == i2.Tag.Group && i1.Tag.Element < i2.Tag.Element)
                    {
                        AddItem(i1, null);
                        e1.Dequeue();
                        continue;
                    }
                }

                if (i2 != null && i1 != null)
                {
                    if (i2.Tag.Group < i1.Tag.Group)
                    {
                        AddItem(null, i2);
                        e2.Dequeue();
                        continue;
                    }

                    if (i2.Tag.Group == i1.Tag.Group && i2.Tag.Element < i1.Tag.Element)
                    {
                        AddItem(null, i2);
                        e2.Dequeue();
                        continue;
                    }
                }

                AddItem(i1, i2);

                if (i1 != null)
                {
                    e1.Dequeue();
                }

                if (i2 != null)
                {
                    e2.Dequeue();
                }
            }
        }

        private void CompareSequences(DicomSequence s1, DicomSequence s2)
        {
            if (s1 == null)
            {
                AddItem(s1, lvFile1, _gray);
                AddItem(s2, lvFile2, _green);
            }
            else if (s2 == null)
            {
                AddItem(s1, lvFile1, _green);
                AddItem(s2, lvFile2, _gray);
            }
            else
            {
                AddItem(s1, lvFile1, _none);
                AddItem(s2, lvFile2, _none);
            }

            Level++;

            int count = 0;
            if (s1 != null)
            {
                count = s1.Items.Count;
            }

            if (s2 != null && s2.Items.Count > count)
            {
                count = s2.Items.Count;
            }

            for (int i = 0; i < count; i++)
            {
                DicomDataset d1 = null;
                if (s1 != null && i < s1.Items.Count)
                {
                    d1 = s1.Items[i];
                }

                DicomDataset d2 = null;
                if (s2 != null && i < s2.Items.Count)
                {
                    d2 = s2.Items[i];
                }

                if (d1 == null)
                {
                    AddItem(string.Empty, uint.MaxValue, string.Empty, lvFile1, _gray);
                    AddItem(GetTagName(DicomTag.Item), uint.MaxValue, string.Empty, lvFile2, _green);
                }
                else if (d2 == null)
                {
                    AddItem(GetTagName(DicomTag.Item), uint.MaxValue, string.Empty, lvFile1, _green);
                    AddItem(string.Empty, uint.MaxValue, string.Empty, lvFile2, _gray);
                }
                else
                {
                    AddItem(GetTagName(DicomTag.Item), uint.MaxValue, string.Empty, lvFile1, _none);
                    AddItem(GetTagName(DicomTag.Item), uint.MaxValue, string.Empty, lvFile2, _none);
                }

                Level++;
                CompareDatasets(d1, d2);
                Level--;
            }

            Level--;
        }

        private void CompareFragments(DicomItem i1, DicomItem i2)
        {
            DicomFragmentSequence s1 = null;
            DicomFragmentSequence s2 = null;

            bool pixel = cbIgnorePixelData.Checked && i1.Tag == DicomTag.PixelData;

            if (i1 == null)
            {
                AddItem(i1, lvFile1, _gray);
                AddItem(i2, lvFile2, _green);
                s2 = i2 as DicomFragmentSequence;
            }
            else if (i2 == null)
            {
                AddItem(i1, lvFile1, _green);
                AddItem(i2, lvFile2, _gray);
                s1 = i1 as DicomFragmentSequence;
            }
            else if (!(i1 is DicomFragmentSequence))
            {
                AddItem(i1, lvFile1, pixel ? _yellow : _red);
                AddItem(i2, lvFile2, pixel ? _yellow : _red);
                s2 = i2 as DicomFragmentSequence;
            }
            else if (!(i2 is DicomFragmentSequence))
            {
                AddItem(i1, lvFile1, pixel ? _yellow : _red);
                AddItem(i2, lvFile2, pixel ? _yellow : _red);
                s1 = i1 as DicomFragmentSequence;
            }
            else
            {
                AddItem(i1, lvFile1, pixel ? _yellow : _none);
                AddItem(i2, lvFile2, pixel ? _yellow : _none);
                s1 = i1 as DicomFragmentSequence;
                s2 = i2 as DicomFragmentSequence;
            }

            Level++;

            if (s1 == null)
            {
                AddItem(string.Empty, uint.MaxValue, string.Empty, lvFile1, _gray);
                AddItem(
                    _indent + "Offset Table",
                    (uint)s2.OffsetTable.Count * 4,
                    string.Format("@entries={0}", s2.OffsetTable.Count),
                    lvFile2,
                    pixel ? _yellow : _red);
            }
            else if (s2 == null)
            {
                AddItem(
                    _indent + "Offset Table",
                    (uint)s1.OffsetTable.Count * 4,
                    string.Format("@entries={0}", s1.OffsetTable.Count),
                    lvFile1,
                    pixel ? _yellow : _red);
                AddItem(string.Empty, uint.MaxValue, string.Empty, lvFile2, _gray);
            }
            else
            {
                Color c = _none;
                if (s1.OffsetTable.Count != s2.OffsetTable.Count)
                {
                    c = _red;
                }
                else
                {
                    for (int i = 0; i < s1.OffsetTable.Count; i++)
                    {
                        if (s1.OffsetTable[i] != s2.OffsetTable[i])
                        {
                            c = _red;
                            break;
                        }
                    }
                }
                AddItem(
                    _indent + "Offset Table",
                    (uint)s2.OffsetTable.Count * 4,
                    string.Format("@entries={0}", s1.OffsetTable.Count),
                    lvFile1,
                    pixel ? _yellow : c);
                AddItem(
                    _indent + "Offset Table",
                    (uint)s2.OffsetTable.Count * 4,
                    string.Format("@entries={0}", s2.OffsetTable.Count),
                    lvFile2,
                    pixel ? _yellow : c);
            }

            int count = 0;
            if (s1 != null)
            {
                count = s1.Fragments.Count;
            }

            if (s2 != null && s2.Fragments.Count > count)
            {
                count = s2.Fragments.Count;
            }

            string name = _indent + "Fragment";

            for (int i = 0; i < count; i++)
            {
                IByteBuffer b1 = null;
                if (s1 != null && i < s1.Fragments.Count)
                {
                    b1 = s1.Fragments[i];
                }

                IByteBuffer b2 = null;
                if (s2 != null && i < s2.Fragments.Count)
                {
                    b2 = s2.Fragments[i];
                }

                if (b1 == null)
                {
                    AddItem(string.Empty, uint.MaxValue, string.Empty, lvFile1, _gray);
                    AddItem(name, (uint)b2.Size, string.Empty, lvFile2, pixel ? _yellow : _red);
                    continue;
                }
                else if (b2 == null)
                {
                    AddItem(name, (uint)b1.Size, string.Empty, lvFile1, pixel ? _yellow : _red);
                    AddItem(string.Empty, uint.MaxValue, string.Empty, lvFile2, _gray);
                    continue;
                }

                Color c = _none;
                if (pixel)
                {
                    c = _yellow;
                }
                else if (!Compare(b1.Data, b2.Data))
                {
                    c = _red;
                }

                AddItem(name, (uint)b1.Size, string.Empty, lvFile1, c);
                AddItem(name, (uint)b2.Size, string.Empty, lvFile2, c);
            }

            Level--;
        }

        private string GetTagName(DicomTag t)
        {
            return string.Format("{0}{1}  {2}", _indent, t.ToString().ToUpper(), t.DictionaryEntry.Name);
        }

        private void AddItem(string t, uint l, string v, ListView lv, Color c)
        {
            var lvi = lv.Items.Add(t);
            lvi.SubItems.Add(!string.IsNullOrEmpty(t) ? "--" : string.Empty);
            if (l == uint.MaxValue)
            {
                lvi.SubItems.Add(!string.IsNullOrEmpty(t) ? "-" : string.Empty);
            }
            else
            {
                lvi.SubItems.Add(l.ToString());
            }

            lvi.SubItems.Add(v);
            lvi.UseItemStyleForSubItems = true;
            lvi.BackColor = c;
        }

        private void AddItem(DicomItem i, ListView lv, Color c)
        {
            ListViewItem lvi = null;

            if (i != null)
            {
                var tag = GetTagName(i.Tag);
                lvi = lv.Items.Add(tag);
                lvi.SubItems.Add(i.ValueRepresentation.Code);
                if (i is DicomElement)
                {
                    var e = i as DicomElement;
                    lvi.SubItems.Add(e.Length.ToString());
                    string value = "<large value not displayed>";
                    if (e.Length <= 2048)
                    {
                        value = string.Join("\\", e.Get<string[]>());
                    }

                    lvi.SubItems.Add(value);
                }
                else
                {
                    lvi.SubItems.Add("-");
                    lvi.SubItems.Add(string.Empty);
                }
                lvi.Tag = i;
            }
            else
            {
                lvi = lv.Items.Add(string.Empty);
                lvi.SubItems.Add(string.Empty);
                lvi.SubItems.Add(string.Empty);
                lvi.SubItems.Add(string.Empty);
            }

            lvi.UseItemStyleForSubItems = true;
            lvi.BackColor = c;
        }

        private void AddItem(DicomItem i1, DicomItem i2)
        {
            if (i1 is DicomSequence || i2 is DicomSequence)
            {
                CompareSequences(i1 as DicomSequence, i2 as DicomSequence);
                return;
            }

            if (i2 == null)
            {
                AddItem(i1, lvFile1, _green);
                AddItem(i2, lvFile2, _gray);
                return;
            }

            if (i1 == null)
            {
                AddItem(i1, lvFile1, _gray);
                AddItem(i2, lvFile2, _green);
                return;
            }

            if (i1 is DicomElement && i2 is DicomElement)
            {
                var e1 = i1 as DicomElement;
                var e2 = i2 as DicomElement;

                var c = _none;
                if (!cbIgnoreVR.Checked && e1.ValueRepresentation != e2.ValueRepresentation)
                {
                    c = _red;
                }
                else if (!Compare(e1.Buffer.Data, e2.Buffer.Data))
                {
                    c = _red;
                }

                if (cbIgnoreGroupLengths.Checked && e1.Tag.Element == 0x0000)
                {
                    c = _yellow;
                }

                if (cbIgnoreUIDs.Checked && e1.ValueRepresentation == DicomVR.UI)
                {
                    var uid = (i1 as DicomElement).Get<DicomUID>(0);
                    if (uid != null && (uid.Type == DicomUidType.SOPInstance || uid.Type == DicomUidType.Unknown))
                    {
                        c = _yellow;
                    }
                }

                if (cbIgnorePixelData.Checked && i1.Tag == DicomTag.PixelData)
                {
                    c = _yellow;
                }

                AddItem(i1, lvFile1, c);
                AddItem(i2, lvFile2, c);
                return;
            }

            if (i1 is DicomFragmentSequence || i2 is DicomFragmentSequence)
            {
                CompareFragments(i1, i2);
                return;
            }

            if (i1 is DicomElement || i2 is DicomElement)
            {
                AddItem(i1, lvFile1, _red);
                AddItem(i2, lvFile2, _red);
                return;
            }

            AddItem(i1, lvFile1, _yellow);
            AddItem(i2, lvFile2, _yellow);
        }

        private static bool Compare(byte[] b1, byte[] b2)
        {
            if (b1.Length != b2.Length)
            {
                return false;
            }

            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] != b2[i])
                {
                    return false;
                }
            }

            return true;
        }

        private void OnScroll(object sender, ScrollEventArgs e)
        {
            if (sender == lvFile1)
            {
                int index = lvFile1.TopItem.Index;
                lvFile2.TopItem = lvFile2.Items[index];
            }
            else
            {
                int index = lvFile2.TopItem.Index;
                lvFile1.TopItem = lvFile1.Items[index];
            }
        }

        private void OnSelect(object sender, EventArgs e)
        {
            if (sender == lvFile1)
            {
                if (lvFile1.SelectedIndices.Count > 0)
                {
                    int index = lvFile1.SelectedIndices[0];
                    lvFile2.Items[index].Selected = true;
                }
            }
            else
            {
                if (lvFile2.SelectedIndices.Count > 0)
                {
                    int index = lvFile2.SelectedIndices[0];
                    lvFile1.Items[index].Selected = true;
                }
            }
        }

        private void OnMouseEnter(object sender, EventArgs e)
        {
            ((Control)sender).Focus();
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            var lv = (ListViewEx)sender;
            var width = lv.Columns[0].Width + lv.Columns[1].Width + lv.Columns[2].Width;
            lv.Columns[3].Width = Math.Max(lv.ClientSize.Width - width, 440);
        }

        private void OnOptionChanged(object sender, EventArgs e)
        {
            int index = -1;
            if (lvFile1.TopItem != null)
            {
                index = lvFile1.TopItem.Index;
            }

            CompareFiles();

            if (index != -1 && index < lvFile1.Items.Count)
            {
                lvFile1.TopItem = lvFile1.Items[index];
                lvFile2.TopItem = lvFile2.Items[index];
            }
        }
    }
}
