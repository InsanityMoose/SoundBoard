using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSCore;
using CSCore.SoundOut;
using CSCore.Codecs;
using System.Threading;
using CSCore.SoundOut.DirectSound;
using CSCore.CoreAudioAPI;
using Utilities;
using System.Configuration;
using SoundBoard.Properties;

namespace SoundBoard
{
    public partial class SoundBoard : Form
    {
        List<SoundBite> soundBites = new List<SoundBite>();
        string outputDevice = "";
        private int numSoundBites = 14;
        globalKeyboardHook gkh = new globalKeyboardHook();
        private SoundBite sbSetKey;
        public delegate void OnPlaybackStopHandler(object sender, EventArgs e);
        public event OnPlaybackStopHandler OnPlaybackStop;
        private List<SoundBite> SoundBitePlaying = new List<SoundBite>();
        private ISoundOut soundOut;

        public SoundBoard()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            createSettings();
            this.KeyPreview = true;
            gkh.KeyDown += new KeyEventHandler(gkh_KeyDown);
            OnPlaybackStop += SoundBoard_OnPlaybackStop;

            outputDevice = (String)Properties.Settings.Default["OutputDevice"];

            if (WasapiOut.IsSupportedOnCurrentPlatform)
            {
                foreach (MMDevice dev in EnumerateWasapiDevices())
                {
                    listOutputs.Items.Add(dev);
                    if (dev.DeviceID == Properties.Settings.Default["OutputDevice"] as string)
                        listOutputs.SelectedItem = dev;
                    if(outputDevice == "Default")
                    {
                        if(GetDefaultDevice().ToString() == dev.ToString())
                        {
                            listOutputs.SelectedItem = dev;
                        }
                    }
                }                  
            }

            for (int i = 0; i < numSoundBites; i++)
            {
                addSoundBite(i);
            }

            
        }

        void SoundBoard_OnPlaybackStop(object sender, EventArgs e)
        {
            foreach(SoundBite sb in SoundBitePlaying)
            {
                sb.Stop();
            }
        }

        private bool DoesSettingExist(string settingName)
        {
            try
            {
                return Properties.Settings.Default.Properties.Cast<SettingsProperty>().Any(prop => prop.Name == settingName);
            }
            catch(NullReferenceException e)
            {
                return false;
            }
        }

        private void createSettings()
        {
            if (!DoesSettingExist("numSoundBites"))
            {
                SettingsProperty property = new SettingsProperty(Settings.Default.Properties["baseSetting"]);
                property.Name = "numSoundBites";
                property.PropertyType = typeof(int);
                Settings.Default.Properties.Add(property);
                Settings.Default["numSoundBites"] = numSoundBites;
            }
            if (!DoesSettingExist("OutputDevice"))
            {
                SettingsProperty property = new SettingsProperty(Settings.Default.Properties["baseSetting"]);
                property.Name = "OutputDevice";
                property.PropertyType = typeof(string);
                Settings.Default.Properties.Add(property);
                Settings.Default["OutputDevice"] = "Default";
            }
            for(int i = 0; i < numSoundBites; i++)
            {
                if(!DoesSettingExist("sb"+i))
                {
                    SettingsProperty property = new SettingsProperty(Settings.Default.Properties["baseSetting"]);
                    property.Name = "sb"+i;
                    property.PropertyType = typeof(string);
                    Settings.Default.Properties.Add(property);
                }
                if (!DoesSettingExist("sbkey" + i))
                {
                    SettingsProperty property = new SettingsProperty(Settings.Default.Properties["baseSetting"]);
                    property.Name = "sbkey" + i;
                    property.PropertyType = typeof(string);
                    Settings.Default.Properties.Add(property);
                }
                if (!DoesSettingExist("sbkeymod" + i))
                {
                    SettingsProperty property = new SettingsProperty(Settings.Default.Properties["baseSetting"]);
                    property.Name = "sbkeymod" + i;
                    property.PropertyType = typeof(string);
                    Settings.Default.Properties.Add(property);
                }
            }
        }



        private void addSoundBite(int id)
        {
            SoundBite sb = new SoundBite();
            sb.PlayPressed += sb_PlayPressed;
            sb.OnSoundFileChanged += sb_OnSoundFileChanged;
            sb.id = id;
            if (Properties.Settings.Default["sb" + sb.id] != null)
            {
                string file = Properties.Settings.Default["sb" + sb.id].ToString();
                if (System.IO.File.Exists(file))
                    sb.fileName = file;
            }
            
            // Hotkeys 
            sb.OnHotkeySetStart += sb_OnHotkeySetStart;
            string hkstring = Settings.Default["sbkey" + id] as string;
            KeysConverter keysConverter = new KeysConverter();
            if (hkstring != "")
            {
                Keys hk = (Keys)keysConverter.ConvertFromString(hkstring);
                if (hk != Keys.None)
                {
                    sb.hotkey = hk;
                    gkh.HookedKeys.Add(hk);
                }

                string hkmodstring = Settings.Default["sbkeymod" + id] as string;
                keysConverter = new KeysConverter();
                if (hkmodstring != "")
                {
                    Keys hkmod = (Keys)keysConverter.ConvertFromString(hkmodstring);
                    if (hkmod != Keys.None)
                    {
                        sb.hotkeymod = hkmod;
                    }
                }
            }

            
            sb.OnPlayStop += sb_OnPlayStop;
            soundBites.Add(sb);
            sb.Parent = flowLayoutPanel1;
        }

