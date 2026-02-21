using UnityEngine;

public sealed class DmThreadController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private DmThreadPanel panel;

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
        _player.StartDm(script, clearUi);
    }

    public void RequestAdvance() => _player.RequestAdvance();
}