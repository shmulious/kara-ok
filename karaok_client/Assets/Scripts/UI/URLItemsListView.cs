using System;
using System.Collections.Generic;
using System.Linq;
using DataClasses;
using SYncTest;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class URLItemsListView : MonoBehaviour
    {
        [SerializeField] private YouTubeURLListItemView _itemViewPrefab;
        [SerializeField] private LyricsSyncPanel _lyricsSyncPanel;
        [SerializeField] private Sprite _entryTypeSpriteYouTube;
        [SerializeField] private Sprite _entryTypeSpriteSmule;
        public List<YouTubeURLListItemView> _listItems { get; private set; }

        private HotkeyManager _hotkeysManager;
        [SerializeField] private Button _addItemButton;
        [SerializeField] private LoadingAnimationManager _loadingAnimationManager;

        private void Awake()
        {
            _addItemButton.onClick.AddListener(InstantiateListItem);
            _listItems = new List<YouTubeURLListItemView>();
            _hotkeysManager = gameObject.AddComponent<HotkeyManager>();
            _hotkeysManager.RegisterHotkeyAction(KeyCode.A, InstantiateListItem);
            _hotkeysManager.RegisterHotkeyAction(KeyCode.D, InstantiateListItemFromClipboard);
            InstantiateListItemFromClipboard();
        }

        private async void InstantiateListItemFromClipboard()
        {
            await System.Threading.Tasks.Task.Delay(500);
            string clipboardContent = GUIUtility.systemCopyBuffer;
            InstantiateListItemWithText(clipboardContent);
        }

        private void InstantiateListItem()
        {
            InstantiateListItemWithText();
        }
        
        private void InstantiateListItemWithText(string initialText = null)
        {
            var go = Instantiate(_itemViewPrefab, transform, true);
            go.transform.localScale = Vector3.one;
            go.RegisterOnRemove(RemoveItemFromList);
            go.RegisterOnProcess(OnProcessClicked);
            go.RegisterOnVideo(OnVideoClicked);
            go.SetLoadingAnimationManager(_loadingAnimationManager);
            YouTubeURLListItemView.EntryTypeImageYouTube = _entryTypeSpriteYouTube;
            YouTubeURLListItemView.EntryTypeImageSmule = _entryTypeSpriteSmule;
            _addItemButton.transform.parent.SetAsLastSibling();

            if (initialText != null)
            {
                go.SetURL(initialText);
            }
            
            _listItems.Add(go);
        }

        private void OnProcessClicked(YouTubeURLListItemView itemClicked, string arg1)
        {
            LockListItemsExcept(itemClicked, true);
        }

        private void OnVideoClicked(SongMetadata itemClickedMetadata)
        {
            _lyricsSyncPanel.gameObject.SetActive(true);
            _lyricsSyncPanel.Init(itemClickedMetadata);
        }

        public void LockListItemsExcept(YouTubeURLListItemView itemNotToLock, bool shouldLock)
        {
            foreach (var item in _listItems)
            {
                if (item != itemNotToLock)
                {
                    item.LockUI(shouldLock);
                }
            }
        }

        private void RemoveItemFromList(YouTubeURLListItemView listItemView)
        {
            if (_listItems.Contains(listItemView))
            {
                _listItems.Remove(listItemView);
            }
            Destroy(listItemView.gameObject);
        }

        private void OnDestroy()
        {
            _addItemButton.onClick.RemoveAllListeners();
        }
    }
}