using UnityEngine;

public sealed class DmThreadController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private DmThreadPanel panel;

    [Header("Auto")]
    [SerializeField] private bool autoEnabled = false;
    [SerializeField] private float autoDelaySeconds = 1.2f;

    private DmThreadPlayer _player;

    private void Awake()
    {
        if (!panel)
        {
            Debug.LogError("[DmThreadController] panel is null.", this);
            enabled = false;
            return;
        }

        _player = new DmThreadPlayer(panel);
        _player.SetAutoEnabled(autoEnabled);
        _player.AutoDelaySeconds = autoDelaySeconds;

        panel.OnChoiceOptionSelected -= HandleChoiceSelected;
        panel.OnChoiceOptionSelected += HandleChoiceSelected;
    }

    private void Update()
    {
        if (_player == null) return;
        _player.Tick(Time.unscaledDeltaTime);

        // 예: PC에서 스페이스로 다음
        // if (Input.GetKeyDown(KeyCode.Space)) _player.RequestAdvance();
    }

    private void HandleChoiceSelected(int index)
    {
        _player.ChooseOption(index);
    }

    // 외부에서 시작시키기
    public void Play(DmScript script, bool clearUi = true)
    {
        _player.Start(script, clearUi);
    }

    public void RequestAdvance() => _player.RequestAdvance();
    public void SetAuto(bool enabled) => _player.SetAutoEnabled(enabled);
    public void SetInputBlocked(bool blocked) => _player.SetInputBlocked(blocked);
}