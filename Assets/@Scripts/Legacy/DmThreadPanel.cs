// using System;
// using TMPro;
// using UnityEngine;
// using UnityEngine.UI;
// using static UIRefValidation;
//
// public sealed class DmThreadPanel : UIBase<DmThreadPanel.Refs>, IUIPanel
// {
//     public event Action OnListRequested;
//     public event Action<string> OnSendRequested;
//     public event Action<int> OnChoiceSelected; // 0..2
//
//     public enum Refs
//     {
//         Root_Root,
//
//         // Top bar
//         TopBar_Root,
//         ProfileIcon_Image,
//         TitleText_Text,
//         SubtitleText_Text,
//         ListButton_Button,
//         ListButtonText_Text,
//
//         // Thread
//         ScrollRect_ScrollRect,
//         Content_Root,
//
//         // Input
//         InputBar_Root,
//         InputField_TMPInput,
//         SendButton_Button,
//
//         // Choice row (optional, if you want DM replies as choices)
//         ChoiceRow_Root,
//         Choice0_Button,
//         Choice0_Text,
//         Choice1_Button,
//         Choice1_Text,
//         Choice2_Button,
//         Choice2_Text,
//     }
//
//     private bool _valid;
//
//     private CanvasGroup _rootCg;
//
//     private Image _profileIcon;
//     private TMP_Text _title;
//     private TMP_Text _subtitle;
//     private Button _listBtn;
//
//     private ScrollRect _scroll;
//     private RectTransform _content;
//
//     private RectTransform _inputBar;
//     private TMP_InputField _inputField;
//     private Button _sendBtn;
//
//     private RectTransform _choiceRow;
//     private Button[] _choiceBtns;
//     private TMP_Text[] _choiceTexts;
//
//     protected override void Initialize()
//     {
//         CacheRefs();
//
// #if UNITY_EDITOR || DEVELOPMENT_BUILD
//         _valid = ValidateRefs();
//         if (!_valid) return;
// #else
//         _valid = true;
// #endif
//         BindHandlers();
//
//         // Defaults
//         Show(false);
//         SetInputVisible(false);
//         SetChoiceVisible(false);
//     }
//
//     private void CacheRefs()
//     {
//         _rootCg = View.CanvasGroup(Refs.Root_Root);
//
//         _profileIcon = View.Image(Refs.ProfileIcon_Image);
//         _title = View.Text(Refs.TitleText_Text);
//         _subtitle = View.Text(Refs.SubtitleText_Text);
//         _listBtn = View.Button(Refs.ListButton_Button);
//
//         var scrollRt = View.Rect(Refs.ScrollRect_ScrollRect);
//         _scroll = scrollRt ? scrollRt.GetComponent<ScrollRect>() : null;
//         _content = View.Rect(Refs.Content_Root);
//
//         _inputBar = View.Rect(Refs.InputBar_Root);
//         //_inputField = View.GetCached<TMP_InputField>(Refs.InputField_TMPInput); // View에 직접 메서드 없으니 GetCached 사용
//         _sendBtn = View.Button(Refs.SendButton_Button);
//
//         _choiceRow = View.Rect(Refs.ChoiceRow_Root);
//         _choiceBtns = new[]
//         {
//             View.Button(Refs.Choice0_Button),
//             View.Button(Refs.Choice1_Button),
//             View.Button(Refs.Choice2_Button),
//         };
//         _choiceTexts = new[]
//         {
//             View.Text(Refs.Choice0_Text),
//             View.Text(Refs.Choice1_Text),
//             View.Text(Refs.Choice2_Text),
//         };
//     }
//
//     private void BindHandlers()
//     {
//         BindEvent(_listBtn, _ => OnListRequested?.Invoke());
//
//         BindEvent(_sendBtn, _ =>
//         {
//             if (!_inputField) return;
//             string msg = _inputField.text;
//             if (string.IsNullOrWhiteSpace(msg)) return;
//
//             _inputField.text = "";
//             OnSendRequested?.Invoke(msg);
//         });
//
//         for (int i = 0; i < _choiceBtns.Length; i++)
//         {
//             int idx = i;
//             var btn = _choiceBtns[i];
//             if (!btn) continue;
//             BindEvent(btn, _ => OnChoiceSelected?.Invoke(idx));
//         }
//     }
//
//     // --------------------
//     // Public API
//     // --------------------
//
//     public void Show(bool visible)
//     {
//         if (!_valid) return;
//         SetCanvasGroupVisible(_rootCg, visible);
//     }
//
//     public void SetHeader(Sprite profileIcon, string title, string subtitle)
//     {
//         if (!_valid) return;
//         if (_profileIcon) _profileIcon.sprite = profileIcon;
//         if (_title) _title.text = title ?? "";
//         if (_subtitle) _subtitle.text = subtitle ?? "";
//     }
//
//     public RectTransform GetContentRoot() => _content;
//
//     public void SetInputVisible(bool visible)
//     {
//         if (!_valid) return;
//         if (_inputBar) _inputBar.gameObject.SetActive(visible);
//     }
//
//     public void SetChoiceVisible(bool visible)
//     {
//         if (!_valid) return;
//         if (_choiceRow) _choiceRow.gameObject.SetActive(visible);
//     }
//
//     public void SetChoices(string c0, string c1, string c2)
//     {
//         if (!_valid) return;
//
//         SetChoiceVisible(true);
//
//         SetChoice(0, c0);
//         SetChoice(1, c1);
//         SetChoice(2, c2);
//     }
//
//     public void ScrollToBottom()
//     {
//         if (!_valid) return;
//         if (!_scroll) return;
//
//         Canvas.ForceUpdateCanvases();
//         _scroll.verticalNormalizedPosition = 0f;
//     }
//
//     private void SetChoice(int index, string text)
//     {
//         if ((uint)index >= (uint)_choiceBtns.Length) return;
//
//         bool has = !string.IsNullOrEmpty(text);
//
//         if (_choiceBtns[index])
//         {
//             _choiceBtns[index].gameObject.SetActive(has);
//             _choiceBtns[index].interactable = has;
//         }
//
//         if (_choiceTexts[index])
//             _choiceTexts[index].text = text ?? "";
//     }
//
//     private static void SetCanvasGroupVisible(CanvasGroup cg, bool visible)
//     {
//         if (!cg) return;
//         cg.alpha = visible ? 1f : 0f;
//         cg.interactable = visible;
//         cg.blocksRaycasts = visible;
//     }
//
//     private bool ValidateRefs()
//     {
//         string missing = "";
//
//         AppendMissing(ref missing, _rootCg, Refs.Root_Root);
//
//         AppendMissing(ref missing, _title, Refs.TitleText_Text);
//         AppendMissing(ref missing, _subtitle, Refs.SubtitleText_Text);
//
//         AppendMissing(ref missing, _scroll, Refs.ScrollRect_ScrollRect);
//         AppendMissing(ref missing, _content, Refs.Content_Root);
//
//         // ListButton은 옵션이면 빼도 됨
//         // AppendMissing(ref missing, _listBtn, Refs.ListButton_Button);
//
//         if (missing.Length > 0)
//         {
//             Debug.LogWarning($"[DmThreadPanel] Missing refs:\n{missing}", this);
//             return false;
//         }
//         return true;
//     }
// }