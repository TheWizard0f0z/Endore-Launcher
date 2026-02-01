using System;
using System.IO;
using NAudio.Wave;
using System.Windows;

namespace AktualizatorEME.Services
{
    public class MusicService
    {
        private IWavePlayer _waveOutDevice;
        private WaveStream _waveStream;
        private bool _isInitialized = false;

        public MusicService()
        {
            _waveOutDevice = new WaveOutEvent();
        }

        private void InitializePlayer()
        {
            if (_isInitialized)
                return;

            try
            {
                // Pobierz strumień z pliku MP3 osadzonego w zasobach
                var musicFileUri = new Uri("pack://application:,,,/Resources/Clarelynn Rose - The Redwood Sidthe.mp3", UriKind.Absolute);
                Stream musicStream = Application.GetResourceStream(musicFileUri)?.Stream;

                if (musicStream != null)
                {
                    _waveStream = new Mp3FileReader(musicStream);
                    _waveOutDevice.Init(_waveStream);
                    _isInitialized = true;
                }
                else
                {
                    throw new FileNotFoundException("Nie znaleziono pliku muzycznego w zasobach.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas inicjalizacji odtwarzacza muzyki: {ex.Message}");
            }
        }

        public void PlayMusic()
        {
            try
            {
                InitializePlayer();
                _waveOutDevice.Play();
                Console.WriteLine("Muzyka została włączona.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas odtwarzania muzyki: {ex.Message}");
            }
        }

        public void PauseMusic()
        {
            try
            {
                if (_waveOutDevice.PlaybackState == PlaybackState.Playing)
                {
                    _waveOutDevice.Pause();
                    Console.WriteLine("Muzyka została wstrzymana.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas wstrzymywania muzyki: {ex.Message}");
            }
        }

        public void StopMusic()
        {
            try
            {
                if (_waveOutDevice.PlaybackState != PlaybackState.Stopped)
                {
                    _waveOutDevice.Stop();
                    _waveStream?.Dispose();
                    _waveOutDevice.Dispose();
                    _isInitialized = false;
                    Console.WriteLine("Muzyka została zatrzymana.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd podczas zatrzymywania muzyki: {ex.Message}");
            }
        }
    }
}
