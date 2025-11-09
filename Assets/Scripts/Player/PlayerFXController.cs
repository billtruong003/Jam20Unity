using System;
using System.Collections.Generic;
using EchoMage.Core;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public struct AudioFX
{
    [SerializeField, HorizontalGroup("ID"), LabelWidth(40)]
    private string id;

    [SerializeField, HorizontalGroup("Clip")]
    private AudioClip audioClip;

    [SerializeField, Tooltip("Vị trí phát âm thanh (nếu null thì dùng vị trí của GameObject gọi)")]
    private Transform position;

    public AudioFX(string id, AudioClip clip, Transform pos = null)
    {
        this.id = id;
        this.audioClip = clip;
        this.position = pos;
    }

    public bool CompareID(string otherId) =>
        string.Equals(id, otherId, StringComparison.OrdinalIgnoreCase);

    public string ID => id;
    public AudioClip Clip => audioClip;
    public Transform Position => position;
}

public class PlayerFXController : MonoBehaviour
{
    [Header("VFX"), FoldoutGroup("VFX")]
    [SerializeField] private AfterImageController afterImageController;

    [FoldoutGroup("VFX")]
    [SerializeField] private GameObject visual;

    [Header("SFX"), FoldoutGroup("SFX")]
    [SerializeField, TableList(ShowIndexLabels = true)]
    private AudioFX[] audioFX;
    private Dictionary<string, AudioFX> _audioFXMap;

    private void Awake()
    {
        // Khởi tạo VFX
        InitializeVFX();
        InitializeAudioFXMap();
    }

    private void InitializeVFX()
    {
        afterImageController ??= GameManager.Instance.AfterImageController;
        if (afterImageController != null && visual != null)
        {
            afterImageController.SetRoot(visual);
            afterImageController.SetOrigin(transform);
            afterImageController.enabled = true;
        }
        else
        {
            Debug.LogWarning("[PlayerFXController] AfterImageController hoặc visual bị thiếu!", this);
        }
    }

    private void InitializeAudioFXMap()
    {
        _audioFXMap = new Dictionary<string, AudioFX>(StringComparer.OrdinalIgnoreCase);
        foreach (var fx in audioFX)
        {
            if (fx.Clip == null) continue;
            if (!string.IsNullOrEmpty(fx.ID) && !_audioFXMap.ContainsKey(fx.ID))
            {
                _audioFXMap[fx.ID] = fx;
            }
            else if (!string.IsNullOrEmpty(fx.ID))
            {
                Debug.LogWarning($"[PlayerFXController] Trùng ID SFX: {fx.ID}", this);
            }
        }
    }

    #region VFX Controls

    public void SwitchMode(bool state)
    {
        if (afterImageController == null) return;

        if (state)
            afterImageController.SwitchModeAlways();
        else
            afterImageController.SwitchModeCommand();
    }

    #endregion

    #region SFX Controls

    /// <summary>
    /// Phát âm thanh theo AudioClip trực tiếp
    /// </summary>
    public void PlaySFX(AudioClip clip, Transform customPosition = null)
    {
        if (clip == null) return;
        var pos = customPosition ?? transform;
        SoundManager.Instance.PlaySfxRandomPitch(clip, pos.position);
    }

    /// <summary>
    /// Phát âm thanh theo ID đã định nghĩa trong mảng audioFX
    /// </summary>
    public void PlaySFX(string id, Transform customPosition = null)
    {
        if (string.IsNullOrEmpty(id) || _audioFXMap == null) return;

        if (_audioFXMap.TryGetValue(id, out AudioFX fx))
        {
            if (fx.Clip == null)
            {
                Debug.LogWarning($"[PlayerFXController] AudioClip cho ID '{id}' là null!", this);
                return;
            }

            var pos = customPosition ?? fx.Position ?? transform;
            SoundManager.Instance.PlaySfxRandomPitch(fx.Clip, pos.position);
        }
        else
        {
            Debug.LogWarning($"[PlayerFXController] Không tìm thấy SFX với ID: '{id}'", this);
        }
    }

    /// <summary>
    /// Dừng tất cả SFX liên quan (nếu SoundManager hỗ trợ)
    /// </summary>
    public void StopAllSFX()
    {
        SoundManager.Instance.StopAllSFX();
    }

    /// <summary>
    /// Kiểm tra xem ID có tồn tại không
    /// </summary>
    public bool HasSFX(string id)
    {
        return !string.IsNullOrEmpty(id) && _audioFXMap?.ContainsKey(id) == true;
    }

    /// <summary>
    /// Lấy AudioClip theo ID (dùng cho logic khác)
    /// </summary>
    public AudioClip GetClipByID(string id)
    {
        return _audioFXMap != null && _audioFXMap.TryGetValue(id, out var fx) ? fx.Clip : null;
    }

    #endregion

    #region Editor Helpers (Odin)

#if UNITY_EDITOR
    [FoldoutGroup("SFX"), Button("Refresh SFX Map", ButtonSizes.Medium)]
    private void Editor_RefreshMap()
    {
        InitializeAudioFXMap();
    }
#endif

    #endregion
}