using System.Collections.Generic;
using _LumenLib.PoolingSystem.Runtime;
using UnityEngine;
using Services = _LumenLib.ServiceLocator.ServiceLocator;

namespace _LumenLib.SoundSystem.Runtime
{
    [DisallowMultipleComponent]
    public sealed class AudioService : MonoBehaviour, IAudioService
    {
        [SerializeField] private PoolItemSO soundPlayerPoolItem;

        private readonly Dictionary<int, SoundPlayer> _channelPlayers = new();
        private readonly HashSet<SoundPlayer> _activeSfxPlayers = new();
        private ObjectPool _objectPool;
        private SoundPlayer _bgmPlayer;
        private bool _registered;

        private void Awake()
        {
            if (soundPlayerPoolItem == null)
            {
                Debug.LogError("[AudioService] SoundPlayer pool item is not assigned.", this);
                return;
            }

            if (!Services.TryGet<ObjectPool>(out _objectPool) || _objectPool == null)
            {
                Debug.LogError("[AudioService] ObjectPool is not available.", this);
                return;
            }

            _bgmPlayer = CreatePlayer("BGM Player");
            if (_bgmPlayer == null)
                return;

            Services.Register<IAudioService>(this);
            _registered = true;
        }

        private void OnDestroy()
        {
            SoundPlayer[] activePlayers = new SoundPlayer[_activeSfxPlayers.Count];
            _activeSfxPlayers.CopyTo(activePlayers);
            foreach (SoundPlayer player in activePlayers)
                Release(player);

            _channelPlayers.Clear();
            Release(_bgmPlayer);
            _bgmPlayer = null;

            if (_registered && Services.Unregister<IAudioService>(this))
                Services.Register<IAudioService>(NullAudioService.Instance);
        }

        public void PlaySfx(SoundClipSO clipData, int channel = 0)
        {
            if (_objectPool == null)
                return;

            SoundPlayer player = CreatePlayer(channel > 0 ? $"SFX Channel {channel}" : "SFX Player");
            if (player == null)
                return;

            if (!player.Play(clipData))
            {
                Release(player);
                return;
            }

            player.SoundFinished += HandleSoundFinished;
            _activeSfxPlayers.Add(player);

            if (channel <= 0)
                return;

            if (_channelPlayers.TryGetValue(channel, out SoundPlayer previous))
                Release(previous);

            _channelPlayers[channel] = player;
        }

        public void StopSfx(int channel)
        {
            if (!_channelPlayers.Remove(channel, out SoundPlayer player))
                return;

            Release(player);
        }

        public void PlayBgm(SoundClipSO clipData)
        {
            if (_bgmPlayer == null)
                return;

            _bgmPlayer.Stop();
            _bgmPlayer.Play(clipData);
        }

        public void StopBgm()
        {
            _bgmPlayer?.Stop();
        }

        private SoundPlayer CreatePlayer(string objectName)
        {
            IPoolable poolable = _objectPool.Pop(soundPlayerPoolItem.ItemName);
            if (poolable is not SoundPlayer player)
            {
                if (poolable != null)
                    _objectPool.Push(poolable);

                Debug.LogError("[AudioService] The configured pool item is not a SoundPlayer.", this);
                return null;
            }

            player.name = objectName;
            return player;
        }

        private void HandleSoundFinished(SoundPlayer player)
        {
            int finishedChannel = 0;

            foreach (KeyValuePair<int, SoundPlayer> entry in _channelPlayers)
            {
                if (entry.Value != player)
                    continue;

                finishedChannel = entry.Key;
                break;
            }

            if (finishedChannel > 0)
                _channelPlayers.Remove(finishedChannel);

            Release(player);
        }

        private void Release(SoundPlayer player)
        {
            if (player == null)
                return;

            _activeSfxPlayers.Remove(player);
            player.SoundFinished -= HandleSoundFinished;
            player.Stop();

            if (_objectPool != null)
                _objectPool.Push(player);
            else
                Destroy(player.gameObject);
        }
    }
}
