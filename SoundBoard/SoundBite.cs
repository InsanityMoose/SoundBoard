using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSCore;
using CSCore.SoundOut;

namespace SoundBoard
{
    public delegate void PlayPressedEventHandler(object sender, EventArgs e);
    public delegate void SoundFileChanged(object sender, EventArgs e);
    public delegate void HotKeySetHandler(object sender, EventArgs e);
    public delegate void SetHotKeyStartHandler(object sender, EventArgs e);
    public delegate void OnPlaybackStopHandler(object sender, EventArgs e);

    public static class ControlExtensions
    {
        public static TResult InvokeEx<TControl, TResult>(this TControl control,
                                                   Func<TControl, TResult> func)
            where TControl : Control
            {
                return control.InvokeRequired
                        ? (TResult)control.Invoke(func, control)
                        : func(control);
            }

        public static void InvokeEx<TControl>(this TControl control,
                                              Action<TControl> func)
            where TControl : Control
            {
                control.InvokeEx(c => { func(c); return c; });
            }

        public static void InvokeEx<TControl>(this TControl control, Action action)
            where TControl : Control
            {
                control.InvokeEx(c => action());
            }
    }

    public partial class SoundBite : UserControl
    {

        public event PlayPressedEventHandler PlayPressed;
        public event SoundFileChanged OnSoundFileChanged;
        public event HotKeySetHandler OnHotKeySet;
        public event SetHotKeyStartHandler OnHotkeySetStart;
        public event OnPlaybackStopHandler OnPlayStop;

        public System.Drawing.Color color { get { return this.BackColor; } set { this.BackColor = value; } }

        public int id = -1;
        public Keys hotkey { get { return _hotkey; } set { _hotkey = value; OnHotKeySet(this, null); } }
        private Keys _hotkey;
        public Keys hotkeymod { get { return _hotkeymod; } set { _hotkeymod = value; OnHotKeySet(this, null); } }
        private Keys _hotkeymod;

        public string fileName { get { return _fileName; } set { _fileName = value; OnSoundFileChanged(this, null); } }
        private string _fileName;
        public bool isplaying = false;

        public SoundBite()
        {
            InitializeComponent();
            this.OnSoundFileChanged += SoundBite_OnSoundFileChanged;
            this.OnHotKeySet += SoundBite_OnHotKeySet;
            this.OnPlayStop += SoundBite_OnPlayStop;
        }

        public void Stop()
        {
            this.OnPlayStop(this, null);
        }

        void SoundBite_OnPlayStop(object sender, EventArgs e)
        {
            this.color = SystemColors.Control;
            buttonPlay.InvokeEx(() => buttonPlay.Text = "Play");
            isplaying = false;
        }

        void SoundBite_OnHotKeySet(object sender, EventArgs e)
        {
            if(hotkeymod == Keys.None)
                this.buttonHotKey.Text = _hotkey.ToString();
            else
                this.buttonHotKey.Text = hotkeymod.ToString() + "+" + _hotkey.ToString();
        }

        void SoundBite_OnSoundFileChanged(object sender, EventArgs e)
        {
            labelFile.Text = System.IO.Path.GetFileName(this.fileName);
        }

        public void Play()
        {
            if (fileName == null) return;

            if (PlayPressed != null)
            {
                PlayPressed(this, null);
            }
            this.color = Color.Green;
            isplaying = true;
            buttonPlay.Text = "Stop";
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (isplaying)
            {
                Stop();
            }
            else
            {
                Play();
            }
        }

        private void labelFile_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if(files[0] != null)
            {
                fileName = files[0];
                if(OnSoundFileChanged != null)
                    OnSoundFileChanged(this, e);
            }
        }

        private void labelFile_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void buttonHotKey_Click(object sender, EventArgs e)
        {
            buttonHotKey.Text = "Press a key";
            OnHotkeySetStart(this, e);
        }
    }
}
