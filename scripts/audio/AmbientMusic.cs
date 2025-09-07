#nullable enable
using Godot;
using System.Collections.Generic;

namespace PlantProject.Audio
{
    [GlobalClass]
    public partial class AmbientMusic : AudioStreamPlayer
    {
        [Export] public string MusicDir { get; set; } = "res://assets/audio/music";
        [Export] public float LoopDelaySeconds { get; set; } = 2f;
        [Export] public float FadeSeconds { get; set; } = 0.5f;
        [Export] public float SwitchSilenceSeconds { get; set; } = 0.25f;

        private readonly List<AudioStream> _tracks = new();
        private readonly Dictionary<string, AudioStream> _byName = new();
        private int _trackIndex = 0;
        private Timer? _loopTimer;
        private Timer? _switchTimer;
        private string? _currentTrackName;
        private bool _lastPaused;
        private float _baseDb;
        private float _duckLinear = 1f;
        private float _fadeLinear = 1f;
        private int _fadeMode = 0; // -1 fading out, +1 fading in
        private float _fadeTime = 0f;
        private AudioStream? _pendingStream;
        private string? _pendingTrackName;

        public override void _Ready()
        {
            // Ensure we still work when the game is paused
            ProcessMode = ProcessModeEnum.Always;

            LoadTracks();

            _loopTimer = new Timer
            {
                OneShot = true,
                WaitTime = LoopDelaySeconds,
                ProcessMode = ProcessModeEnum.Always
            };
            AddChild(_loopTimer);
            _loopTimer.Timeout += OnLoopTimeout;

            Finished += OnFinished;

            _baseDb = VolumeDb; // remember baseline volume
            ApplyPauseVolume(GetTree().Paused);

            _switchTimer = new Timer { OneShot = true, ProcessMode = ProcessModeEnum.Always };
            AddChild(_switchTimer);
            _switchTimer.Timeout += OnSwitchTimeout;
        }

        public override void _Process(double delta)
        {
            bool paused = GetTree().Paused;
            if (paused != _lastPaused) ApplyPauseVolume(paused);

            // Handle fades
            if (_fadeMode != 0)
            {
                _fadeTime += (float)delta;
                float dur = Mathf.Max(0.0001f, FadeSeconds);
                float t = Mathf.Clamp(_fadeTime / dur, 0f, 1f);
                if (_fadeMode < 0)
                {
                    _fadeLinear = 1f - t; // fade out
                    if (t >= 1f)
                    {
                        _fadeMode = 0;
                        _fadeLinear = 0f;
                        Stop();
                        // Start brief silence before switching
                        if (_pendingStream != null)
                        {
                            _switchTimer?.Stop();
                            if (_switchTimer != null)
                            {
                                _switchTimer.WaitTime = Mathf.Max(0f, SwitchSilenceSeconds);
                                _switchTimer.Start();
                            }
                        }
                    }
                }
                else if (_fadeMode > 0)
                {
                    _fadeLinear = t; // fade in
                    if (t >= 1f)
                    {
                        _fadeMode = 0;
                        _fadeLinear = 1f;
                    }
                }
                UpdateVolume();
            }
        }

        private void ApplyPauseVolume(bool paused)
        {
            _lastPaused = paused;
            // Reduce volume by 33% (multiply linear gain by 0.67) when paused
            _duckLinear = paused ? 0.67f : 1f;
            UpdateVolume();
        }

        private void UpdateVolume()
        {
            float lin = Mathf.Max(0.0001f, _duckLinear * _fadeLinear);
            VolumeDb = _baseDb + Mathf.LinearToDb(lin);
        }

        private void LoadTracks()
        {
            _tracks.Clear();
            _byName.Clear();
            var dir = DirAccess.Open(MusicDir);
            if (dir == null)
            {
                GD.PrintErr($"AmbientMusic: Could not open directory: {MusicDir}");
                return;
            }

            dir.ListDirBegin();
            while (true)
            {
                var file = dir.GetNext();
                if (string.IsNullOrEmpty(file)) break;
                if (dir.CurrentIsDir()) continue;
                if (file.StartsWith(".")) continue;

                var lower = file.ToLowerInvariant();
                if (!(lower.EndsWith(".mp3") || lower.EndsWith(".ogg") || lower.EndsWith(".wav")))
                    continue;

                var full = MusicDir.TrimEnd('/') + "/" + file;
                var stream = ResourceLoader.Load<AudioStream>(full);
                if (stream == null) continue;

                // Ensure no stream-level looping; we handle loop with delay
                if (stream is AudioStreamOggVorbis ogg) ogg.Loop = false;
                else if (stream is AudioStreamMP3 mp3) mp3.Loop = false;
                else if (stream is AudioStreamWav wav) wav.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;

                _tracks.Add(stream);
                // Map both bare name and filename to the stream for convenience
                string nameNoExt = file;
                int dot = nameNoExt.LastIndexOf('.');
                if (dot >= 0) nameNoExt = nameNoExt.Substring(0, dot);
                _byName[nameNoExt.ToLowerInvariant()] = stream;
                _byName[file.ToLowerInvariant()] = stream;
            }
            dir.ListDirEnd();
        }