        void sb_OnPlayStop(object sender, EventArgs e)
        {
            if(soundOut.PlaybackState != PlaybackState.Stopped) soundOut.Stop();
        }

        void sb_OnHotkeySetStart(object sender, EventArgs e)
        {
            this.KeyPreview = true;
            this.sbSetKey = sender as SoundBite;
            this.KeyDown += SoundBoard_KeyDown;
            gkh.unhook();
        }

        void gkh_KeyDown(object sender, KeyEventArgs e)
        {
            foreach (SoundBite sb in soundBites)
            {
                if (sb.hotkey == e.KeyCode && sb.hotkeymod == Control.ModifierKeys)
                    sb.Play();
            }
            e.Handled = true;
        }

        void SoundBoard_KeyDown(object sender, KeyEventArgs e)
        {          
            // Ignore modifiers only
            if (e.KeyCode == Keys.ControlKey
                || e.KeyCode == Keys.ShiftKey
                || e.KeyCode == Keys.Menu
                ) return;

            this.KeyDown -= SoundBoard_KeyDown;

            // Unhook old hotkey
            if(sbSetKey.hotkey != Keys.None)
            {
                gkh.HookedKeys.Remove(sbSetKey.hotkey);
            }

            // Add key to global hooks
            gkh.HookedKeys.Add(e.KeyCode);

            // Find and remove previous uses of same key combo
            foreach(SoundBite sb in soundBites)
            {
                if(sb.hotkey == e.KeyCode && sb.hotkeymod == e.Modifiers)
                {
                    sb.hotkey = Keys.None;
                    sb.hotkeymod = Keys.None;
                    Properties.Settings.Default["sbkey" + sb.id] = "";
                    Properties.Settings.Default["sbkeymod" + sb.id] = "";
                }
            }

            sbSetKey.hotkey = e.KeyCode;
            sbSetKey.hotkeymod = e.Modifiers;

            Properties.Settings.Default["sbkey" + sbSetKey.id] = e.KeyCode.ToString();
            Properties.Settings.Default["sbkeymod" + sbSetKey.id] = e.Modifiers.ToString();
            Properties.Settings.Default.Save();
            gkh.hook();
        }

        void sb_OnSoundFileChanged(object sender, EventArgs e)
        {
            SoundBite sb = sender as SoundBite;
            Properties.Settings.Default["sb" + sb.id] = sb.fileName;
            Properties.Settings.Default.Save();
        }

        void sb_PlayPressed(object sender, EventArgs e)
        {
            SoundBite sb = (SoundBite)sender;
            Thread playThread = new Thread(new ParameterizedThreadStart(PlaySoundFile));
            SoundBitePlaying.Add(sb);
            playThread.Start(sb.fileName);
        }

        private void PlaySoundFile(object filename)
        {
            using (IWaveSource soundSource = CodecFactory.Instance.GetCodec(filename as string))
            {
                //SoundOut implementation which plays the sound
                using (soundOut = GetSoundOut())
                {                   
                    //Tell the SoundOut which sound it has to play
                    soundOut.Initialize(soundSource);
                    
                    //Play the sound
                    soundOut.Play();

                    while (soundOut.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(500);
                    }

                    //Stop the playback
                    if(soundOut.PlaybackState != PlaybackState.Stopped) soundOut.Stop();
                    OnPlaybackStop(this, null);
                }
            }
        }

        private ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
            {
                WasapiOut wasapiOut = new WasapiOut();
                wasapiOut.Device = getListOutputsSelected();
                return wasapiOut;
            }
            else return null;
        }

        public MMDevice getListOutputsSelected()
        {
            if(toolStrip1.InvokeRequired)
            {
                return (MMDevice)toolStrip1.Invoke(new Func<MMDevice>(() => getListOutputsSelected()));
            }
            else return (MMDevice)listOutputs.SelectedItem;
        }

        public IEnumerable<DirectSoundDevice> EnumerateDirectSoundDevices()
        {
            return new DirectSoundDeviceEnumerator().Devices;
        }

        public IEnumerable<MMDevice> EnumerateWasapiDevices()
        {
            using (MMDeviceEnumerator enumerator = new MMDeviceEnumerator())
            {
                return enumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
            }
        }

        private void listOutputs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(WasapiOut.IsSupportedOnCurrentPlatform)
            {
                listOutputs.Text = getListOutputsSelected().ToString();
                Properties.Settings.Default["OutputDevice"] = getListOutputsSelected().DeviceID;
                Properties.Settings.Default.Save();
            }          
        }

        private MMDevice GetDefaultDevice()
        {
            if(WasapiOut.IsSupportedOnCurrentPlatform)
            {
                using (var enumerator = new MMDeviceEnumerator())
                {
                    MMDevice dev = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
                    return dev;
                }
            }
            return null;
        }
    }
}
