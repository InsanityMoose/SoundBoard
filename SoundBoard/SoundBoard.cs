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
        MMDevice outputDeviceMM;
        DirectSoundOut outputDeviceDS;
        private int numSoundBites = 14;
        globalKeyboardHook gkh = new globalKeyboardHook();
        private SoundBite sbSetKey;
        public delegate void OnPlaybackStopHandler(object sender, EventArgs e);
        public event OnPlaybackStopHandler OnPlaybackStop;
        private List<SoundBite> SoundBitePlaying = new List<SoundBite>();

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

            if (WasapiOut.IsSupportedOnCurrentPlatform)
            {
                foreach (MMDevice dev in EnumerateWasapiDevices())
                {
                    listOutputs.Items.Add(dev);
                    if (dev.DeviceID == Properties.Settings.Default["OutputDevice"] as string)
                        listOutputs.SelectedItem = dev;
                }
                if(listOutputs.SelectedItem == null)
                    listOutputs.Select(0, 1);
                //listOutputs.Text = listOutputs.Items[0].ToString();
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

        void gkh_KeyDown(object sender, KeyEventArgs e)
        {
            //lstLog.Items.Add("Down\t" + e.KeyCode.ToString());
            foreach(SoundBite sb in soundBites)
            {
                if (sb.hotkey == e.KeyCode)
                    sb.Play();
            }
            e.Handled = true;
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
            }
            soundBites.Add(sb);
            sb.Parent = flowLayoutPanel1;
        }

        void sb_OnHotkeySetStart(object sender, EventArgs e)
        {
            this.KeyPreview = true;
            this.sbSetKey = sender as SoundBite;
            this.KeyDown += SoundBoard_KeyDown;
        }

        void SoundBoard_KeyDown(object sender, KeyEventArgs e)
        {
            this.KeyDown -= SoundBoard_KeyDown;
            this.sbSetKey.hotkey = e.KeyCode;
            gkh.HookedKeys.Add(e.KeyCode);
            Properties.Settings.Default["sbkey" + sbSetKey.id] = e.KeyCode.ToString();
            Properties.Settings.Default.Save();
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
                using (ISoundOut soundOut = GetSoundOut())
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
                    soundOut.Stop();
                    OnPlaybackStop(this, null);
                }
            }
        }

        private ISoundOut GetSoundOut()
        {
            if (WasapiOut.IsSupportedOnCurrentPlatform)
            {
                WasapiOut wasapiOut = new WasapiOut();
                wasapiOut.Device = outputDeviceMM;
                return wasapiOut;
            }
            else
            {
                DirectSoundOut directSoundOut = new DirectSoundOut();
                directSoundOut.Device = outputDeviceDS.Device;
                return directSoundOut;
            }
                
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
                outputDeviceMM = listOutputs.SelectedItem as MMDevice;
                listOutputs.Text = outputDeviceMM.ToString();
                Properties.Settings.Default["OutputDevice"] = outputDeviceMM.DeviceID;
                Properties.Settings.Default.Save();
            }
            else
            {
                outputDeviceDS = listOutputs.SelectedItem as DirectSoundOut;
                listOutputs.Text = outputDeviceDS.ToString();
                Properties.Settings.Default["OutputDevice"] = outputDeviceDS.Device.ToString();
                Properties.Settings.Default.Save();
            }
            
        }
    }
}