        private void OnFinished()
        {
            // If we're in the middle of switching tracks due to a fade-out Stop(),
            // don't schedule a loop restart of the old track.
            if (_pendingStream != null)
                return;
            // Schedule replay after the delay
            if (_loopTimer == null) { Play(); return; }
            _loopTimer.Stop();
            _loopTimer.WaitTime = LoopDelaySeconds;
            _loopTimer.Start();
        }

        private void OnLoopTimeout()
        {
            // Replay the same track (or advance if multiple could be supported later)
            if (_tracks.Count == 0) return;
            if (Stream != _tracks[_trackIndex]) Stream = _tracks[_trackIndex];
            Play();
        }

        public void PlayTrackByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (_byName.Count == 0 && _tracks.Count == 0)
            {
                LoadTracks();
            }

            string key = name.ToLowerInvariant();
            AudioStream? stream = null;
            if (_byName.TryGetValue(key, out var exact)) stream = exact;
            else
            {
                // Fallback: partial match without extension
                foreach (var kv in _byName)
                {
                    if (kv.Key.Contains(key)) { stream = kv.Value; break; }
                }
            }

            if (stream == null) return;

            // If already on this stream, only early-exit when it's actually playing
            if (Stream == stream && _pendingStream == null)
            {
                _currentTrackName = name;
                if (!Playing)
                {
                    // Stream matches but was stopped (e.g., after a loop delay or scene change)
                    // Restart with a fade-in so it doesn't remain silent.
                    _fadeMode = +1;
                    _fadeTime = 0f;
                    _fadeLinear = 0f;
                    Play();
                    UpdateVolume();
                }
                return;
            }

            // If not currently playing anything, start new track with fade in (no fade out)
            if (!Playing || Stream == null)
            {
                _loopTimer?.Stop();
                _pendingStream = null;
                _pendingTrackName = null;
                int idx0 = _tracks.IndexOf(stream);
                if (idx0 >= 0) _trackIndex = idx0;
                Stream = stream;
                _currentTrackName = name;
                _fadeMode = +1;
                _fadeTime = 0f;
                _fadeLinear = 0f;
                Play();
                UpdateVolume();
                return;
            }

            // Prepare fade transition
            _pendingStream = stream;
            _pendingTrackName = name;
            int idx = _tracks.IndexOf(stream);
            if (idx >= 0) _trackIndex = idx;
            _loopTimer?.Stop();
            // Start fade out
            _fadeMode = -1;
            _fadeTime = 0f;
            UpdateVolume();
        }

        public string? CurrentTrackName => _currentTrackName;

        private void OnSwitchTimeout()
        {
            if (_pendingStream == null) return;
            // Switch stream and fade in
            Stream = _pendingStream;
            _currentTrackName = _pendingTrackName;
            _pendingStream = null;
            _pendingTrackName = null;
            _fadeMode = +1;
            _fadeTime = 0f;
            _fadeLinear = 0f;
            Play();
            UpdateVolume();
        }

        // Explicit path version to avoid any directory listing/lookup issues
        public void PlayTrackByPath(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return;
            // Try to use a preloaded reference from our directory scan so indexing/looping works
            AudioStream? stream = null;
            string fileKey = resourcePath;
            int slash = fileKey.LastIndexOf('/') + 1;
            if (slash > 0 && slash < fileKey.Length) fileKey = fileKey.Substring(slash);
            string fileNoExt = fileKey;
            int dot = fileNoExt.LastIndexOf('.');
            if (dot >= 0) fileNoExt = fileNoExt.Substring(0, dot);
            string keyLower = fileKey.ToLowerInvariant();
            string noExtLower = fileNoExt.ToLowerInvariant();
            if (_byName.TryGetValue(keyLower, out var s1)) stream = s1;
            else if (_byName.TryGetValue(noExtLower, out var s2)) stream = s2;
            // Fallback: load directly
            stream ??= ResourceLoader.Load<AudioStream>(resourcePath);
            if (stream == null) return;

            // normalize looping off
            if (stream is AudioStreamOggVorbis ogg) ogg.Loop = false;
            else if (stream is AudioStreamMP3 mp3) mp3.Loop = false;
            else if (stream is AudioStreamWav wav) wav.LoopMode = AudioStreamWav.LoopModeEnum.Disabled;

            // Track index for loop replay if present in _tracks
            int idx = _tracks.IndexOf(stream);
            if (idx >= 0) _trackIndex = idx;

            if (!Playing || Stream == null)
            {
                _loopTimer?.Stop();
                _pendingStream = null;
                _pendingTrackName = null;
                Stream = stream;
                _currentTrackName = resourcePath;
                _fadeMode = +1;
                _fadeTime = 0f;
                _fadeLinear = 0f;
                Play();
                UpdateVolume();
                return;
            }

            if (Stream == stream && _pendingStream == null)
            {
                _currentTrackName = resourcePath;
                return;
            }

            _pendingStream = stream;
            _pendingTrackName = resourcePath;
            _loopTimer?.Stop();
            _fadeMode = -1;
            _fadeTime = 0f;
            UpdateVolume();
        }
    }
}
