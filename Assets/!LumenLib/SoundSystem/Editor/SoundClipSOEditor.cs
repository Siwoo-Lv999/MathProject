using System;
using _LumenLib.SoundSystem.Runtime;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _LumenLib.SoundSystem.Editor
{
    [CustomEditor(typeof(SoundClipSO))]
    public sealed class SoundClipSOEditor : UnityEditor.Editor
    {
        private const float WaveformHeight = 100f;
        private const float HandleWidth = 14f;
        private const float MinimumSelectionDuration = 0.01f;
        private const int FramesPerRead = 4096;

        private static readonly Color WaveformBackground = new(0.12f, 0.12f, 0.12f, 1f);
        private static readonly Color WaveformColor = new(0.25f, 0.65f, 1f, 1f);
        private static readonly Color OutsideSelectionColor = new(0f, 0f, 0f, 0.58f);
        private static readonly Color StartHandleColor = new(0.25f, 0.9f, 0.3f, 1f);
        private static readonly Color EndHandleColor = new(0.95f, 0.35f, 0.2f, 1f);

        private SerializedProperty _audioType;
        private SerializedProperty _clip;
        private SerializedProperty _isLoop;
        private SerializedProperty _randomizePitch;
        private SerializedProperty _randomPitchModifier;
        private SerializedProperty _volume;
        private SerializedProperty _pitch;
        private SerializedProperty _startTime;
        private SerializedProperty _endTime;

        private Texture2D _waveformTexture;
        private AudioClip _waveformClip;
        private int _waveformWidth;
        private bool _waveformUnavailable;
        private bool _draggingStart;
        private bool _draggingEnd;

        private GameObject _previewObject;
        private AudioSource _previewSource;
        private bool _isPreviewing;
        private float _previewEndTime;

        private void OnEnable()
        {
            _audioType = serializedObject.FindProperty("audioType");
            _clip = serializedObject.FindProperty("clip");
            _isLoop = serializedObject.FindProperty("isLoop");
            _randomizePitch = serializedObject.FindProperty("randomizePitch");
            _randomPitchModifier = serializedObject.FindProperty("randomPitchModifier");
            _volume = serializedObject.FindProperty("volume");
            _pitch = serializedObject.FindProperty("pitch");
            _startTime = serializedObject.FindProperty("startTime");
            _endTime = serializedObject.FindProperty("endTime");

            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopPreview();
            DisposeWaveform();

            if (_previewObject != null)
                DestroyImmediate(_previewObject);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Sound Clip", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_audioType);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_clip);
            if (EditorGUI.EndChangeCheck())
                HandleClipChanged();

            EditorGUILayout.PropertyField(_isLoop);
            EditorGUILayout.PropertyField(_randomizePitch);

            using (new EditorGUI.DisabledScope(!_randomizePitch.boolValue))
                EditorGUILayout.PropertyField(_randomPitchModifier);

            EditorGUILayout.PropertyField(_volume);
            EditorGUILayout.PropertyField(_pitch);

            AudioClip clip = _clip.objectReferenceValue as AudioClip;

            if (clip == null)
            {
                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.HelpBox("Assign an AudioClip to edit and preview its waveform.", MessageType.Info);
                return;
            }

            DrawRangeFields(clip);
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Clip Preview", EditorStyles.boldLabel);
            DrawWaveform(clip);
            DrawTimeLabels();

            if (GUILayout.Button(_isPreviewing ? "Stop Preview" : "Play Selection", GUILayout.Height(24f)))
                TogglePreview(clip);
        }

        private void HandleClipChanged()
        {
            StopPreview();
            DisposeWaveform();

            AudioClip selectedClip = _clip.objectReferenceValue as AudioClip;
            _startTime.floatValue = 0f;
            _endTime.floatValue = selectedClip != null ? selectedClip.length : 0f;
        }

        private void DrawRangeFields(AudioClip clip)
        {
            float duration = clip.length;
            float start = Mathf.Clamp(_startTime.floatValue, 0f, duration);
            float end = Mathf.Clamp(_endTime.floatValue, start, duration);
            float minimumDuration = Mathf.Min(MinimumSelectionDuration, duration);

            EditorGUI.BeginChangeCheck();

            using (new EditorGUILayout.HorizontalScope())
            {
                start = EditorGUILayout.FloatField("Start", start);
                end = EditorGUILayout.FloatField("End", end);
            }

            EditorGUILayout.MinMaxSlider(ref start, ref end, 0f, duration);

            if (!EditorGUI.EndChangeCheck())
                return;

            start = Mathf.Clamp(start, 0f, Mathf.Max(0f, end - minimumDuration));
            end = Mathf.Clamp(end, start + minimumDuration, duration);
            _startTime.floatValue = start;
            _endTime.floatValue = end;
            StopPreview();
        }

        private void DrawWaveform(AudioClip clip)
        {
            Rect rect = GUILayoutUtility.GetRect(
                GUIContent.none,
                GUIStyle.none,
                GUILayout.Height(WaveformHeight),
                GUILayout.ExpandWidth(true));

            int width = Mathf.Max(1, Mathf.RoundToInt(rect.width));
            EnsureWaveform(clip, width, Mathf.RoundToInt(WaveformHeight));

            EditorGUI.DrawRect(rect, WaveformBackground);

            if (_waveformTexture != null)
                GUI.DrawTexture(rect, _waveformTexture, ScaleMode.StretchToFill);
            else if (_waveformUnavailable)
                GUI.Label(rect, "Waveform unavailable for this clip", EditorStyles.centeredGreyMiniLabel);

            DrawSelection(rect, clip);
        }

        private void DrawSelection(Rect rect, AudioClip clip)
        {
            if (clip.length <= 0f)
                return;

            float startX = rect.x + (_startTime.floatValue / clip.length) * rect.width;
            float endX = rect.x + (_endTime.floatValue / clip.length) * rect.width;

            EditorGUI.DrawRect(
                new Rect(rect.x, rect.y, Mathf.Max(0f, startX - rect.x), rect.height),
                OutsideSelectionColor);
            EditorGUI.DrawRect(
                new Rect(endX, rect.y, Mathf.Max(0f, rect.xMax - endX), rect.height),
                OutsideSelectionColor);

            EditorGUI.DrawRect(new Rect(startX - 1f, rect.y, 2f, rect.height), StartHandleColor);
            EditorGUI.DrawRect(new Rect(endX - 1f, rect.y, 2f, rect.height), EndHandleColor);
            EditorGUI.DrawRect(new Rect(startX - 5f, rect.y, 10f, 10f), StartHandleColor);
            EditorGUI.DrawRect(new Rect(endX - 5f, rect.y, 10f, 10f), EndHandleColor);

            if (_isPreviewing && _previewSource != null && _previewSource.clip == clip)
            {
                float playheadX = rect.x + (_previewSource.time / clip.length) * rect.width;
                EditorGUI.DrawRect(new Rect(playheadX - 1f, rect.y, 2f, rect.height), Color.white);
            }

            Rect startHandle = new(startX - HandleWidth * 0.5f, rect.y, HandleWidth, rect.height);
            Rect endHandle = new(endX - HandleWidth * 0.5f, rect.y, HandleWidth, rect.height);
            EditorGUIUtility.AddCursorRect(startHandle, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(endHandle, MouseCursor.ResizeHorizontal);

            HandleRangeDrag(rect, startHandle, endHandle, clip.length);
        }

        private void HandleRangeDrag(Rect rect, Rect startHandle, Rect endHandle, float duration)
        {
            int controlId = GUIUtility.GetControlID("LumenSoundClipRange".GetHashCode(), FocusType.Passive, rect);
            Event current = Event.current;

            switch (current.GetTypeForControl(controlId))
            {
                case EventType.MouseDown when current.button == 0:
                    if (!startHandle.Contains(current.mousePosition) && !endHandle.Contains(current.mousePosition))
                        return;

                    _draggingStart = startHandle.Contains(current.mousePosition);
                    _draggingEnd = !_draggingStart && endHandle.Contains(current.mousePosition);
                    GUIUtility.hotControl = controlId;
                    current.Use();
                    break;

                case EventType.MouseDrag when GUIUtility.hotControl == controlId:
                    float time = Mathf.Clamp01((current.mousePosition.x - rect.x) / rect.width) * duration;
                    float minimumDuration = Mathf.Min(MinimumSelectionDuration, duration);

                    serializedObject.Update();

                    if (_draggingStart)
                        _startTime.floatValue = Mathf.Clamp(time, 0f, _endTime.floatValue - minimumDuration);
                    else if (_draggingEnd)
                        _endTime.floatValue = Mathf.Clamp(time, _startTime.floatValue + minimumDuration, duration);

                    serializedObject.ApplyModifiedProperties();
                    StopPreview();
                    Repaint();
                    current.Use();
                    break;

                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                    _draggingStart = false;
                    _draggingEnd = false;
                    GUIUtility.hotControl = 0;
                    current.Use();
                    break;
            }
        }

        private void DrawTimeLabels()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Start: {_startTime.floatValue:F3}s", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(
                    $"End: {_endTime.floatValue:F3}s",
                    EditorStyles.miniLabel,
                    GUILayout.Width(100f));
            }
        }

        private void EnsureWaveform(AudioClip clip, int width, int height)
        {
            if (_waveformClip == clip && _waveformWidth == width)
                return;

            DisposeWaveform();
            _waveformClip = clip;
            _waveformWidth = width;
            _waveformTexture = BuildWaveform(clip, width, height);
            _waveformUnavailable = _waveformTexture == null;
        }

        private static Texture2D BuildWaveform(AudioClip clip, int width, int height)
        {
            if (clip.samples <= 0 || clip.channels <= 0)
                return null;

            int framesPerPixel = Mathf.Max(1, Mathf.CeilToInt(clip.samples / (float)width));
            int bufferFrames = Mathf.Min(FramesPerRead, clip.samples);
            float[] sampleBuffer = new float[bufferFrames * clip.channels];
            Color32[] pixels = new Color32[width * height];
            Color32 background = WaveformBackground;
            Color32 waveform = WaveformColor;

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = background;

            try
            {
                for (int x = 0; x < width; x++)
                {
                    int firstFrame = Mathf.FloorToInt(x / (float)width * clip.samples);
                    int lastFrame = Mathf.Min(clip.samples, firstFrame + framesPerPixel);
                    float minimum = 0f;
                    float maximum = 0f;

                    for (int frame = firstFrame; frame < lastFrame; frame += bufferFrames)
                    {
                        int framesToScan = Mathf.Min(bufferFrames, lastFrame - frame);

                        if (!clip.GetData(sampleBuffer, frame))
                            return null;

                        int samplesToScan = framesToScan * clip.channels;

                        for (int sample = 0; sample < samplesToScan; sample++)
                        {
                            minimum = Mathf.Min(minimum, sampleBuffer[sample]);
                            maximum = Mathf.Max(maximum, sampleBuffer[sample]);
                        }
                    }

                    int minY = Mathf.Clamp(
                        Mathf.RoundToInt((minimum * 0.5f + 0.5f) * (height - 1)),
                        0,
                        height - 1);
                    int maxY = Mathf.Clamp(
                        Mathf.RoundToInt((maximum * 0.5f + 0.5f) * (height - 1)),
                        0,
                        height - 1);

                    for (int y = minY; y <= maxY; y++)
                        pixels[y * width + x] = waveform;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[SoundClipSOEditor] Could not read waveform for {clip.name}: {exception.Message}");
                return null;
            }

            Texture2D texture = new(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{clip.name} Waveform",
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void TogglePreview(AudioClip clip)
        {
            if (_isPreviewing)
            {
                StopPreview();
                return;
            }

            AudioSource source = EnsurePreviewSource();
            source.clip = clip;
            source.loop = false;
            source.volume = _volume.floatValue;
            source.pitch = ResolvePreviewPitch();
            source.time = Mathf.Clamp(_startTime.floatValue, 0f, clip.length);
            _previewEndTime = Mathf.Clamp(_endTime.floatValue, source.time, clip.length);
            source.Play();
            _isPreviewing = true;
        }

        private float ResolvePreviewPitch()
        {
            if (!_randomizePitch.boolValue)
                return _pitch.floatValue;

            return Mathf.Clamp(
                _pitch.floatValue + Random.Range(
                    -_randomPitchModifier.floatValue,
                    _randomPitchModifier.floatValue),
                0.1f,
                3f);
        }

        private AudioSource EnsurePreviewSource()
        {
            if (_previewSource != null)
                return _previewSource;

            _previewObject = EditorUtility.CreateGameObjectWithHideFlags(
                "LumenLib Sound Preview",
                HideFlags.HideAndDontSave,
                typeof(AudioSource));
            _previewSource = _previewObject.GetComponent<AudioSource>();
            _previewSource.playOnAwake = false;
            return _previewSource;
        }

        private void OnEditorUpdate()
        {
            if (!_isPreviewing)
                return;

            bool stopped = _previewSource == null || !_previewSource.isPlaying;
            bool reachedSelectionEnd = _previewSource != null && _previewSource.time >= _previewEndTime;

            if (stopped || reachedSelectionEnd)
                StopPreview();

            Repaint();
        }

        private void StopPreview()
        {
            if (_previewSource != null)
                _previewSource.Stop();

            _isPreviewing = false;
        }

        private void DisposeWaveform()
        {
            if (_waveformTexture != null)
                DestroyImmediate(_waveformTexture);

            _waveformTexture = null;
            _waveformClip = null;
            _waveformWidth = 0;
            _waveformUnavailable = false;
        }
    }
}
